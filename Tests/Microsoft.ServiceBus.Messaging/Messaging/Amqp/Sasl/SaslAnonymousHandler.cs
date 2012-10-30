//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
    using System;
    using System.Text;
    using System.Collections.Generic;
    using Microsoft.ServiceBus.Messaging.Amqp;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    sealed class SaslAnonymousHandler : SaslHandler
    {
        public static readonly string Name = "ANONYMOUS";

        public SaslAnonymousHandler()
        {
            this.Mechanism = Name;
        }

        public string Identity
        {
            get;
            set;
        }

        public override SaslHandler Clone()
        {
            return new SaslAnonymousHandler();
        }

        public override void OnChallenge(SaslChallenge challenge)
        {
            throw new NotImplementedException();
        }

        public override void OnResponse(SaslResponse response)
        {
            throw new NotImplementedException();
        }

        protected override void OnStart(SaslInit init, bool isClient)
        {
            if (isClient)
            {
                if (this.Identity != null)
                {
                    init.InitialResponse = new ArraySegment<byte>(Encoding.UTF8.GetBytes(this.Identity));
                }

                this.Negotiator.WriteFrame(init, true);
            }
            else
            {
                // server side. send outcome
                this.Negotiator.CompleteNegotiation(SaslCode.Ok, null);
            }
        }
    }
}
