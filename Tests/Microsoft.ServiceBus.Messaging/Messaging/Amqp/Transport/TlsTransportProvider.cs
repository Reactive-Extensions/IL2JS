//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
    using System;
    using System.Security.Cryptography.X509Certificates;

    sealed class TlsTransportProvider : TransportProvider
    {
        TlsTransportSettings tlsSettings;

        public TlsTransportProvider(TlsTransportSettings tlsSettings)
        {
            this.tlsSettings = tlsSettings;
            this.ProtocolId = ProtocolId.AmqpTls;
        }

        public TlsTransportSettings Settings
        {
            get { return this.tlsSettings; }
        }

        public override string ToString()
        {
            return "tls-provider";
        }

        protected override TransportBase OnCreateTransport(TransportBase innerTransport, bool isInitiator)
        {
            return new TlsTransport(innerTransport, this.tlsSettings);
        }
    }
}
