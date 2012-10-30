//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    sealed class IntEncoding : EncodingBase
    {
        public IntEncoding()
            : base(FormatCode.Int)
        {
        }

        public static int GetEncodeSize(int? value)
        {
            if (value.HasValue)
            {
                return value.Value < sbyte.MinValue || value.Value > sbyte.MaxValue ?
                    FixedWidth.IntEncoded :
                    FixedWidth.ByteEncoded;
            }
            else
            {
                return FixedWidth.NullEncoded;
            }
        }

        public static void Encode(int? value, ByteBuffer buffer)
        {
            if (value.HasValue)
            {
                if (value < sbyte.MinValue || value > sbyte.MaxValue)
                {
                    AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.Int);
                    AmqpBitConverter.WriteInt(buffer, value.Value);
                }
                else
                {
                    AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.SmallInt);
                    AmqpBitConverter.WriteByte(buffer, (sbyte)value.Value);
                }
            }
            else
            {
                AmqpEncoding.EncodeNull(buffer);
            }
        }

        public static int? Decode(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == 0 && (formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return null;
            }

            VerifyFormatCode(formatCode, buffer.Offset, FormatCode.Int, FormatCode.SmallInt);
            return formatCode == FormatCode.SmallInt ?
                AmqpBitConverter.ReadByte(buffer) :
                AmqpBitConverter.ReadInt(buffer);
        }

        public override int GetObjectEncodeSize(object value, bool arrayEncoding)
        {
            if (arrayEncoding)
            {
                return FixedWidth.Int;
            }
            else
            {
                return IntEncoding.GetEncodeSize((int)value);
            }
        }

        public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
        {
            if (arrayEncoding)
            {
                AmqpBitConverter.WriteInt(buffer, (int)value);
            }
            else
            {
                IntEncoding.Encode((int)value, buffer);
            }
        }

        public override object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
        {
            return IntEncoding.Decode(buffer, formatCode);
        }
    }
}
