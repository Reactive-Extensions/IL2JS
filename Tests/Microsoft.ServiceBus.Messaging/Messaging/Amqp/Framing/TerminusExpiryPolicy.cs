//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    class TerminusExpiryPolicy
    {
        TerminusExpiryPolicy()
        {
        }

        public static readonly AmqpSymbol LinkDetach = "link-detach";
        public static readonly AmqpSymbol SessionEnd = "session-end";
        public static readonly AmqpSymbol ConnectionClose = "connection-close";
        public static readonly AmqpSymbol Never = "never";
    }
}