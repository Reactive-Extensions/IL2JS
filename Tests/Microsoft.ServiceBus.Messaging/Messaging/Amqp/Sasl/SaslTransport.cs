//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
    using System;
    using System.Collections.Generic;
    using System.Security.Principal;
    using Microsoft.ServiceBus.Common;
    using Microsoft.ServiceBus.Messaging.Amqp.Transport;

    sealed class SaslTransport : TransportBase
    {
        bool isInitiator;
        TransportBase innerTransport;
        SaslTransportProvider provider;
        SaslNegotiator negotiator;

        public SaslTransport(TransportBase transport, SaslTransportProvider provider, bool isInitiator)
        {
            this.innerTransport = transport;
            this.provider = provider;
            this.isInitiator = isInitiator;
        }

        public bool IsInitiator
        {
            get { return this.isInitiator; }
        }

        public override bool IsSecure
        {
            get { return this.innerTransport.IsSecure; }
        }

        protected override string Type
        {
            get { return "Sasl"; }
        }

        public override void Shutdown(TransportShutdownOption how)
        {
            this.innerTransport.Shutdown(how);
        }

        public override bool WriteAsync(TransportAsyncCallbackArgs args)
        {
            return this.innerTransport.WriteAsync(args);
        }

        public override bool ReadAsync(TransportAsyncCallbackArgs args)
        {
            return this.innerTransport.ReadAsync(args);
        }

        public void OnNegotiationSucceed(IPrincipal principal)
        {
            Utils.Trace(TraceLevel.Verbose, "{0}: negotiation succeed", this);
            this.negotiator = null;
            this.Principal = principal;
            this.CompleteOpen(false, null);
        }

        public void OnNegotiationFail(Exception exception)
        {
            Utils.Trace(TraceLevel.Error, "{0}: negotiation fail with {1}", this, exception);
            this.negotiator = null;
            this.innerTransport.TryClose(exception);
            this.CompleteOpen(false, exception);
        }

        protected override bool OpenInternal()
        {
            this.negotiator = new SaslNegotiator(this, this.provider);
            return this.negotiator.Start();
        }

        protected override void AbortInternal()
        {
            this.innerTransport.Abort();
        }

        protected override bool CloseInternal()
        {
            this.innerTransport.Close();
            return true;
        }

        protected override void TryCloseInternal()
        {
            this.innerTransport.TryClose(this.TerminalException);
        }
    }
}
