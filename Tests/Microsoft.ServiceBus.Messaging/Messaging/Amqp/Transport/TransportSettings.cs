//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
    using System;

    abstract class TransportSettings
    {
        public int ListenerAcceptorCount
        {
            get;
            set;
        }

        public abstract TransportInitiator CreateInitiator();

        public abstract TransportListener CreateListener();
    }
}
