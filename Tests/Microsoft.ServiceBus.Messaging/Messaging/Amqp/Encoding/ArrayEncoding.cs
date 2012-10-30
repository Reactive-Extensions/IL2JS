//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    using System;
    using System.Collections;

    sealed class ArrayEncoding : EncodingBase
    {
        public ArrayEncoding()
            : base(FormatCode.Array32)
        {
        }

        public static int GetEncodeSize<T>(T[] value)
        {
            return value == null ? FixedWidth.NullEncoded : ArrayEncoding.GetEncodeSize(value, false);
        }

        public static void Encode<T>(T[] value, ByteBuffer buffer)
        {
            if (value == null)
            {
                AmqpEncoding.EncodeNull(buffer);
            }
            else
            {
                int width;
                int encodeSize = ArrayEncoding.GetEncodeSize(value, false, out width);
                AmqpBitConverter.WriteUByte(buffer, width == FixedWidth.UByte ? (byte)FormatCode.Array8 : (byte)FormatCode.Array32);
                ArrayEncoding.Encode(value, width, encodeSize, buffer);
            }
        }

        public static T[] Decode<T>(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == 0 && (formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return null;
            }

            int size = 0;
            int count = 0;
            AmqpEncoding.ReadSizeAndCount(buffer, formatCode, FormatCode.Array8, FormatCode.Array32, out size, out count);

            formatCode = AmqpEncoding.ReadFormatCode(buffer);
            return ArrayEncoding.Decode<T>(buffer, size, count, formatCode);
        }

        public override int GetObjectEncodeSize(object value, bool arrayEncoding)
        {
            return ArrayEncoding.GetEncodeSize((Array)value, arrayEncoding);
        }

        public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
        {
            Array array = (Array)value;
            int width;
            int encodeSize = ArrayEncoding.GetEncodeSize(array, arrayEncoding, out width);
            AmqpBitConverter.WriteUByte(buffer, width == FixedWidth.UByte ? (byte)FormatCode.Array8 : (byte)FormatCode.Array32);
            ArrayEncoding.Encode(array, width, encodeSize, buffer);
        }

        public override object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == 0 && (formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return null;
            }

            int size = 0;
            int count = 0;
            AmqpEncoding.ReadSizeAndCount(buffer, formatCode, FormatCode.Array8, FormatCode.Array32, out size, out count);

            formatCode = AmqpEncoding.ReadFormatCode(buffer);
            Array array = null;
            switch (formatCode)
            {
                case FormatCode.Boolean:
                    array = ArrayEncoding.Decode<bool>(buffer, size, count, formatCode);
                    break;
                case FormatCode.UByte:
                    array = ArrayEncoding.Decode<byte>(buffer, size, count, formatCode);
                    break;
                case FormatCode.UShort:
                    array = ArrayEncoding.Decode<ushort>(buffer, size, count, formatCode);
                    break;
                case FormatCode.UInt:
                case FormatCode.SmallUInt:
                    array = ArrayEncoding.Decode<uint>(buffer, size, count, formatCode);
                    break;
                case FormatCode.ULong:
                case FormatCode.SmallULong:
                    array = ArrayEncoding.Decode<ulong>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Byte:
                    array = ArrayEncoding.Decode<sbyte>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Short:
                    array = ArrayEncoding.Decode<short>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Int:
                case FormatCode.SmallInt:
                    array = ArrayEncoding.Decode<int>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Long:
                case FormatCode.SmallLong:
                    array = ArrayEncoding.Decode<long>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Float:
                    array = ArrayEncoding.Decode<float>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Double:
                    array = ArrayEncoding.Decode<double>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Char:
                    array = ArrayEncoding.Decode<char>(buffer, size, count, formatCode);
                    break;
                case FormatCode.TimeStamp:
                    array = ArrayEncoding.Decode<DateTime>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Uuid:
                    array = ArrayEncoding.Decode<Guid>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Binary32:
                case FormatCode.Binary8:
                    array = ArrayEncoding.Decode<ArraySegment<byte>>(buffer, size, count, formatCode);
                    break;
                case FormatCode.String32Utf8:
                case FormatCode.String8Utf8:
                    array = ArrayEncoding.Decode<string>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Symbol32:
                case FormatCode.Symbol8:
                    array = ArrayEncoding.Decode<AmqpSymbol>(buffer, size, count, formatCode);
                    break;
                case FormatCode.List32:
                case FormatCode.List8:
                    array = ArrayEncoding.Decode<IList>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Map32:
                case FormatCode.Map8:
                    array = ArrayEncoding.Decode<AmqpMap>(buffer, size, count, formatCode);
                    break;
                case FormatCode.Array32:
                case FormatCode.Array8:
                    array = ArrayEncoding.Decode<Array>(buffer, size, count, formatCode);
                    break;
                default:
                    throw new NotSupportedException(SRClient.NotSupportFrameCode(formatCode));
            };

            return array;
        }

        static int GetEncodeSize(Array array, bool arrayEncoding)
        {
            int unused;
            return ArrayEncoding.GetEncodeSize(array, arrayEncoding, out unused);
        }

        static int GetEncodeSize(Array array, bool arrayEncoding, out int width)
        {
            int size = FixedWidth.FormatCode + ArrayEncoding.GetValueSize(array, null);
            width = arrayEncoding ? FixedWidth.UInt : AmqpEncoding.GetEncodeWidthByCountAndSize(array.Length, size);
            size += FixedWidth.FormatCode + width + width;
            return size;
        }

        static int GetValueSize(Array value, Type type)
        {
            if (value.Length == 0)
            {
                return 0;
            }

            if (type == null)
            {
                type = value.GetValue(0).GetType();
            }

            EncodingBase encoding = AmqpEncoding.GetEncoding(type);
            int valueSize = 0;
            foreach (object item in value)
            {
                bool arrayEncoding = true;
                if (encoding.FormatCode == FormatCode.Described && valueSize == 0)
                {
                    arrayEncoding = false;
                }

                valueSize += encoding.GetObjectEncodeSize(item, arrayEncoding);
            }

            return valueSize;
        }

        static void Encode(Array value, int width, int encodeSize, ByteBuffer buffer)
        {
            encodeSize -= (FixedWidth.FormatCode + width);
            if (width == FixedWidth.UByte)
            {
                AmqpBitConverter.WriteUByte(buffer, (byte)encodeSize);
                AmqpBitConverter.WriteUByte(buffer, (byte)value.Length);
            }
            else
            {
                AmqpBitConverter.WriteUInt(buffer, (uint)encodeSize);
                AmqpBitConverter.WriteUInt(buffer, (uint)value.Length);
            }

            if (value.Length > 0)
            {
                object firstItem = value.GetValue(0);
                EncodingBase encoding = AmqpEncoding.GetEncoding(firstItem);
                AmqpBitConverter.WriteUByte(buffer, (byte)encoding.FormatCode);
                if (encoding.FormatCode == FormatCode.Described)
                {
                    DescribedType describedValue = (DescribedType)firstItem;
                    AmqpEncoding.EncodeObject(describedValue.Descriptor, buffer);
                    AmqpBitConverter.WriteUByte(buffer, (byte)AmqpEncoding.GetEncoding(describedValue.Value).FormatCode);
                }

                foreach (object item in value)
                {
                    encoding.EncodeObject(item, true, buffer);
                }
            }
        }

        static T[] Decode<T>(ByteBuffer buffer, int size, int count, FormatCode formatCode)
        {
            T[] array = new T[count];
            EncodingBase encoding = AmqpEncoding.GetEncoding(formatCode);
            object descriptor = null;
            if (formatCode == FormatCode.Described)
            {
                descriptor = AmqpEncoding.DecodeObject(buffer);
                formatCode = AmqpEncoding.ReadFormatCode(buffer);
            }

            for (int i = 0; i < count; ++i)
            {
                object value = encoding.DecodeObject(buffer, formatCode);
                if (descriptor != null)
                {
                    value = new DescribedType(descriptor, value);
                }

                array[i] = (T)value;
            }

            return array;
        }
    }
}
