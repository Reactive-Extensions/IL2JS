//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    sealed class ShortEncoding : EncodingBase
    {
        public ShortEncoding()
            : base(FormatCode.Short)
        {
        }

        public static int GetEncodeSize(short? value)
        {
            return value.HasValue ? FixedWidth.ShortEncoded : FixedWidth.NullEncoded;
        }

        public static void Encode(short? value, ByteBuffer buffer)
        {
            if (value.HasValue)
            {
                AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.Short);
                AmqpBitConverter.WriteShort(buffer, value.Value);
            }
            else
            {
                AmqpEncoding.EncodeNull(buffer);
            }
        }

        public static short? Decode(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == 0 && (formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return null;
            }

            return AmqpBitConverter.ReadShort(buffer);
        }

        public override int GetObjectEncodeSize(object value, bool arrayEncoding)
        {
            if (arrayEncoding)
            {
                return FixedWidth.Short;
            }
            else
            {
                return ShortEncoding.GetEncodeSize((short)value);
            }
        }

        public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
        {
            if (arrayEncoding)
            {
                AmqpBitConverter.WriteShort(buffer, (short)value);
            }
            else
            {
                ShortEncoding.Encode((short)value, buffer);
            }
        }

        public override object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
        {
            return ShortEncoding.Decode(buffer, formatCode);
        }
    }
}
