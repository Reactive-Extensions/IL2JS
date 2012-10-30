//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    sealed class DoubleEncoding : EncodingBase
    {
        public DoubleEncoding()
            : base(FormatCode.Double)
        {
        }

        public static int GetEncodeSize(double? value)
        {
            return value.HasValue ? FixedWidth.DoubleEncoded : FixedWidth.NullEncoded;
        }

        public static void Encode(double? value, ByteBuffer buffer)
        {
            if (value.HasValue)
            {
                AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.Double);
                AmqpBitConverter.WriteDouble(buffer, value.Value);
            }
            else
            {
                AmqpEncoding.EncodeNull(buffer);
            }
        }

        public static double? Decode(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == 0 && (formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return null;
            }

            return AmqpBitConverter.ReadDouble(buffer);
        }

        public override int GetObjectEncodeSize(object value, bool arrayEncoding)
        {
            if (arrayEncoding)
            {
                return FixedWidth.Double;
            }
            else
            {
                return DoubleEncoding.GetEncodeSize((double)value);
            }
        }

        public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
        {
            if (arrayEncoding)
            {
                AmqpBitConverter.WriteDouble(buffer, (double)value);
            }
            else
            {
                DoubleEncoding.Encode((double)value, buffer);
            }
        }

        public override object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
        {
            return DoubleEncoding.Decode(buffer, formatCode);
        }
    }
}
