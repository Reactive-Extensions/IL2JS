//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;
    using Microsoft.ServiceBus.Messaging.Amqp.Transport;

    sealed class SaslTransportProvider : TransportProvider
    {
        Dictionary<string, SaslHandler> handlers;

        public SaslTransportProvider()
        {
            this.ProtocolId = ProtocolId.AmqpSasl;
            this.handlers = new Dictionary<string, SaslHandler>();
        }

        public IEnumerable<string> Mechanisms
        {
            get { return this.handlers.Keys; }
        }

        public void AddHandler(SaslHandler handler)
        {
            Utils.Trace(TraceLevel.Info, "{0}: Add a SASL handler: {1}", this, handler);
            this.handlers.Add(handler.Mechanism, handler);
        }

        public SaslHandler GetHandler(string mechanism, bool clone)
        {
            SaslHandler handler;
            if (!this.handlers.TryGetValue(mechanism, out handler))
            {
                throw new AmqpException(AmqpError.NotImplemented, mechanism);
            }

            return clone ? handler.Clone() : handler;
        }

        public override string ToString()
        {
            return "sasl-provider";
        }

        protected override TransportBase OnCreateTransport(TransportBase innerTransport, bool isInitiator)
        {
            return new SaslTransport(innerTransport, this, isInitiator);
        }
    }
}
