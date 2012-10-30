//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    sealed class DescribedEncoding : EncodingBase
    {
        public DescribedEncoding()
            : base(FormatCode.Described)
        {
        }

        public static int GetEncodeSize(DescribedType value)
        {
            return value == null ?
                FixedWidth.NullEncoded :
                FixedWidth.FormatCode + AmqpEncoding.GetObjectEncodeSize(value.Descriptor) + AmqpEncoding.GetObjectEncodeSize(value.Value);
        }

        public static void Encode(DescribedType value, ByteBuffer buffer)
        {
            if (value.Value == null)
            {
                AmqpEncoding.EncodeNull(buffer);
            }
            else
            {
                AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.Described);
                AmqpEncoding.EncodeObject(value.Descriptor, buffer);
                AmqpEncoding.EncodeObject(value.Value, buffer);
            }
        }

        public static DescribedType Decode(ByteBuffer buffer)
        {
            FormatCode formatCode;
            if ((formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return null;
            }

            return DescribedEncoding.Decode(buffer, formatCode);
        }

        public override int GetObjectEncodeSize(object value, bool arrayEncoding)
        {
            if (arrayEncoding)
            {
                object describedValue = ((DescribedType)value).Value;
                EncodingBase encoding = AmqpEncoding.GetEncoding(describedValue);
                return encoding.GetObjectEncodeSize(describedValue, true);
            }
            else
            {
                return DescribedEncoding.GetEncodeSize((DescribedType)value);
            }
        }

        public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
        {
            if (arrayEncoding)
            {
                object describedValue = ((DescribedType)value).Value;
                EncodingBase encoding = AmqpEncoding.GetEncoding(describedValue);
                encoding.EncodeObject(describedValue, true, buffer);
            }
            else
            {
                DescribedEncoding.Encode((DescribedType)value, buffer);
            }
        }

        public override object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == FormatCode.Described)
            {
                return DescribedEncoding.Decode(buffer, formatCode);
            }
            else
            {
                return AmqpEncoding.DecodeObject(buffer, formatCode);
            }
        }

        static DescribedType Decode(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode != FormatCode.Described)
            {
                throw AmqpEncoding.GetInvalidFormatCodeException(formatCode, buffer.Offset);
            }

            object descriptor = AmqpEncoding.DecodeObject(buffer);
            object value = AmqpEncoding.DecodeObject(buffer);
            return new DescribedType(descriptor, value);
        }
    }
}
