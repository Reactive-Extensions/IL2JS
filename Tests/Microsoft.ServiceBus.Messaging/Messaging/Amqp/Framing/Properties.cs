//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System;
    using System.Text;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    sealed class Properties : DescribedList
    {
        public static readonly string Name = "amqp:properties:list";
        public static readonly ulong Code = 0x0000000000000073;
        const int Fields = 13;

        public Properties() : base(Name, Code) { }

        public MessageId MessageId { get; set; }

        public ArraySegment<byte> UserId { get; set; }

        public Address To { get; set; }

        public string Subject { get; set; }

        public Address ReplyTo { get; set; }

        public MessageId CorrelationId { get; set; }

        public AmqpSymbol ContentType { get; set; }

        public AmqpSymbol ContentEncoding { get; set; }

        public DateTime? AbsoluteExpiryTime { get; set; }

        public DateTime? CreationTime { get; set; }

        public string GroupId { get; set; }

        public uint? GroupSequence { get; set; }

        public string ReplyToGroupId { get; set; }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("properties(");
            int count = 0;
            this.AddFieldToString(this.MessageId != null, sb, "message-id", this.MessageId, ref count);
            this.AddFieldToString(this.UserId.Array != null, sb, "user-id", this.UserId, ref count);
            this.AddFieldToString(this.To != null, sb, "to", this.To, ref count);
            this.AddFieldToString(this.Subject != null, sb, "subject", this.Subject, ref count);
            this.AddFieldToString(this.ReplyTo != null, sb, "reply-to", this.ReplyTo, ref count);
            this.AddFieldToString(this.CorrelationId != null, sb, "correlation-id", this.CorrelationId, ref count);
            this.AddFieldToString(this.ContentType.Value != null, sb, "content-type", this.ContentType, ref count);
            this.AddFieldToString(this.ContentEncoding.Value != null, sb, "content-encoding", this.ContentEncoding, ref count);
            this.AddFieldToString(this.AbsoluteExpiryTime != null, sb, "absolute-expiry-time", this.AbsoluteExpiryTime, ref count);
            this.AddFieldToString(this.CreationTime != null, sb, "creation-time", this.CreationTime, ref count);
            this.AddFieldToString(this.GroupId != null, sb, "group-id", this.GroupId, ref count);
            this.AddFieldToString(this.GroupSequence != null, sb, "group-sequence", this.GroupSequence, ref count);
            this.AddFieldToString(this.ReplyToGroupId != null, sb, "reply-to-group-id", this.ReplyToGroupId, ref count);
            sb.Append(')');
            return sb.ToString();
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
            MessageId.Encode(buffer, this.MessageId);
            AmqpCodec.EncodeBinary(this.UserId, buffer);
            Address.Encode(buffer, this.To);
            AmqpCodec.EncodeString(this.Subject, buffer);
            Address.Encode(buffer, this.ReplyTo);
            MessageId.Encode(buffer, this.CorrelationId);
            AmqpCodec.EncodeSymbol(this.ContentType, buffer);
            AmqpCodec.EncodeSymbol(this.ContentEncoding, buffer);
            AmqpCodec.EncodeTimeStamp(this.AbsoluteExpiryTime, buffer);
            AmqpCodec.EncodeTimeStamp(this.CreationTime, buffer);
            AmqpCodec.EncodeString(this.GroupId, buffer);
            AmqpCodec.EncodeUInt(this.GroupSequence, buffer);
            AmqpCodec.EncodeString(this.ReplyToGroupId, buffer);
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.MessageId = MessageId.Decode(buffer);
            }

            if (count-- > 0)
            {
                this.UserId = AmqpCodec.DecodeBinary(buffer);
            }

            if (count-- > 0)
            {
                this.To = Address.Decode(buffer);
            }

            if (count-- > 0)
            {
                this.Subject = AmqpCodec.DecodeString(buffer);
            }

            if (count-- > 0)
            {
                this.ReplyTo = Address.Decode(buffer);
            }

            if (count-- > 0)
            {
                this.CorrelationId = MessageId.Decode(buffer);
            }

            if (count-- > 0)
            {
                this.ContentType = AmqpCodec.DecodeSymbol(buffer);
            }

            if (count-- > 0)
            {
                this.ContentEncoding = AmqpCodec.DecodeSymbol(buffer);
            }

            if (count-- > 0)
            {
                this.AbsoluteExpiryTime = AmqpCodec.DecodeTimeStamp(buffer);
            }

            if (count-- > 0)
            {
                this.CreationTime = AmqpCodec.DecodeTimeStamp(buffer);
            }

            if (count-- > 0)
            {
                this.GroupId = AmqpCodec.DecodeString(buffer);
            }

            if (count-- > 0)
            {
                this.GroupSequence = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.ReplyToGroupId = AmqpCodec.DecodeString(buffer);
            }
        }

        protected override int OnValueSize()
        {
            int valueSize = 0;

            valueSize = MessageId.GetEncodeSize(this.MessageId);
            valueSize += AmqpCodec.GetBinaryEncodeSize(this.UserId);
            valueSize += Address.GetEncodeSize(this.To);
            valueSize += AmqpCodec.GetStringEncodeSize(this.Subject);
            valueSize += Address.GetEncodeSize(this.ReplyTo);
            valueSize += MessageId.GetEncodeSize(this.CorrelationId);
            valueSize += AmqpCodec.GetSymbolEncodeSize(this.ContentType);
            valueSize += AmqpCodec.GetSymbolEncodeSize(this.ContentEncoding);
            valueSize += AmqpCodec.GetTimeStampEncodeSize(this.AbsoluteExpiryTime);
            valueSize += AmqpCodec.GetTimeStampEncodeSize(this.CreationTime);
            valueSize += AmqpCodec.GetStringEncodeSize(this.GroupId);
            valueSize += AmqpCodec.GetUIntEncodeSize(this.GroupSequence);
            valueSize += AmqpCodec.GetStringEncodeSize(this.ReplyToGroupId);

            return valueSize;
        }
    }
}
