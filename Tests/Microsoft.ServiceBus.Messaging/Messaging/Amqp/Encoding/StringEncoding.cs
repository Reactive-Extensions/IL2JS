//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    using System;
    using System.Text;

    sealed class StringEncoding : EncodingBase
    {
        public StringEncoding()
            : base(FormatCode.String32Utf8)
        {
        }

        public static int GetEncodeSize(string value)
        {
            if (value == null)
            {
                return FixedWidth.NullEncoded;
            }

            int stringSize = Encoding.UTF8.GetByteCount(value);
            return FixedWidth.FormatCode + AmqpEncoding.GetEncodeWidthBySize(stringSize) + stringSize;
        }

        public static void Encode(string value, ByteBuffer buffer)
        {
            if (value == null)
            {
                AmqpEncoding.EncodeNull(buffer);
            }
            else
            {
                StringEncoding.Encode(value, 0, buffer);
            }
        }

        public static string Decode(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == 0 && (formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return null;
            }

            int count = 0;
            Encoding encoding = null;

            if (formatCode == FormatCode.String8Utf8)
            {
                count = (int)AmqpBitConverter.ReadUByte(buffer);
                encoding = Encoding.UTF8;
            }
            else if (formatCode == FormatCode.String32Utf8)
            {
                count = (int)AmqpBitConverter.ReadUInt(buffer);
                encoding = Encoding.UTF8;
            }
            else
            {
                throw AmqpEncoding.GetInvalidFormatCodeException(formatCode, buffer.Offset);
            }

            ArraySegment<byte> bytes = buffer.GetBytes(count);
            return encoding.GetString(bytes.Array, bytes.Offset, count);
        }

        public override int GetObjectEncodeSize(object value, bool arrayEncoding)
        {
            if (arrayEncoding)
            {
                return FixedWidth.UInt + Encoding.UTF8.GetByteCount((string)value);
            }
            else
            {
                return StringEncoding.GetEncodeSize((string)value);
            }
        }

        public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
        {
            if (arrayEncoding)
            {
                StringEncoding.Encode((string)value, FixedWidth.UInt, buffer);
            }
            else
            {
                StringEncoding.Encode((string)value, buffer);
            }
        }

        public override object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
        {
            return StringEncoding.Decode(buffer, formatCode);
        }

        static void Encode(string value, int width, ByteBuffer buffer)
        {
            int stringSize = Encoding.UTF8.GetByteCount(value);
            if (width == 0)
            {
                width = AmqpEncoding.GetEncodeWidthBySize(stringSize);
            }

            if (width == FixedWidth.UByte)
            {
                AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.String8Utf8);
                AmqpBitConverter.WriteUByte(buffer, (byte)stringSize);
            }
            else
            {
                AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.String32Utf8);
                AmqpBitConverter.WriteUInt(buffer, (uint)stringSize);
            }

            buffer.EnsureSize(stringSize);
            int written = Encoding.UTF8.GetBytes(value, 0, value.Length, buffer.Buffer, buffer.End);
            buffer.Append(written);
        }
    }
}
