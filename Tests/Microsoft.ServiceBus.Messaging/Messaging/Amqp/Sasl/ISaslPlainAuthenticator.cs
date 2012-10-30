//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
    using System.Security.Principal;

    interface ISaslPlainAuthenticator
    {
        IPrincipal Authenticate(string identity, string password);
    }
}
