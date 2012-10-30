//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    sealed class AmqpValue : AmqpDescribed
    {
        public static readonly string Name = "amqp:amqp-value:*";
        public static readonly ulong Code = 0x0000000000000077;

        public AmqpValue() : base(Name, Code) { }

        public override int GetValueEncodeSize()
        {
            IAmqpSerializable amqpSerializable = this.Value as IAmqpSerializable;
            if (amqpSerializable != null)
            {
                return amqpSerializable.EncodeSize;
            }
            else
            {
                return base.GetValueEncodeSize();
            }
        }

        public override void EncodeValue(ByteBuffer buffer)
        {
            IAmqpSerializable amqpSerializable = this.Value as IAmqpSerializable;
            if (amqpSerializable != null)
            {
                amqpSerializable.Encode(buffer);
            }
            else
            {
                base.EncodeValue(buffer);
            }
        }

        public override void DecodeValue(ByteBuffer buffer)
        {
            this.Value = AmqpCodec.DecodeObject(buffer);
        }

        public override string ToString()
        {
            return "value()";
        }
    }
}
