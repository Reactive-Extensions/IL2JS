//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    sealed class LongEncoding : EncodingBase
    {
        public LongEncoding()
            : base(FormatCode.Long)
        {
        }

        public static int GetEncodeSize(long? value)
        {
            if (value.HasValue)
            {
                return value < sbyte.MinValue || value > sbyte.MaxValue ?
                    FixedWidth.LongEncoded :
                    FixedWidth.UByteEncoded;
            }
            else
            {
                return FixedWidth.NullEncoded;
            }
        }

        public static void Encode(long? value, ByteBuffer buffer)
        {
            if (value.HasValue)
            {
                if (value < sbyte.MinValue || value > sbyte.MaxValue)
                {
                    AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.Long);
                    AmqpBitConverter.WriteLong(buffer, value.Value);
                }
                else
                {
                    AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.SmallLong);
                    AmqpBitConverter.WriteByte(buffer, (sbyte)value);
                }
            }
            else
            {
                AmqpEncoding.EncodeNull(buffer);
            }
        }

        public static long? Decode(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == 0 && (formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return null;
            }

            VerifyFormatCode(formatCode, buffer.Offset, FormatCode.Long, FormatCode.SmallLong);
            return formatCode == FormatCode.SmallLong ?
                AmqpBitConverter.ReadByte(buffer) :
                AmqpBitConverter.ReadLong(buffer);
        }

        public override int GetObjectEncodeSize(object value, bool arrayEncoding)
        {
            if (arrayEncoding)
            {
                return FixedWidth.Long;
            }
            else
            {
                return LongEncoding.GetEncodeSize((long)value);
            }
        }

        public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
        {
            if (arrayEncoding)
            {
                AmqpBitConverter.WriteLong(buffer, (long)value);
            }
            else
            {
                LongEncoding.Encode((long)value, buffer);
            }
        }

        public override object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
        {
            return LongEncoding.Decode(buffer, formatCode);
        }
    }
}
