//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
    using System;
    using System.Text;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    sealed class SaslChallenge : Performative
    {
        public static readonly string Name = "amqp:sasl-challenge:list";
        public static readonly ulong Code = 0x0000000000000042;
        const int Fields = 1;

        public SaslChallenge() : base(Name, Code) { }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        public ArraySegment<byte> Challenge { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("sasl-challenge(");
            int count = 0;
            this.AddFieldToString(this.Challenge.Array != null, sb, "challenge", this.Challenge, ref count);
            sb.Append(')');
            return sb.ToString();
        }

        protected override void EnsureRequired()
        {
            if (this.Challenge.Array == null)
            {
                throw new AmqpException(AmqpError.InvalidField, "sasl-challenge:challenge");
            }
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
            AmqpCodec.EncodeBinary(this.Challenge, buffer);
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.Challenge = AmqpCodec.DecodeBinary(buffer);
            }
        }

        protected override int OnValueSize()
        {
            int valueSize = 0;
            valueSize += AmqpCodec.GetBinaryEncodeSize(this.Challenge);
            return valueSize;
        }
    }
}
