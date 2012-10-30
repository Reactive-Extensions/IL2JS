//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
    using System;
    using System.Security.Principal;
    using System.Text;
    using Microsoft.ServiceBus.Common;
    using Microsoft.ServiceBus.Messaging.Amqp;

    sealed class SaslSwtHandler : SaslHandler
    {
        public static readonly string Name = "SWT";
        TokenProvider tokenProvider;
        string action;
        string appliesTo;
        Func<string, string[]> tokenAuthenticator;

        SaslSwtHandler()
        {
            this.Mechanism = Name;
        }

        public SaslSwtHandler(TokenProvider tokenProvider, string action, string appliesTo)
            : this()
        {
            this.action = action;
            this.tokenProvider = tokenProvider;
            this.appliesTo = appliesTo;
        }

        public SaslSwtHandler(Func<string, string[]> tokenAuthenticator)
            : this()
        {
            this.tokenAuthenticator = tokenAuthenticator;
        }

        public override SaslHandler Clone()
        {
            return new SaslSwtHandler(this.tokenProvider, this.action, this.appliesTo);
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
                string message = this.GetClientMessage();
                Utils.Trace(TraceLevel.Verbose, "client message: {0}", message);
                init.InitialResponse = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
                this.Negotiator.WriteFrame(init, true);
            }
            else
            {
                this.OnInit(init);
            }
        }

        void OnInit(SaslInit init)
        {
            SaslCode code = SaslCode.Ok;

            if (init.InitialResponse.Count > 0)
            {
                string token = Encoding.UTF8.GetString(init.InitialResponse.Array, init.InitialResponse.Offset, init.InitialResponse.Count);
                Utils.Trace(TraceLevel.Verbose, "Received token: {0}", token);

                if (this.tokenAuthenticator != null)
                {
                    try
                    {
                        string[] claimSet = this.tokenAuthenticator(token);
                        this.Principal = new GenericPrincipal(
                            new GenericIdentity("acs-client", SaslSwtHandler.Name),
                            claimSet);
                    }
                    catch (Exception exception)
                    {
                        if (Fx.IsFatal(exception))
                        {
                            throw;
                        }

                        code = SaslCode.Auth;
                    }
                }
            }

            this.Negotiator.CompleteNegotiation(code, null);
        }

        string GetClientMessage()
        {
            SimpleWebSecurityToken swtToken = (SimpleWebSecurityToken)this.tokenProvider.GetToken(
                this.appliesTo, 
                this.action, 
                false, 
                TimeSpan.FromSeconds(5));
            return swtToken.Token;
        }
    }
}
