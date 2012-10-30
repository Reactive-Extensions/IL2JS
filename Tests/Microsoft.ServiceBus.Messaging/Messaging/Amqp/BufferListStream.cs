//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    sealed class BufferListStream : Stream, ICloneable
    {
        ArraySegment<byte>[] bufferList;
        int readArray;
        int readOffset;
        long length;
        long position;
        bool disposed;

        public BufferListStream(ArraySegment<byte>[] bufferList)
        {
            this.bufferList = bufferList;
            for (int i = 0; i < this.bufferList.Length; ++i)
            {
                this.length += this.bufferList[i].Count;
            }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get
            {
                this.ThrowIfDisposed();
                return this.length;
            }
        }

        public override long Position
        {
            get
            {
                this.ThrowIfDisposed();
                return this.position;
            }

            set
            {
                this.ThrowIfDisposed();
                this.SetPosition(value);
            }
        }

        public object Clone()
        {
            this.ThrowIfDisposed();
            return new BufferListStream(this.bufferList);
        }

        public override void Flush()
        {
            throw new InvalidOperationException();
        }

        public override int ReadByte()
        {
            this.ThrowIfDisposed();
            if (this.readArray == this.bufferList.Length)
            {
                return -1;
            }

            ArraySegment<byte> segment = this.bufferList[this.readArray];
            int value = segment.Array[segment.Offset + this.readOffset];
            this.Advance(1, segment.Count);
            return value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            this.ThrowIfDisposed();
            if (this.readArray == this.bufferList.Length)
            {
                return 0;
            }

            int bytesRead = 0;
            while (count > 0 && this.readArray < this.bufferList.Length)
            {
                ArraySegment<byte> segment = this.bufferList[this.readArray];
                int bytesRemaining = segment.Count - this.readOffset;
                int bytesToCopy = Math.Min(bytesRemaining, count);
                Buffer.BlockCopy(segment.Array, segment.Offset + this.readOffset, buffer, offset, bytesToCopy);

                this.Advance(bytesToCopy, segment.Count);
                count -= bytesToCopy;
                offset += bytesToCopy;
                bytesRead += bytesToCopy;
            }

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            this.ThrowIfDisposed();
            long pos = 0;
            if (origin == SeekOrigin.Begin)
            {
                pos = offset;
            }
            else if (origin == SeekOrigin.Current)
            {
                pos += this.position + offset;
            }
            else if (origin == SeekOrigin.End)
            {
                pos = this.length + offset;
            }

            this.SetPosition(pos);
            return pos;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public ArraySegment<byte> ReadBytes(int count)
        {
            this.ThrowIfDisposed();
            if (this.readArray == this.bufferList.Length)
            {
                return default(ArraySegment<byte>);
            }

            ArraySegment<byte> segment = this.bufferList[this.readArray];
            if (segment.Count - this.readOffset >= count)
            {
                int segmentCount = segment.Count;
                segment = new ArraySegment<byte>(segment.Array, segment.Offset + this.readOffset, count);
                this.Advance(count, segmentCount);
                return segment;
            }
            else
            {
                count = Math.Min(count, (int)(this.length - this.position));
                byte[] buffer = new byte[count];
                this.Read(buffer, 0, count);
                segment = new ArraySegment<byte>(buffer);
            }

            return segment;
        }

        public ArraySegment<byte>[] ReadBuffers(int count, bool advance, out bool more)
        {
            this.ThrowIfDisposed();
            more = false;
            if (this.readArray == this.bufferList.Length)
            {
                return null;
            }

            List<ArraySegment<byte>> buffers = new List<ArraySegment<byte>>();
            int readArrayCopy = this.readArray;
            int readOffsetCopy = this.readOffset;
            long positionCopy = this.position;
            while (count > 0 && this.readArray < this.bufferList.Length)
            {
                ArraySegment<byte> segment = this.bufferList[this.readArray];
                int bytesRemaining = segment.Count - this.readOffset;
                int bytesToCopy = Math.Min(bytesRemaining, count);
                buffers.Add(new ArraySegment<byte>(segment.Array, segment.Offset + this.readOffset, bytesToCopy));
                this.Advance(bytesToCopy, segment.Count);
                count -= bytesToCopy;
            }

            more = this.readArray < this.bufferList.Length;

            if (!advance)
            {
                this.readArray = readArrayCopy;
                this.readOffset = readOffsetCopy;
                this.position = positionCopy;
            }

            return buffers.ToArray();
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!this.disposed && disposing)
                {
                    this.bufferList = null;
                    this.disposed = true;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }
        }

        void SetPosition(long pos)
        {
            if (pos < 0)
            {
                throw new ArgumentOutOfRangeException("position");
            }

            this.position = pos;
            int arrayIndex = 0;
            while (arrayIndex < this.bufferList.Length && pos > 0)
            {
                if (pos >= this.bufferList[arrayIndex].Count)
                {
                    pos -= this.bufferList[arrayIndex].Count;
                    ++arrayIndex;
                }
                else
                {
                    break;
                }
            }

            this.readArray = arrayIndex;
            this.readOffset = (int)pos;
        }

        void Advance(int count, int segmentCount)
        {
            if (count > segmentCount)
            {
                throw new ArgumentOutOfRangeException("count");
            }

            this.position += count;
            this.readOffset += count;
            if (this.readOffset == segmentCount)
            {
                ++this.readArray;
                this.readOffset = 0;
            }
        }
    }
}