//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    using System;
    using System.Text;

    sealed class SymbolEncoding : EncodingBase
    {
        public SymbolEncoding()
            : base(FormatCode.Symbol32)
        {
        }

        public static int GetValueSize(AmqpSymbol value)
        {
            return value.Value == null ? FixedWidth.Null : Encoding.ASCII.GetByteCount(value.Value);
        }

        public static int GetEncodeSize(AmqpSymbol value)
        {
            return value.Value == null ?
                FixedWidth.NullEncoded :
                FixedWidth.FormatCode + AmqpEncoding.GetEncodeWidthBySize(value.ValueSize) + value.ValueSize;
        }

        public static void Encode(AmqpSymbol value, ByteBuffer buffer)
        {
            if (value.Value == null)
            {
                AmqpEncoding.EncodeNull(buffer);
            }
            else
            {
                SymbolEncoding.Encode(value, 0, buffer);
            }
        }

        public static AmqpSymbol Decode(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == 0 && (formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return new AmqpSymbol();
            }

            int count = 0;
            AmqpEncoding.ReadCount(buffer, formatCode, FormatCode.Symbol8, FormatCode.Symbol32, out count);
            ArraySegment<byte> bytes = buffer.GetBytes(count);
            return Encoding.ASCII.GetString(bytes.Array, bytes.Offset, count);
        }

        public override int GetObjectEncodeSize(object value, bool arrayEncoding)
        {
            if (arrayEncoding)
            {
                return FixedWidth.UInt + Encoding.ASCII.GetByteCount(((AmqpSymbol)value).Value);
            }
            else
            {
                return SymbolEncoding.GetEncodeSize((AmqpSymbol)value);
            }
        }

        public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
        {
            if (arrayEncoding)
            {
                SymbolEncoding.Encode((AmqpSymbol)value, FixedWidth.UInt, buffer);
            }
            else
            {
                SymbolEncoding.Encode((AmqpSymbol)value, buffer);
            }
        }

        public override object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
        {
            return SymbolEncoding.Decode(buffer, formatCode);
        }

        static void Encode(AmqpSymbol value, int width, ByteBuffer buffer)
        {
            int stringSize = Encoding.ASCII.GetByteCount(value.Value);
            if (width == 0)
            {
                width = AmqpEncoding.GetEncodeWidthBySize(stringSize);
            }

            if (width == FixedWidth.UByte)
            {
                AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.Symbol8);
                AmqpBitConverter.WriteUByte(buffer, (byte)stringSize);
            }
            else
            {
                AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.Symbol32);
                AmqpBitConverter.WriteUInt(buffer, (uint)stringSize);
            }

            buffer.EnsureSize(stringSize);
            int written = Encoding.ASCII.GetBytes(value.Value, 0, value.Value.Length, buffer.Buffer, buffer.End);
            buffer.Append(written);
        }
    }
}
