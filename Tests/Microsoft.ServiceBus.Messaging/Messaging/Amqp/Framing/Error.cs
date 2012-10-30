//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System.Text;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    sealed class Error : DescribedList
    {
        public static readonly string Name = "amqp:error:list";
        public static readonly ulong Code = 0x000000000000001d;
        const int Fields = 3;

        public Error() : base(Name, Code) { }

        public AmqpSymbol Condition { get; set; }

        public string Description { get; set; }

        public Fields Info { get; set; }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("error(");
            int count = 0;
            this.AddFieldToString(this.Condition.Value != null, sb, "condition", this.Condition, ref count);
            this.AddFieldToString(this.Description != null, sb, "description", this.Description, ref count);
            //this.AddFieldToString(this.Info != null, sb, "info", this.Info, ref count);
            sb.Append(')');
            return sb.ToString();
        }

        protected override void EnsureRequired()
        {
            if (this.Condition.Value == null)
            {
                throw AmqpEncoding.GetEncodingException("error.condition");
            }
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
            AmqpCodec.EncodeSymbol(this.Condition, buffer);
            AmqpCodec.EncodeString(this.Description, buffer);
            AmqpCodec.EncodeMap(this.Info, buffer);
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.Condition = AmqpCodec.DecodeSymbol(buffer);
            }

            if (count-- > 0)
            {
                this.Description = AmqpCodec.DecodeString(buffer);
            }

            if (count-- > 0)
            {
                this.Info = AmqpCodec.DecodeMap<Fields>(buffer);
            }
        }

        protected override int OnValueSize()
        {
            int valueSize = 0;

            valueSize = AmqpCodec.GetSymbolEncodeSize(this.Condition);
            valueSize += AmqpCodec.GetStringEncodeSize(this.Description);
            valueSize += AmqpCodec.GetMapEncodeSize(this.Info);

            return valueSize;
        }
    }
}
