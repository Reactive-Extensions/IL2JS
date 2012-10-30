//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
    using System;
    using Microsoft.ServiceBus.Common;
    using Microsoft.ServiceBus.Messaging.Amqp;

    abstract class TransportListener : AmqpObject
    {
        Action<object> notifyAccept;
        Action<TransportAsyncCallbackArgs> acceptCallback;

        public void Listen(Action<TransportAsyncCallbackArgs> callback)
        {
            this.notifyAccept = this.NotifyAccept;
            this.acceptCallback = callback;

            this.OnListen();
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            this.State = AmqpObjectState.Opened;
        }

        protected override bool OpenInternal()
        {
            return true;
        }

        protected override bool CloseInternal()
        {
            return true;
        }

        protected override void AbortInternal()
        {
        }

        protected void OnTransportAccepted(TransportAsyncCallbackArgs args)
        {
            if (args.CompletedSynchronously)
            {
                ActionItem.Schedule(this.notifyAccept, args);
            }
            else
            {
                this.NotifyAccept(args);
            }
        }

        protected abstract void OnListen();

        void NotifyAccept(object state)
        {
            TransportAsyncCallbackArgs args = (TransportAsyncCallbackArgs)state;
            this.acceptCallback(args);
        }
    }
}
