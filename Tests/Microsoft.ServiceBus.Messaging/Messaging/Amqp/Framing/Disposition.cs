//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System.Globalization;
    using System.Text;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    sealed class Disposition : Performative
    {
        public static readonly string Name = "amqp:disposition:list";
        public static readonly ulong Code = 0x0000000000000015;
        const int Fields = 6;

        public Disposition() : base(Name, Code) { }

        public bool? Role { get; set; }

        public uint? First { get; set; }

        public uint? Last { get; set; }

        public bool? Settled { get; set; }

        public DeliveryState State { get; set; }

        public bool? Batchable { get; set; }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("disposition(");
            int count = 0;
            this.AddFieldToString(this.Role != null, sb, "role", this.Role, ref count);
            this.AddFieldToString(this.First != null, sb, "first", this.First, ref count);
            this.AddFieldToString(this.Last != null, sb, "last", this.Last, ref count);
            this.AddFieldToString(this.Settled != null, sb, "settled", this.Settled, ref count);
            this.AddFieldToString(this.State != null, sb, "state", this.State, ref count);
            this.AddFieldToString(this.Batchable != null, sb, "batchable", this.Batchable, ref count);
            sb.Append(')');
            return sb.ToString();
        }

        protected override void EnsureRequired()
        {
            if (!this.Role.HasValue)
            {
                throw AmqpEncoding.GetEncodingException("disposition.role");
            }

            if (!this.First.HasValue)
            {
                throw AmqpEncoding.GetEncodingException("disposition.first");
            }
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
            AmqpCodec.EncodeBoolean(this.Role, buffer);
            AmqpCodec.EncodeUInt(this.First, buffer);
            AmqpCodec.EncodeUInt(this.Last, buffer);
            AmqpCodec.EncodeBoolean(this.Settled, buffer);
            AmqpCodec.EncodeSerializable(this.State, buffer);
            AmqpCodec.EncodeBoolean(this.Batchable, buffer);
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.Role = AmqpCodec.DecodeBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.First = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.Last = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.Settled = AmqpCodec.DecodeBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.State = (DeliveryState)AmqpCodec.DecodeAmqpDescribed(buffer);
            }

            if (count-- > 0)
            {
                this.Batchable = AmqpCodec.DecodeBoolean(buffer);
            }
        }

        protected override int OnValueSize()
        {
            int valueSize = 0;

            valueSize += AmqpCodec.GetBooleanEncodeSize(this.Role);
            valueSize += AmqpCodec.GetUIntEncodeSize(this.First);
            valueSize += AmqpCodec.GetUIntEncodeSize(this.Last);
            valueSize += AmqpCodec.GetBooleanEncodeSize(this.Settled);
            valueSize += AmqpCodec.GetSerializableEncodeSize(this.State);
            valueSize += AmqpCodec.GetBooleanEncodeSize(this.Batchable);

            return valueSize;
        }
    }
}
