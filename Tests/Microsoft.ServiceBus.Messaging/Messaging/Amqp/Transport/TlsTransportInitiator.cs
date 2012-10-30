//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
    using System;
    using Microsoft.ServiceBus.Common;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    /// <summary>
    /// This initiator establishes an SSL connection (no AMQP security upgrade)
    /// </summary>
    sealed class TlsTransportInitiator : TransportInitiator
    {
        static readonly AsyncCallback onTransportOpened = OnTransportOpened;

        TlsTransportSettings transportSettings;
        TransportAsyncCallbackArgs callbackArgs;
        TimeoutHelper timeoutHelper;

        public TlsTransportInitiator(TlsTransportSettings transportSettings)
        {
            this.transportSettings = transportSettings;
        }

        public override string ToString()
        {
            return "tls-initiator";
        }

        public override bool ConnectAsync(TimeSpan timeout, TransportAsyncCallbackArgs callbackArgs)
        {
            Utils.Trace(TraceLevel.Info, "{0}: Tls initiator start connecting...", this);
            this.callbackArgs = callbackArgs;
            this.timeoutHelper = new TimeoutHelper(timeout);
            TransportInitiator innerInitiator = this.transportSettings.InnerTransportSettings.CreateInitiator();

            TransportAsyncCallbackArgs innerArgs = new TransportAsyncCallbackArgs();
            innerArgs.CompletedCallback = OnInnerTransportConnected;
            innerArgs.UserToken = this;
            if (innerInitiator.ConnectAsync(timeout, innerArgs))
            {
                // pending
                return true;
            }
            else
            {
                this.HandleInnerTransportConnected(innerArgs);
                return !this.callbackArgs.CompletedSynchronously;
            }
        }

        static void OnInnerTransportConnected(TransportAsyncCallbackArgs innerArgs)
        {
            TlsTransportInitiator thisPtr = (TlsTransportInitiator)innerArgs.UserToken;
            thisPtr.HandleInnerTransportConnected(innerArgs);
        }

        static void OnTransportOpened(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            TlsTransportInitiator thisPtr = (TlsTransportInitiator)result.AsyncState;
            try
            {
                thisPtr.HandleTransportOpened(result);
            }
            catch (Exception exception)
            {
                if (Fx.IsFatal(exception))
                {
                    throw;
                }

                thisPtr.callbackArgs.Exception = exception;
            }

            thisPtr.Complete();
        }

        void HandleInnerTransportConnected(TransportAsyncCallbackArgs innerArgs)
        {
            this.callbackArgs.CompletedSynchronously = innerArgs.CompletedSynchronously;
            if (innerArgs.Exception != null)
            {
                this.callbackArgs.Exception = innerArgs.Exception;
                this.Complete();
            }
            else
            {
                Fx.Assert(innerArgs.Transport != null, "must have a valid inner transport");
                // upgrade transport
                this.callbackArgs.Transport = new TlsTransport(innerArgs.Transport, this.transportSettings);
                try
                {
                    IAsyncResult result = this.callbackArgs.Transport.BeginOpen(this.timeoutHelper.RemainingTime(), onTransportOpened, this);
                    if (result.CompletedSynchronously)
                    {
                        this.HandleTransportOpened(result);
                        this.Complete();
                    }
                }
                catch (Exception exception)
                {
                    if (Fx.IsFatal(exception))
                    {
                        throw;
                    }

                    this.callbackArgs.Exception = exception;
                    this.Complete();
                }
            }
        }

        void HandleTransportOpened(IAsyncResult result)
        {
            this.callbackArgs.Transport.EndOpen(result);
            if (this.callbackArgs.CompletedSynchronously)
            {
                this.callbackArgs.CompletedSynchronously = result.CompletedSynchronously;
            }
        }

        void Complete()
        {
            if (this.callbackArgs.Exception != null && this.callbackArgs.Transport != null)
            {
                this.callbackArgs.Transport.TryClose(this.callbackArgs.Exception);
                this.callbackArgs.Transport = null;
            }

            this.callbackArgs.CompletedCallback(this.callbackArgs);
        }
    }
}
