//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    abstract class DeliveryState : DescribedList
    {
        public DeliveryState(AmqpSymbol name, ulong code)
            : base(name, code)
        {
        }
    }
}
