//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System;
    using System.Text;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    sealed class Transfer : LinkPerformative
    {
        public static readonly string Name = "amqp:transfer:list";
        public static readonly ulong Code = 0x0000000000000014;
        const int Fields = 11;

        public Transfer() : base(Name, Code) { }

        // public uint? Handle { get; set; }

        public uint? DeliveryId { get; set; }

        public ArraySegment<byte> DeliveryTag { get; set; }

        public uint? MessageFormat { get; set; }

        public bool? Settled { get; set; }

        public bool? More { get; set; }

        public byte? RcvSettleMode { get; set; }

        public DeliveryState State { get; set; }

        public bool? Resume { get; set; }

        public bool? Aborted { get; set; }

        public bool? Batchable { get; set; }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("transfer(");
            int count = 0;
            this.AddFieldToString(this.Handle != null, sb, "handle", this.Handle, ref count);
            this.AddFieldToString(this.DeliveryId != null, sb, "delivery-id", this.DeliveryId, ref count);
            this.AddFieldToString(this.DeliveryTag.Array != null, sb, "delivery-tag", this.DeliveryTag, ref count);
            this.AddFieldToString(this.MessageFormat != null, sb, "message-format", this.MessageFormat, ref count);
            this.AddFieldToString(this.Settled != null, sb, "settled", this.Settled, ref count);
            this.AddFieldToString(this.More != null, sb, "more", this.More, ref count);
            this.AddFieldToString(this.RcvSettleMode != null, sb, "rcv-settle-mode", this.RcvSettleMode, ref count);
            this.AddFieldToString(this.State != null, sb, "state", this.State, ref count);
            this.AddFieldToString(this.Resume != null, sb, "resume", this.Resume, ref count);
            this.AddFieldToString(this.Aborted != null, sb, "aborted", this.Aborted, ref count);
            this.AddFieldToString(this.Batchable != null, sb, "batchable", this.Batchable, ref count);
            sb.Append(')');
            return sb.ToString();
        }

        protected override void EnsureRequired()
        {
            if (!this.Handle.HasValue)
            {
                throw AmqpEncoding.GetEncodingException("handle");
            }
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
            AmqpCodec.EncodeUInt(this.Handle, buffer);
            AmqpCodec.EncodeUInt(this.DeliveryId, buffer);
            AmqpCodec.EncodeBinary(this.DeliveryTag, buffer);
            AmqpCodec.EncodeUInt(this.MessageFormat, buffer);
            AmqpCodec.EncodeBoolean(this.Settled, buffer);
            AmqpCodec.EncodeBoolean(this.More, buffer);
            AmqpCodec.EncodeUByte(this.RcvSettleMode, buffer);
            AmqpCodec.EncodeSerializable(this.State, buffer);
            AmqpCodec.EncodeBoolean(this.Resume, buffer);
            AmqpCodec.EncodeBoolean(this.Aborted, buffer);
            AmqpCodec.EncodeBoolean(this.Batchable, buffer);
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.Handle = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.DeliveryId = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.DeliveryTag = AmqpCodec.DecodeBinary(buffer);
            }

            if (count-- > 0)
            {
                this.MessageFormat = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.Settled = AmqpCodec.DecodeBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.More = AmqpCodec.DecodeBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.RcvSettleMode = AmqpCodec.DecodeUByte(buffer);
            }

            if (count-- > 0)
            {
                this.State = (DeliveryState)AmqpCodec.DecodeAmqpDescribed(buffer);
            }

            if (count-- > 0)
            {
                this.Resume = AmqpCodec.DecodeBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.Aborted = AmqpCodec.DecodeBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.Batchable = AmqpCodec.DecodeBoolean(buffer);
            }
        }

        protected override int OnValueSize()
        {
            int valueSize = 0;

            valueSize += AmqpCodec.GetUIntEncodeSize(this.Handle);
            valueSize += AmqpCodec.GetUIntEncodeSize(this.DeliveryId);
            valueSize += AmqpCodec.GetBinaryEncodeSize(this.DeliveryTag);
            valueSize += AmqpCodec.GetUIntEncodeSize(this.MessageFormat);
            valueSize += AmqpCodec.GetBooleanEncodeSize(this.Settled);
            valueSize += AmqpCodec.GetBooleanEncodeSize(this.More);
            valueSize += AmqpCodec.GetUByteEncodeSize(this.RcvSettleMode);
            valueSize += AmqpCodec.GetSerializableEncodeSize(this.State);
            valueSize += AmqpCodec.GetBooleanEncodeSize(this.Resume);
            valueSize += AmqpCodec.GetBooleanEncodeSize(this.Aborted);
            valueSize += AmqpCodec.GetBooleanEncodeSize(this.Batchable);

            return valueSize;
        }
    }
}
