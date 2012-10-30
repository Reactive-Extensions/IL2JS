//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    using System;
    using Microsoft.ServiceBus.Common;

    unsafe sealed class AmqpBitConverter
    {
        AmqpBitConverter()
        {
        }

        public static sbyte ReadByte(ByteBuffer buffer)
        {
            buffer.EnsureLength(FixedWidth.Byte);
            sbyte data = (sbyte)buffer.Buffer[buffer.Offset];
            buffer.Complete(FixedWidth.Byte);
            return data;
        }

        public static byte ReadUByte(ByteBuffer buffer)
        {
            buffer.EnsureLength(FixedWidth.UByte);
            byte data = buffer.Buffer[buffer.Offset];
            buffer.Complete(FixedWidth.UByte);
            return data;
        }

        public static short ReadShort(ByteBuffer buffer)
        {
            buffer.EnsureLength(FixedWidth.Short);
            short data;
            fixed (byte* p = &buffer.Buffer[buffer.Offset])
            {
                byte* d = (byte*)&data;
                d[0] = p[1];
                d[1] = p[0];
            }

            buffer.Complete(FixedWidth.Short);
            return data;
        }

        public static ushort ReadUShort(ByteBuffer buffer)
        {
            buffer.EnsureLength(FixedWidth.UShort);
            ushort data;
            fixed (byte* p = &buffer.Buffer[buffer.Offset])
            {
                byte* d = (byte*)&data;
                d[0] = p[1];
                d[1] = p[0];
            }

            buffer.Complete(FixedWidth.UShort);
            return data;
        }

        public static int ReadInt(ByteBuffer buffer)
        {
            buffer.EnsureLength(FixedWidth.Int);
            int data;
            fixed (byte* p = &buffer.Buffer[buffer.Offset])
            {
                byte* d = (byte*)&data;
                d[0] = p[3];
                d[1] = p[2];
                d[2] = p[1];
                d[3] = p[0];
            }

            buffer.Complete(FixedWidth.Int);
            return data;
        }

        public static uint ReadUInt(ByteBuffer buffer)
        {
            buffer.EnsureLength(FixedWidth.UInt);
            uint data;
            fixed (byte* p = &buffer.Buffer[buffer.Offset])
            {
                byte* d = (byte*)&data;
                d[0] = p[3];
                d[1] = p[2];
                d[2] = p[1];
                d[3] = p[0];
            }

            buffer.Complete(FixedWidth.UInt);
            return data;
        }

        public static uint ReadUInt(byte[] buffer, int offset, int count)
        {
            Validate(count, FixedWidth.UInt);
            uint data;
            fixed (byte* p = &buffer[offset])
            {
                byte* d = (byte*)&data;
                d[0] = p[3];
                d[1] = p[2];
                d[2] = p[1];
                d[3] = p[0];
            }

            return data;
        }

        public static long ReadLong(ByteBuffer buffer)
        {
            buffer.EnsureLength(FixedWidth.Long);
            long data;
            fixed (byte* p = &buffer.Buffer[buffer.Offset])
            {
                byte* d = (byte*)&data;
                d[0] = p[7];
                d[1] = p[6];
                d[2] = p[5];
                d[3] = p[4];
                d[4] = p[3];
                d[5] = p[2];
                d[6] = p[1];
                d[7] = p[0];
            }

            buffer.Complete(FixedWidth.Long);
            return data;
        }

        public static ulong ReadULong(ByteBuffer buffer)
        {
            buffer.EnsureLength(FixedWidth.ULong);
            ulong data = ReadULong(buffer.Buffer, buffer.Offset, buffer.Length);
            buffer.Complete(FixedWidth.ULong);
            return data;
        }

        public static ulong ReadULong(byte[] buffer, int offset, int count)
        {
            Validate(count, FixedWidth.ULong);
            ulong data;
            fixed (byte* p = &buffer[offset])
            {
                byte* d = (byte*)&data;
                d[0] = p[7];
                d[1] = p[6];
                d[2] = p[5];
                d[3] = p[4];
                d[4] = p[3];
                d[5] = p[2];
                d[6] = p[1];
                d[7] = p[0];
            }

            return data;
        }

        public static float ReadFloat(ByteBuffer buffer)
        {
            buffer.EnsureLength(FixedWidth.Float);
            float data;
            fixed (byte* p = &buffer.Buffer[buffer.Offset])
            {
                byte* d = (byte*)&data;
                d[0] = p[3];
                d[1] = p[2];
                d[2] = p[1];
                d[3] = p[0];
            }

            buffer.Complete(FixedWidth.Float);
            return data;
        }

        public static double ReadDouble(ByteBuffer buffer)
        {
            buffer.EnsureLength(FixedWidth.Double);
            double data;
            fixed (byte* p = &buffer.Buffer[buffer.Offset])
            {
                byte* d = (byte*)&data;
                d[0] = p[7];
                d[1] = p[6];
                d[2] = p[5];
                d[3] = p[4];
                d[4] = p[3];
                d[5] = p[2];
                d[6] = p[1];
                d[7] = p[0];
            }

            buffer.Complete(FixedWidth.Double);
            return data;
        }

        public static Guid ReadUuid(ByteBuffer buffer)
        {
            buffer.EnsureLength(FixedWidth.Uuid);
            Guid data;
            fixed (byte* p = &buffer.Buffer[buffer.Offset])
            {
                byte* d = (byte*)&data;
                d[0] = p[3];
                d[1] = p[2];
                d[2] = p[1];
                d[3] = p[0];

                d[4] = p[5];
                d[5] = p[4];

                d[6] = p[7];
                d[7] = p[6];

                *((ulong*)&d[8]) = *((ulong*)&p[8]);
            }

            buffer.Complete(FixedWidth.Uuid);
            return data;
        }

        public static void WriteByte(ByteBuffer buffer, sbyte data)
        {
            buffer.EnsureSize(FixedWidth.Byte);
            buffer.Buffer[buffer.End] = (byte)data;
            buffer.Append(FixedWidth.Byte);
        }

        public static void WriteUByte(ByteBuffer buffer, byte data)
        {
            buffer.EnsureSize(FixedWidth.UByte);
            buffer.Buffer[buffer.End] = data;
            buffer.Append(FixedWidth.UByte);
        }

        public static void WriteUByte(byte[] buffer, int offset, byte data)
        {
            Validate(buffer.Length - offset, FixedWidth.UByte);
            buffer[offset] = data;
        }

        public static void WriteShort(ByteBuffer buffer, short data)
        {
            buffer.EnsureSize(FixedWidth.Short);
            fixed (byte* d = &buffer.Buffer[buffer.End])
            {
                byte* p = (byte*)&data;
                d[0] = p[1];
                d[1] = p[0];
            }

            buffer.Append(FixedWidth.Short);
        }

        public static void WriteUShort(ByteBuffer buffer, ushort data)
        {
            buffer.EnsureSize(FixedWidth.UShort);
            fixed (byte* d = &buffer.Buffer[buffer.End])
            {
                byte* p = (byte*)&data;
                d[0] = p[1];
                d[1] = p[0];
            }

            buffer.Append(FixedWidth.UShort);
        }

        public static void WriteUShort(byte[] buffer, int offset, ushort data)
        {
            Validate(buffer.Length - offset, FixedWidth.UShort);
            fixed (byte* d = &buffer[offset])
            {
                byte* p = (byte*)&data;
                d[0] = p[1];
                d[1] = p[0];
            }
        }

        public static void WriteInt(ByteBuffer buffer, int data)
        {
            buffer.EnsureSize(FixedWidth.Int);
            fixed (byte* d = &buffer.Buffer[buffer.End])
            {
                byte* p = (byte*)&data;
                d[0] = p[3];
                d[1] = p[2];
                d[2] = p[1];
                d[3] = p[0];
            }

            buffer.Append(FixedWidth.Int);
        }

        public static void WriteUInt(ByteBuffer buffer, uint data)
        {
            buffer.EnsureSize(FixedWidth.UInt);
            fixed (byte* d = &buffer.Buffer[buffer.End])
            {
                byte* p = (byte*)&data;
                d[0] = p[3];
                d[1] = p[2];
                d[2] = p[1];
                d[3] = p[0];
            }

            buffer.Append(FixedWidth.UInt);
        }

        public static void WriteUInt(byte[] buffer, int offset, uint data)
        {
            Validate(buffer.Length - offset, FixedWidth.UInt);
            fixed (byte* d = &buffer[offset])
            {
                byte* p = (byte*)&data;
                d[0] = p[3];
                d[1] = p[2];
                d[2] = p[1];
                d[3] = p[0];
            }
        }

        public static void WriteLong(ByteBuffer buffer, long data)
        {
            buffer.EnsureSize(FixedWidth.Long);
            fixed (byte* d = &buffer.Buffer[buffer.End])
            {
                byte* p = (byte*)&data;
                d[0] = p[7];
                d[1] = p[6];
                d[2] = p[5];
                d[3] = p[4];
                d[4] = p[3];
                d[5] = p[2];
                d[6] = p[1];
                d[7] = p[0];
            }

            buffer.Append(FixedWidth.Long);
        }

        public static void WriteULong(ByteBuffer buffer, ulong data)
        {
            buffer.EnsureSize(FixedWidth.ULong);
            fixed (byte* d = &buffer.Buffer[buffer.End])
            {
                byte* p = (byte*)&data;
                d[0] = p[7];
                d[1] = p[6];
                d[2] = p[5];
                d[3] = p[4];
                d[4] = p[3];
                d[5] = p[2];
                d[6] = p[1];
                d[7] = p[0];
            }

            buffer.Append(FixedWidth.ULong);
        }

        public static void WriteFloat(ByteBuffer buffer, float data)
        {
            buffer.EnsureSize(FixedWidth.Float);
            fixed (byte* d = &buffer.Buffer[buffer.End])
            {
                byte* p = (byte*)&data;
                d[0] = p[3];
                d[1] = p[2];
                d[2] = p[1];
                d[3] = p[0];
            }

            buffer.Append(FixedWidth.Float);
        }

        public static void WriteDouble(ByteBuffer buffer, double data)
        {
            buffer.EnsureSize(FixedWidth.Double);
            fixed (byte* d = &buffer.Buffer[buffer.End])
            {
                byte* p = (byte*)&data;
                d[0] = p[7];
                d[1] = p[6];
                d[2] = p[5];
                d[3] = p[4];
                d[4] = p[3];
                d[5] = p[2];
                d[6] = p[1];
                d[7] = p[0];
            }

            buffer.Append(FixedWidth.Double);
        }

        public static void WriteUuid(ByteBuffer buffer, Guid data)
        {
            buffer.EnsureSize(FixedWidth.Uuid);
            fixed (byte* d = &buffer.Buffer[buffer.End])
            {
                byte* p = (byte*)&data;
                d[0] = p[3];
                d[1] = p[2];
                d[2] = p[1];
                d[3] = p[0];

                d[4] = p[5];
                d[5] = p[4];

                d[6] = p[7];
                d[7] = p[6];

                *((ulong*)&d[8]) = *((ulong*)&p[8]);
            }

            buffer.Append(FixedWidth.Uuid);
        }

        static void Validate(int bufferSize, int dataSize)
        {
            if (bufferSize < dataSize)
            {
                throw new AmqpException(AmqpError.DecodeError);
            }
        }
    }
}
