//----------------------------------------------------------------
// Copyright (c) Microsoft Corp.oration.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System.Text;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    sealed class Source : DescribedList
    {
        public static readonly string Name = "amqp:source:list";
        public static readonly ulong Code = 0x0000000000000028;
        const int Fields = 11;

        public Source() : base(Name, Code) { }

        public Address Address { get; set; }

        public uint? Durable { get; set; }

        public AmqpSymbol ExpiryPolicy { get; set; }

        public uint? Timeout { get; set; }

        public bool? Dynamic { get; set; }

        public Fields DynamicNodeProperties { get; set; }

        public AmqpSymbol DistributionMode { get; set; }

        public FilterSet FilterSet { get; set; }

        public Outcome DefaultOutcome { get; set; }

        public Multiple<AmqpSymbol> Outcomes { get; set; }

        public Multiple<AmqpSymbol> Capabilities { get; set; }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("source(");
            int count = 0;
            this.AddFieldToString(this.Address != null, sb, "address", this.Address, ref count);
            this.AddFieldToString(this.Durable != null, sb, "durable", this.Durable, ref count);
            this.AddFieldToString(this.ExpiryPolicy.Value != null, sb, "expiry-policy", this.ExpiryPolicy, ref count);
            this.AddFieldToString(this.Timeout != null, sb, "timeout", this.Timeout, ref count);
            this.AddFieldToString(this.Dynamic != null, sb, "dynamic", this.Dynamic, ref count);
            this.AddFieldToString(this.DynamicNodeProperties != null, sb, "dynamic-node-properties", this.DynamicNodeProperties, ref count);
            this.AddFieldToString(this.DistributionMode.Value != null, sb, "distribution-mode", this.DistributionMode, ref count);
            this.AddFieldToString(this.FilterSet != null, sb, "filter", this.FilterSet, ref count);
            this.AddFieldToString(this.DefaultOutcome != null, sb, "default-outcome", this.DefaultOutcome, ref count);
            this.AddFieldToString(this.Outcomes != null, sb, "outcomes", this.Outcomes, ref count);
            this.AddFieldToString(this.Capabilities != null, sb, "capabilities", this.Capabilities, ref count);
            sb.Append(')');
            return sb.ToString();
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
            Address.Encode(buffer, this.Address);
            AmqpCodec.EncodeUInt(this.Durable, buffer);
            AmqpCodec.EncodeSymbol(this.ExpiryPolicy, buffer);
            AmqpCodec.EncodeUInt(this.Timeout, buffer);
            AmqpCodec.EncodeBoolean(this.Dynamic, buffer);
            AmqpCodec.EncodeMap(this.DynamicNodeProperties, buffer);
            AmqpCodec.EncodeSymbol(this.DistributionMode, buffer);
            AmqpCodec.EncodeMap(this.FilterSet, buffer);
            AmqpCodec.EncodeSerializable(this.DefaultOutcome, buffer);
            AmqpCodec.EncodeMultiple(this.Outcomes, buffer);
            AmqpCodec.EncodeMultiple(this.Capabilities, buffer);
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.Address = Address.Decode(buffer);
            }

            if (count-- > 0)
            {
                this.Durable = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.ExpiryPolicy = AmqpCodec.DecodeSymbol(buffer);
            }

            if (count-- > 0)
            {
                this.Timeout = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.Dynamic = AmqpCodec.DecodeBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.DynamicNodeProperties = AmqpCodec.DecodeMap<Fields>(buffer);
            }

            if (count-- > 0)
            {
                this.DistributionMode = AmqpCodec.DecodeSymbol(buffer);
            }

            if (count-- > 0)
            {
                this.FilterSet = AmqpCodec.DecodeMap<FilterSet>(buffer);
            }

            if (count-- > 0)
            {
                this.DefaultOutcome = (Outcome)AmqpCodec.DecodeAmqpDescribed(buffer);
            }

            if (count-- > 0)
            {
                this.Outcomes = AmqpCodec.DecodeMultiple<AmqpSymbol>(buffer);
            }

            if (count-- > 0)
            {
                this.Capabilities = AmqpCodec.DecodeMultiple<AmqpSymbol>(buffer);
            }
        }

        protected override int OnValueSize()
        {
            int valueSize = 0;

            valueSize += Address.GetEncodeSize(this.Address);
            valueSize += AmqpCodec.GetUIntEncodeSize(this.Durable);
            valueSize += AmqpCodec.GetSymbolEncodeSize(this.ExpiryPolicy);
            valueSize += AmqpCodec.GetUIntEncodeSize(this.Timeout);
            valueSize += AmqpCodec.GetBooleanEncodeSize(this.Dynamic);
            valueSize += AmqpCodec.GetMapEncodeSize(this.DynamicNodeProperties);
            valueSize += AmqpCodec.GetSymbolEncodeSize(this.DistributionMode);
            valueSize += AmqpCodec.GetMapEncodeSize(this.FilterSet);
            valueSize += AmqpCodec.GetSerializableEncodeSize(this.DefaultOutcome);
            valueSize += AmqpCodec.GetMultipleEncodeSize(this.Outcomes);
            valueSize += AmqpCodec.GetMultipleEncodeSize(this.Capabilities);

            return valueSize;
        }
    }
}
