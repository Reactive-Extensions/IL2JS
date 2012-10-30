//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    sealed class UByteEncoding : EncodingBase
    {
        public UByteEncoding()
            : base(FormatCode.UByte)
        {
        }

        public static int GetEncodeSize(byte? value)
        {
            return value.HasValue ? FixedWidth.UByteEncoded : FixedWidth.NullEncoded;
        }

        public static void Encode(byte? value, ByteBuffer buffer)
        {
            if (value.HasValue)
            {
                AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.UByte);
                AmqpBitConverter.WriteUByte(buffer, value.Value);
            }
            else
            {
                AmqpEncoding.EncodeNull(buffer);
            }
        }

        public static byte? Decode(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == 0 && (formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return null;
            }

            return AmqpBitConverter.ReadUByte(buffer);
        }

        public override int GetObjectEncodeSize(object value, bool arrayEncoding)
        {
            if (arrayEncoding)
            {
                return FixedWidth.UByte;
            }
            else
            {
                return UByteEncoding.GetEncodeSize((byte)value);
            }
        }

        public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
        {
            if (arrayEncoding)
            {
                AmqpBitConverter.WriteUByte(buffer, (byte)value);
            }
            else
            {
                UByteEncoding.Encode((byte)value, buffer);
            }
        }

        public override object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
        {
            return UByteEncoding.Decode(buffer, formatCode);
        }
    }
}
