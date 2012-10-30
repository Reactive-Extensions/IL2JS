//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Security.Principal;
    using Microsoft.ServiceBus.Common;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;
    using Microsoft.ServiceBus.Messaging.Amqp.Transport;

    /// <summary>
    /// The base class for AMQP connection. It should be version independent.
    /// </summary>
    abstract class AmqpConnectionBase : AmqpObject
    {
        AmqpConnectionSettings settings;
        AsyncIO asyncIO;
        SerializedWorker<ByteBuffer> bufferHandler;
        IPrincipal principal;

        public AmqpConnectionBase(TransportBase transport, AmqpConnectionSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            this.settings = settings;

            Fx.Assert(transport != null, "transport must not be null.");
            this.principal = transport.Principal;
            this.asyncIO = new AsyncIO(
                AmqpConstants.AsyncBufferSize,
                transport,
                new Action<ByteBuffer>(this.OnReceiveBuffer),
                this.OnAsyncIoFaulted);
            this.bufferHandler = new SerializedWorker<ByteBuffer>(this.OnReceiveFrameBuffer, null, false);
        }

        public AmqpConnectionSettings Settings
        {
            get { return this.settings; }
        }

        public IPrincipal Principal
        {
            get { return this.principal; }
        }

        protected AsyncIO AsyncIO
        {
            get { return this.asyncIO; }
        }

        protected override string Type
        {
            get { return "connection-base"; }
        }

        public void WriteBuffer(ArraySegment<byte> buffer, ArraySegment<byte>[] extraBuffers, Action<object> callback, object state)
        {
            this.asyncIO.Writer.WriteBuffer(buffer, extraBuffers, callback, state);
        }

        protected abstract ProtocolHeader ParseProtocolHeader(ByteBuffer buffer);

        protected abstract void ParseFrameBuffers(ByteBuffer buffer, SerializedWorker<ByteBuffer> bufferHandler);

        protected abstract void OnProtocolHeader(ProtocolHeader header);

        protected abstract void OnFrameBuffer(ByteBuffer buffer);

        /// <summary>
        /// This method handles the buffer received from the transport.
        /// Override this method to take over full control on the raw buffer.
        /// </summary>
        protected virtual void OnReceiveBuffer(ByteBuffer buffer)
        {
            if (this.State <= AmqpObjectState.OpenClosePipe)
            {
                try
                {
                    ProtocolHeader header = this.ParseProtocolHeader(buffer);
                    this.OnProtocolHeader(header);
                }
                catch (Exception exception)
                {
                    if (Fx.IsFatal(exception))
                    {
                        throw;
                    }

                    Utils.Trace(TraceLevel.Error, "{0}: Handle exception: {1}", this, exception);
                    this.TryClose(exception);
                    return;
                }
            }

            try
            {
                this.ParseFrameBuffers(buffer, this.bufferHandler);
            }
            catch (Exception exception)
            {
                if (Fx.IsFatal(exception))
                {
                    throw;
                }

                Utils.Trace(TraceLevel.Error, "{0}: Handle exception: {1}", this, exception);
                this.TryClose(exception);
            }
        }

        bool OnReceiveFrameBuffer(ByteBuffer buffer)
        {
            this.OnFrameBuffer(buffer);
            return true;
        }

        void OnAsyncIoFaulted(Exception exception)
        {
            Utils.Trace(TraceLevel.Error, "{0}: Faulted with exception {1}", this, exception.Message);
            this.TerminalException = exception;
            this.Abort();
        }
    }
}
