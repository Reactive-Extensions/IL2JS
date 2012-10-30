//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ServiceBus.Common;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;
    using Microsoft.ServiceBus.Messaging.Amqp.Transport;
    using CloseCommand = Microsoft.ServiceBus.Messaging.Amqp.Framing.Close;
    using OpenCommand = Microsoft.ServiceBus.Messaging.Amqp.Framing.Open;

    /// <summary>
    /// Implements the AMQP 1.0 connection.
    /// </summary>
    sealed class AmqpConnection : AmqpConnectionBase, ISessionFactory
    {
        bool isInitiator;
        ProtocolHeader initialHeader;
        FrameDecoder frameDecoder;
        AmqpSettings amqpSettings;

        HandleTable<AmqpSession> sessionsByLocalHandle;
        HandleTable<AmqpSession> sessionsByRemoteHandle;
        IOThreadTimer heartBeatTimer;
        int heartBeatInterval;
        bool active;

        public AmqpConnection(TransportBase transport, AmqpSettings amqpSettings, AmqpConnectionSettings connectionSettings) :
            this(transport, amqpSettings.GetDefaultHeader(), true, amqpSettings, connectionSettings)
        {
        }

        public AmqpConnection(TransportBase transport, ProtocolHeader protocolHeader, AmqpSettings amqpSettings, AmqpConnectionSettings connectionSettings) :
            this(transport, protocolHeader, true, amqpSettings, connectionSettings)
        {
        }

        public AmqpConnection(TransportBase transport, ProtocolHeader protocolHeader, bool isInitiator, AmqpSettings amqpSettings, AmqpConnectionSettings connectionSettings) :
            base(transport, connectionSettings)
        {
            if (amqpSettings == null)
            {
                throw new ArgumentNullException("amqpSettings");
            }

            this.initialHeader = protocolHeader;
            this.isInitiator = isInitiator;
            this.amqpSettings = amqpSettings;
            this.frameDecoder = new FrameDecoder((int)this.Settings.MaxFrameSize);
            this.sessionsByLocalHandle = new HandleTable<AmqpSession>(this.Settings.ChannelMax.Value);
            this.sessionsByRemoteHandle = new HandleTable<AmqpSession>(this.Settings.ChannelMax.Value);
            this.SessionFactory = this;
        }

        public AmqpSettings AmqpSettings
        {
            get { return this.amqpSettings; }
        }

        public ISessionFactory SessionFactory
        {
            get;
            set;
        }

        protected override string Type
        {
            get { return "connection"; }
        }

        public AmqpSession CreateSession(AmqpSessionSettings sessionSettings)
        {
            if (this.IsClosing())
            {
                throw new InvalidOperationException(SRClient.CreateSessionOnClosingConnection);
            }

            AmqpSession session = this.SessionFactory.CreateSession(this, sessionSettings);
            this.AddSession(session, null);
            return session;
        }

        public void SendCommand(Performative command, ushort channel)
        {
            this.SendCommand(command, channel, null, null);
        }

        public void SendCommand(Performative command, ushort channel, Action<object> callback, object state)
        {
            Frame frame = new Frame(FrameType.Amqp, channel, command);
            Utils.Trace(TraceLevel.Frame, "SEND  {0}", frame);
            this.AsyncIO.Writer.WriteBuffer(frame.Buffer, command.PayloadList, callback, state);
            this.active = true;
        }

        protected override bool OpenInternal()
        {
            if (this.isInitiator)
            {
                this.AsyncIO.Open();
                this.SendProtocolHeader(this.initialHeader);
                this.SendOpen();
            }
            else if (this.initialHeader != null)
            {
                this.OnProtocolHeader(this.initialHeader);
                this.AsyncIO.Open();
            }
            else
            {
                this.AsyncIO.Open();
            }

            return false;
        }

        protected override bool CloseInternal()
        {
            this.CancelHeartBeatTimer();
            foreach (AmqpSession session in this.sessionsByLocalHandle.Values)
            {
                if (this.State == AmqpObjectState.Opened)
                {
                    session.BeginClose(session.DefaultCloseTimeout, this.OnSessionCloseComplete, session);
                }
                else
                {
                    session.Abort();
                }
            }

            try
            {
                this.SendClose();
            }
            catch (AmqpException)
            {
                this.State = AmqpObjectState.End;
            }

            bool completed = this.State == AmqpObjectState.End;
            if (completed)
            {
                this.AsyncIO.Close();
            }

            return completed;
        }

        protected override void AbortInternal()
        {
            this.CancelHeartBeatTimer();
            this.AbortSessions();
            this.AsyncIO.Abort();
        }

        protected override void TryCloseInternal()
        {
            this.CancelHeartBeatTimer();
            this.CompleteOpen(false, this.TerminalException);

            IEnumerable<AmqpSession> sessionSnapshot = null;
            lock (this.ThisLock)
            {
                sessionSnapshot = this.sessionsByLocalHandle.Values;
                this.sessionsByLocalHandle.Clear();
                this.sessionsByRemoteHandle.Clear();
            }

            foreach (AmqpSession session in sessionSnapshot)
            {
                session.TryClose(new AmqpException(AmqpError.ConnectionForced));
            }
        }

        protected override ProtocolHeader ParseProtocolHeader(ByteBuffer buffer)
        {
            return this.frameDecoder.ExtractProtocolHeader(buffer);
        }

        protected override void ParseFrameBuffers(ByteBuffer buffer, SerializedWorker<ByteBuffer> bufferHandler)
        {
            this.frameDecoder.ExtractFrameBuffers(buffer, bufferHandler);
        }

        protected override void OnProtocolHeader(ProtocolHeader header)
        {
            Utils.Trace(TraceLevel.Frame, "RECV  {0}", header);
            this.TransitState("R:HDR", StateTransition.ReceiveHeader);
            Exception exception = null;

            if (this.isInitiator)
            {
                if (!this.initialHeader.Equals(header))
                {
                    exception = new AmqpException(AmqpError.NotImplemented, SRClient.ProtocolVersionNotSupported(this.initialHeader.ToString(), header.ToString()));
                }
            }
            else
            {
                ProtocolHeader supportedHeader = this.amqpSettings.GetSupportedHeader(header);                
                this.SendProtocolHeader(supportedHeader);
                if (!supportedHeader.Equals(header))
                {
                    exception = new AmqpException(AmqpError.NotImplemented, SRClient.ProtocolVersionNotSupported(this.initialHeader.ToString(), header.ToString()));
                }
            }

            if (exception != null)
            {
                this.CompleteOpen(false, exception);
            }
        }

        protected override void OnFrameBuffer(ByteBuffer buffer)
        {
            Utils.TraceRaw(false, buffer);

            if (this.State == AmqpObjectState.End)
            {
                return;
            }
            
            this.active = true;
            Frame frame = Frame.Decode(buffer, false);
            if (frame.Command == null)
            {
                // Heart beat frame
                return;
            }

            if (!frame.IsValid((int)this.Settings.MaxFrameSize))
            {
                throw new AmqpException(AmqpError.FramingError);
            }

            if (frame.Command.DescriptorCode == OpenCommand.Code ||
                frame.Command.DescriptorCode == CloseCommand.Code ||
                frame.Command.DescriptorCode == Begin.Code ||
                frame.Command.DescriptorCode == End.Code)
            {
                // Lazy decoding link commands
                frame.Command.DecodeValue(buffer);
                Utils.Trace(TraceLevel.Frame, "RECV  {0}", frame);
            }
            else
            {
                frame.Command.ValueBuffer = buffer;
            }

            this.ProcessFrame(frame);
        }

        static void OnHeartBeatTimer(object state)
        {
            AmqpConnection thisPtr = (AmqpConnection)state;
            bool wasActive = thisPtr.active;
            thisPtr.active = false;
            if (thisPtr.State != AmqpObjectState.Opened)
            {
                return;
            }

            try
            {
                if (!wasActive)
                {
                    thisPtr.AsyncIO.Writer.WriteBuffer(Frame.Empty.Buffer, null, null, null);
                }

                thisPtr.heartBeatTimer.Set(thisPtr.heartBeatInterval);
            }
            catch (Exception exception)
            {
                if (Fx.IsFatal(exception))
                {
                    throw;
                }

                Utils.Trace(TraceLevel.Warning, "{0}: OnHeartBeatTimer failed with exception {1}", thisPtr, exception);
            }
        }

        void CancelHeartBeatTimer()
        {
            if (this.heartBeatTimer != null)
            {
                this.heartBeatTimer.Cancel();
            }
        }

        void AbortSessions()
        {
            IEnumerable<AmqpSession> sessionSnapshot = null;
            lock (this.ThisLock)
            {
                sessionSnapshot = this.sessionsByLocalHandle.Values;
                this.sessionsByLocalHandle.Clear();
                this.sessionsByRemoteHandle.Clear();
            }

            foreach (AmqpSession session in sessionSnapshot)
            {
                session.Abort();
            }
        }

        void ProcessFrame(Frame frame)
        {
            Performative command = frame.Command;
            Fx.Assert(command != null, "Must have a valid command");

            if (command.DescriptorCode == OpenCommand.Code)
            {
                this.OnReceiveOpen((Open)frame.Command);
            }
            else if (command.DescriptorCode == CloseCommand.Code)
            {
                this.OnReceiveClose((Close)frame.Command);
            }
            else if (!this.IsClosing())
            {
                this.OnReceiveSessionFrame(frame);
            }
        }

        void SendProtocolHeader(ProtocolHeader header)
        {
            this.TransitState("S:HDR", StateTransition.SendHeader);
            this.AsyncIO.Writer.WriteBuffer(header.Buffer, null, null, null);
            Utils.Trace(TraceLevel.Frame, "SEND  {0}", header);
        }

        void SendOpen()
        {
            this.TransitState("S:OPEN", StateTransition.SendOpen);
            this.SendCommand(this.Settings.Clone(), 0);
        }

        void SendClose()
        {
            this.TransitState("S:CLOSE", StateTransition.SendClose);
            Close close = new Close();
            if (this.TerminalException != null)
            {
                close.Error = AmqpError.FromException(this.TerminalException);
            }

            this.SendCommand(close, 0);
        }

        void OnReceiveOpen(Open open)
        {
            Action<Open> openCallback = this.Settings.OnOpenCallback;
            if (openCallback != null)
            {
                openCallback(open);
            }

            this.Negotiate(open);

            StateTransition stateTransition;
            this.TransitState("R:OPEN", StateTransition.ReceiveOpen, out stateTransition);
            if (stateTransition.To == AmqpObjectState.OpenReceived)
            {
                this.SendOpen();
            }

            if (this.isInitiator && this.Settings.IdleTimeOut.Value != uint.MaxValue)
            {
                this.heartBeatInterval = (int)(this.Settings.IdleTimeOut.Value * 3 / 8);
                if (this.heartBeatInterval < 500)
                {
                    this.heartBeatInterval = 500;
                }

                this.heartBeatTimer = new IOThreadTimer(OnHeartBeatTimer, this, false);
                this.heartBeatTimer.Set(this.heartBeatInterval);
                Utils.Trace(TraceLevel.Info, "{0}: enabled heart beat timer ({1}ms)", this, this.heartBeatInterval);
            }

            this.CompleteOpen(stateTransition.From == AmqpObjectState.Start, null);
        }

        void OnReceiveClose(Close close)
        {
            this.OnReceiveCloseCommand("R:CLOSE", close.Error);
            if (this.State == AmqpObjectState.End)
            {
                this.AsyncIO.Close();
            }
        }

        void OnReceiveSessionFrame(Frame frame)
        {
            AmqpSession session = null;
            Performative command = frame.Command;
            ushort channel = frame.Channel;

            if (command.DescriptorCode == Begin.Code)
            {
                Begin begin = (Begin)command;
                if (begin.RemoteChannel.HasValue)
                {
                    // reply to begin
                    lock (this.ThisLock)
                    {
                        if (!this.sessionsByLocalHandle.TryGetObject(begin.RemoteChannel.Value, out session))
                        {
                            throw new AmqpException(AmqpError.NotFound, SRClient.AmqpChannelNotFound(begin.RemoteChannel.Value));
                        }

                        session.RemoteChannel = channel;
                        this.sessionsByRemoteHandle.Add(channel, session);
                    }
                }
                else
                {
                    // new begin request
                    AmqpSessionSettings settings = AmqpSessionSettings.Create(begin);
                    settings.RemoteChannel = channel;
                    session = this.SessionFactory.CreateSession(this, settings);
                    this.AddSession(session, channel);
                }
            }
            else
            {
                if (!this.sessionsByRemoteHandle.TryGetObject((uint)channel, out session))
                {
                    if (frame.Command.DescriptorCode == End.Code ||
                        frame.Command.DescriptorCode == Detach.Code)
                    {
                        return;
                    }

                    throw new AmqpException(AmqpError.NotFound, SRClient.AmqpChannelNotFound((uint)channel));
                }
            }

            session.ProcessFrame(frame);
        }

        void Negotiate(Open open)
        {
            this.Settings.RemoteHostName = open.HostName;
            this.Settings.ChannelMax = Math.Min(this.Settings.ChannelMax(), open.ChannelMax());
            this.Settings.IdleTimeOut = Math.Min(open.IdleTimeOut(), this.Settings.IdleTimeOut());
            this.FindMutualCapabilites(this.Settings.DesiredCapabilities, open.OfferedCapabilities);
            if (open.MaxFrameSize.HasValue)
            {
                this.Settings.MaxFrameSize = Math.Min(this.Settings.MaxFrameSize.Value, open.MaxFrameSize.Value);
            }
        }

        AmqpSession ISessionFactory.CreateSession(AmqpConnection connection, AmqpSessionSettings sessionSettings)
        {
            return new AmqpSession(this, sessionSettings, this.amqpSettings.RuntimeProvider);
        }

        void AddSession(AmqpSession session, ushort? channel)
        {
            session.Closed += new EventHandler(this.OnSessionClosed);
            lock (this.ThisLock)
            {
                session.LocalChannel = (ushort)this.sessionsByLocalHandle.Add(session);
                if (channel != null)
                {
                    this.sessionsByRemoteHandle.Add(channel.Value, session);
                }
            }

            Utils.Trace(TraceLevel.Info, "{0}: Added {1} (local={2} remote={3})", this, session, session.LocalChannel, channel);
        }

        void OnSessionClosed(object sender, EventArgs e)
        {
            AmqpSession session = (AmqpSession)sender;
            lock (this.ThisLock)
            {
                this.sessionsByLocalHandle.Remove(session.LocalChannel);
                if (session.RemoteChannel.HasValue)
                {
                    this.sessionsByRemoteHandle.Remove(session.RemoteChannel.Value);
                }
            }

            Utils.Trace(TraceLevel.Info, "{0}: {1} [{2},{3}] closed and removed", this, session, session.LocalChannel, session.RemoteChannel);
        }

        void OnSessionCloseComplete(IAsyncResult result)
        {
            AmqpSession session = (AmqpSession)result.AsyncState;
            try
            {
                session.EndClose(result);
            }
            catch (Exception exception)
            {
                if (Fx.IsFatal(exception))
                {
                    throw;
                }

                Fx.Exception.AsError(exception);
            }
        }
    }
}
