//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System;
    using System.Text;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    sealed class Flow : LinkPerformative
    {
        public static readonly string Name = "amqp:flow:list";
        public static readonly ulong Code = 0x0000000000000013;
        const int Fields = 11;

        public Flow() : base(Name, Code) { }

        public uint? NextIncomingId { get; set; }

        public uint? IncomingWindow { get; set; }

        public uint? NextOutgoingId { get; set; }

        public uint? OutgoingWindow { get; set; }

        // public uint? Handle { get; set; }

        public uint? DeliveryCount { get; set; }

        public uint? LinkCredit { get; set; }

        public uint? Available { get; set; }

        public bool? Drain { get; set; }

        public bool? Echo { get; set; }

        public Fields Properties { get; set; }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("flow(");
            int count = 0;
            this.AddFieldToString(this.NextIncomingId != null, sb, "next-in-id", this.NextIncomingId, ref count);
            this.AddFieldToString(this.IncomingWindow != null, sb, "in-window", this.IncomingWindow, ref count);
            this.AddFieldToString(this.NextOutgoingId != null, sb, "next-out-id", this.NextOutgoingId, ref count);
            this.AddFieldToString(this.OutgoingWindow != null, sb, "out-window", this.OutgoingWindow, ref count);
            this.AddFieldToString(this.Handle != null, sb, "handle", this.Handle, ref count);
            this.AddFieldToString(this.LinkCredit != null, sb, "link-credit", this.LinkCredit, ref count);
            this.AddFieldToString(this.Available != null, sb, "available", this.Available, ref count);
            this.AddFieldToString(this.Drain != null, sb, "drain", this.Drain, ref count);
            this.AddFieldToString(this.Echo != null, sb, "echo", this.Echo, ref count);
            this.AddFieldToString(this.Properties != null, sb, "properties", this.Properties, ref count);
            sb.Append(')');
            return sb.ToString();
        }

        protected override void EnsureRequired()
        {
            if (!this.IncomingWindow.HasValue)
            {
                throw new AmqpException(AmqpError.InvalidField, "flow.incoming-window");
            }

            if (!this.NextOutgoingId.HasValue)
            {
                throw new AmqpException(AmqpError.InvalidField, "flow.next-outgoing-id");
            }

            if (!this.OutgoingWindow.HasValue)
            {
                throw new AmqpException(AmqpError.InvalidField, "flow.outgoing-window");
            }
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
            AmqpCodec.EncodeUInt(this.NextIncomingId, buffer);
            AmqpCodec.EncodeUInt(this.IncomingWindow, buffer);
            AmqpCodec.EncodeUInt(this.NextOutgoingId, buffer);
            AmqpCodec.EncodeUInt(this.OutgoingWindow, buffer);
            AmqpCodec.EncodeUInt(this.Handle, buffer);
            AmqpCodec.EncodeUInt(this.DeliveryCount, buffer);
            AmqpCodec.EncodeUInt(this.LinkCredit, buffer);
            AmqpCodec.EncodeUInt(this.Available, buffer);
            AmqpCodec.EncodeBoolean(this.Drain, buffer);
            AmqpCodec.EncodeBoolean(this.Echo, buffer);
            AmqpCodec.EncodeMap(this.Properties, buffer);
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.NextIncomingId = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.IncomingWindow = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.NextOutgoingId = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.OutgoingWindow = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.Handle = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.DeliveryCount = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.LinkCredit = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.Available = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.Drain = AmqpCodec.DecodeBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.Echo = AmqpCodec.DecodeBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.Properties = AmqpCodec.DecodeMap<Fields>(buffer);
            }
        }

        protected override int OnValueSize()
        {
            int valueSize = 0;

            valueSize += AmqpCodec.GetUIntEncodeSize(this.NextIncomingId);
            valueSize += AmqpCodec.GetUIntEncodeSize(this.IncomingWindow);
            valueSize += AmqpCodec.GetUIntEncodeSize(this.NextOutgoingId);
            valueSize += AmqpCodec.GetUIntEncodeSize(this.OutgoingWindow);
            valueSize += AmqpCodec.GetUIntEncodeSize(this.Handle);
            valueSize += AmqpCodec.GetUIntEncodeSize(this.DeliveryCount);
            valueSize += AmqpCodec.GetUIntEncodeSize(this.LinkCredit);
            valueSize += AmqpCodec.GetUIntEncodeSize(this.Available);
            valueSize += AmqpCodec.GetBooleanEncodeSize(this.Drain);
            valueSize += AmqpCodec.GetBooleanEncodeSize(this.Echo);
            valueSize += AmqpCodec.GetMapEncodeSize(this.Properties);

            return valueSize;
        }
    }
}
