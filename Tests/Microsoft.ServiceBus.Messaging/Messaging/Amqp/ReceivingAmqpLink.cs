//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.ServiceBus.Common;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;
    using Microsoft.ServiceBus.Messaging.Amqp.Transaction;

    sealed class ReceivingAmqpLink : AmqpLink
    {
        static readonly TimeSpan InitialTimeout = TimeSpan.FromSeconds(10);
        readonly object syncRoot;
        Queue<AmqpMessage> messageQueue;
        LinkedList<ReceiveAsyncResult> waiterList;
        TimeSpan minTimeout;
        Action<AmqpMessage> messageListener;
        ReceivedDelivery currentDelivery;

        public ReceivingAmqpLink(AmqpSession session, AmqpLinkSettings settings) :
            base(session, settings)
        {
            this.minTimeout = InitialTimeout;
            this.syncRoot = new object();
        }

        public void RegisterMessageListener(Action<AmqpMessage> messageListener)
        {
            if (Interlocked.Exchange(ref this.messageListener, messageListener) != null)
            {
                throw new InvalidOperationException(SRClient.MessageListenerAlreadyRegistered);
            }
        }

        public IAsyncResult BeginReceiveMessage(TimeSpan timeout, AsyncCallback callback, object state)
        {
            this.ThrowIfNotOpen();
            if (timeout < this.minTimeout)
            {
                timeout = this.minTimeout;
            }

            AmqpMessage message = null;
            lock (this.syncRoot)
            {
                if (this.messageQueue.Count > 0)
                {
                    message = this.messageQueue.Dequeue();
                }
            }

            if (message == null && timeout > TimeSpan.Zero)
            {
                ReceiveAsyncResult waiter = new ReceiveAsyncResult(this, timeout, callback, state);
                lock (this.syncRoot)
                {
                    if (this.messageQueue.Count > 0)
                    {
                        message = this.messageQueue.Dequeue();
                    }
                    else
                    {
                        LinkedListNode<ReceiveAsyncResult> node = this.waiterList.AddLast(waiter);
                        waiter.Initialize(node);
                    }
                }

                if (message != null)
                {
                    waiter.Signal(message, true);
                }

                return waiter;
            }
            else
            {
                return new CompletedAsyncResult<AmqpMessage>(message, callback, state);
            }
        }

        public AmqpMessage EndReceiveMessage(IAsyncResult result)
        {
            if (result is ReceiveAsyncResult)
            {
                return ReceiveAsyncResult.End(result);
            }
            else
            {
                return CompletedAsyncResult<AmqpMessage>.End(result);
            }
        }

        public IAsyncResult BeginDisposeMessage(ArraySegment<byte> deliveryTag, Outcome outcome, bool batchable, TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new DisposeAsyncResult(this, deliveryTag, outcome, batchable, timeout, callback, state);
        }

        public Outcome EndDisposeMessage(IAsyncResult result)
        {
            return DisposeAsyncResult.End(result);
        }

        public void AcceptMessage(AmqpMessage message, bool batchable)
        {
            bool settled = this.Settings.SettleType != SettleMode.SettleOnDispose;
            this.AcceptMessage(message, settled, batchable);
        }

        public void AcceptMessage(AmqpMessage message, bool settled, bool batchable)
        {
            this.DisposeMessage(message, AmqpConstants.AcceptedOutcome, settled, batchable);
        }

        public void RejectMessage(AmqpMessage message, Exception exception)
        {
            Rejected rejected = new Rejected();
            rejected.Error = AmqpError.FromException(exception);

            this.DisposeMessage(message, rejected, true, false);
        }

        public void ReleaseMessage(AmqpMessage message)
        {
            this.DisposeMessage(message, AmqpConstants.ReleasedOutcome, true, false);
        }

        public void ModifyMessage(AmqpMessage message, bool deliveryFailed, bool deliverElseWhere, Fields messageAttributes)
        {
            Modified modified = new Modified();
            modified.DeliveryFailed = deliveryFailed;
            modified.UndeliverableHere = deliverElseWhere;
            modified.MessageAnnotations = messageAttributes;

            this.DisposeMessage(message, modified, true, false);
        }

        public void DisposeMessage(AmqpMessage message, DeliveryState state, bool settled, bool batchable)
        {
            message.Batchable = batchable;
            this.DisposeDelivery(message, settled, state);
        }

        public override Delivery CreateDelivery()
        {
            this.currentDelivery = new ReceivedDelivery();
            return this.currentDelivery;
        }

        protected override bool OpenInternal()
        {
            this.messageQueue = new Queue<AmqpMessage>();
            this.waiterList = new LinkedList<ReceiveAsyncResult>();
            bool syncComplete = base.OpenInternal();
            if (this.LinkCredit > 0)
            {
                this.SendFlow(false);
            }

            return syncComplete;
        }

        protected override void OnProcessTransfer(Delivery delivery, Transfer transfer)
        {
            Fx.Assert(delivery == null || object.ReferenceEquals(delivery, this.currentDelivery), "The delivery must be null or must be the same as the current message.");
            this.currentDelivery.AddPayload(transfer.Payload);
            if (!transfer.More())
            {
                Utils.Trace(TraceLevel.Debug, "{0}: Complete a message with payload from {1} transfers.", this, this.currentDelivery.Count);
                AmqpMessage message = this.currentDelivery.GetMessage();
                this.currentDelivery = null;
                this.OnReceiveMessage(message);
            }
        }

        protected override void OnCreditAvailable(uint link, bool drain, ArraySegment<byte> txnId)
        {
            this.minTimeout = TimeSpan.Zero;
        }

        protected override bool CloseInternal()
        {
            bool closeSync = base.CloseInternal();
            Queue<AmqpMessage> messages = null;
            LinkedList<ReceiveAsyncResult> waiters = null;
            lock (this.syncRoot)
            {
                messages = this.messageQueue;
                waiters = this.waiterList;
                this.messageQueue = null;
                this.waiterList = null;
            }

            if (messages != null)
            {
                foreach (AmqpMessage message in messages)
                {
                    this.ReleaseMessage(message);
                }
            }

            if (waiters != null)
            {
                foreach (ReceiveAsyncResult waiter in waiters)
                {
                    waiter.Cancel();
                }
            }

            return closeSync;
        }

        void OnReceiveMessage(AmqpMessage message)
        {
            if (this.messageListener != null)
            {
                this.messageListener(message);
            }
            else
            {
                ReceiveAsyncResult waiter = null;
                lock (this.syncRoot)
                {
                    if (this.waiterList != null && this.waiterList.Count > 0)
                    {
                        waiter = this.waiterList.First.Value;
                        this.waiterList.RemoveFirst();
                        waiter.OnRemoved();
                    }
                    else if (this.messageQueue != null)
                    {
                        this.messageQueue.Enqueue(message);
                    }
                }

                if (waiter != null)
                {
                    waiter.Signal(message, false);
                }
            }

            this.minTimeout = TimeSpan.Zero;
        }

        sealed class ReceivedDelivery : Delivery
        {
            List<ArraySegment<byte>> bufferList = new List<ArraySegment<byte>>(4);

            public int Count
            {
                get { return this.bufferList.Count; }
            }

            public void AddPayload(ArraySegment<byte> payload)
            {
                this.bufferList.Add(payload);
            }

            public AmqpMessage GetMessage()
            {
                AmqpMessage message = AmqpMessage.Create(this.bufferList.ToArray());
                this.bufferList = null;

                message.DeliveryTag = this.DeliveryTag;
                message.DeliveryId = this.DeliveryId;
                message.TxnId = this.TxnId;
                message.Settled = this.Settled;
                message.Batchable = this.Batchable;
                message.State = this.State;
                message.StateChanged = this.StateChanged;
                message.Link = this.Link;

                return message;
            }

            protected override ArraySegment<byte>[] GetPayload(int payloadSize, out bool more)
            {
                throw new NotImplementedException();
            }

            protected override void OnCompletePayload(int payloadSize)
            {
                throw new NotImplementedException();
            }
        }

        sealed class ReceiveAsyncResult : AsyncResult
        {
            static Action<object> onTimer = OnTimer;
            readonly ReceivingAmqpLink parent;
            readonly TimeSpan timeout;
            IOThreadTimer timer;
            LinkedListNode<ReceiveAsyncResult> node;
            int completed;
            AmqpMessage message;

            public ReceiveAsyncResult(ReceivingAmqpLink parent, TimeSpan timeout, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.parent = parent;
                Fx.Assert(timeout > TimeSpan.Zero, "must have a non-zero timeout");
                this.timeout = timeout;
            }

            public void Initialize(LinkedListNode<ReceiveAsyncResult> node)
            {
                this.node = node;
                if (this.timeout != TimeSpan.MaxValue)
                {
                    timer = new IOThreadTimer(onTimer, this, false);
                    timer.Set(timeout);
                }
            }

            public static AmqpMessage End(IAsyncResult result)
            {
                return AsyncResult.End<ReceiveAsyncResult>(result).message;
            }

            // Ensure the lock is held when calling this function
            public void OnRemoved()
            {
                this.node = null;
            }

            public void Cancel()
            {
                this.Signal(null, false);
            }

            public void Signal(AmqpMessage message, bool syncComplete)
            {
                IOThreadTimer t = this.timer;
                if (t != null)
                {
                    t.Cancel();
                }

                this.CompleteInternal(message, syncComplete);
            }

            void CompleteInternal(AmqpMessage message, bool syncComplete)
            {
                if (Interlocked.Exchange(ref this.completed, 1) == 0)
                {
                    this.message = message;
                    this.Complete(syncComplete);
                }
            }

            static void OnTimer(object state)
            {
                ReceiveAsyncResult thisPtr = (ReceiveAsyncResult)state;
                lock (thisPtr.parent.syncRoot)
                {
                    if (thisPtr.parent.waiterList == null || thisPtr.node == null)
                    {
                        return;
                    }

                    thisPtr.parent.waiterList.Remove(thisPtr.node);
                    thisPtr.node = null;
                }

                thisPtr.CompleteInternal(null, false);
            }
        }

        sealed class DisposeAsyncResult : AsyncResult
        {
            static readonly Action<object> onDisposeComplete = OnDisposeComplete;
            readonly Delivery delivery;
            Outcome outcome;

            public DisposeAsyncResult(
                ReceivingAmqpLink link,
                ArraySegment<byte> deliveryTag, 
                Outcome outcome, 
                bool batchable, 
                TimeSpan timeout, 
                AsyncCallback callback, 
                object state)
                : base(callback, state)
            {
                if (link.TryGetDelivery(deliveryTag, out this.delivery))
                {
                    delivery.CompleteCallback = onDisposeComplete;
                    delivery.UserToken = this;
                    link.DisposeDelivery(delivery, false, outcome);
                }
                else
                {
                    // Delivery tag not found
                    this.outcome = new Rejected() { Error = AmqpError.NotFound };
                    this.Complete(true);
                }
            }

            public static Outcome End(IAsyncResult result)
            {
                return AsyncResult.End<DisposeAsyncResult>(result).outcome;
            }

            public void Cancel()
            {
                this.Complete(false, new OperationCanceledException());
            }

            static void OnDisposeComplete(object state)
            {
                DisposeAsyncResult thisPtr = (DisposeAsyncResult)state;
                thisPtr.delivery.CompleteCallback = null;
                thisPtr.delivery.UserToken = null;
                DeliveryState deliveryState = thisPtr.delivery.State;
                if (deliveryState is TransactionalState)
                {
                    deliveryState = ((TransactionalState)deliveryState).Outcome;
                }

                thisPtr.outcome = (Outcome)deliveryState;
                thisPtr.Complete(false);
            }
        }
    }
}
