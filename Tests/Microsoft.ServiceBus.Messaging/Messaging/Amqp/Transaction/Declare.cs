//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transaction
{
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    sealed class Declare : Performative
    {
        public static readonly string Name = "amqp:declare:list";
        public static readonly ulong Code = 0x0000000000000031;
        const int Fields = 1;

        public Declare() : base(Name, Code) { }

        public object GlobalId { get; set; }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        public override string ToString()
        {
            return "declare()";
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
            AmqpCodec.EncodeObject(this.GlobalId, buffer);
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.GlobalId = AmqpEncoding.DecodeObject(buffer);
            }
        }

        protected override int OnValueSize()
        {
            int valueSize = 0;

            valueSize += AmqpCodec.GetObjectEncodeSize(this.GlobalId);

            return valueSize;
        }
    }
}
