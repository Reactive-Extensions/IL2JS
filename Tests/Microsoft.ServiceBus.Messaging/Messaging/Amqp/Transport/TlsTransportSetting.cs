//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
    using System;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    sealed class TlsTransportSettings : TransportSettings
    {
        TransportSettings innerSettings;

        public TlsTransportSettings()
            : this(null, true)
        {
            // Called to create a ssl upgrade transport setting. No inner settings is
            // required as the inner transport already exists for upgrading.
        }

        public TlsTransportSettings(TransportSettings innerSettings)
            : this(innerSettings, true)
        {
        }

        public TlsTransportSettings(TransportSettings innerSettings, bool isInitiator)
        {
            this.innerSettings = innerSettings;
            this.IsInitiator = isInitiator;
        }

        public bool IsInitiator
        {
            get;
            set;
        }

        public string TargetHost
        {
            get;
            set;
        }

        public X509Certificate2 Certificate
        {
            get;
            set;
        }

        public TransportSettings InnerTransportSettings
        {
            get { return this.innerSettings; }
        }

        public RemoteCertificateValidationCallback CertificateValidationCallback
        {
            get;
            set;
        }

        public override TransportInitiator CreateInitiator()
        {
            if (this.TargetHost == null)
            {
                throw new InvalidOperationException(SRClient.TargetHostNotSet);
            }

            return new TlsTransportInitiator(this);
        }

        public override TransportListener CreateListener()
        {
            if (this.Certificate == null)
            {
                throw new InvalidOperationException(SRClient.ServerCertificateNotSet);
            }

            return new TlsTransportListener(this);
        }

        public override string ToString()
        {
            return this.TargetHost ?? this.Certificate.Subject;
        }
    }
}
