//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ServiceBus.Messaging.Amqp;

    abstract class TransportProvider
    {
        List<AmqpVersion> versions;

        public ProtocolId ProtocolId
        {
            get;
            protected set;
        }

        /// <summary>
        /// Supported versions in preferred order.
        /// </summary>
        public IList<AmqpVersion> Versions
        {
            get
            {
                if (this.versions == null)
                {
                    this.versions = new List<AmqpVersion>();
                }

                return this.versions;
            }
        }

        public AmqpVersion DefaultVersion
        {
            get 
            {
                if (this.Versions.Count == 0)
                {
                    throw new ArgumentException(SRClient.ProtocolVersionNotSet);
                }

                return this.Versions[0]; 
            }
        }

        public bool TryGetVersion(AmqpVersion requestedVersion, out AmqpVersion supportedVersion)
        {
            supportedVersion = this.DefaultVersion;
            foreach (AmqpVersion version in this.Versions)
            {
                if (version.Equals(requestedVersion))
                {
                    supportedVersion = requestedVersion;
                    return true;
                }
            }

            return false;
        }

        public TransportBase CreateTransport(TransportBase innerTransport, bool isInitiator)
        {
            return this.OnCreateTransport(innerTransport, isInitiator);
        }

        protected abstract TransportBase OnCreateTransport(TransportBase innerTransport, bool isInitiator);
    }
}
