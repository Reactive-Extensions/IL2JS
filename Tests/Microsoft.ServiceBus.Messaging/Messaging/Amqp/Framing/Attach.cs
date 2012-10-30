//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System;
    using System.Text;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    class Attach : LinkPerformative
    {
        public static readonly string Name = "amqp:attach:list";
        public static readonly ulong Code = 0x0000000000000012;
        const int Fields = 14;

        public Attach() : base(Name, Code) { }

        public string LinkName { get; set; }

        // public uint? Handle { get; set; }

        public bool? Role { get; set; }

        public byte? SndSettleMode { get; set; }

        public byte? RcvSettleMode { get; set; }

        public object Source { get; set; }

        public object Target { get; set; }

        public AmqpMap Unsettled { get; set; }

        public bool? IncompleteUnsettled { get; set; }

        public uint? InitialDeliveryCount { get; set; }

        public ulong? MaxMessageSize { get; set; }

        public Multiple<AmqpSymbol> OfferedCapabilities { get; set; }

        public Multiple<AmqpSymbol> DesiredCapabilities { get; set; }

        public Fields Properties { get; set; }

        protected override int FieldCount
        {
            get { return Fields; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("attach(");
            int count = 0;
            this.AddFieldToString(this.LinkName != null, sb, "name", this.LinkName, ref count);
            this.AddFieldToString(this.Handle != null, sb, "handle", this.Handle, ref count);
            this.AddFieldToString(this.Role != null, sb, "role", this.Role, ref count);
            this.AddFieldToString(this.SndSettleMode != null, sb, "snd-settle-mode", this.SndSettleMode, ref count);
            this.AddFieldToString(this.RcvSettleMode != null, sb, "rcv-settle-mode", this.RcvSettleMode, ref count);
            this.AddFieldToString(this.Source != null, sb, "source", this.Source, ref count);
            this.AddFieldToString(this.Target != null, sb, "target", this.Target, ref count);
            this.AddFieldToString(this.IncompleteUnsettled != null, sb, "incomplete-unsettled", this.IncompleteUnsettled, ref count);
            this.AddFieldToString(this.InitialDeliveryCount != null, sb, "initial-delivery-count", this.InitialDeliveryCount, ref count);
            this.AddFieldToString(this.OfferedCapabilities != null, sb, "offered-capabilities", this.OfferedCapabilities, ref count);
            this.AddFieldToString(this.DesiredCapabilities != null, sb, "desired-capabilities", this.DesiredCapabilities, ref count);
            this.AddFieldToString(this.Properties != null, sb, "properties", this.Properties, ref count);
            sb.Append(')');
            return sb.ToString();
        }

        protected override void EnsureRequired()
        {
            if (this.LinkName == null)
            {
                throw AmqpEncoding.GetEncodingException("attach.name");
            }

            if (!this.Handle.HasValue)
            {
                throw AmqpEncoding.GetEncodingException("attach.handle");
            }

            if (!this.Role.HasValue)
            {
                throw AmqpEncoding.GetEncodingException("attach.role");
            }

            //if (!this.Role.Value && this.InitialDeliveryCount == null)
            //{
            //    throw AmqpEncoding.GetEncodingException("attach.initial-delivery-count");
            //}
        }

        protected override void OnEncode(ByteBuffer buffer)
        {
            AmqpCodec.EncodeString(this.LinkName, buffer);
            AmqpCodec.EncodeUInt(this.Handle, buffer);
            AmqpCodec.EncodeBoolean(this.Role, buffer);
            AmqpCodec.EncodeUByte(this.SndSettleMode, buffer);
            AmqpCodec.EncodeUByte(this.RcvSettleMode, buffer);
            AmqpCodec.EncodeObject(this.Source, buffer);
            AmqpCodec.EncodeObject(this.Target, buffer);
            AmqpCodec.EncodeMap(this.Unsettled, buffer);
            AmqpCodec.EncodeBoolean(this.IncompleteUnsettled, buffer);
            AmqpCodec.EncodeUInt(this.InitialDeliveryCount, buffer);
            AmqpCodec.EncodeULong(this.MaxMessageSize, buffer);
            AmqpCodec.EncodeMultiple(this.OfferedCapabilities, buffer);
            AmqpCodec.EncodeMultiple(this.DesiredCapabilities, buffer);
            AmqpCodec.EncodeMap(this.Properties, buffer);
        }

        protected override void OnDecode(ByteBuffer buffer, int count)
        {
            if (count-- > 0)
            {
                this.LinkName = AmqpCodec.DecodeString(buffer);
            }

            if (count-- > 0)
            {
                this.Handle = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.Role = AmqpCodec.DecodeBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.SndSettleMode = AmqpCodec.DecodeUByte(buffer);
            }

            if (count-- > 0)
            {
                this.RcvSettleMode = AmqpCodec.DecodeUByte(buffer);
            }

            if (count-- > 0)
            {
                this.Source = AmqpCodec.DecodeObject(buffer);
            }

            if (count-- > 0)
            {
                this.Target = AmqpCodec.DecodeObject(buffer);
            }

            if (count-- > 0)
            {
                this.Unsettled = AmqpCodec.DecodeMap(buffer);
            }

            if (count-- > 0)
            {
                this.IncompleteUnsettled = AmqpCodec.DecodeBoolean(buffer);
            }

            if (count-- > 0)
            {
                this.InitialDeliveryCount = AmqpCodec.DecodeUInt(buffer);
            }

            if (count-- > 0)
            {
                this.MaxMessageSize = AmqpCodec.DecodeULong(buffer);
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

            valueSize += AmqpCodec.GetStringEncodeSize(this.LinkName);
            valueSize += AmqpCodec.GetUIntEncodeSize(this.Handle);
            valueSize += AmqpCodec.GetBooleanEncodeSize(this.Role);
            valueSize += AmqpCodec.GetUByteEncodeSize(this.SndSettleMode);
            valueSize += AmqpCodec.GetUByteEncodeSize(this.RcvSettleMode);
            valueSize += AmqpCodec.GetObjectEncodeSize(this.Source);
            valueSize += AmqpCodec.GetObjectEncodeSize(this.Target);
            valueSize += AmqpCodec.GetMapEncodeSize(this.Unsettled);
            valueSize += AmqpCodec.GetBooleanEncodeSize(this.IncompleteUnsettled);
            valueSize += AmqpCodec.GetUIntEncodeSize(this.InitialDeliveryCount);
            valueSize += AmqpCodec.GetULongEncodeSize(this.MaxMessageSize);
            valueSize += AmqpCodec.GetMultipleEncodeSize(this.OfferedCapabilities);
            valueSize += AmqpCodec.GetMultipleEncodeSize(this.DesiredCapabilities);
            valueSize += AmqpCodec.GetMapEncodeSize(this.Properties);

            return valueSize;
        }
    }
}
