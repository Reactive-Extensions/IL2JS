//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    class DistributionMode
    {
        DistributionMode()
        {
        }

        public static readonly AmqpSymbol Move = "move";
        public static readonly AmqpSymbol Copy = "copy";
    }
}