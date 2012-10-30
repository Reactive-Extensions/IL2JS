//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System;
    using System.Text;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    class Begin : Performative
    {
        public static readonly string Name = "amqp:begin:list";
        public static readonly ulong Code = 0x0000000000000011;
        const int Fields = 8;

        public Begin() : base(Name, Code) { }

        public ushort? RemoteChannel { get; set; }

        public uint? NextOutgoingId { get; set; }

        public uint? IncomingWindow { get; set; }

        public uint? OutgoingWindow { get; set; }

        public uint? HandleMax { get; set; }

        public Multiple<AmqpSymbol> OfferedCapabilities { get; set; }

        public Multiple<AmqpSymbol> DesiredCapabilities { get; set; }

        public Fields Properties { get; set; }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("begin(");
            int count = 0;
            this.AddFieldToString(this.RemoteChannel != null, sb, "remote-channel", this.RemoteChannel, ref count);
            this.AddFieldToString(this.NextOutgoingId != null, sb, "next-outgoing-id", this.NextOutgoingId, ref count);
            this.AddFieldToString(this.IncomingWindow != null, sb, "incoming-window", this.IncomingWindow, ref count);
            this.AddFieldToString(this.OutgoingWindow != null, sb, "outgoing-window", this.OutgoingWindow, ref count);
            this.AddFieldToString(this.HandleMax != null, sb, "handle-max", this.HandleMax, ref count);
            this.AddFieldToString(this.OfferedCapabilities != null, sb, "offered-capabilities", this.OfferedCapabilities, ref count);
            this.AddFieldToString(this.DesiredCapabilities != null, sb, "desired-capabilities", this.DesiredCapabilities, ref count);
            this.AddFieldToString(this.Properties != null, sb, "properties", this.Properties, ref count);
            sb.Append(')');
            return sb.ToString();
        }

        protected override void EnsureRequired()
        {
            if (!this.NextOutgoingId.HasValue)
            {
                throw new AmqpException(AmqpError.InvalidField, "begin.next-outgoing-id");
            }

            if (!this.IncomingWindow.HasValue)
            {
                throw new AmqpException(AmqpError.InvalidField, "begin.incoming-window");
            }

            if (!this.OutgoingWindow.HasValue)
            {
                throw new AmqpException(AmqpError.InvalidField, "begin.outgoing-window");
            }
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
            AmqpCodec.EncodeUShort(this.RemoteChannel, buffer);
            AmqpCodec.EncodeUInt(this.NextOutgoingId, buffer);
            AmqpCodec.EncodeUInt(this.IncomingWindow, buffer);
            AmqpCodec.EncodeUInt(this.OutgoingWindow, buffer);
            AmqpCodec.EncodeUInt(this.HandleMax, buffer);
            AmqpCodec.EncodeMultiple(this.OfferedCapabilities, buffer);
            AmqpCodec.EncodeMultiple(this.DesiredCapabilities, buffer);
            AmqpCodec.EncodeMap(this.Properties, buffer);
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.RemoteChannel = AmqpCodec.DecodeUShort(buffer);
            }

            if (count-- > 0)
            {
                this.NextOutgoingId = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.IncomingWindow = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.OutgoingWindow = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.HandleMax = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.OfferedCapabilities = AmqpCodec.DecodeMultiple<AmqpSymbol>(buffer);
            }

            if (count-- > 0)
            {
                this.DesiredCapabilities = AmqpCodec.DecodeMultiple<AmqpSymbol>(buffer);
            }

            if (count-- > 0)
            {
                this.Properties = AmqpCodec.DecodeMap<Fields>(buffer);
            }
        }

        protected override int OnValueSize()
        {
            int valueSize = 0;

            valueSize += AmqpCodec.GetUShortEncodeSize(this.RemoteChannel);
            valueSize += AmqpCodec.GetUIntEncodeSize(this.NextOutgoingId);
            valueSize += AmqpCodec.GetUIntEncodeSize(this.IncomingWindow);
            valueSize += AmqpCodec.GetUIntEncodeSize(this.OutgoingWindow);
            valueSize += AmqpCodec.GetUIntEncodeSize(this.HandleMax);
            valueSize += AmqpCodec.GetMultipleEncodeSize(this.OfferedCapabilities);
            valueSize += AmqpCodec.GetMultipleEncodeSize(this.DesiredCapabilities);
            valueSize += AmqpCodec.GetMapEncodeSize(this.Properties);

            return valueSize;
        }
    }
}
