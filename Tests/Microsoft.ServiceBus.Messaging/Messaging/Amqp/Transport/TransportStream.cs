//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
    using System;
    using System.IO;
    using System.Threading;

    /// <summary>
    /// A stream designed to be used by SslStream. Not thread safe. Not for general purpose uses.
    /// </summary>
    sealed class TransportStream : Stream
    {
        readonly TransportBase transport;
        readonly ReadAsyncResult readResult;
        readonly WriteAsyncResult writeResult;

        public TransportStream(TransportBase transport)
        {
            this.transport = transport;
            this.readResult = new ReadAsyncResult(this.transport);
            this.writeResult = new WriteAsyncResult(this.transport);
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { throw new InvalidOperationException(); }
        }

        public override long Position
        {
            get
            {
                throw new InvalidOperationException();
            }

            set
            {
                throw new InvalidOperationException();
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // only supports async read
            throw new InvalidOperationException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            // only supports async write
            throw new InvalidOperationException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new InvalidOperationException();
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return this.writeResult.Begin(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            this.writeResult.End(asyncResult);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return this.readResult.Begin(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return this.readResult.End(asyncResult);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.transport.Abort();
            }
        }

        /// <summary>
        /// An async result that can be reused, given that the read is single threaded.
        /// </summary>
        sealed class ReadAsyncResult : IAsyncResult
        {
            readonly TransportBase transport;
            readonly TransportAsyncCallbackArgs readArgs;
            readonly byte[] readBuffer;
            int bufferOffset;
            int bufferEnd;
            int reading;

            byte[] buffer;
            int offset;
            int count;
            AsyncCallback callback;
            object asyncState;
            bool completedSynchronously;

            public ReadAsyncResult(TransportBase transport)
            {
                this.transport = transport;
                this.readBuffer = new byte[AmqpConstants.AsyncBufferSize];
                this.readArgs = new TransportAsyncCallbackArgs();
                this.readArgs.SetBuffer(this.readBuffer, 0, this.readBuffer.Length);
                this.readArgs.CompletedCallback = OnReadComplete;
                this.readArgs.UserToken = this;
            }

            public IAsyncResult Begin(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                if (Interlocked.Exchange(ref this.reading, 1) == 1)
                {
                    throw new InvalidOperationException(SRClient.AsyncResultInUse);
                }

                this.buffer = buffer;
                this.offset = offset;
                this.count = count;
                this.callback = callback;
                this.asyncState = state;
                this.completedSynchronously = false;

                if (this.bufferOffset < this.bufferEnd)
                {
                    this.completedSynchronously = true;
                }
                else
                {
                    if (!this.transport.ReadAsync(this.readArgs))
                    {
                        OnReadComplete(this.readArgs);
                    }
                }

                if (this.completedSynchronously)
                {
                    this.Complete();
                }

                return this;
            }

            public int End(IAsyncResult result)
            {
                int returnCount = this.count;

                this.buffer = null;
                this.offset = this.count = 0;
                this.callback = null;
                this.asyncState = null;

                if (Interlocked.Exchange(ref this.reading, 0) == 0)
                {
                    throw new InvalidOperationException(SRClient.AsyncResultNotInUse);
                }

                return returnCount;
            }

            object IAsyncResult.AsyncState 
            {
                get { return this.asyncState; }
            }

            WaitHandle IAsyncResult.AsyncWaitHandle
            {
                get { throw new InvalidOperationException(); }
            }

            bool IAsyncResult.CompletedSynchronously 
            {
                get { return this.completedSynchronously; }
            }

            bool IAsyncResult.IsCompleted 
            {
                get { return this.reading == 0; }
            }

            static void OnReadComplete(TransportAsyncCallbackArgs args)
            {
                ReadAsyncResult thisPtr = (ReadAsyncResult)args.UserToken;
                thisPtr.bufferOffset = 0;
                thisPtr.bufferEnd = args.BytesTransfered;
                thisPtr.completedSynchronously = args.CompletedSynchronously;
                thisPtr.Complete();
            }

            void Complete()
            {
                this.count = Math.Min(this.count, this.bufferEnd - this.bufferOffset);
                Buffer.BlockCopy(this.readBuffer, this.bufferOffset, this.buffer, this.offset, this.count);
                this.bufferOffset += this.count;
                if (this.callback != null)
                {
                    this.callback(this);
                }
            }
        }

        /// <summary>
        /// An async result that can be reused, given that the write is single threaded.
        /// </summary>
        sealed class WriteAsyncResult : IAsyncResult
        {
            readonly TransportBase transport;
            readonly TransportAsyncCallbackArgs args;
            AsyncCallback callback;
            object asyncState;
            bool completedSynchronously;
            int writing;

            public WriteAsyncResult(TransportBase transport)
            {
                this.transport = transport;
                this.args = new TransportAsyncCallbackArgs();
                this.args.CompletedCallback = OnWriteComplete;
                this.args.UserToken = this;
            }

            public IAsyncResult Begin(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
            {
                if (Interlocked.Exchange(ref this.writing, 1) == 1)
                {
                    throw new InvalidOperationException(SRClient.AsyncResultInUse);
                }

                this.callback = callback;
                this.asyncState = state;
                this.args.SetBuffer(buffer, offset, count);
                if (!this.transport.WriteAsync(this.args))
                {
                    this.Complete();
                }

                return this;
            }

            public void End(IAsyncResult result)
            {
                this.args.SetBuffer(null, 0, 0);
                this.callback = null;
                this.asyncState = null;
                if (Interlocked.Exchange(ref this.writing, 0) == 0)
                {
                    throw new InvalidOperationException(SRClient.AsyncResultNotInUse);
                }
            }

            object IAsyncResult.AsyncState
            {
                get { return this.asyncState; }
            }

            WaitHandle IAsyncResult.AsyncWaitHandle
            {
                get { throw new InvalidOperationException(); }
            }

            bool IAsyncResult.CompletedSynchronously
            {
                get { return this.completedSynchronously; }
            }

            bool IAsyncResult.IsCompleted
            {
                get { return this.writing == 0; }
            }

            static void OnWriteComplete(TransportAsyncCallbackArgs args)
            {
                WriteAsyncResult thisPtr = (WriteAsyncResult)args.UserToken;
                thisPtr.completedSynchronously = args.CompletedSynchronously;
                thisPtr.Complete();
            }

            void Complete()
            {
                if (this.callback != null)
                {
                    this.callback(this);
                }
            }
        }
    }
}
