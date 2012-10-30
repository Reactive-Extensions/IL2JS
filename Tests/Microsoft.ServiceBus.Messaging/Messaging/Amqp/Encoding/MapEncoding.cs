//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    using System.Collections.Generic;

    sealed class MapEncoding : EncodingBase
    {
        public MapEncoding()
            : base(FormatCode.Map32)
        {
        }

        public static int GetValueSize(AmqpMap value)
        {
            int size = 0;
            if (value.Count > 0)
            {
                foreach (KeyValuePair<MapKey, object> item in value)
                {
                    size += AmqpEncoding.GetObjectEncodeSize(item.Key.Key);
                    size += AmqpEncoding.GetObjectEncodeSize(item.Value);
                }
            }

            return size;
        }

        public static int GetEncodeSize(AmqpMap value)
        {
            return value == null ?
                FixedWidth.NullEncoded :
                FixedWidth.FormatCode + (MapEncoding.GetEncodeWidth(value) * 2) + value.ValueSize;
        }

        public static void Encode(AmqpMap value, ByteBuffer buffer)
        {
            if (value == null)
            {
                AmqpEncoding.EncodeNull(buffer);
            }
            else
            {
                int encodeWidth = MapEncoding.GetEncodeWidth(value);
                AmqpBitConverter.WriteUByte(buffer, encodeWidth == FixedWidth.UByte ? (byte)FormatCode.Map8 : (byte)FormatCode.Map32);

                int size = encodeWidth + value.ValueSize;
                MapEncoding.Encode(value, encodeWidth, size, buffer);
            }
        }

        public static AmqpMap Decode(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == 0 && (formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return null;
            }

            int size = 0;
            int count = 0;
            AmqpEncoding.ReadSizeAndCount(buffer, formatCode, FormatCode.Map8, FormatCode.Map32, out size, out count);
            AmqpMap map = new AmqpMap();
            MapEncoding.ReadMapValue(buffer, map, size, count);
            return map;
        }

        public static void ReadMapValue(ByteBuffer buffer, AmqpMap map, int size, int count)
        {
            for (; count > 0; count -= 2)
            {
                object key = AmqpEncoding.DecodeObject(buffer);
                object item = AmqpEncoding.DecodeObject(buffer);
                map[new MapKey(key)] = item;
            }
        }

        static int GetEncodeWidth(AmqpMap value)
        {
            return AmqpEncoding.GetEncodeWidthByCountAndSize(value.Count * 2, value.ValueSize);
        }

        public override int GetObjectEncodeSize(object value, bool arrayEncoding)
        {
            if (arrayEncoding)
            {
                return FixedWidth.UInt + FixedWidth.UInt + GetValueSize((AmqpMap)value);
            }
            else
            {
                return MapEncoding.GetEncodeSize((AmqpMap)value);
            }
        }

        public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
        {
            if (arrayEncoding)
            {
                AmqpMap mapValue = (AmqpMap)value;
                int size = FixedWidth.UInt + mapValue.ValueSize;
                MapEncoding.Encode(mapValue, FixedWidth.UInt, size, buffer);
            }
            else
            {
                MapEncoding.Encode((AmqpMap)value, buffer);
            }
        }

        public override object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
        {
            return MapEncoding.Decode(buffer, formatCode);
        }

        static void Encode(AmqpMap value, int width, int size, ByteBuffer buffer)
        {
            if (width == FixedWidth.UByte)
            {
                AmqpBitConverter.WriteUByte(buffer, (byte)size);
                AmqpBitConverter.WriteUByte(buffer, (byte)(value.Count * 2));
            }
            else
            {
                AmqpBitConverter.WriteUInt(buffer, (uint)size);
                AmqpBitConverter.WriteUInt(buffer, (uint)(value.Count * 2));
            }

            if (value.Count > 0)
            {
                foreach (KeyValuePair<MapKey, object> item in value)
                {
                    AmqpEncoding.EncodeObject(item.Key.Key, buffer);
                    AmqpEncoding.EncodeObject(item.Value, buffer);
                }
            }
        }
    }
}
