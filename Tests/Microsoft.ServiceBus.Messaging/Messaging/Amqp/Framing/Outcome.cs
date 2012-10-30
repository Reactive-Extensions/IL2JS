//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    abstract class Outcome : DeliveryState
    {
        protected Outcome(AmqpSymbol name, ulong code)
            : base(name, code)
        {
        }
    }
}
