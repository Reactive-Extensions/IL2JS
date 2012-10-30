//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
    sealed class AmqpTransportProvider : TransportProvider
    {
        public AmqpTransportProvider()
        {
            this.ProtocolId = ProtocolId.Amqp;
        }

        protected override TransportBase OnCreateTransport(TransportBase innerTransport, bool isInitiator)
        {
            return innerTransport;
        }
    }
}
