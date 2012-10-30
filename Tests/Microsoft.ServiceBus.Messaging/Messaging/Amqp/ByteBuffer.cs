//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.IO;
    using Microsoft.ServiceBus.Common;

    // This class is not thread safe
    abstract class ByteBuffer : IDisposable
    {
        static InternalBufferManager BufferPool = InternalBufferManager.Create(64 * 1024 * 1024, int.MaxValue);

        // | - - - - - - - - - - - - - - - - - - - - - - - |
        // |   consumed  |  length        |  size          |
        // s             p                e                c
        byte[] buffer;
        int start;
        int position;
        int end;
        int capacity;
        bool disposed;

        public static ByteBuffer Wrap(byte[] buffer)
        {
            return new BufferedByteBuffer(buffer, 0, 0, 0, buffer.Length, false);
        }

        public static ByteBuffer Wrap(ArraySegment<byte> array)
        {
            return new BufferedByteBuffer(array.Array, array.Offset, array.Offset, array.Offset + array.Count, array.Offset + array.Count, false);
        }

        public static ByteBuffer Wrap(byte[] buffer, int offset, int count)
        {
            return new BufferedByteBuffer(buffer, offset, offset, offset + count, offset + count, false);
        }

        public static ByteBuffer Wrap(byte[] buffer, int offset, int count, int capacity)
        {
            return new BufferedByteBuffer(buffer, offset, offset, offset + count, capacity, false);
        }

        public static ByteBuffer Wrap(int initialCapacity)
        {
            byte[] buffer = BufferPool.TakeBuffer(initialCapacity);
            ByteBuffer byteBuffer = new BufferedByteBuffer(buffer, 0, 0, 0, buffer.Length, true);
            return byteBuffer;
        }

        public static ByteBuffer Wrap(Stream stream)
        {
            Fx.Assert(stream.CanRead, "Stream is not readable");
            return new InputStreamByteBuffer(stream);
        }

        public static void Return(byte[] buffer)
        {
            BufferPool.ReturnBuffer(buffer);
        }

        public byte[] Buffer
        {
            get { return this.buffer; }
        }

        public int Capacity 
        {
            get { return this.capacity; } 
        }

        public int Offset 
        { 
            get { return this.position; } 
        }

        public int Size 
        { 
            get { return this.capacity - this.end; } 
        }

        public int Length 
        {
            get { return this.end - this.position; } 
        }

        public int End
        {
            get { return this.end; }
        }

        public ArraySegment<byte> Array
        {
            get { return new ArraySegment<byte>(this.buffer, this.position, this.Length); }
        }

        // pre-write call
        public abstract void EnsureSize(int size);

        // after-write call
        public abstract void Append(int size);

        // pre-read call
        public abstract void EnsureLength(int length);

        // after-read call
        public abstract void Complete(int size);

        public abstract void ReadBytes(byte[] data, int offset, int count);

        public abstract ArraySegment<byte> GetBytes(int count);

        public abstract void WriteBytes(byte[] data, int offset, int count);

        public abstract void Reset();

        public void Dispose()
        {
            if (!this.disposed)
            {
                this.disposed = true;
                this.OnDispose(true);
                GC.SuppressFinalize(this);
            }
        }

        protected virtual void OnDispose(bool disposing)
        {
        }

        sealed class BufferedByteBuffer : ByteBuffer
        {
            readonly bool autoGrow;

            public BufferedByteBuffer(byte[] buffer, int start, int position, int end, int capacity, bool autoGrow)
            {
                this.buffer = buffer;
                this.start = start;
                this.position = position;
                this.end = end;
                this.capacity = capacity;
                this.autoGrow = autoGrow;
            }

            public override void EnsureSize(int size)
            {
                if (this.Size < size)
                {
                    if (this.autoGrow)
                    {
                        byte[] newBuffer = BufferPool.TakeBuffer(this.capacity * 2);
                        System.Buffer.BlockCopy(this.buffer, this.start, newBuffer, 0, this.Length);
                        BufferPool.ReturnBuffer(this.buffer);
                        this.buffer = newBuffer;
                        this.capacity = newBuffer.Length;
                    }
                    else
                    {
                        throw new InvalidOperationException("EnsureSize");
                    }
                }
            }

            public override void Append(int size)
            {
                Fx.Assert(size >= 0, "size must be positive.");
                Fx.Assert((this.end + size) <= this.capacity, "Append size too large.");
                this.end += size;
            }

            public override void EnsureLength(int length)
            {
                if (this.Length < length)
                {
                    throw new AmqpException(AmqpError.FramingError, "buffer.length");
                }
            }

            public override void Complete(int size)
            {
                Fx.Assert(size >= 0, "size must be positive.");
                Fx.Assert((this.position + size) <= this.end, "Complete size too large.");
                this.position += size;
            }

            public override void ReadBytes(byte[] data, int offset, int count)
            {
                this.EnsureLength(count);
                System.Buffer.BlockCopy(this.buffer, this.position, data, offset, count);
                this.position += count;
            }

            public override void WriteBytes(byte[] data, int offset, int count)
            {
                this.EnsureSize(count);
                System.Buffer.BlockCopy(data, offset, this.buffer, this.end, count);
                this.end += count;
            }

            public override ArraySegment<byte> GetBytes(int count)
            {
                this.EnsureLength(count);
                ArraySegment<byte> bytes = new ArraySegment<byte>(this.buffer, this.position, count);
                this.position += count;
                return bytes;
            }

            public override void Reset()
            {
                this.position = this.start;
                this.end = this.start;
            }
        }

        sealed class InputStreamByteBuffer : ByteBuffer
        {
            Stream inputStream;

            public InputStreamByteBuffer(Stream stream)
            {
                Fx.Assert(stream.CanRead, "Stream is not readable");
                this.inputStream = stream;
                this.buffer = BufferPool.TakeBuffer(512);
                this.capacity = this.buffer.Length;
            }

            public override void EnsureSize(int size)
            {
                throw new InvalidOperationException();
            }

            public override void Append(int size)
            {
                throw new InvalidOperationException("Append");
            }

            public override void EnsureLength(int length)
            {
                if (this.Length < length)
                {
                    int delta = length - this.Length;
                    if (delta > this.Size && this.position > this.start && this.end > this.position)
                    {
                        // shift existing data left to the begining
                        System.Buffer.BlockCopy(this.buffer, this.position, this.buffer, 0, this.Length);
                        this.end = this.Length;
                        this.position = 0;
                    }

                    int count = this.inputStream.Read(this.buffer, this.end, Math.Min(this.Size, delta));
                    this.end += count;
                }
            }

            public override void Complete(int size)
            {
                this.position += size;
                if (this.position == this.end)
                {
                    this.position = this.end = 0;
                }
                else if (this.position > this.end)
                {
                    throw new InvalidOperationException("complete");
                }
            }

            public override void ReadBytes(byte[] data, int offset, int count)
            {
                int bytesRead = 0;
                if (this.end > this.position)
                {
                    bytesRead = Math.Min(count, this.Length);
                    System.Buffer.BlockCopy(this.buffer, this.position, data, offset, bytesRead);
                    this.Complete(bytesRead);
                }

                if (bytesRead < count)
                {
                    bytesRead += this.inputStream.Read(data, offset, count - bytesRead);
                }

                if (bytesRead != count)
                {
                    throw new AmqpException(AmqpError.DecodeError, "stream.eof");
                }
            }

            public override void WriteBytes(byte[] data, int offset, int count)
            {
                throw new InvalidOperationException();
            }

            public override ArraySegment<byte> GetBytes(int count)
            {
                byte[] bytes = new byte[count];
                this.ReadBytes(bytes, 0, count);
                return new ArraySegment<byte>(bytes, 0, count);
            }

            public override void Reset()
            {
                throw new InvalidOperationException();
            }

            protected override void OnDispose(bool disposing)
            {
                this.inputStream = null;
            }
        }
    }
}
