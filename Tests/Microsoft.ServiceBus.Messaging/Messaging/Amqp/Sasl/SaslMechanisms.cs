//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Sasl
{
    using System.Text;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    sealed class SaslMechanisms : Performative
    {
        public static readonly string Name = "amqp:sasl-mechanisms:list";
        public static readonly ulong Code = 0x0000000000000040;
        const int Fields = 1;

        public SaslMechanisms() : base(Name, Code) { }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        public Multiple<AmqpSymbol> SaslServerMechanisms { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("sasl-mechanisms(");
            int count = 0;
            this.AddFieldToString(this.SaslServerMechanisms != null, sb, "sasl-server-mechanisms", this.SaslServerMechanisms, ref count);
            sb.Append(')');
            return sb.ToString();
        }

        protected override void EnsureRequired()
        {
            if (this.SaslServerMechanisms == null)
            {
                throw new AmqpException(AmqpError.InvalidField, "sasl-mechanisms:sasl-server-mechanisms");
            }
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
            AmqpCodec.EncodeMultiple(this.SaslServerMechanisms, buffer);
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.SaslServerMechanisms = AmqpCodec.DecodeMultiple<AmqpSymbol>(buffer);
            }
        }

        protected override int OnValueSize()
        {
            int valueSize = 0;
            valueSize += AmqpCodec.GetMultipleEncodeSize(this.SaslServerMechanisms);
            return valueSize;
        }
    }
}
