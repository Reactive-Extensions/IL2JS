//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    using System;

    sealed class UuidEncoding : EncodingBase
    {
        public UuidEncoding()
            : base(FormatCode.Uuid)
        {
        }

        public static int GetEncodeSize(Guid? value)
        {
            return value.HasValue ? FixedWidth.UuidEncoded : FixedWidth.NullEncoded;
        }

        public static void Encode(Guid? value, ByteBuffer buffer)
        {
            if (value.HasValue)
            {
                AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.Uuid);
                AmqpBitConverter.WriteUuid(buffer, value.Value);
            }
            else
            {
                AmqpEncoding.EncodeNull(buffer);
            }
        }

        public static Guid? Decode(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == 0 && (formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return null;
            }

            return AmqpBitConverter.ReadUuid(buffer);
        }

        public override int GetObjectEncodeSize(object value, bool arrayEncoding)
        {
            if (arrayEncoding)
            {
                return FixedWidth.Uuid;
            }
            else
            {
                return UuidEncoding.GetEncodeSize((Guid)value);
            }
        }

        public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
        {
            if (arrayEncoding)
            {
                AmqpBitConverter.WriteUuid(buffer, (Guid)value);
            }
            else
            {
                UuidEncoding.Encode((Guid)value, buffer);
            }
        }

        public override object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
        {
            return UuidEncoding.Decode(buffer, formatCode);
        }
    }
}
