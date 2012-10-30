//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    using System;

    /// <summary>
    /// Decoding from AMQP decimal to C# decimal can lose precision and 
    /// can also cause OverflowException.
    /// </summary>
    sealed class DecimalEncoding : EncodingBase
    {
        const int Decimal32Bias = 101;
        const int Decimal64Bias = 398;
        const int Decimal128Bias = 6176;

        public DecimalEncoding()
            : base(FormatCode.Decimal128)
        {
        }

        public static int GetEncodeSize(decimal? value)
        {
            return value.HasValue ? FixedWidth.Decimal128Encoded : FixedWidth.NullEncoded;
        }

        public static void Encode(decimal? value, ByteBuffer buffer)
        {
            if (value.HasValue)
            {
                AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.Decimal128);
                DecimalEncoding.EncodeValue(value.Value, buffer);
            }
            else
            {
                AmqpEncoding.EncodeNull(buffer);
            }
        }

        public static decimal? Decode(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == 0 && (formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return null;
            }

            return DecimalEncoding.DecodeValue(buffer, formatCode);
        }

        public override int GetObjectEncodeSize(object value, bool arrayEncoding)
        {
            if (arrayEncoding)
            {
                return FixedWidth.Decimal128;
            }
            else
            {
                return FixedWidth.Decimal128Encoded;
            }
        }

        public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
        {
            if (arrayEncoding)
            {
                DecimalEncoding.EncodeValue((decimal)value, buffer);
            }
            else
            {
                DecimalEncoding.Encode((decimal)value, buffer);
            }
        }

        public override object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
        {
            return DecimalEncoding.Decode(buffer, formatCode);
        }

        unsafe static void EncodeValue(decimal value, ByteBuffer buffer)
        {
            int[] bits = Decimal.GetBits(value);
            int lowSignificant = bits[0];
            int middleSignificant = bits[1];
            int highSignificant = bits[2];
            int signAndExponent = bits[3];

            byte[] bytes = new byte[FixedWidth.Decimal128];
            byte *p = (byte*)&signAndExponent;
            int exponent = Decimal128Bias - p[2];
            bytes[0] = p[3];    // sign
            bytes[0] |= (byte)(exponent >> 9);  // 7 bits in msb
            bytes[1] = (byte)((exponent & 0x7F) << 1);  // 7 bits in 2nd msb
            bytes[2] = 0;
            bytes[3] = 0;

            p = (byte*)&highSignificant;
            bytes[4] = p[3];
            bytes[5] = p[2];
            bytes[6] = p[1];
            bytes[7] = p[0];

            p = (byte*)&middleSignificant;
            bytes[8] = p[3];
            bytes[9] = p[2];
            bytes[10] = p[1];
            bytes[11] = p[0];

            p = (byte*)&lowSignificant;
            bytes[12] = p[3];
            bytes[13] = p[2];
            bytes[14] = p[1];
            bytes[15] = p[0];

            buffer.WriteBytes(bytes, 0, bytes.Length);
        }

        static decimal DecodeValue(ByteBuffer buffer, FormatCode formatCode)
        {
            decimal value = 0;
            switch (formatCode)
            {
                case FormatCode.Decimal32:
                    value = DecimalEncoding.DecodeDecimal32(buffer);
                    break;
                case FormatCode.Decimal64:
                    value = DecimalEncoding.DecodeDecimal64(buffer);
                    break;
                case FormatCode.Decimal128:
                    value = DecimalEncoding.DecodeDecimal128(buffer);
                    break;
                default:
                    throw AmqpEncoding.GetInvalidFormatCodeException(formatCode, buffer.Offset);
            };

            return value;
        }

        unsafe static decimal DecodeDecimal32(ByteBuffer buffer)
        {
            byte[] bytes = new byte[FixedWidth.Decimal32];
            buffer.ReadBytes(bytes, 0, bytes.Length);
            int sign = 1;
            int exponent = 0;

            sign = (bytes[0] & 0x80) != 0 ? -1 : 1;
            if ((bytes[0] & 0x60) != 0x60)
            {
                // s 8-bit-exponent (0)23-bit-significant
                exponent = ((bytes[0] & 0x7F) << 1) | ((bytes[1] & 0x80) >> 7);
                bytes[0] = 0;
                bytes[1] &= 0x7F;
            }
            else if ((bytes[0] & 0x78) != 0)
            {
                // handle NaN and Infinity
            }
            else
            {
                // s 11 8-bit-exponent (100)21-bit-significant
                exponent = ((bytes[0] & 0x1F) << 3) | ((bytes[1] & 0xE0) >> 5);
                bytes[0] = 0;
                bytes[1] &= 0x1F;
                bytes[1] |= 0x80;
            }

            int low = (int)AmqpBitConverter.ReadUInt(bytes, 0, bytes.Length);
            return CreateDecimal(low, 0, 0, sign, exponent - Decimal32Bias);
        }

        static decimal DecodeDecimal64(ByteBuffer buffer)
        {
            byte[] bytes = new byte[FixedWidth.Decimal64];
            buffer.ReadBytes(bytes, 0, bytes.Length);
            int sign = 1;
            int exponent = 0;

            sign = (bytes[0] & 0x80) != 0 ? -1 : 1;
            if ((bytes[0] & 0x60) != 0x60)
            {
                // s 10-bit-exponent (0)53-bit-significant
                exponent = ((bytes[0] & 0x7F) << 3) | ((bytes[1] & 0xE0) >> 5);
                bytes[0] = 0;
                bytes[1] &= 0x1F;
            }
            else if ((bytes[0] & 0x78) != 0)
            {
                // handle NaN and Infinity
            }
            else
            {
                // s 11 10-bit-exponent (100)51-bit-significant
                exponent = ((bytes[0] & 0x1F) << 8) | ((bytes[1] & 0xF8) >> 3);
                bytes[0] = 0;
                bytes[1] &= 0x7;
                bytes[1] |= 0x20;
            }

            int middle = (int)AmqpBitConverter.ReadUInt(bytes, 0, 4);
            int low = (int)AmqpBitConverter.ReadUInt(bytes, 4, 4);
            return CreateDecimal(low, middle, 0, sign, exponent - Decimal64Bias);
        }

        unsafe static decimal DecodeDecimal128(ByteBuffer buffer)
        {
            byte[] bytes = new byte[FixedWidth.Decimal128];
            buffer.ReadBytes(bytes, 0, bytes.Length);
            int sign = 1;
            int exponent = 0;

            sign = (bytes[0] & 0x80) != 0 ? -1 : 1;
            if ((bytes[0] & 0x60) != 0x60)
            {
                // s 14-bit-exponent (0)113-bit-significant
                exponent = ((bytes[0] & 0x7F) << 7) | ((bytes[1] & 0xFE) >> 1);
                bytes[0] = 0;
                bytes[1] &= 0x1;
            }
            else if ((bytes[0] & 0x78) != 0)
            {
                // handle NaN and Infinity
            }
            else
            {
                // s 11 14-bit-exponent (100)111-bit-significant
                // it is out of the valid range already. Should not be used
                return 0;
            }

            int high = (int)AmqpBitConverter.ReadUInt(bytes, 4, 4);
            int middle = (int)AmqpBitConverter.ReadUInt(bytes, 8, 4);
            int low = (int)AmqpBitConverter.ReadUInt(bytes, 12, 4);
            return CreateDecimal(low, middle, high, sign, exponent - Decimal128Bias);
        }

        static void VerifyBufferLength(ByteBuffer buffer, int length)
        {
            if (buffer.Length < length)
            {
                throw AmqpEncoding.GetEncodingException("length");
            }
        }

        static decimal CreateDecimal(int low, int middle, int high, int sign, int exponent)
        {
            if (exponent <= 0)
            {
                return new decimal(low, middle, high, sign < 0, (byte)-exponent);
            }
            else
            {
                decimal value = new decimal(low, middle, high, sign < 0, 0);
                for (int i = 0; i < exponent; ++i)
                {
                    value *= 10;
                }

                return value;
            }
        }
    }
}
