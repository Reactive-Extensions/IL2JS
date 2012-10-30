//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    using System;

    sealed class TimeStampEncoding : EncodingBase
    {
        public TimeStampEncoding()
            : base(FormatCode.TimeStamp)
        {
        }

        public static int GetEncodeSize(DateTime? value)
        {
            return value.HasValue ? FixedWidth.TimeStampEncoded : FixedWidth.NullEncoded;
        }

        public static void Encode(DateTime? value, ByteBuffer buffer)
        {
            if (value.HasValue)
            {
                AmqpBitConverter.WriteUByte(buffer, (byte)FormatCode.TimeStamp);
                AmqpBitConverter.WriteLong(buffer, TimeStampEncoding.GetMilliseconds(value.Value));
            }
            else
            {
                AmqpEncoding.EncodeNull(buffer);
            }
        }

        public static DateTime? Decode(ByteBuffer buffer, FormatCode formatCode)
        {
            if (formatCode == 0 && (formatCode = AmqpEncoding.ReadFormatCode(buffer)) == FormatCode.Null)
            {
                return null;
            }

            long millSeconds = AmqpBitConverter.ReadLong(buffer);
            DateTime dt = AmqpConstants.StartOfEpoch + TimeSpan.FromMilliseconds(millSeconds);
            return dt;
        }

        public override int GetObjectEncodeSize(object value, bool arrayEncoding)
        {
            if (arrayEncoding)
            {
                return FixedWidth.TimeStamp;
            }
            else
            {
                return TimeStampEncoding.GetEncodeSize((DateTime)value);
            }
        }

        public override void EncodeObject(object value, bool arrayEncoding, ByteBuffer buffer)
        {
            if (arrayEncoding)
            {
                AmqpBitConverter.WriteLong(buffer, TimeStampEncoding.GetMilliseconds((DateTime)value));
            }
            else
            {
                TimeStampEncoding.Encode((DateTime)value, buffer);
            }
        }

        public override object DecodeObject(ByteBuffer buffer, FormatCode formatCode)
        {
            return TimeStampEncoding.Decode(buffer, formatCode);
        }

        static long GetMilliseconds(DateTime value)
        {
            DateTime utcValue = value.ToUniversalTime();
            double millisends = (utcValue - AmqpConstants.StartOfEpoch).TotalMilliseconds;
            return (long)millisends;
        }
    }
}
