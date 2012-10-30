//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transaction
{
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    sealed class Coordinator : DescribedList
    {
        public static readonly string Name = "amqp:coordinator:list";
        public static readonly ulong Code = 0x0000000000000030;
        const int Fields = 1;

        public Coordinator() : base(Name, Code) { }

        public Multiple<AmqpSymbol> Capabilities { get; set; }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        protected override void EnsureRequired()
        {
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
            AmqpCodec.EncodeMultiple(this.Capabilities, buffer);
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.Capabilities = AmqpCodec.DecodeMultiple<AmqpSymbol>(buffer);
            }
        }

        protected override int OnValueSize()
        {
            int valueSize = 0;

            valueSize += AmqpCodec.GetMultipleEncodeSize(this.Capabilities);

            return valueSize;
        }
    }
}
