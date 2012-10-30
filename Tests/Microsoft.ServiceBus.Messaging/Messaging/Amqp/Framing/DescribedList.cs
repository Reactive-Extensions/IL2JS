//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    /// <summary>
    /// Encoding of DescribedType(symbol/ulong, list) is not efficient if the
    /// descriptor and the list fields are pre-defined, e.g. the performatives
    /// and other types used by the protocol.
    /// </summary>
    abstract class DescribedList : AmqpDescribed
    {
        public DescribedList(AmqpSymbol name, ulong code)
            : base(name, code)
        {
        }

        protected abstract int FieldCount
        {
            get;
        }

        public override int GetValueEncodeSize()
        {
            if (this.FieldCount == 0)
            {
                return FixedWidth.FormatCode;
            }

            int valueSize = this.OnValueSize();
            int width = AmqpEncoding.GetEncodeWidthByCountAndSize(this.FieldCount, valueSize);
            return FixedWidth.FormatCode + width + width + valueSize;
        }

        public override void EncodeValue(ByteBuffer buffer)
        {
            if (this.FieldCount == 0)
            {
                AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.List0);
            }
            else
            {
                int valueSize = this.OnValueSize();
                int encodeWidth = AmqpEncoding.GetEncodeWidthByCountAndSize(this.FieldCount, valueSize);
                int sizeOffset = 0;
                if (encodeWidth == FixedWidth.UByte)
                {
                    AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.List8);
                    sizeOffset = buffer.Length;
                    buffer.Append(FixedWidth.UByte);
                    AmqpBitConverter.WriteUByte(buffer, (byte)this.FieldCount);
                }
                else
                {
                    AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.List32);
                    sizeOffset = buffer.Length;
                    buffer.Append(FixedWidth.UInt);
                    AmqpBitConverter.WriteUInt(buffer, (uint)this.FieldCount);
                }

                this.OnEncode(buffer);

                // the actual encoded value size may be different from the calculated
                // valueSize. However, it can only become smaller. This allows for
                // reserving space in the buffer using the longest encoding form of a 
                // value. For example, if the delivery id of a transfer is unknown, we
                // can use uint.Max for calculating encode size, but the actual encoding
                // could be small uint.
                int size = buffer.Length - sizeOffset - encodeWidth;
                if (encodeWidth == FixedWidth.UByte)
                {
                    AmqpBitConverter.WriteUByte(buffer.Buffer, sizeOffset, (byte)size);
                }
                else
                {
                    AmqpBitConverter.WriteUInt(buffer.Buffer, sizeOffset, (uint)size);
                }
            }
        }

        public override void DecodeValue(ByteBuffer buffer)
        {
            FormatCode formatCode = AmqpEncoding.ReadFormatCode(buffer);
            if (formatCode == FormatCode.List0)
            {
                return;
            }

            int size = 0;
            int count = 0;
            AmqpEncoding.ReadSizeAndCount(buffer, formatCode, FormatCode.List8, FormatCode.List32, out size, out count);
            
            int offset = buffer.Offset;
            this.DecodeValue(buffer, size, count);

            int extraCount = count - this.FieldCount;
            if (extraCount > 0)
            {
                // we just ignore the rest of bytes. ideally we should decode the remaining objects
                // to validate the buffer contains valid AMQP objects.
                int bytesRemaining = size - (buffer.Offset - offset) - (formatCode == FormatCode.List8 ? FixedWidth.UByte : FixedWidth.UInt);
                buffer.Complete(bytesRemaining);
            }
        }

        public void DecodeValue(ByteBuffer buffer, int size, int count)
        {
            if (count > 0)
            {
                this.OnDecode(buffer, count);
                this.EnsureRequired();
            }
        }

        protected virtual void EnsureRequired()
        {
        }

        protected abstract int OnValueSize();

        protected abstract void OnEncode(ByteBuffer buffer);

        protected abstract void OnDecode(ByteBuffer buffer, int count);
    }
}
