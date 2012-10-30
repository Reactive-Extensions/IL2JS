//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System;
    using System.Globalization;
    using System.Text;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    /// <summary>
    /// Descriptor is restricted to symbol and ulong
    /// </summary>
    class AmqpDescribed : DescribedType, IAmqpSerializable
    {
        AmqpSymbol name;
        ulong code;

        public AmqpDescribed(AmqpSymbol name, ulong code)
            : base(name.Value == null ? (object)code : (object)name, null)
        {
            this.name = name;
            this.code = code;
        }

        public AmqpSymbol DescriptorName
        {
            get { return this.name; }
        }

        public ulong DescriptorCode
        {
            get { return this.code; }
        }

        public int EncodeSize
        {
            get
            {
                int encodeSize = FixedWidth.FormatCode + ULongEncoding.GetEncodeSize(this.DescriptorCode);
                encodeSize += this.GetValueEncodeSize();
                return encodeSize;
            }
        }

        public static void DecodeDescriptor(ByteBuffer buffer, out AmqpSymbol name, out ulong code)
        {
            name = default(AmqpSymbol);
            code = 0;

            FormatCode formatCode = AmqpEncoding.ReadFormatCode(buffer);
            if (formatCode == FormatCode.Described)
            {
                formatCode = AmqpEncoding.ReadFormatCode(buffer);
            }

            if (formatCode == FormatCode.Symbol8 ||
                formatCode == FormatCode.Symbol32)
            {
                name = SymbolEncoding.Decode(buffer, formatCode);
            }
            else if (formatCode == FormatCode.ULong ||
                formatCode == FormatCode.ULong0 ||
                formatCode == FormatCode.SmallULong)
            {
                code = ULongEncoding.Decode(buffer, formatCode).Value;
            }
            else
            {
                throw AmqpEncoding.GetInvalidFormatCodeException(formatCode, buffer.Offset);
            }
        }

        public override string ToString()
        {
            return this.name.Value;
        }

        public void Encode(ByteBuffer buffer)
        {
            AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.Described);
            ULongEncoding.Encode(this.DescriptorCode, buffer);
            this.EncodeValue(buffer);
        }

        public void Decode(ByteBuffer buffer)
        {
            // TODO: validate that the name or code is actually correct
            DecodeDescriptor(buffer, out this.name, out this.code);
            this.DecodeValue(buffer);
        }

        public virtual int GetValueEncodeSize()
        {
            return AmqpEncoding.GetObjectEncodeSize(this.Value);
        }

        public virtual void EncodeValue(ByteBuffer buffer)
        {
            AmqpEncoding.EncodeObject(this.Value, buffer);
        }

        public virtual void DecodeValue(ByteBuffer buffer)
        {
            this.Value = AmqpEncoding.DecodeObject(buffer);
        }

        protected void AddFieldToString(bool condition, StringBuilder sb, string fieldName, object value, ref int count)
        {
            if (condition)
            {
                if (count > 0)
                {
                    sb.Append(',');
                }

                if (value is ArraySegment<byte>)
                {
                    sb.Append(fieldName);
                    sb.Append(':');
                    ArraySegment<byte> binValue = (ArraySegment<byte>)value;
                    int size = Math.Min(binValue.Count, 64);
                    for (int i = 0; i < size; ++i)
                    {
                        sb.AppendFormat("{0:X2}", binValue.Array[binValue.Offset + i]);
                    }
                }
                else
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "{0}:{1}", fieldName, value);
                }

                ++count;
            }
        }
    }
}
