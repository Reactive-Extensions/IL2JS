//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    sealed class UIntEncoding : EncodingBase
    {
        public UIntEncoding()
            : base(FormatCode.UInt)
        {
        }

        public static int GetEncodeSize(uint? value)
        {
            if (value.HasValue)
            {
                if (value.Value == 0)
                {
                    return FixedWidth.ZeroEncoded;
                }
                else
                {
                    return value.Value <= byte.MaxValue ? FixedWidth.UByteEncoded : FixedWidth.UIntEncoded;
                }
            }
            else
            {
                return FixedWidth.NullEncoded;
            }
        }

        public static void Encode(uint? value, ByteBuffer buffer)
        {
            if (value.HasValue)
            {
                if (value == 0)
                {
                    AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.UInt0);
                }
                else if (value.Value <= byte.MaxValue)
                {
                    AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.SmallUInt);
                    AmqpBitConverter.WriteUByte(buffer, (byte)value.Value);
                }
                else
                {
                    AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.UInt);
                    AmqpBitConverter.WriteUInt(buffer, value.Value);
                }
            }
            else
            {
                AmqpEncoding.EncodeNull(buffer);
            }
        }

        public static uint? Decode(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == 0 && (formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return null;
            }

            VerifyFormatCode(formatCode, buffer.Offset, FormatCode.UInt, FormatCode.SmallUInt, FormatCode.UInt0);
            if (formatCode == FormatCode.UInt0)
            {
                return 0;
            }
            else
            {
                return formatCode == FormatCode.SmallUInt ?
                    AmqpBitConverter.ReadUByte(buffer) :
                    AmqpBitConverter.ReadUInt(buffer);
            }
        }

        public override int GetObjectEncodeSize(object value, bool arrayEncoding)
        {
            if (arrayEncoding)
            {
                return FixedWidth.UInt;
            }
            else
            {
                return UIntEncoding.GetEncodeSize((uint)value);
            }
        }

        public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
        {
            if (arrayEncoding)
            {
                AmqpBitConverter.WriteUInt(buffer, (uint)value);
            }
            else
            {
                UIntEncoding.Encode((uint)value, buffer);
            }
        }

        public override object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
        {
            return UIntEncoding.Decode(buffer, formatCode);
        }
    }
}
