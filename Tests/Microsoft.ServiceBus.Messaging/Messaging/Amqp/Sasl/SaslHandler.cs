//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
    using System;
    using System.Security.Principal;
    using Microsoft.ServiceBus.Common;

    abstract class SaslHandler
    {
        SaslNegotiator saslNegotiator;

        public string Mechanism
        {
            get;
            protected set;
        }

        public IPrincipal Principal
        {
            get;
            protected set;
        }

        protected SaslNegotiator Negotiator
        {
            get { return this.saslNegotiator; }
        }

        public void Start(SaslNegotiator saslNegotiator, SaslInit init, bool isClient)
        {
            this.saslNegotiator = saslNegotiator;

            try
            {
                this.OnStart(init, isClient);
            }
            catch (Exception exception)
            {
                if (Fx.IsFatal(exception))
                {
                    throw;
                }

                this.saslNegotiator.CompleteNegotiation(SaslCode.Sys, exception);
            }
        }

        public override string ToString()
        {
            return this.Mechanism;
        }

        public abstract SaslHandler Clone();

        public abstract void OnChallenge(SaslChallenge challenge);

        public abstract void OnResponse(SaslResponse response);

        protected abstract void OnStart(SaslInit init, bool isClient);
    }
}
