//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
    using System;
    using Microsoft.ServiceBus.Common;

    /// <summary>
    /// This listener accepts SSL transport directly (no AMQP security upgrade)
    /// </summary>
    sealed class TlsTransportListener : TransportListener
    {
        readonly AsyncCallback onTransportOpened;
        readonly TlsTransportSettings transportSettings;
        TransportListener innerListener;

        public TlsTransportListener(TlsTransportSettings transportSettings)
        {
            this.transportSettings = transportSettings;
            this.onTransportOpened = this.OnTransportOpened;
        }

        protected override string Type
        {
            get { return "tls-listener"; }
        }

        protected override bool CloseInternal()
        {
            if (this.innerListener != null)
            {
                this.innerListener.Close();
            }

            return true;
        }

        protected override void AbortInternal()
        {
            if (this.innerListener != null)
            {
                this.innerListener.Abort();
            }
        }

        protected override void TryCloseInternal()
        {
            if (this.innerListener != null)
            {
                this.innerListener.TryClose();
            }
        }

        protected override void OnListen()
        {
            TransportSettings innerSettings = this.transportSettings.InnerTransportSettings;
            this.innerListener = innerSettings.CreateListener();
            this.innerListener.Closed += new EventHandler(OnInnerListenerClosed);
            this.innerListener.Listen(this.OnAcceptInnerTransport);
        }

        void OnInnerListenerClosed(object sender, EventArgs e)
        {
            if (!this.IsClosing())
            {
                TransportListener innerListener = (TransportListener)sender;
                this.TryClose(innerListener.TerminalException);
            }
        }

        void OnAcceptInnerTransport(TransportAsyncCallbackArgs innerArgs)
        {
            Fx.Assert(innerArgs.Exception == null, "Should not be called with an exception.");
            Fx.Assert(innerArgs.Transport != null, "Should be called with a transport.");
            Utils.Trace(TraceLevel.Info, "{0}: Tls listener accepted an inner transport {1}", this, innerArgs.Transport);

            try
            {
                // upgrade transport
                innerArgs.Transport = new TlsTransport(innerArgs.Transport, this.transportSettings);
                IAsyncResult result = innerArgs.Transport.BeginOpen(
                    innerArgs.Transport.DefaultOpenTimeout, 
                    this.onTransportOpened,
                    innerArgs);
                if (result.CompletedSynchronously)
                {
                    this.HandleTransportOpened(result);
                    return;
                }
            }
            catch (Exception exception)
            {
                if (Fx.IsFatal(exception))
                {
                    throw;
                }

                innerArgs.Transport.TryClose(exception);
            }
        }

        void OnTransportOpened(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            try
            {
                this.HandleTransportOpened(result);
            }
            catch (Exception exception)
            {
                if (Fx.IsFatal(exception))
                {
                    throw;
                }

                TransportAsyncCallbackArgs innerArgs = (TransportAsyncCallbackArgs)result.AsyncState;
                innerArgs.Transport.TryClose(exception);
            }
        }

        void HandleTransportOpened(IAsyncResult result)
        {
            TransportAsyncCallbackArgs innerArgs = (TransportAsyncCallbackArgs)result.AsyncState;
            innerArgs.Transport.EndOpen(result);
            if (innerArgs.CompletedSynchronously)
            {
                innerArgs.CompletedSynchronously = result.CompletedSynchronously;
            }

            this.OnTransportAccepted(innerArgs);
        }
    }
}
