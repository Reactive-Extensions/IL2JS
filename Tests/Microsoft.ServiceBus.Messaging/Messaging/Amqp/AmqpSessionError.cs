//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    static class AmqpSessionError
    {
        public static readonly AmqpSymbol UnsettledLimitExceeded = "amqp:session:unsettled-limit-exceeded";
        public static readonly AmqpSymbol UnsettledLinkError = "amqp:session:link-error";
    }
}