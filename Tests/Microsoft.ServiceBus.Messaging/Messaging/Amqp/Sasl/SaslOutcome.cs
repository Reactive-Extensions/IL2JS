//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
    using System;
    using System.Text;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    sealed class SaslOutcome : Performative
    {
        public static readonly string Name = "amqp:sasl-outcome:list";
        public static readonly ulong Code = 0x0000000000000044;
        const int Fields = 2;

        public SaslOutcome() : base(Name, Code) { }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        public SaslCode? OutcomeCode { get; set; }

        public ArraySegment<byte> AdditionalData { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("sasl-outcome(");
            int count = 0;
            this.AddFieldToString(this.OutcomeCode != null, sb, "code", this.OutcomeCode, ref count);
            this.AddFieldToString(this.AdditionalData.Array != null, sb, "additional-data", this.AdditionalData, ref count);
            sb.Append(')');
            return sb.ToString();
        }

        protected override void EnsureRequired()
        {
            if (this.OutcomeCode == null)
            {
                throw new AmqpException(AmqpError.InvalidField, "sasl-outcome:code");
            }
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
            AmqpCodec.EncodeUByte((byte?)this.OutcomeCode, buffer);
            AmqpCodec.EncodeBinary(this.AdditionalData, buffer);
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.OutcomeCode = (SaslCode?)AmqpCodec.DecodeUByte(buffer);
            }

            if (count-- > 0)
            {
                this.AdditionalData = AmqpCodec.DecodeBinary(buffer);
            }
        }

        protected override int OnValueSize()
        {
            int valueSize = 0;
            valueSize += AmqpCodec.GetUByteEncodeSize((byte?)this.OutcomeCode);
            valueSize += AmqpCodec.GetBinaryEncodeSize(this.AdditionalData);
            return valueSize;
        }
    }
}
