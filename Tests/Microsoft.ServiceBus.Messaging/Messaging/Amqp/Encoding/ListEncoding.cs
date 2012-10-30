//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    using System.Collections;
    using System.Collections.Generic;

    sealed class ListEncoding : EncodingBase
    {
        public ListEncoding()
            : base(FormatCode.List32)
        {
        }

        public static int GetEncodeSize(IList value)
        {
            if (value == null)
            {
                return FixedWidth.NullEncoded;
            }
            else if (value.Count == 0)
            {
                return FixedWidth.FormatCode;
            }
            else
            {
                int valueSize = ListEncoding.GetValueSize(value);
                int width = AmqpEncoding.GetEncodeWidthByCountAndSize(value.Count, valueSize);
                return FixedWidth.FormatCode + width * 2 + valueSize;
            }
        }

        public static void Encode(IList value, ByteBuffer buffer)
        {
            if (value == null)
            {
                AmqpEncoding.EncodeNull(buffer);
            }
            else if (value.Count == 0)
            {
                AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.List0);
            }
            else
            {
                int valueSize = ListEncoding.GetValueSize(value);
                int width = AmqpEncoding.GetEncodeWidthByCountAndSize(value.Count, valueSize);
                AmqpBitConverter.WriteUByte(buffer, width == FixedWidth.UByte ? (byte)FormatCode.List8 : (byte)FormatCode.List32);

                int size = width + valueSize;
                ListEncoding.Encode(value, width, size, buffer);
            }
        }

        public static IList Decode(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == 0 && (formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return null;
            }

            IList list = new List<object>();
            if (formatCode == FormatCode.List0)
            {
                return list;
            }

            int size = 0;
            int count = 0;
            AmqpEncoding.ReadSizeAndCount(buffer, formatCode, FormatCode.List8, FormatCode.List32, out size, out count);

            for (; count > 0; --count)
            {
                object item = AmqpEncoding.DecodeObject(buffer);
                list.Add(item);
            }

            return list;
        }

        public override int GetObjectEncodeSize(object value, bool arrayEncoding)
        {
            if (arrayEncoding)
            {
                return FixedWidth.UInt + FixedWidth.UInt + GetValueSize((IList)value);
            }
            else
            {
                return ListEncoding.GetEncodeSize((IList)value);
            }
        }

        public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
        {
            if (arrayEncoding)
            {
                IList listValue = (IList)value;
                int size = FixedWidth.UInt + GetValueSize(listValue);
                ListEncoding.Encode(listValue, FixedWidth.UInt, size, buffer);
            }
            else
            {
                ListEncoding.Encode((IList)value, buffer);
            }
        }

        public override object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
        {
            return ListEncoding.Decode(buffer, formatCode);
        }

        public static int GetValueSize(IList value)
        {
            int size = 0;
            if (value.Count > 0)
            {
                foreach (object item in value)
                {
                    size += AmqpEncoding.GetObjectEncodeSize(item);
                }
            }

            return size;
        }

        static void Encode(IList value, int width, int size, ByteBuffer buffer)
        {
            if (width == FixedWidth.UByte)
            {
                AmqpBitConverter.WriteUByte(buffer, (byte)size);
                AmqpBitConverter.WriteUByte(buffer, (byte)value.Count);
            }
            else
            {
                AmqpBitConverter.WriteUInt(buffer, (uint)size);
                AmqpBitConverter.WriteUInt(buffer, (uint)value.Count);
            }

            if (value.Count > 0)
            {
                foreach (object item in value)
                {
                    AmqpEncoding.EncodeObject(item, buffer);
                }
            }
        }
    }
}
