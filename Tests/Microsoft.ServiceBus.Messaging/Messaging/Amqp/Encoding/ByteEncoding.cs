//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    sealed class ByteEncoding : EncodingBase
    {
        public ByteEncoding()
            : base(FormatCode.Byte)
        {
        }

        public static int GetEncodeSize(sbyte? value)
        {
            return value.HasValue ? FixedWidth.ByteEncoded : FixedWidth.NullEncoded;
        }

        public static void Encode(sbyte? value, ByteBuffer buffer)
        {
            if (value.HasValue)
            {
                AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.Byte);
                AmqpBitConverter.WriteByte(buffer, value.Value);
            }
            else
            {
                AmqpEncoding.EncodeNull(buffer);
            }
        }

        public static sbyte? Decode(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == 0 && (formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return null;
            }

            return AmqpBitConverter.ReadByte(buffer);
        }

        public override int GetObjectEncodeSize(object value, bool arrayEncoding)
        {
            if (arrayEncoding)
            {
                return FixedWidth.Byte;
            }
            else
            {
                return ByteEncoding.GetEncodeSize((sbyte)value);
            }
        }

        public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
        {
            if (arrayEncoding)
            {
                AmqpBitConverter.WriteByte(buffer, (sbyte)value);
            }
            else
            {
                ByteEncoding.Encode((sbyte)value, buffer);
            }
        }

        public override object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
        {
            return ByteEncoding.Decode(buffer, formatCode);
        }
    }
}
