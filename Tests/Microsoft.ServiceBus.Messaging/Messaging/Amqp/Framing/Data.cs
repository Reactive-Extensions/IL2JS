//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    sealed class Data : AmqpDescribed
    {
        public static readonly string Name = "amqp:data:binary";
        public static readonly ulong Code = 0x0000000000000075;

        public Data() : base(Name, Code) { }

        public static ArraySegment<byte> GetEncodedPrefix(int valueLength)
        {
            byte[] buffer = new byte[8] { (byte)FormatCode.Described, (byte)FormatCode.SmallULong, (byte)Data.Code, 0x00, 0x00, 0x00, 0x00, 0x00 };
            int count = 0;
            if (valueLength <= byte.MaxValue)
            {
                buffer[3] = (byte)FormatCode.Binary8;
                buffer[4] = (byte)valueLength;
                count = 5;
            }
            else
            {
                buffer[3] = (byte)FormatCode.Binary32;
                AmqpBitConverter.WriteUInt(buffer, 4, (uint)valueLength);
                count = 8;
            }

            return new ArraySegment<byte>(buffer, 0, count);
        }
        
        public override int GetValueEncodeSize()
        {
            return BinaryEncoding.GetEncodeSize((ArraySegment<byte>)this.Value);
        }

        public override void EncodeValue(ByteBuffer buffer)
        {
            BinaryEncoding.Encode((ArraySegment<byte>)this.Value, buffer);
        }

        public override void DecodeValue(ByteBuffer buffer)
        {
            this.Value = BinaryEncoding.Decode(buffer, 0);
        }

        public override string ToString()
        {
            return "data()";
        }
    }
}
