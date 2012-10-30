//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    sealed class ULongEncoding : EncodingBase
    {
        public ULongEncoding()
            : base(FormatCode.ULong)
        {
        }

        public static int GetEncodeSize(ulong? value)
        {
            if (value.HasValue)
            {
                if (value.Value == 0)
                {
                    return FixedWidth.ZeroEncoded;
                }
                else
                {
                    return value.Value <= byte.MaxValue ? FixedWidth.UByteEncoded : FixedWidth.ULongEncoded;
                }
            }
            else
            {
                return FixedWidth.NullEncoded;
            }
        }

        public static void Encode(ulong? value, ByteBuffer buffer)
        {
            if (value.HasValue)
            {
                if (value == 0)
                {
                    AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.ULong0);
                }
                else if (value <= byte.MaxValue)
                {
                    AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.SmallULong);
                    AmqpBitConverter.WriteUByte(buffer, (byte)value.Value);
                }
                else
                {
                    AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.ULong);
                    AmqpBitConverter.WriteULong(buffer, value.Value);
                }
            }
            else
            {
                AmqpEncoding.EncodeNull(buffer);
            }
        }

        public static ulong? Decode(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == 0 && (formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return null;
            }

            VerifyFormatCode(formatCode, buffer.Offset, FormatCode.ULong, FormatCode.SmallULong, FormatCode.ULong0);
            if (formatCode == FormatCode.ULong0)
            {
                return 0;
            }
            else
            {
                return formatCode == FormatCode.SmallULong ?
                    AmqpBitConverter.ReadUByte(buffer) :
                    AmqpBitConverter.ReadULong(buffer);
            }
        }

        public override int GetObjectEncodeSize(object value, bool arrayEncoding)
        {
            if (arrayEncoding)
            {
                return FixedWidth.ULong;
            }
            else
            {
                return ULongEncoding.GetEncodeSize((ulong)value);
            }
        }

        public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
        {
            if (arrayEncoding)
            {
                AmqpBitConverter.WriteULong(buffer, (ulong)value);
            }
            else
            {
                ULongEncoding.Encode((ulong)value, buffer);
            }
        }

        public override object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
        {
            return ULongEncoding.Decode(buffer, formatCode);
        }
    }
}
