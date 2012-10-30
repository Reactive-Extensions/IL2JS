//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Encode and decode data within the AMQP type system.
    /// </summary>
    static class AmqpEncoding
    {
        static Dictionary<Type, EncodingBase> encodingsByType;
        static Dictionary<FormatCode, EncodingBase> encodingsByCode;

        static BooleanEncoding booleanEncoding = new BooleanEncoding();
        static UByteEncoding ubyteEncoding = new UByteEncoding();
        static UShortEncoding ushortEncoding = new UShortEncoding();
        static UIntEncoding uintEncoding = new UIntEncoding();
        static ULongEncoding ulongEncoding = new ULongEncoding();
        static ByteEncoding byteEncoding = new ByteEncoding();
        static ShortEncoding shortEncoding = new ShortEncoding();
        static IntEncoding intEncoding = new IntEncoding();
        static LongEncoding longEncoding = new LongEncoding();
        static FloatEncoding floatEncoding = new FloatEncoding();
        static DoubleEncoding doubleEncoding = new DoubleEncoding();
        static DecimalEncoding decimal128Encoding = new DecimalEncoding();
        static CharEncoding charEncoding = new CharEncoding();
        static TimeStampEncoding timeStampEncoding = new TimeStampEncoding();
        static UuidEncoding uuidEncoding = new UuidEncoding();
        static BinaryEncoding binaryEncoding = new BinaryEncoding();
        static SymbolEncoding symbolEncoding = new SymbolEncoding();
        static StringEncoding stringEncoding = new StringEncoding();
        static ListEncoding listEncoding = new ListEncoding();
        static MapEncoding mapEncoding = new MapEncoding();
        static ArrayEncoding arrayEncoding = new ArrayEncoding();
        static DescribedEncoding describedTypeEncoding = new DescribedEncoding();

        static AmqpEncoding()
        {
            encodingsByType = new Dictionary<Type, EncodingBase>()
            {
                { typeof(bool),             booleanEncoding },
                { typeof(byte),             ubyteEncoding },
                { typeof(ushort),           ushortEncoding },
                { typeof(uint),             uintEncoding },
                { typeof(ulong),            ulongEncoding },
                { typeof(sbyte),            byteEncoding },
                { typeof(short),            shortEncoding },
                { typeof(int),              intEncoding },
                { typeof(long),             longEncoding },
                { typeof(float),            floatEncoding },
                { typeof(double),           doubleEncoding },
                { typeof(decimal),          decimal128Encoding },
                { typeof(char),         charEncoding },
                { typeof(DateTime),         timeStampEncoding },
                { typeof(Guid),             uuidEncoding },
                { typeof(ArraySegment<byte>), binaryEncoding },
                { typeof(AmqpSymbol),       symbolEncoding },
                { typeof(string),           stringEncoding },
                { typeof(AmqpMap),          mapEncoding },
            };

            encodingsByCode = new Dictionary<FormatCode, EncodingBase>()
            {
                { FormatCode.BooleanFalse,  booleanEncoding },
                { FormatCode.BooleanTrue,   booleanEncoding },
                { FormatCode.Boolean,       booleanEncoding },
                { FormatCode.UByte,         ubyteEncoding },
                { FormatCode.UShort,        ushortEncoding },
                { FormatCode.UInt,          uintEncoding },
                { FormatCode.SmallUInt,     uintEncoding },
                { FormatCode.UInt0,         uintEncoding },
                { FormatCode.ULong,         ulongEncoding },
                { FormatCode.SmallULong,    ulongEncoding },
                { FormatCode.ULong0,        ulongEncoding },
                { FormatCode.Byte,          byteEncoding },
                { FormatCode.Short,         shortEncoding },
                { FormatCode.Int,           intEncoding },
                { FormatCode.SmallInt,      intEncoding },
                { FormatCode.Long,          longEncoding },
                { FormatCode.SmallLong,     longEncoding },
                { FormatCode.Float,         floatEncoding },
                { FormatCode.Double,        doubleEncoding },
                { FormatCode.Decimal128,    decimal128Encoding },
                { FormatCode.Char,          charEncoding },
                { FormatCode.TimeStamp,     timeStampEncoding },
                { FormatCode.Uuid,          uuidEncoding },
                { FormatCode.Binary8,       binaryEncoding },
                { FormatCode.Binary32,      binaryEncoding },
                { FormatCode.Symbol8,       symbolEncoding },
                { FormatCode.Symbol32,      symbolEncoding },
                { FormatCode.String8Utf8,   stringEncoding },
                { FormatCode.String32Utf8,  stringEncoding },
                { FormatCode.List0,         listEncoding },
                { FormatCode.List8,         listEncoding },
                { FormatCode.List32,        listEncoding },
                { FormatCode.Map8,          mapEncoding },
                { FormatCode.Map32,         mapEncoding },
                { FormatCode.Array8,        arrayEncoding },
                { FormatCode.Array32,       arrayEncoding },
                { FormatCode.Described,     describedTypeEncoding }
            };
        }

        public static EncodingBase GetEncoding(object value)
        {
            EncodingBase encoding = null;
            Type type = value.GetType();
            if (encodingsByType.TryGetValue(type, out encoding))
            {
                return encoding;
            }
            else if (type.IsArray)
            {
                return arrayEncoding;
            }
            else if (value is IList)
            {
                return listEncoding;
            }
            else if (value is DescribedType)
            {
                return describedTypeEncoding;
            }

            throw GetEncodingException(type.ToString());
        }

        public static EncodingBase GetEncoding(Type type)
        {
            EncodingBase encoding = null;
            if (encodingsByType.TryGetValue(type, out encoding))
            {
                return encoding;
            }
            else if (type.IsArray)
            {
                return arrayEncoding;
            }
            else if (typeof(IList).IsAssignableFrom(type))
            {
                return listEncoding;
            }
            else if (typeof(DescribedType).IsAssignableFrom(type))
            {
                return describedTypeEncoding;
            }

            throw GetEncodingException(type.ToString());
        }

        public static EncodingBase GetEncoding(FormatCode formatCode)
        {
            EncodingBase encoding;
            if (encodingsByCode.TryGetValue(formatCode, out encoding))
            {
                return encoding;
            }

            return null;
        }

        public static int GetEncodeWidthBySize(int size)
        {
            return size <= byte.MaxValue ? FixedWidth.UByte : FixedWidth.UInt;
        }

        public static int GetEncodeWidthByCountAndSize(int count, int valueSize)
        {
            return count < byte.MaxValue && valueSize < byte.MaxValue ? FixedWidth.UByte : FixedWidth.UInt;
        }

        public static FormatCode ReadFormatCode(ByteBuffer buffer)
        {
            int formatCode = AmqpBitConverter.ReadUByte(buffer);
            if ((formatCode & 0x0F) == 0x0F)
            {
                formatCode = (formatCode << 8) + AmqpBitConverter.ReadUByte(buffer);
            }

            return (FormatCode)formatCode;
        }

        public static void ReadCount(ByteBuffer buffer, FormatCode formatCode, FormatCode formatCode8, FormatCode formatCode32, out int count)
        {
            if (formatCode == formatCode8)
            {
                count = AmqpBitConverter.ReadUByte(buffer);
            }
            else if (formatCode == formatCode32)
            {
                count = (int)AmqpBitConverter.ReadUInt(buffer);
            }
            else
            {
                throw GetInvalidFormatCodeException(formatCode, buffer.Offset);
            }
        }

        public static void ReadSizeAndCount(ByteBuffer buffer, FormatCode formatCode, FormatCode formatCode8, FormatCode formatCode32, out int size, out int count)
        {
            if (formatCode == formatCode8)
            {
                size = AmqpBitConverter.ReadUByte(buffer);
                count = AmqpBitConverter.ReadUByte(buffer);
            }
            else if (formatCode == formatCode32)
            {
                size = (int)AmqpBitConverter.ReadUInt(buffer);
                count = (int)AmqpBitConverter.ReadUInt(buffer);
            }
            else
            {
                throw GetInvalidFormatCodeException(formatCode, buffer.Offset);
            }
        }
        
        public static int GetObjectEncodeSize(object value)
        {
            if (value == null)
            {
                return FixedWidth.NullEncoded;
            }

            IAmqpSerializable serializable = value as IAmqpSerializable;
            if (serializable != null)
            {
                return serializable.EncodeSize;
            }

            EncodingBase encoding = GetEncoding(value);
            return encoding.GetObjectEncodeSize(value, false);
        }

        public static void EncodeNull(ByteBuffer buffer)
        {
            AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.Null);
        }

        public static void EncodeObject(object value, ByteBuffer buffer)
        {
            if (value == null)
            {
                EncodeNull(buffer);
                return;
            }

            IAmqpSerializable serializable = value as IAmqpSerializable;
            if (serializable != null)
            {
                serializable.Encode(buffer);
                return;
            }

            EncodingBase encoding = GetEncoding(value); ;
            encoding.EncodeObject(value, false, buffer);
        }

        public static object DecodeObject(ByteBuffer buffer)
        {
            FormatCode formatCode = AmqpEncoding.ReadFormatCode(buffer);
            if (formatCode == FormatCode.Null)
            {
                return null;
            }

            return DecodeObject(buffer, formatCode);
        }

        public static object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
        {
            EncodingBase encoding;
            if (encodingsByCode.TryGetValue(formatCode, out encoding))
            {
                return encoding.DecodeObject(buffer, formatCode);
            }

            throw GetInvalidFormatCodeException(formatCode, buffer.Offset);
        }

        public static AmqpException GetEncodingException(string fieldName)
        {
            return new AmqpException(AmqpError.InvalidField, fieldName);
        }

        public static AmqpException GetInvalidFormatCodeException(FormatCode formatCode, int position)
        {
            return GetEncodingException(string.Format(CultureInfo.CurrentCulture, "Invalid format code: {0} at position {1}.", formatCode, position));
        }
    }
}
