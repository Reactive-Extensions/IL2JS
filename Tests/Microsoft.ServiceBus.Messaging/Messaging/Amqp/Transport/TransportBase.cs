//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
    using System;
    using System.Security.Principal;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    abstract class TransportBase : AmqpObject
    {
        protected TransportBase()
        {
        }

        public IPrincipal Principal
        {
            get;
            protected set;
        }

        public virtual bool IsSecure
        {
            get { return false; }
        }

        public bool IsAuthenticated
        {
            get { return this.Principal != null && this.Principal.Identity.IsAuthenticated; }
        }

        public abstract void Shutdown(TransportShutdownOption how);

        public abstract bool WriteAsync(TransportAsyncCallbackArgs args);

        public abstract bool ReadAsync(TransportAsyncCallbackArgs args);

        protected override void OnOpen(TimeSpan timeout)
        {
            this.State = AmqpObjectState.Opened;
        }

        protected override bool OpenInternal()
        {
            return true;
        }
    }
}
