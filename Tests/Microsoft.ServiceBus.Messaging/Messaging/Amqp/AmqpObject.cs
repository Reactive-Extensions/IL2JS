//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.ServiceBus.Common;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    abstract class AmqpObject
    {
        readonly static AsyncCallback onTryCloseComplete = OnTryCloseComplete;
        static SequenceNumber nextId;

        Action<bool, Exception> openCompleteCallback;
        Action<bool, Exception> closeCompleteCallback;

        int openCalled;
        bool closeCalled;
        bool abortCalled;
        bool tryCloseCalled;
        string identifier;
        IList<AmqpSymbol> mutualCapabilities;

        object thisLock = new object();

        public event EventHandler Opened;
        public event EventHandler Closed;

        public AmqpObject()
        {
            this.identifier = this.Type + AmqpObject.nextId.InterlockedIncrement();
            this.DefaultOpenTimeout = TimeSpan.FromSeconds(AmqpConstants.DefaultTimeout);
            this.DefaultCloseTimeout = TimeSpan.FromSeconds(AmqpConstants.DefaultTimeout);
        }

        public string Identifier
        {
            get { return this.identifier; }
        }

        public AmqpObjectState State
        {
            get;
            protected set;
        }

        public Exception TerminalException
        {
            get;
            protected set;
        }

        public TimeSpan DefaultOpenTimeout
        {
            get;
            protected set;
        }

        public TimeSpan DefaultCloseTimeout
        {
            get;
            protected set;
        }

        public IList<AmqpSymbol> MutualCapabilities
        {
            get
            {
                if (this.mutualCapabilities == null)
                {
                    this.mutualCapabilities = new List<AmqpSymbol>();
                }

                return this.mutualCapabilities;
            }
        }

        protected abstract string Type
        {
            get;
        }

        protected object ThisLock
        {
            get { return this.thisLock; }
        }

        public void Open()
        {
            this.Open(this.DefaultOpenTimeout);
        }

        public void Open(TimeSpan timeout)
        {
            this.OnOpen(timeout);
            this.NotifyOpened();
        }

        public IAsyncResult BeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            if (Interlocked.Exchange(ref this.openCalled, 1) == 1)
            {
                throw new InvalidOperationException(SRClient.InvalidReOpenOperation);
            }

            Utils.Trace(TraceLevel.Verbose, "{0}: opening", this);
            return new OpenAsyncResult(this, timeout, callback, state);
        }

        public void EndOpen(IAsyncResult result)
        {
            OpenAsyncResult.End(result);
            Utils.Trace(TraceLevel.Verbose, "{0}: opened", this);
        }

        public void Close()
        {
            this.Close(this.DefaultCloseTimeout);
        }

        public void Close(TimeSpan timeout)
        {
            this.EndClose(this.BeginClose(timeout, null, null));
        }

        public IAsyncResult BeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            bool closed = false;
            lock (this.ThisLock)
            {
                closed = (this.closeCalled ||
                    this.State == AmqpObjectState.End ||
                    this.State == AmqpObjectState.CloseSent);
                this.closeCalled = true;
            }

            if (closed)
            {
                return new CompletedAsyncResult(callback, state);
            }
            else
            {
                Utils.Trace(TraceLevel.Verbose, "{0}: closing", this);
                return new CloseAsyncResult(this, timeout, callback, state);
            }
        }

        public void EndClose(IAsyncResult result)
        {
            try
            {
                if (result is CompletedAsyncResult)
                {
                    CompletedAsyncResult.End(result);
                }
                else
                {
                    CloseAsyncResult.End(result);
                }
            }
            finally
            {
                this.NotifyClosed();
            }
        }

        /// <summary>
        /// Move to End state without closing
        /// </summary>
        public void Abort()
        {
            AmqpObjectState state;
            lock (this.ThisLock)
            {
                state = this.State;
                if (this.abortCalled || state == AmqpObjectState.End)
                {
                    return;
                }

                this.abortCalled = true;
            }

            this.AbortInternal();

            try
            {
                this.CompleteOpen(false, new OperationCanceledException("Abort", this.TerminalException));
            }
            finally
            {
                this.State = AmqpObjectState.End;
                this.CompleteClose(false, new OperationCanceledException("Abort", this.TerminalException));
            }
        }

        /// <summary>
        /// Try close with best effort
        /// </summary>
        public void TryClose()
        {
            this.TryClose(new AmqpException(AmqpError.InternalError));
        }

        public void TryClose(Exception exception)
        {
            lock (this.ThisLock)
            {
                if (this.abortCalled || this.tryCloseCalled || this.State == AmqpObjectState.End)
                {
                    return;
                }

                if (!this.IsClosing())
                {
                    this.State = AmqpObjectState.Faulted;
                }

                this.tryCloseCalled = true;
            }

            if (exception != null)
            {
                Utils.Trace(TraceLevel.Error, "{0}: Fault with exception: {1}", this, exception != null ? exception.Message : string.Empty);
                this.TerminalException = exception;
                this.CompleteOpen(false, exception);
            }

            try
            {
                this.TryCloseInternal();
                this.BeginClose(TimeSpan.FromSeconds(AmqpConstants.DefaultTryCloseTimeout), onTryCloseComplete, this);
            }
            catch (Exception exp)
            {
                if (Fx.IsFatal(exp))
                {
                    throw;
                }

                this.NotifyClosed();
            }
        }

        public override string ToString()
        {
            return this.identifier;
        }

        protected virtual void OnOpen(TimeSpan timeout)
        {
            this.EndOpen(this.BeginOpen(timeout, null, null));
        }

        protected abstract bool OpenInternal();
        
        protected abstract bool CloseInternal();

        protected abstract void AbortInternal();

        protected virtual void TryCloseInternal()
        {
        }

        protected void FindMutualCapabilites(Multiple<AmqpSymbol> desired, Multiple<AmqpSymbol> offered)
        {
            this.mutualCapabilities = Multiple<AmqpSymbol>.Intersect(desired, offered);
        }

        protected void ThrowIfNotOpen()
        {
            if (this.State != AmqpObjectState.Opened && this.State != AmqpObjectState.OpenPipe)
            {
                throw new AmqpException(AmqpError.IllegalState, SRClient.AmqpUnopenObject);
            }
        }

        protected bool IsClosing()
        {
            AmqpObjectState state = this.State;
            return state == AmqpObjectState.CloseSent ||
                state == AmqpObjectState.CloseReceived ||
                state == AmqpObjectState.ClosePipe ||
                state == AmqpObjectState.End ||
                state == AmqpObjectState.Faulted;
        }

        protected void CompleteOpen(bool syncComplete, Exception exception)
        {
            Action<bool, Exception> completeCallback = Interlocked.Exchange(ref this.openCompleteCallback, null);
            if (completeCallback != null)
            {
                Utils.Trace(TraceLevel.Verbose, "{0}: Complete open, sync: {1}", this, syncComplete);
                completeCallback(syncComplete, exception);
            }
            else
            {
                this.NotifyOpened();
            }
        }

        protected void CompleteClose(bool syncComplete, Exception exception)
        {
            Action<bool, Exception> completeCallback = Interlocked.Exchange(ref this.closeCompleteCallback, null);
            if (completeCallback != null)
            {
                Utils.Trace(TraceLevel.Verbose, "{0}: Completing close, sync: {1}", this, syncComplete);
                completeCallback(syncComplete, exception);
            }
            else
            {
                this.NotifyClosed();
            }
        }

        protected void TransitState(string operation, StateTransition[] states)
        {
            StateTransition state;
            this.TransitState(operation, states, out state);
        }

        protected void TransitState(string operation, StateTransition[] states, out StateTransition state)
        {
            state = null;

            lock (this.ThisLock)
            {
                foreach (StateTransition st in states)
                {
                    if (st.From == this.State)
                    {
                        this.State = st.To;
                        state = st;
                        break;
                    }
                }
            }

            if (state == null)
            {
                throw new AmqpException(AmqpError.IllegalState, SRClient.AmqpIllegalOperationState(operation, this.State));
            }

            Utils.Trace(TraceLevel.Info, "{0}: {1} {2} -> {3}", this, operation, state.From, state.To);
        }

        protected void OnReceiveCloseCommand(string command, Error error)
        {
            this.TerminalException = error == null ? null : new AmqpException(error);

            try
            {
                StateTransition stateTransition;
                this.TransitState(command, StateTransition.ReceiveClose, out stateTransition);
                if (stateTransition.To == AmqpObjectState.End)
                {
                    this.CompleteClose(false, this.TerminalException);
                }
                else if (stateTransition.To == AmqpObjectState.CloseReceived)
                {
                    this.Close();
                }
            }
            catch (AmqpException)
            {
                this.State = AmqpObjectState.CloseReceived;
                this.TryClose(this.TerminalException);
            }
        }

        static void OnTryCloseComplete(IAsyncResult result)
        {
            AmqpObject thisPtr = (AmqpObject)result.AsyncState;
            try
            {
                thisPtr.EndClose(result);
            }
            catch (Exception exception)
            {
                if (Fx.IsFatal(exception))
                {
                    throw;
                }

                Fx.Exception.AsError(exception);
            }
            finally
            {
                thisPtr.NotifyClosed();
            }
        }

        void NotifyOpened()
        {
            EventHandler opened = Interlocked.Exchange(ref this.Opened, null);
            if (opened != null)
            {
                opened(this, EventArgs.Empty);
            }
        }

        void NotifyClosed()
        {
            EventHandler closed = Interlocked.Exchange(ref this.Closed, null);
            if (closed != null)
            {
                closed(this, EventArgs.Empty);
            }
        }

        abstract class TimeoutAsyncResult : AsyncResult
        {
            static Action<object> timerCallback = new Action<object>(OnTimerCallback);
            volatile int completed;
            IOThreadTimer timer;

            public TimeoutAsyncResult(TimeSpan timeout, AsyncCallback callback, object state)
                : base(callback, state)
            {
                if (timeout != TimeSpan.MaxValue)
                {
                    this.timer = new IOThreadTimer(timerCallback, this, true);
                    this.timer.Set(timeout);
                }
            }

            public new void Complete(bool syncComplete)
            {
                this.Complete(syncComplete, null);
            }

            public new void Complete(bool syncComplete, Exception exception)
            {
                if (this.timer != null)
                {
                    this.timer.Cancel();
                }

                this.CompleteInternal(syncComplete, exception);
            }

            static void OnTimerCallback(object state)
            {
                TimeoutAsyncResult thisPtr = (TimeoutAsyncResult)state;
                thisPtr.CompleteInternal(false, new TimeoutException());
            }

            void CompleteInternal(bool syncComplete, Exception exception)
            {
#pragma warning disable 0420
                if (Interlocked.CompareExchange(ref this.completed, 1, 0) == 0)
#pragma warning restore 0420
                {
                    if (exception == null)
                    {
                        base.Complete(syncComplete);
                    }
                    else
                    {
                        base.Complete(syncComplete, exception);
                    }
                }
            }
        }

        abstract class AmqpObjectAsyncResult : TimeoutAsyncResult
        {
            AmqpObject amqpObject;

            public AmqpObjectAsyncResult(AmqpObject amqpObject, TimeSpan timeout, AsyncCallback callback, object asyncState)
                : base(timeout, callback, asyncState)
            {
                this.amqpObject = amqpObject;
            }

            protected AmqpObject AmqpObject
            {
                get { return this.amqpObject; }
            }

            protected void OnComplete(bool syncComplete, Exception exception)
            {
                if (exception != null)
                {
                    this.Complete(syncComplete, exception);
                }
                else
                {
                    this.UpdateState();
                    this.Complete(syncComplete);
                }
            }

            protected abstract void UpdateState();
        }

        sealed class OpenAsyncResult : AmqpObjectAsyncResult
        {
            public OpenAsyncResult(AmqpObject amqpObject, TimeSpan timeout, AsyncCallback callback, object asyncState)
                : base(amqpObject, timeout, callback, asyncState)
            {
                amqpObject.openCompleteCallback = this.OnComplete;
                if (amqpObject.OpenInternal())
                {
                    this.OnComplete(true, null);
                }
            }

            public static void End(IAsyncResult result)
            {
                OpenAsyncResult thisPtr = AsyncResult.End<OpenAsyncResult>(result);
                thisPtr.AmqpObject.NotifyOpened();
            }

            protected override void UpdateState()
            {
                if (this.AmqpObject.State != AmqpObjectState.Opened)
                {
                    this.AmqpObject.State = AmqpObjectState.Opened;
                }
            }
        }

        sealed class CloseAsyncResult : AmqpObjectAsyncResult
        {
            public CloseAsyncResult(AmqpObject amqpObject, TimeSpan timeout, AsyncCallback callback, object asyncState)
                : base(amqpObject, timeout, callback, asyncState)
            {
                amqpObject.closeCompleteCallback = this.OnComplete;
                if (amqpObject.CloseInternal())
                {
                    this.OnComplete(true, null);
                }
            }

            public static void End(IAsyncResult result)
            {
                CloseAsyncResult thisPtr = AsyncResult.End<CloseAsyncResult>(result);
            }

            protected override void UpdateState()
            {
                if (this.AmqpObject.State != AmqpObjectState.End)
                {
                    this.AmqpObject.State = AmqpObjectState.End;
                }
            }
        }
    }
}
