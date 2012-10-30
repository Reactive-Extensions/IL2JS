//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using Microsoft.ServiceBus.Common;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;
    using Microsoft.ServiceBus.Messaging.Amqp.Transport;
    using CloseCommand = Microsoft.ServiceBus.Messaging.Amqp.Framing.Close;
    using OpenCommand = Microsoft.ServiceBus.Messaging.Amqp.Framing.Open;

    public sealed class AmqpStream
    {
        readonly bool encoding;
        bool isReceiver;
        string node;
        AmqpFrameConnection connection;
        InputQueue<ByteBuffer> messages;

        public AmqpStream(bool encoding)
        {
            this.encoding = encoding;
            this.messages = new InputQueue<ByteBuffer>();
        }

        public void Write(byte[] buffer, int offset, int count, Action<object> callback, object state)
        {
            this.connection.Write(buffer, offset, count, callback, state);
        }

        public void Read(out byte[] buffer, out int offset, out int count)
        {
            ByteBuffer byteBuffer = this.messages.Dequeue(TimeSpan.MaxValue);
            buffer = byteBuffer.Buffer;
            offset = byteBuffer.Offset;
            count = byteBuffer.Length;
        }

        public void Connect(string address, TimeSpan timeout)
        {
            IAsyncResult result = new OpenSenderAsyncResult(this, address, timeout, null, null);
            this.connection = OpenSenderAsyncResult.End(result);
        }

        public void Accept(string address, TimeSpan timeout)
        {
            IAsyncResult result = new OpenReceiverAsyncResult(this, address, timeout, null, null);
            this.connection = OpenReceiverAsyncResult.End(result);
        }

        public void Close(TimeSpan timeout)
        {
            this.connection.Close();
        }

        abstract class OpenAsyncResult : AsyncResult
        {
            static readonly AsyncCompletion onConnectionOpen = OnConnectionOpen;
            readonly AmqpStream parent;
            TimeoutHelper timeoutHelper;
            AmqpFrameConnection connection;

            protected OpenAsyncResult(AmqpStream parent, string address, bool listen, TimeSpan timeout, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.parent = parent;
                this.parent.isReceiver = listen;
                this.timeoutHelper = new TimeoutHelper(timeout);

                Uri addressUri = new Uri(address);
                this.parent.node = addressUri.PathAndQuery;
                int port = addressUri.Port;
                if (port == -1)
                {
                    port = AmqpConstants.DefaultPort;
                }

                TcpTransportSettings tcpSettings = new TcpTransportSettings();
                tcpSettings.TcpBacklog = 20;
                tcpSettings.TcpBufferSize = 4096;
                tcpSettings.SetEndPoint(addressUri.Host, port, listen);
                TransportSettings transportSettings = tcpSettings;

                this.Start(transportSettings);
            }

            protected TimeoutHelper TimeoutHelper
            {
                get { return this.timeoutHelper; }
            }

            public static AmqpFrameConnection End(IAsyncResult result)
            {
                return AsyncResult.End<OpenAsyncResult>(result).connection;
            }

            protected abstract void Start(TransportSettings transportSettings);

            protected void OnTransport(TransportBase transport)
            {
                this.connection = new AmqpFrameConnection(this.parent, transport, (int)AmqpConstants.DefaultMaxFrameSize);
                IAsyncResult result = this.connection.BeginOpen(this.timeoutHelper.RemainingTime(), this.PrepareAsyncCompletion(onConnectionOpen), this);
                this.SyncContinue(result);
            }

            static bool OnConnectionOpen(IAsyncResult result)
            {
                var thisPtr = (OpenAsyncResult)result.AsyncState;
                thisPtr.connection.EndOpen(result);
                thisPtr.parent.connection = thisPtr.connection;
                return true;
            }
        }

        sealed class OpenSenderAsyncResult : OpenAsyncResult
        {
            TransportInitiator initiator;

            public OpenSenderAsyncResult(AmqpStream parent, string address, TimeSpan timeout, AsyncCallback callback, object state)
                : base(parent, address, false, timeout, callback, state)
            {
            }

            protected override void Start(TransportSettings transportSettings)
            {
                this.initiator = transportSettings.CreateInitiator();
                TransportAsyncCallbackArgs args = new TransportAsyncCallbackArgs();
                args.CompletedCallback = this.OnEstablishTransport;
                if (!initiator.ConnectAsync(this.TimeoutHelper.RemainingTime(), args))
                {
                    this.OnEstablishTransport(args);
                }
            }

            void OnEstablishTransport(TransportAsyncCallbackArgs args)
            {
                if (args.Exception != null)
                {
                    this.Complete(false, args.Exception);
                    return;
                }

                this.OnTransport(args.Transport);
            }
        }

        sealed class OpenReceiverAsyncResult : OpenAsyncResult
        {
            TransportListener listener;

            public OpenReceiverAsyncResult(AmqpStream parent, string address, TimeSpan timeout, AsyncCallback callback, object state)
                : base(parent, address, true, timeout, callback, state)
            {
            }

            protected override void Start(TransportSettings transportSettings)
            {
                this.listener = transportSettings.CreateListener();
                this.listener.Listen(this.OnAcceptTransport);
            }

            void OnAcceptTransport(TransportAsyncCallbackArgs args)
            {
                this.listener.Close();

                if (args.Exception != null)
                {
                    this.Complete(false, args.Exception);
                    return;
                }

                this.OnTransport(args.Transport);
            }
        }

        sealed class AmqpFrameConnection : AmqpConnectionBase
        {
            static ArraySegment<byte> deliveryTag = new ArraySegment<byte>(new byte[] { 0 });
            readonly AmqpStream parent;
            readonly FrameDecoder decoder;
            int sn;

            public AmqpFrameConnection(AmqpStream parent, TransportBase transport, int maxFrameSize) :
                base(transport, new AmqpConnectionSettings() { MaxFrameSize = (uint)maxFrameSize })
            {
                this.parent = parent;
                this.decoder = new FrameDecoder(maxFrameSize);
            }

            unsafe public void Write(byte[] buffer, int offset, int count, Action<object> callback, object state)
            {
                if (this.parent.encoding)
                {
                    this.WriteTransferFrame(buffer, offset, count, callback, state);
                }
                else
                {
                    ByteBuffer byteBuffer = ByteBuffer.Wrap(Frame.HeaderSize);
                    AmqpBitConverter.WriteUInt(byteBuffer, (uint)(Frame.HeaderSize + count));
                    byteBuffer.Append(4);
                    this.AsyncIO.Writer.WriteBuffer(
                        byteBuffer.Array, 
                        new ArraySegment<byte>[] { new ArraySegment<byte>(buffer, offset, count) },
                        callback, 
                        state);
                }
            }

            protected override bool OpenInternal()
            {
                this.AsyncIO.Open();
                this.AsyncIO.Writer.WriteBuffer(ProtocolHeader.Amqp100.Buffer, null, null, null);
                if (this.parent.encoding && !this.parent.isReceiver)
                {
                    WriteCommand(new Open() { ContainerId = "C" + System.Diagnostics.Process.GetCurrentProcess().Id.ToString() });
                    WriteCommand(new Begin() { NextOutgoingId = 0, IncomingWindow = uint.MaxValue, OutgoingWindow = uint.MaxValue });
                    WriteCommand(new Attach() { LinkName = Guid.NewGuid().ToString("N"), Handle = 0, Role = false, Target = new Target() { Address = this.parent.node } });
                }
                
                return false;
            }

            protected override bool CloseInternal()
            {
                if (this.parent.encoding)
                {
                    WriteCommand(new Detach() { Handle = 0, Closed = true });
                    WriteCommand(new End());
                    WriteCommand(new Close());
                    return false;
                }
                else
                {
                    return true;
                }
            }

            protected override void AbortInternal()
            {
                this.AsyncIO.Abort();
            }

            protected override void TryCloseInternal()
            {
            }

            protected override ProtocolHeader ParseProtocolHeader(ByteBuffer buffer)
            {
                return this.decoder.ExtractProtocolHeader(buffer);
            }

            protected override void ParseFrameBuffers(ByteBuffer buffer, SerializedWorker<ByteBuffer> bufferHandler)
            {
                this.decoder.ExtractFrameBuffers(buffer, bufferHandler);
            }

            protected override void OnProtocolHeader(ProtocolHeader header)
            {
                if (!this.parent.encoding)
                {
                    this.CompleteOpen(false, null);
                }
                else
                {
                    this.State = AmqpObjectState.HeaderExchanged;
                }
            }

            protected override void OnFrameBuffer(ByteBuffer buffer)
            {
                try
                {
                    if (this.parent.encoding)
                    {
                        this.HandleFrameBuffer(buffer);
                    }
                    else
                    {
                        this.HandleRawBuffer(buffer);
                    }
                }
                catch
                {
                }
            }

            unsafe void WriteTransferFrame(byte[] buffer, int offset, int count, Action<object> callback, object state)
            {
                ArraySegment<byte>[] packet = new ArraySegment<byte>[]
                    {
                        Data.GetEncodedPrefix(count), 
                        new ArraySegment<byte>(buffer, offset, count) 
                    };

                int payloadSize = packet[0].Count + packet[1].Count;
                ByteBuffer byteBuffer = ByteBuffer.Wrap(128);
                int index = 4;
                fixed (byte* p = byteBuffer.Buffer)
                {
                    p[index++] = 2;
                    p[index++] = p[index++] = p[index++] = 0;

                    p[index++] = 0;
                    p[index++] = (byte)FormatCode.SmallULong;
                    p[index++] = (byte)Transfer.Code;
                    p[index++] = (byte)FormatCode.List8;
                    int listSizeIndex = index++;
                    p[index++] = 11;

                    // handle
                    p[index++] = (byte)FormatCode.UInt0;

                    // delivery id
                    uint deliveryId = (uint)this.sn++;
                    byte* d = (byte*)&deliveryId;
                    p[index++] = (byte)FormatCode.UInt;
                    p[index++] = d[3];
                    p[index++] = d[2];
                    p[index++] = d[1];
                    p[index++] = d[0];

                    // delivery tag
                    p[index++] = (byte)FormatCode.Binary8;
                    p[index++] = 0;

                    // message format
                    p[index++] = (byte)FormatCode.UInt0;

                    // settled
                    p[index++] = (byte)FormatCode.BooleanTrue;

                    // more
                    p[index++] = (byte)FormatCode.BooleanFalse;

                    // rcv settle mode, state, resume, abort
                    p[index++] = p[index++] = p[index++] = p[index++] = (byte)FormatCode.Null;

                    // batchable
                    p[index++] = (byte)FormatCode.BooleanTrue;

                    // write list size
                    p[listSizeIndex] = (byte)(index - listSizeIndex);

                    // write the frame size
                    int frameSize = index + payloadSize;
                    d = (byte*)&frameSize;
                    p[0] = d[3];
                    p[1] = d[2];
                    p[2] = d[1];
                    p[3] = d[0];
                }

                byteBuffer.Append(index);
                this.AsyncIO.Writer.WriteBuffer(byteBuffer.Array, packet, callback, state);
            }

            void WriteCommand(Performative command)
            {
                Frame frame = new Frame(FrameType.Amqp, 0, command);
                ByteBuffer byteBuffer = ByteBuffer.Wrap(Frame.HeaderSize + command.EncodeSize);
                frame.Encode(byteBuffer);
                this.AsyncIO.Writer.WriteBuffer(frame.Buffer, command.PayloadList, null, null);
            }

            void HandleRawBuffer(ByteBuffer buffer)
            {
                buffer.Complete(Frame.HeaderSize);
                this.parent.messages.EnqueueAndDispatch(buffer);
            }

            void HandleFrameBuffer(ByteBuffer buffer)
            {
                Frame frame = Frame.Decode(buffer);
                if (frame.Command.DescriptorCode == OpenCommand.Code)
                {
                    if (this.parent.encoding && this.parent.isReceiver)
                    {
                        this.WriteCommand(frame.Command);
                    }
                }
                else if (frame.Command.DescriptorCode == Begin.Code)
                {
                    if (this.parent.encoding && this.parent.isReceiver)
                    {
                        this.WriteCommand(frame.Command);
                    }
                }
                else if (frame.Command.DescriptorCode == Attach.Code)
                {
                    if (this.parent.encoding && this.parent.isReceiver)
                    {
                        Attach attach = (Attach)frame.Command;
                        attach.Role = !attach.Role;
                        this.WriteCommand(attach);
                        this.WriteCommand(new Flow() { NextIncomingId = 0, NextOutgoingId = 0, IncomingWindow = uint.MaxValue, OutgoingWindow = uint.MaxValue, Handle = 0, LinkCredit = uint.MaxValue });
                    }

                    this.State = AmqpObjectState.Opened;
                    this.CompleteOpen(false, null);
                }
                else if (frame.Command.DescriptorCode == Transfer.Code)
                {
                    Transfer transfer = (Transfer)frame.Command;
                    Data data = AmqpCodec.DecodeKnownType<Data>(ByteBuffer.Wrap(transfer.Payload));
                    ArraySegment<byte> payload = (ArraySegment<byte>)data.Value;
                    this.parent.messages.EnqueueAndDispatch(ByteBuffer.Wrap(payload));
                }
                else if (frame.Command.DescriptorCode == Detach.Code)
                {
                    if (this.parent.encoding)
                    {
                        this.WriteCommand(frame.Command);
                    }
                }
                else if (frame.Command.DescriptorCode == End.Code)
                {
                    if (this.parent.encoding)
                    {
                        this.WriteCommand(frame.Command);
                    }
                }
                else if (frame.Command.DescriptorCode == CloseCommand.Code)
                {
                    if (this.parent.encoding)
                    {
                        this.WriteCommand(frame.Command);
                    }

                    this.State = AmqpObjectState.End;
                    this.CompleteClose(false, null);
                    this.AsyncIO.Close();
                }
            }
        }
    }
}
