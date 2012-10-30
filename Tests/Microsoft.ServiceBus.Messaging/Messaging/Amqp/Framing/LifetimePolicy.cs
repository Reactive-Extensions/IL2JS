//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    abstract class LifeTimePolicy : DescribedList
    {
        const int Fields = 0;

        protected LifeTimePolicy(AmqpSymbol name, ulong code)
            : base(name, code)
        {
        }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
        }

        protected override int OnValueSize()
        {
            return 0;
        }
    }
}
