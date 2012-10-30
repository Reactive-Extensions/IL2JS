//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ServiceBus.Common;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;
    using Microsoft.ServiceBus.Messaging.Amqp.Sasl;
    using Microsoft.ServiceBus.Messaging.Amqp.Transport;

    sealed class AmqpSettings
    {
        List<TransportProvider> transportProviders;

        public AmqpSettings()
        {
            this.DefaultLinkCredit = AmqpConstants.DefaultLinkCredit;
            this.AllowAnonymousConnection = true;
            this.AuthorizationDisabled = true;
        }

        public uint DefaultLinkCredit
        {
            get;
            set;
        }

        public bool RequireSecureTransport
        {
            get;
            set;
        }

        public bool AllowAnonymousConnection
        {
            get;
            set;
        }

        public bool AuthorizationDisabled
        {
            get;
            set;
        }

        /// <summary>
        /// Providers should be added in preferred order.
        /// </summary>
        public IList<TransportProvider> TransportProviders
        {
            get 
            {
                if (this.transportProviders == null)
                {
                    this.transportProviders = new List<TransportProvider>();
                }

                return this.transportProviders; 
            }
        }

        public IRuntimeProvider RuntimeProvider
        {
            get;
            set;
        }

        public T GetTransportProvider<T>() where T : TransportProvider
        {
            foreach (TransportProvider provider in this.transportProviders)
            {
                if (provider is T)
                {
                    return (T)provider;
                }
            }

            return null;
        }

        public bool TryGetTransportProvider(ProtocolHeader header, out TransportProvider provider)
        {
            if (this.TransportProviders.Count == 0)
            {
                throw new ArgumentException("TransportProviders");
            }

            provider = null;
            foreach (TransportProvider transportProvider in this.TransportProviders)
            {
                if (transportProvider.ProtocolId == header.ProtocolId)
                {
                    provider = transportProvider;
                    return true;
                }
            }

            // Not found. Return the preferred one based on settings
            provider = this.GetDefaultProvider();
            return false;
        }

        public ProtocolHeader GetDefaultHeader()
        {
            TransportProvider provider = this.GetDefaultProvider();
            return new ProtocolHeader(provider.ProtocolId, provider.DefaultVersion);
        }

        public ProtocolHeader GetSupportedHeader(ProtocolHeader requestedHeader)
        {
            // Protocol id negotiation
            TransportProvider provider = null;
            if (!this.TryGetTransportProvider(requestedHeader, out provider))
            {
                return this.GetDefaultHeader();
            }

            // Protocol version negotiation
            AmqpVersion version;
            if (!provider.TryGetVersion(requestedHeader.Version, out version))
            {
                return new ProtocolHeader(provider.ProtocolId, provider.DefaultVersion);
            }

            return requestedHeader;
        }

        public AmqpSettings Clone()
        {
            AmqpSettings settings = new AmqpSettings();
            settings.DefaultLinkCredit = this.DefaultLinkCredit;
            settings.transportProviders = new List<TransportProvider>(this.TransportProviders);
            settings.RuntimeProvider = this.RuntimeProvider;
            settings.RequireSecureTransport = this.RequireSecureTransport;
            settings.AllowAnonymousConnection = this.AllowAnonymousConnection;
            settings.AuthorizationDisabled = this.AuthorizationDisabled;
            return settings;
        }

        public void ValidateInitiatorSettings()
        {
            if (this.TransportProviders.Count == 0)
            {
                throw new ArgumentException("TransportProviders");
            }
        }

        public void ValidateListenerSettings()
        {
            if (this.TransportProviders.Count == 0)
            {
                throw new ArgumentException("TransportProviders");
            }
        }

        TransportProvider GetDefaultProvider()
        {
            TransportProvider provider = null;
            if (this.RequireSecureTransport)
            {
                provider = this.GetTransportProvider<TlsTransportProvider>();
            }
            else if (!this.AllowAnonymousConnection)
            {
                provider = this.GetTransportProvider<SaslTransportProvider>();
            }
            else
            {
                provider = this.GetTransportProvider<AmqpTransportProvider>();
            }

            return provider;
        }
    }
}
