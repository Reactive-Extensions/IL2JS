//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    sealed class BooleanEncoding : EncodingBase
    {
        public BooleanEncoding()
            : base(FormatCode.Boolean)
        {
        }

        public static int GetEncodeSize(bool? value)
        {
            return value.HasValue ? FixedWidth.BooleanEncoded : FixedWidth.NullEncoded;
        }

        public static void Encode(bool? value, ByteBuffer buffer)
        {
            if (value.HasValue)
            {
                AmqpBitConverter.WriteUByte(buffer, value.Value ? (byte)FormatCode.BooleanTrue : (byte)FormatCode.BooleanFalse);
            }
            else
            {
                AmqpEncoding.EncodeNull(buffer);
            }
        }

        public static bool? Decode(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == 0 && (formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return null;
            }

            VerifyFormatCode(formatCode, buffer.Offset, FormatCode.Boolean, FormatCode.BooleanFalse, FormatCode.BooleanTrue);
            if (formatCode == FormatCode.Boolean)
            {
                return AmqpBitConverter.ReadUByte(buffer) != 0;
            }
            else
            {
                return formatCode == FormatCode.BooleanTrue ? true : false;
            }
        }

        public override int GetObjectEncodeSize(object value, bool arrayEncoding)
        {
            if (arrayEncoding)
            {
                return FixedWidth.BooleanVar;
            }
            else
            {
                return BooleanEncoding.GetEncodeSize((bool)value);
            }
        }

        public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
        {
            if (arrayEncoding)
            {
                AmqpBitConverter.WriteUByte(buffer, (byte)((bool)value ? 1 : 0));
            }
            else
            {
                BooleanEncoding.Encode((bool)value, buffer);
            }
        }

        public override object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
        {
            return BooleanEncoding.Decode(buffer, formatCode);
        }
    }
}
