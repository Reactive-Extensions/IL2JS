//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    abstract class LinkPerformative : Performative
    {
        protected LinkPerformative(AmqpSymbol name, ulong code)
            : base(name, code)
        {
        }

        public uint? Handle { get; set; }
    }
}
