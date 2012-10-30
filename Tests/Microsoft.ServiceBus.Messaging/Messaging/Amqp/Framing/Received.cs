//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System.Text;

    sealed class Received : DeliveryState
    {
        public static readonly string Name = "amqp:received:list";
        public static readonly ulong Code = 0x0000000000000023;
        const int Fields = 2;

        public Received() : base(Name, Code) { }

        public uint? SectionNumber { get; set; }

        public ulong? SectionOffset { get; set; }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("received(");
            int count = 0;
            this.AddFieldToString(this.SectionNumber != null, sb, "section-number", this.SectionNumber, ref count);
            this.AddFieldToString(this.SectionOffset != null, sb, "section-offset", this.SectionOffset, ref count);
            sb.Append(')');
            return sb.ToString();
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
            AmqpCodec.EncodeUInt(this.SectionNumber, buffer);
            AmqpCodec.EncodeULong(this.SectionOffset, buffer);
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.SectionNumber = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.SectionOffset = AmqpCodec.DecodeULong(buffer);
            }
        }

        protected override int OnValueSize()
        {
            int valueSize = 0;

            valueSize += AmqpCodec.GetUIntEncodeSize(this.SectionNumber);
            valueSize += AmqpCodec.GetULongEncodeSize(this.SectionOffset);

            return valueSize;
        }
    }
}
