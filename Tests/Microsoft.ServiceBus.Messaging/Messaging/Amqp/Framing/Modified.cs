//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System.Text;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    sealed class Modified : Outcome
    {
        public static readonly string Name = "amqp:modified:list";
        public static readonly ulong Code = 0x0000000000000027;
        const int Fields = 3;

        public Modified() : base(Name, Code) { }

        public bool? DeliveryFailed { get; set; }

        public bool? UndeliverableHere { get; set; }

        public Fields MessageAnnotations { get; set; }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("modified(");
            int count = 0;
            this.AddFieldToString(this.DeliveryFailed != null, sb, "delivery-failed", this.DeliveryFailed, ref count);
            this.AddFieldToString(this.UndeliverableHere != null, sb, "undeliverable-here", this.UndeliverableHere, ref count);
            this.AddFieldToString(this.MessageAnnotations != null, sb, "message-annotations", this.MessageAnnotations, ref count);
            sb.Append(')');
            return sb.ToString();
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
            AmqpCodec.EncodeBoolean(this.DeliveryFailed, buffer);
            AmqpCodec.EncodeBoolean(this.UndeliverableHere, buffer);
            AmqpCodec.EncodeMap(this.MessageAnnotations, buffer);
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.DeliveryFailed = AmqpCodec.DecodeBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.UndeliverableHere = AmqpCodec.DecodeBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.MessageAnnotations = AmqpCodec.DecodeMap<Fields>(buffer);
            }
        }

        protected override int OnValueSize()
        {
            int valueSize = 0;

            valueSize += AmqpCodec.GetBooleanEncodeSize(this.DeliveryFailed);
            valueSize += AmqpCodec.GetBooleanEncodeSize(this.UndeliverableHere);
            valueSize += AmqpCodec.GetMapEncodeSize(this.MessageAnnotations);

            return valueSize;
        }
    }
}
