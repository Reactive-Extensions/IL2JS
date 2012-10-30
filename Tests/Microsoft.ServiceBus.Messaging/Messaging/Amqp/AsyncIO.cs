//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using Microsoft.ServiceBus.Common;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
    using Microsoft.ServiceBus.Messaging.Amqp.Transport;

    sealed class AsyncIO : AmqpObject
    {
        readonly TransportBase transport;
        readonly AsyncReader reader;
        readonly AsyncWriter writer;
        readonly Action<Exception> faultHandler;

        public AsyncIO(
            int bufferSize,
            TransportBase transport,
            Action<ByteBuffer> receiveBufferHandler,
            Action<Exception> faultHandler)
        {
            Fx.Assert(transport != null, "transport required");
            Fx.Assert(receiveBufferHandler != null, "receiveBufferHandler required");
            this.transport = transport;
            this.faultHandler = faultHandler;
            this.reader = new AsyncReader(this, bufferSize, receiveBufferHandler);
            this.writer = new AsyncWriter(this, bufferSize);
        }

        public AsyncWriter Writer
        {
            get { return this.writer; }
        }

        protected override string Type
        {
            get { return "async-io"; }
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            this.OpenInternal();
            this.State = AmqpObjectState.Opened;
        }

        protected override bool OpenInternal()
        {
            this.reader.StartReading();
            return true;
        }

        protected override bool CloseInternal()
        {
            return this.writer.TryClose();
        }

        protected override void AbortInternal()
        {
            this.transport.Abort();
        }

        void OnWriterClosed()
        {
            this.CompleteClose(false, null);
            this.transport.Close();
        }

        void OnIoFault(Exception exception)
        {
            this.faultHandler(exception);
        }

        /// <summary>
        /// A reader that pumps data from the transport and hands it over to the callback (push).
        /// </summary>
        public sealed class AsyncReader
        {
            readonly AsyncIO parent;
            readonly Action<ByteBuffer> receiveBufferHandler;
            readonly TransportAsyncCallbackArgs readAsyncEventArgs;
            readonly ByteBuffer readBuffer;

            public AsyncReader(
                AsyncIO parent,
                int readBufferSize,
                Action<ByteBuffer> receiveBufferHandler)
            {
                this.parent = parent;
                this.receiveBufferHandler = receiveBufferHandler;

                this.readAsyncEventArgs = new TransportAsyncCallbackArgs();
                this.readAsyncEventArgs.CompletedCallback = this.OnBufferReadComplete;
                this.readBuffer = ByteBuffer.Wrap(new byte[readBufferSize]);
            }

            public void StartReading()
            {
                this.ReadBuffer(this.readBuffer);
            }

            void ReadBuffer(ByteBuffer buffer)
            {
                try
                {
                    while (this.parent.State != AmqpObjectState.End)
                    {
                        this.readAsyncEventArgs.SetBuffer(buffer.Buffer, buffer.Length, buffer.Size);
                        this.readAsyncEventArgs.UserToken = buffer;
                        if (!this.parent.transport.ReadAsync(this.readAsyncEventArgs))
                        {
                            this.OnBufferReadComplete(this.readAsyncEventArgs);
                            this.AdjustBufferForNextRead(buffer);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch (Exception exception)
                {
                    this.parent.OnIoFault(exception);
                }
            }

            void OnBufferReadComplete(TransportAsyncCallbackArgs args)
            {
                ByteBuffer buffer = (ByteBuffer)args.UserToken;
                if (args.Exception != null)
                {
                    this.parent.OnIoFault(args.Exception);
                }
                else if (args.BytesTransfered == 0)
                {
                    // connection closed by other side
                    this.parent.OnIoFault(new AmqpException(AmqpError.ConnectionForced));
                }
                else
                {
                    buffer.Append(args.BytesTransfered);
                    this.receiveBufferHandler(buffer);

                    if (!args.CompletedSynchronously)
                    {
                        this.AdjustBufferForNextRead(buffer);
                        this.ReadBuffer(buffer);
                    }
                }
            }

            void AdjustBufferForNextRead(ByteBuffer buffer)
            {
                if (buffer.Length > 0)
                {
                    int oldLength = buffer.Length;
                    Buffer.BlockCopy(
                        buffer.Buffer,
                        buffer.Offset,
                        buffer.Buffer,
                        0,
                        oldLength);
                    buffer.Reset();
                    buffer.Append(oldLength);
                }
                else
                {
                    buffer.Reset();
                }
            }
        }

        /// <summary>
        /// A reader that reads specified bytes and notifies caller upon completion (pull).
        /// </summary>
        public sealed class AsyncBufferReader
        {
            readonly TransportBase transport;
            readonly Action<TransportAsyncCallbackArgs> onReadComplete;

            public AsyncBufferReader(TransportBase transport)
            {
                this.transport = transport;
                this.onReadComplete = this.OnReadComplete;
            }

            public void ReadBuffer(TransportAsyncCallbackArgs args)
            {
                TransportAsyncCallbackArgs wrapperArgs = new TransportAsyncCallbackArgs();
                wrapperArgs.SetBuffer(args.Buffer, args.Offset, args.Count);
                wrapperArgs.UserToken = args;
                wrapperArgs.CompletedCallback = this.onReadComplete;
                this.Read(wrapperArgs);
            }

            void Read(TransportAsyncCallbackArgs args)
            {
                while (true)
                {
                    if (this.transport.ReadAsync(args))
                    {
                        break;
                    }

                    if (this.HandleReadComplete(args))
                    {
                        break;
                    }
                }
            }

            bool HandleReadComplete(TransportAsyncCallbackArgs args)
            {
                bool done = true;
                Exception exception = null;
                if (args.Exception != null)
                {
                    exception = args.Exception;
                }
                else if (args.BytesTransfered == 0)
                {
                    exception = new ObjectDisposedException(this.transport.ToString());
                }
                else if (args.BytesTransfered < args.Count)
                {
                    int bytesLeft = args.Count - args.BytesTransfered;
                    args.SetBuffer(args.Buffer, args.Offset + args.BytesTransfered, args.Count - args.BytesTransfered);
                    done = false;
                }

                if (done)
                {
                    TransportAsyncCallbackArgs innerArgs = (TransportAsyncCallbackArgs)args.UserToken;
                    innerArgs.Exception = exception;
                    innerArgs.BytesTransfered = innerArgs.Count;
                    innerArgs.CompletedCallback(innerArgs);
                }

                return done;
            }

            void OnReadComplete(TransportAsyncCallbackArgs args)
            {
                if (!this.HandleReadComplete(args) && !args.CompletedSynchronously)
                {
                    this.Read(args);
                }
            }
        }

        /// <summary>
        /// A reader that reads AMQP frame buffers. Not thread safe.
        /// </summary>
        public sealed class FrameBufferReader
        {
            readonly TransportBase transport;
            readonly Action<TransportAsyncCallbackArgs> onSizeComplete;
            readonly Action<TransportAsyncCallbackArgs> onFrameComplete;
            readonly byte[] sizeBuffer;

            public FrameBufferReader(TransportBase transport)
            {
                this.transport = transport;
                this.onSizeComplete = this.OnReadSizeComplete;
                this.onFrameComplete = this.OnReadFrameComplete;
                this.sizeBuffer = new byte[FixedWidth.UInt];
            }

            public void Read(Action<ByteBuffer, Exception> callback)
            {
                TransportAsyncCallbackArgs args = new TransportAsyncCallbackArgs();
                args.UserToken = callback;
                args.SetBuffer(this.sizeBuffer, 0, this.sizeBuffer.Length);
                args.CompletedCallback = this.onSizeComplete;
                this.ReadCore(args);
            }

            void ReadCore(TransportAsyncCallbackArgs args)
            {
                while (!this.transport.ReadAsync(args))
                {
                    if (this.HandleReadComplete(args))
                    {
                        break;
                    }
                }
            }

            void OnReadSizeComplete(TransportAsyncCallbackArgs args)
            {
                if (!this.HandleReadComplete(args))
                {
                    this.ReadCore(args);
                }
            }

            void OnReadFrameComplete(TransportAsyncCallbackArgs args)
            {
                if (!this.HandleReadComplete(args))
                {
                    this.ReadCore(args);
                }
            }

            bool HandleReadComplete(TransportAsyncCallbackArgs args)
            {
                bool completed = true;
                Exception exception = null;
                if (args.Exception != null)
                {
                    exception = args.Exception;
                }
                else if (args.BytesTransfered == 0)
                {
                    exception = new ObjectDisposedException(this.transport.ToString());
                }
                else if (args.BytesTransfered < args.Count)
                {
                    args.SetBuffer(args.Buffer, args.Offset + args.BytesTransfered, args.Count - args.BytesTransfered);
                    completed = false;
                }

                if (completed)
                {
                    if (exception != null || object.ReferenceEquals(args.CompletedCallback, this.onFrameComplete))
                    {
                        Action<ByteBuffer, Exception> callback = (Action<ByteBuffer, Exception>)args.UserToken;
                        ByteBuffer buffer = null;
                        if (exception == null)
                        {
                            buffer = ByteBuffer.Wrap(args.Buffer, 0, args.Buffer.Length);
                        }

                        callback(buffer, exception);
                    }
                    else
                    {
                        // read size completed ok
                        uint size = AmqpBitConverter.ReadUInt(this.sizeBuffer, 0, this.sizeBuffer.Length);
                        byte[] frameBuffer = new byte[size];
                        Buffer.BlockCopy(this.sizeBuffer, 0, frameBuffer, 0, this.sizeBuffer.Length);
                        args.SetBuffer(frameBuffer, this.sizeBuffer.Length, (int)size - this.sizeBuffer.Length);
                        args.CompletedCallback = this.onFrameComplete;
                        completed = false;
                    }
                }

                return completed;
            }
        }

        /// <summary>
        /// A writer that writes buffers. Buffer writes may be batched.
        /// </summary>
        public sealed class AsyncWriter
        {
            readonly AsyncIO parent;
            readonly TransportBase transport;
            readonly TransportAsyncCallbackArgs writeAsyncEventArgs;
            readonly ByteBuffer writeBuffer;
            readonly object syncRoot;
            WriteRequest firstRequest;
            WriteRequest lastRequest;
            int state;  //0: idle, 1: busy, 2: closed

            public AsyncWriter(AsyncIO parent, int bufferSize)
            {
                this.parent = parent;
                this.transport = parent.transport;
                this.writeAsyncEventArgs = new TransportAsyncCallbackArgs();
                this.writeAsyncEventArgs.CompletedCallback = this.WriteCompleteCallback;
                this.writeBuffer = ByteBuffer.Wrap(new byte[bufferSize]);
                this.syncRoot = new object();
            }

            public void WriteBuffer(ArraySegment<byte> buffer, ArraySegment<byte>[] payload, Action<object> callback, object state)
            {
                WriteRequest request = WriteRequest.Get(buffer, payload, callback, state);
                bool doWrite = true;
                lock (this.syncRoot)
                {
                    if (this.state == 2)
                    {
                        doWrite = false;
                    }
                    else
                    {
                        this.AddRequest(request);
                        if (this.state == 1)
                        {
                            doWrite = false;
                        }
                        else
                        {
                            this.state = 1;
                        }
                    }
                }

                if (doWrite)
                {
                    this.BuildWriteBuffer();
                    this.WriteBufferInternal();
                }
            }

            public bool TryClose()
            {
                bool closed = true;
                lock (this.syncRoot)
                {
                    if (this.state != 2)
                    {
                        this.state = 2;
                        closed = this.firstRequest == null;
                    }
                }

                return closed;
            }

            void AddRequest(WriteRequest request)
            {
                if (this.lastRequest == null)
                {
                    this.firstRequest = this.lastRequest = request;
                }
                else
                {
                    this.lastRequest.Next = request;
                    this.lastRequest = request;
                }
            }

            // This function should be called in busy state (state == 1)
            void BuildWriteBuffer()
            {
                Fx.Assert(this.state == 1, "Should be busy at this time");
                Fx.Assert(this.writeBuffer.Length == 0, "Cannot have payload in the write buffer at this point");
                Fx.Assert(this.firstRequest != null && this.lastRequest != null, "Must have buffer to write at this time");
                int bufferSize = this.writeBuffer.Capacity;
                // take a snapshot so we don't have to lock
                WriteRequest first = this.firstRequest;
                WriteRequest last = this.lastRequest;
                WriteRequest request = first;
                WriteRequest lastCompleted = null;
                while (true)
                {
                    if (!request.WriteTo(this.writeBuffer))
                    {
                        break;
                    }

                    lastCompleted = request;
                    request = request.Next;
                    if (lastCompleted == last)
                    {
                        break;
                    }
                }
                
                this.writeAsyncEventArgs.SetBuffer(this.writeBuffer.Buffer, this.writeBuffer.Offset, this.writeBuffer.Length);
                if (lastCompleted != null)
                {
                    this.writeAsyncEventArgs.UserToken = first;
                    lock (this.syncRoot)
                    {
                        this.firstRequest = lastCompleted.Next;
                        lastCompleted.Next = null;
                        if (this.firstRequest == null)
                        {
                            this.lastRequest = null;
                        }
                    }
                }
            }

            void WriteBufferInternal()
            {
                while (!this.transport.WriteAsync(this.writeAsyncEventArgs) &&      // completed synchronously
                    this.HandleWriteBufferComplete(this.writeAsyncEventArgs) &&     // succeeded
                    this.ShouldContinue())                                          // still open and more requests
                {
                    this.BuildWriteBuffer();
                }
            }

            void WriteCompleteCallback(TransportAsyncCallbackArgs args)
            {
                if (this.HandleWriteBufferComplete(args) && this.ShouldContinue())
                {
                    this.BuildWriteBuffer();
                    this.WriteBufferInternal();
                }
            }

            bool ShouldContinue()
            {
                bool shouldContinue = true;
                lock (this.syncRoot)
                {
                    if (this.firstRequest == null)
                    {
                        if (this.state == 2)
                        {
                            this.parent.OnWriterClosed();
                        }
                        else
                        {
                            this.state = 0;
                        }

                        shouldContinue = false;
                    }
                }

                return shouldContinue;
            }

            bool HandleWriteBufferComplete(TransportAsyncCallbackArgs args)
            {
                WriteRequest request = (WriteRequest)args.UserToken;
                args.Reset();
                this.writeBuffer.Reset();

                if (args.Exception != null)
                {
                    this.parent.OnIoFault(args.Exception);
                    return false;
                }

                if (request != null)
                {
                    WriteRequest.Complete(request);
                }

                Fx.Assert(args.BytesTransfered == args.Count, "Bytes transferred not equal to the bytes set.");
                return true;
            }

            sealed class WriteRequest
            {
                static readonly Pool<WriteRequest> writeRequestPool = new Pool<WriteRequest>(500, () => { return new WriteRequest(); });

                ArraySegment<byte> buffer;
                ArraySegment<byte>[] payload;
                int segments;
                Action<object> callback;
                object state;
                ArraySegment<byte> current;
                int segment;
                int offset;

                public WriteRequest Next { get; set; }

                public static WriteRequest Get(ArraySegment<byte> buffer, ArraySegment<byte>[] payload, Action<object> callback, object state)
                {
                    WriteRequest request = writeRequestPool.Take();
                    request.Initialize(buffer, payload, callback, state);
                    return request;
                }

                public static void Complete(WriteRequest request)
                {
                    while (request != null)
                    {
                        request.Complete();
                        WriteRequest next = request.Next;
                        request.Next = null;
                        writeRequestPool.Return(request);
                        request = next;
                    }
                }

                public bool WriteTo(ByteBuffer buffer)
                {
                    if (this.segment >= this.segments)
                    {
                        throw new InvalidOperationException();
                    }

                    while (buffer.Size > 0)
                    {
                        int size = Math.Min(buffer.Size, this.current.Count - this.offset);
                        buffer.WriteBytes(this.current.Array, this.current.Offset + this.offset, size);

                        this.offset += size;
                        if (this.offset == this.current.Count)
                        {
                            this.offset = 0;
                            if (++this.segment == this.segments)
                            {
                                return true;
                            }
                            else
                            {
                                this.current = this.payload[this.segment - 1];
                            }
                        }
                    }

                    return false;
                }

                void Complete()
                {
                    this.buffer = default(ArraySegment<byte>);
                    this.payload = null;
                    this.segments = -1;
                    this.current = default(ArraySegment<byte>);
                    this.segment = -1;
                    this.offset = -1;

                    if (this.callback != null)
                    {
                        this.callback(this.state);

                        this.callback = null;
                        this.state = null;
                    }
                }

                void Initialize(ArraySegment<byte> buffer, ArraySegment<byte>[] payload, Action<object> callback, object state)
                {
                    this.buffer = buffer;
                    this.payload = payload;
                    this.callback = callback;
                    this.state = state;
                    this.segments = 1 + (payload != null ? payload.Length : 0);
                    this.current = buffer;
                    this.segment = 0;
                    this.offset = 0;
                }
            }
        }

        /// <summary>
        /// A writer that writes fixed-size buffer and notify caller upon completion.
        /// </summary>
        public sealed class AsyncBufferWriter
        {
            readonly TransportBase transport;
            readonly Action<TransportAsyncCallbackArgs> onWriteComplete;

            public AsyncBufferWriter(TransportBase transport)
            {
                this.transport = transport;
                this.onWriteComplete = this.OnWriteComplete;
            }

            public void WriteBuffer(TransportAsyncCallbackArgs args)
            {
                TransportAsyncCallbackArgs wrapperArgs = new TransportAsyncCallbackArgs();
                wrapperArgs.SetBuffer(args.Buffer, args.Offset, args.Count);
                wrapperArgs.CompletedCallback = this.onWriteComplete;
                wrapperArgs.UserToken = args;
                this.Write(wrapperArgs);
            }

            void Write(TransportAsyncCallbackArgs args)
            {
                try
                {
                    while (true)
                    {
                        if (this.transport.WriteAsync(args))
                        {
                            break;
                        }

                        if (this.HandleWriteComplete(args))
                        {
                            break;
                        }
                    }
                }
                catch(Exception exception)
                {
                    args.Exception = exception;
                    this.HandleWriteComplete(args);
                }
            }

            bool HandleWriteComplete(TransportAsyncCallbackArgs args)
            {
                bool done = true;
                Exception exception = null;
                if (args.Exception != null)
                {
                    exception = args.Exception;
                }
                else if (args.BytesTransfered == 0)
                {
                    exception = new ObjectDisposedException(this.transport.ToString());
                }
                else if (args.BytesTransfered < args.Count)
                {
                    args.SetBuffer(args.Buffer, args.Offset + args.BytesTransfered, args.Count - args.BytesTransfered);
                    done = false;
                }

                TransportAsyncCallbackArgs innerArgs = (TransportAsyncCallbackArgs)args.UserToken;
                if (done && innerArgs.CompletedCallback != null)
                {
                    innerArgs.Exception = exception;
                    innerArgs.BytesTransfered = innerArgs.Count;
                    innerArgs.CompletedCallback(innerArgs);
                }

                return done;
            }

            void OnWriteComplete(TransportAsyncCallbackArgs args)
            {
                if (!this.HandleWriteComplete(args) && !args.CompletedSynchronously)
                {
                    this.Write(args);
                }
            }
        }
    }
}
