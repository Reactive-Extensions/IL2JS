//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System.Text;
    sealed class Header : DescribedList
    {
        public static readonly string Name = "amqp:header:list";
        public static readonly ulong Code = 0x0000000000000070;
        const int Fields = 5;

        public Header() : base(Name, Code) { }

        public bool? Durable { get; set; }

        public byte? Priority { get; set; }

        public uint? Ttl { get; set; }

        public bool? FirstAcquirer { get; set; }

        public uint? DeliveryCount { get; set; }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("header(");
            int count = 0;
            this.AddFieldToString(this.Durable != null, sb, "durable", this.Durable, ref count);
            this.AddFieldToString(this.Priority != null, sb, "priority", this.Priority, ref count);
            this.AddFieldToString(this.Ttl != null, sb, "ttl", this.Ttl, ref count);
            this.AddFieldToString(this.FirstAcquirer != null, sb, "first-acquirer", this.FirstAcquirer, ref count);
            this.AddFieldToString(this.DeliveryCount != null, sb, "delivery-count", this.DeliveryCount, ref count);
            sb.Append(')');
            return sb.ToString();
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
            AmqpCodec.EncodeBoolean(this.Durable, buffer);
            AmqpCodec.EncodeUByte(this.Priority, buffer);
            AmqpCodec.EncodeUInt(this.Ttl, buffer);
            AmqpCodec.EncodeBoolean(this.FirstAcquirer, buffer);
            AmqpCodec.EncodeUInt(this.DeliveryCount, buffer);
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.Durable = AmqpCodec.DecodeBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.Priority = AmqpCodec.DecodeUByte(buffer);
            }

            if (count-- > 0)
            {
                this.Ttl = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.FirstAcquirer = AmqpCodec.DecodeBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.DeliveryCount = AmqpCodec.DecodeUInt(buffer);
            }
        }

        protected override int OnValueSize()
        {
            int valueSize = 0;

            valueSize = AmqpCodec.GetBooleanEncodeSize(this.Durable);
            valueSize += AmqpCodec.GetUByteEncodeSize(this.Priority);
            valueSize += AmqpCodec.GetUIntEncodeSize(this.Ttl);
            valueSize += AmqpCodec.GetBooleanEncodeSize(this.FirstAcquirer);
            valueSize += AmqpCodec.GetUIntEncodeSize(this.DeliveryCount);

            return valueSize;
        }
    }
}
