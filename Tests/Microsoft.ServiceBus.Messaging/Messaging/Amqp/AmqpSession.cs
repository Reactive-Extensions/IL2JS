//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.ServiceBus.Common;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    class AmqpSession : AmqpObject
    {
        readonly AmqpConnection connection;
        readonly AmqpSessionSettings settings;
        readonly Dictionary<string, AmqpLink> links;
        readonly ILinkFactory linkFactory;
        readonly HandleTable<AmqpLink> linksByLocalHandle;
        readonly HandleTable<AmqpLink> linksByRemoteHandle;
        readonly OutgoingSessionChannel outgoingChannel;
        readonly IncomingSessionChannel incomingChannel;
        readonly Diagnostics diagnostics;

        public AmqpSession(AmqpConnection connection, AmqpSessionSettings settings, ILinkFactory linkFactory)
        {
            Fx.Assert(connection != null, "connection must not be null");
            Fx.Assert(settings != null, "settings must not be null");
            this.connection = connection;
            this.settings = settings;
            this.linkFactory = linkFactory;
            this.State = AmqpObjectState.Start;
            this.links = new Dictionary<string, AmqpLink>();
            this.linksByLocalHandle = new HandleTable<AmqpLink>(uint.MaxValue);
            this.linksByRemoteHandle = new HandleTable<AmqpLink>(uint.MaxValue);
            this.outgoingChannel = new OutgoingSessionChannel(this);
            this.incomingChannel = new IncomingSessionChannel(this);
            this.diagnostics = new Diagnostics();
        }

        protected AmqpSession(AmqpConnection connection, AmqpSessionSettings settings)
        {
            this.connection = connection;
            this.settings = settings;
        }

        public AmqpSessionSettings Settings
        {
            get { return this.settings; }
        }

        public AmqpConnection Connection
        {
            get { return this.connection; }
        }

        public ushort LocalChannel
        {
            get;
            set;
        }

        public ushort? RemoteChannel
        {
            get { return this.settings.RemoteChannel; }
            set { this.settings.RemoteChannel = value; }
        }

        public string DiagnosticsInfo
        {
            get { return this.diagnostics.ToString(); }
        }

        protected override string Type
        {
            get { return "session"; }
        }

        public void AttachLink(AmqpLink link)
        {
            Fx.Assert(link.Session == this, "The link is not owned by this session.");
            link.Closed += new EventHandler(this.OnLinkClosed);

            lock (this.ThisLock)
            {
                this.links.Add(link.Name, link);
                link.LocalHandle = this.linksByLocalHandle.Add(link);
            }

            Utils.Trace(TraceLevel.Info, "{0}: Attach {1} '{2}' ({3})", this, link, link.Name, link.IsReceiver ? "receiver" : "sender");
        }

        public virtual void ProcessFrame(Frame frame)
        {
            Performative command = frame.Command;

            try
            {
                if (command.ValueBuffer != null)
                {
                    // Deferred link frames are decoded here
                    command.DecodeValue(command.ValueBuffer);
                    if (command.ValueBuffer.Length > 0)
                    {
                        command.Payload = command.ValueBuffer.Array;
                    }

                    command.ValueBuffer = null;
                    Utils.Trace(TraceLevel.Frame, "RECV  {0}", frame);
                }

                if (command.DescriptorCode == Begin.Code)
                {
                    this.OnReceiveBegin((Begin)command);
                }
                else if (command.DescriptorCode == End.Code)
                {
                    this.OnReceiveEnd((End)command);
                }
                else if (command.DescriptorCode == Disposition.Code)
                {
                    this.OnReceiveDisposition((Disposition)command);
                }
                else if (command.DescriptorCode == Flow.Code)
                {
                    this.OnReceiveFlow((Flow)command);
                }
                else
                {
                    this.OnReceiveLinkFrame(frame);
                }
            }
            catch (Exception exception)
            {
                if (Fx.IsFatal(exception))
                {
                    throw;
                }

                Utils.Trace(TraceLevel.Error, "{0}: Fault with exception: {1}", this, exception);
                this.TryClose(exception);
            }
        }

        public void SendFlow(Flow flow)
        {
            // Use the outgoing channel so we can synchronize this with transfers
            this.outgoingChannel.SendFlow(flow);
        }

        public void SendCommand(Performative command)
        {
            this.SendCommand(command, null, null);
        }

        public void SendDelivery(Delivery delivery)
        {
            this.outgoingChannel.SendDelivery(delivery);
        }

        public void DisposeDelivery(AmqpLink link, Delivery delivery, bool settled, DeliveryState state)
        {
            if (link.IsReceiver)
            {
                this.incomingChannel.DisposeDelivery(delivery, settled, state);
            }
            else
            {
                this.outgoingChannel.DisposeDelivery(delivery, settled, state);
            }
        }

        public void OnAcceptIncomingTransfer()
        {
            this.incomingChannel.OnAcceptSubsequentTransfer(1);
        }

        public void Flush()
        {
            this.outgoingChannel.Flush();
            this.incomingChannel.Flush();
        }

        protected override bool OpenInternal()
        {
            this.SendBegin();
            return this.State == AmqpObjectState.Opened;
        }

        protected override bool CloseInternal()
        {
            IEnumerable<AmqpLink> linksSnapshot = null;
            lock (this.ThisLock)
            {
                linksSnapshot = this.linksByLocalHandle.Values;
            }

            foreach (AmqpLink link in linksSnapshot)
            {
                if (!this.IsClosing())
                {
                    link.BeginClose(link.DefaultCloseTimeout, this.OnLinkCloseComplete, link);
                }
                else
                {
                    link.Abort();
                }
            }

            this.SendEnd();
            return this.State == AmqpObjectState.End;
        }

        protected override void AbortInternal()
        {
            IEnumerable<AmqpLink> linksSnapshot = null;
            lock (this.ThisLock)
            {
                linksSnapshot = this.linksByLocalHandle.Values;
                this.linksByLocalHandle.Clear();
                this.linksByRemoteHandle.Clear();
            }

            foreach (AmqpLink link in linksSnapshot)
            {
                link.Abort();
            }
        }

        protected override void TryCloseInternal()
        {
            if (this.State == AmqpObjectState.OpenReceived)
            {
                this.SendBegin();
            }

            IEnumerable<AmqpLink> linksSnapshot = null;
            lock (this.ThisLock)
            {
                linksSnapshot = this.linksByLocalHandle.Values;
                this.linksByLocalHandle.Clear();
                this.linksByRemoteHandle.Clear();
            }

            foreach (AmqpLink link in linksSnapshot)
            {
                link.TryClose(this.TerminalException);
            }
        }

        protected void SendBegin()
        {
            this.TransitState("S:BEGIN", StateTransition.SendOpen);
            this.SendCommand(this.settings);
        }

        protected void SendEnd()
        {
            this.TransitState("S:END", StateTransition.SendClose);

            End end = new End();
            Exception exception = this.TerminalException;
            if (exception != null)
            {
                end.Error = AmqpError.FromException(exception);
            }

            this.SendCommand(end);
        }

        void SendCommand(Performative command, Action<object> callback, object state)
        {
            this.connection.SendCommand(command, this.LocalChannel, callback, state);
            Utils.Trace(TraceLevel.Debug, "{0}: Sent command {1}", this, command);

            if (command.DescriptorCode == Disposition.Code)
            {
                this.diagnostics.SendDisposition();

                Disposition disposition = (Disposition)command;
                this.diagnostics.SetLastDisposition(disposition.Last == null ? disposition.First.Value : disposition.Last.Value);
                Utils.Trace(TraceLevel.Verbose, "{0}: Dispose {1}-{2}, settled:{3}, outcome:{4}.", this, disposition.First.Value, disposition.Last, disposition.Settled, disposition.State.DescriptorName.Value);
            }
            else if (command.DescriptorCode == Flow.Code)
            {
                this.diagnostics.SendFlow();
            }
            else if (command.DescriptorCode == Transfer.Code)
            {
                this.diagnostics.SendTransfer();
            }
        }

        void SendFlow()
        {
            this.SendFlow(new Flow());
            this.diagnostics.SendSessionFlow();
        }

        void OnReceiveBegin(Begin begin)
        {
            StateTransition stateTransition;
            this.TransitState("R:BEGIN", StateTransition.ReceiveOpen, out stateTransition);

            this.incomingChannel.OnBegin(begin);
            if (stateTransition.To == AmqpObjectState.OpenReceived)
            {
                this.Open();
            }
            else
            {
                Exception exception = null;
                Error error = this.Negotiate(begin);
                if (error != null)
                {
                    exception = new AmqpException(error);
                }

                this.CompleteOpen(false, exception);
                if (exception != null)
                {
                    this.TryClose(exception);
                }
            }
        }

        void OnReceiveEnd(End end)
        {
            this.OnReceiveCloseCommand("R:END", end.Error);
        }

        void OnReceiveDisposition(Disposition disposition)
        {
            if (disposition.Role.Value)
            {
                this.outgoingChannel.OnReceiveDisposition(disposition);
            }
            else
            {
                this.incomingChannel.OnReceiveDisposition(disposition);
            }

            this.diagnostics.ReceiveDisposition();
        }

        void OnReceiveFlow(Flow flow)
        {
            this.outgoingChannel.OnFlow(flow);
            this.incomingChannel.OnFlow(flow);

            if (flow.Handle.HasValue)
            {
                AmqpLink link = null;
                if (!this.linksByRemoteHandle.TryGetObject(flow.Handle.Value, out link))
                {
                    this.TryClose(new AmqpException(AmqpError.UnattachedHandle));
                    return;
                }

                link.OnFlow(flow);
            }
            else if (flow.Echo())
            {
                this.SendFlow();
            }

            this.diagnostics.ReceiveFlow(flow.Handle.HasValue);
        }

        void OnReceiveLinkFrame(Frame frame)
        {
            AmqpLink link = null;
            Performative command = frame.Command;
            if (command.DescriptorCode == Attach.Code)
            {
                Attach attach = (Attach)command;
                lock (this.ThisLock)
                {
                    this.links.TryGetValue(attach.LinkName, out link);
                }

                if (link == null)
                {
                    if (!this.TryCreateRemoteLink(attach, out link))
                    {
                        return;
                    }
                }
                else
                {
                    lock (this.ThisLock)
                    {
                        link.RemoteHandle = attach.Handle;
                        this.linksByRemoteHandle.Add(attach.Handle.Value, link);
                    }
                }
            }
            else
            {
                LinkPerformative linkBody = (LinkPerformative)command;
                if (!this.linksByRemoteHandle.TryGetObject(linkBody.Handle.Value, out link))
                {
                    if (linkBody.DescriptorCode != Detach.Code)
                    {
                        this.TryClose(new AmqpException(AmqpError.UnattachedHandle));
                    }

                    return;
                }
            }

            try
            {
                if (command.DescriptorCode == Transfer.Code)
                {
                    // pre-process the transfer on the session
                    this.incomingChannel.OnReceiveTransfer(link, (Transfer)command);
                    this.diagnostics.ReceiveTransfer();
                }
                else
                {
                    link.ProcessFrame(frame);
                }
            }
            catch (AmqpException exception)
            {
                this.TryClose(new AmqpException(AmqpError.ErrantLink, exception));
            }
        }

        bool TryCreateRemoteLink(Attach attach, out AmqpLink link)
        {
            link = null;
            AmqpLinkSettings linkSettings = AmqpLinkSettings.Create(attach);

            try
            {
                link = this.linkFactory.CreateLink(this, linkSettings);
                link.RemoteHandle = attach.Handle;
                this.linksByRemoteHandle.Add(attach.Handle.Value, link);
            }
            catch (Exception exception)
            {
                if (Fx.IsFatal(exception))
                {
                    throw;
                }

                attach.Source = null;
                attach.Target = null;
                this.SendCommand(attach);

                if (link != null)
                {
                    link.TryClose(exception);
                }

                return false;
            }

            return true;
        }

        Error Negotiate(Begin begin)
        {
            this.outgoingChannel.OnBegin(begin);
            if (begin.HandleMax.HasValue)
            {
                this.settings.HandleMax = Math.Min(this.settings.HandleMax.Value, begin.HandleMax.Value);
            }

            return null;
        }

        void OnLinkClosed(object sender, EventArgs e)
        {
            AmqpLink link = (AmqpLink)sender;
            lock (this.ThisLock)
            {
                this.links.Remove(link.Name);
                if (link.LocalHandle.HasValue)
                {
                    this.linksByLocalHandle.Remove(link.LocalHandle.Value);
                }

                if (link.RemoteHandle.HasValue)
                {
                    this.linksByRemoteHandle.Remove(link.RemoteHandle.Value);
                }
            }

            Utils.Trace(TraceLevel.Info, "{0}: {1} closed and removed.", this, link);
        }

        void OnLinkCloseComplete(IAsyncResult result)
        {
            AmqpLink link = (AmqpLink)result.AsyncState;
            try
            {
                link.EndClose(result);
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

        abstract class SessionChannel
        {
            static uint MaxBuferSize = 10000;
            static WaitCallback dispositionWaitCallback = DispositionWaitCallback;

            // This is a circular buffer. Unsettled low-water-mark marks the first
            // unsettled delivery. Next-delivery marks the end of the unsettled
            // deliveries.
            // Note that unsettledLwm can only be changed in Settle(), and 
            // nextDeliveryId can only be incremented in TryAddDelivery(). 
            readonly AmqpSession session;
            readonly Delivery[] deliveryBuffer;
            readonly IOThreadTimer dispositionTimer;
            SequenceNumber unsettledLwm;
            SequenceNumber nextDeliveryId;
            int dispositionThreshold;
            int needDispositionCount;
            int sendingDisposition;
            int dispositionScheduled;
            int settling;
            object syncRoot;

            public SessionChannel(AmqpSession session, int bufferSize)
            {
                this.session = session;
                this.nextDeliveryId = session.settings.InitialDeliveryId;
                this.unsettledLwm = this.nextDeliveryId;
                this.deliveryBuffer = new Delivery[Math.Min(bufferSize, SessionChannel.MaxBuferSize)];
                this.dispositionTimer = new IOThreadTimer(DispositionTimerCallback, this, false);
                this.syncRoot = new object();

                int defaultThreshold = this.deliveryBuffer.Length * 2 / 3;
                this.dispositionThreshold = session.settings.DispositionThreshold == 0 ? defaultThreshold : Math.Min(session.settings.DispositionThreshold, defaultThreshold);
                Utils.Trace(TraceLevel.Verbose, "{0}: buffer-size:{1}, disposition-threshold:{2}.", this, this.deliveryBuffer.Length, this.dispositionThreshold);
            }

            protected AmqpSession Session 
            { 
                get { return this.session; } 
            }

            protected bool IsReceiver 
            { 
                get; 
                set; 
            }

            protected object SyncRoot 
            {
                get { return this.syncRoot; } 
            }

            protected SequenceNumber NextDeliveryId
            {
                get { return this.nextDeliveryId; }
            }

            public void OnReceiveDisposition(Disposition disposition)
            {
                SequenceNumber first = disposition.First.Value;
                SequenceNumber last = disposition.Last ?? first;
                this.session.diagnostics.SetLastDisposition(last.Value);
                bool settled = disposition.Settled();
                for (SequenceNumber i = first; ; i.Increment())
                {
                    Delivery delivery = this.GetDelivery(i);
                    if (delivery != null && delivery.Link != null)
                    {
                        delivery.Settled = settled;
                        delivery.State = disposition.State;
                        delivery.Link.OnDisposeDelivery(delivery);
                    }

                    if (i == last)
                    {
                        break;
                    }
                }

                if (settled)
                {
                    this.Settle();
                }
            }

            public void Flush()
            {
                this.SendDisposition(true);
            }

            public void DisposeDelivery(Delivery delivery, bool settled, DeliveryState state)
            {
                if (delivery.Settled)
                {
                    this.Settle();
                    return;
                }

                Utils.Trace(TraceLevel.Verbose, "{0}: Dispose delivery {1}, settled:{2}.", this, delivery.DeliveryId.Value, settled);
                delivery.Settled = settled;
                delivery.State = state;
                delivery.StateChanged = true;

                int deliveryIndex = this.GetBufferIndex(delivery.DeliveryId.Value);
                Delivery oldDelivery = this.deliveryBuffer[deliveryIndex];
                // replace the placeholder with the real delivery
                if (!object.ReferenceEquals(delivery, oldDelivery))
                {
                    this.deliveryBuffer[deliveryIndex] = delivery;
                }

                bool sendDispositionNow = !delivery.Batchable ||
                    Interlocked.Increment(ref this.needDispositionCount) >= this.dispositionThreshold;
                if (sendDispositionNow)
                {
                    Interlocked.Exchange(ref this.needDispositionCount, 0);
                }

                this.SendDisposition(sendDispositionNow);
            }

            protected bool TryAddDelivery(Delivery delivery)
            {
                if (this.nextDeliveryId >= (this.unsettledLwm + this.deliveryBuffer.Length))
                {
                    // buffer full
                    return false;
                }

                int index = this.GetBufferIndex(this.nextDeliveryId.Value);
                if (this.deliveryBuffer[index] != null)
                {
                    throw new InvalidOperationException(SRClient.DeliveryIDInUse);
                }

                delivery.DeliveryId = this.nextDeliveryId.Value;
                this.nextDeliveryId.Increment();
                if (!delivery.Settled)
                {
                    this.deliveryBuffer[index] = delivery;
                }

                return true;
            }

            protected void OnReceiveFirstTransfer(Transfer transfer)
            {
                Fx.Assert(transfer.DeliveryId.HasValue, "The first transfer must have a delivery id.");
                this.nextDeliveryId = transfer.DeliveryId.Value;
                this.unsettledLwm = this.nextDeliveryId;
            }

            // make sure disposition has been sent for settled deliveries
            protected void Settle()
            {
                if (Interlocked.Increment(ref this.settling) != 1)
                {
                    return;
                }

                SequenceNumber currentNextId = this.nextDeliveryId;
                int settledCount = 0;

                while (true)
                {
                    if (this.unsettledLwm == currentNextId)
                    {
                        // completed the scanning. check to see if new deliveries were processed
                        lock (this.syncRoot)
                        {
                            if (this.nextDeliveryId == currentNextId)
                            {
                                Interlocked.Exchange(ref this.settling, 0);
                                break;
                            }
                            else
                            {
                                currentNextId = this.nextDeliveryId;
                            }
                        }
                    }

                    int index = this.GetBufferIndex(this.unsettledLwm.Value);
                    Delivery delivery = this.deliveryBuffer[index];
                    if (delivery == null || (delivery.Settled && !delivery.StateChanged))
                    {
                        this.unsettledLwm.Increment();
                        this.deliveryBuffer[index] = null;
                        ++settledCount;
                    }
                    else
                    {
                        // this devliery cannot be settled yet. It blocks the following deliveries
                        Interlocked.Exchange(ref this.settling, 0);
                        break;
                    }
                }

                Utils.Trace(TraceLevel.Verbose, "{0}: Lwm:{1}, next-id:{2}", this, this.unsettledLwm.Value, currentNextId.Value);
                if (settledCount > 0)
                {
                    this.OnSettle(settledCount);
                }
            }

            protected int GetBufferIndex(uint deliveryId)
            {
                return (int)(deliveryId % this.deliveryBuffer.Length);
            }

            protected Delivery GetDelivery(SequenceNumber deliveryId)
            {
                int index = this.GetBufferIndex(deliveryId.Value);
                return this.deliveryBuffer[index];
            }

            protected abstract void OnSettle(int count);

            static void DispositionWaitCallback(object state)
            {
                SessionChannel thisPtr = (SessionChannel)state;
                Utils.Trace(TraceLevel.Verbose, "{0}: Wait callback to send a disposition.", thisPtr);

                try
                {
                    thisPtr.DisposeAndSettle();
                }
                catch (Exception exception)
                {
                    if (Fx.IsFatal(exception))
                    {
                        throw;
                    }

                    thisPtr.session.TryClose(exception);
                }
            }
            
            static bool IsEqual(Outcome outcome1, Outcome outcome2)
            {
                if (outcome1 != null && outcome2 != null)
                {
                    // TODO: how about the error messages
                    return outcome1.DescriptorCode == outcome2.DescriptorCode;
                }
                else
                {
                    return outcome1 == null && outcome2 == null;
                }
            }

            static void DispositionTimerCallback(object state)
            {
                SessionChannel thisPtr = (SessionChannel)state;
                Utils.Trace(TraceLevel.Verbose, "{0}: Time to send a disposition.", thisPtr);
                Interlocked.Exchange(ref thisPtr.dispositionScheduled, 0);

                if (Interlocked.Exchange(ref thisPtr.sendingDisposition, 1) == 1)
                {
                    thisPtr.ScheduleDispositionTimer();
                    return;
                }

                try
                {
                    thisPtr.DisposeAndSettle();
                }
                catch (Exception exception)
                {
                    if (Fx.IsFatal(exception))
                    {
                        throw;
                    }

                    thisPtr.session.TryClose(exception);
                }
                finally
                {
                    Interlocked.Exchange(ref thisPtr.sendingDisposition, 0);
                }
            }

            void ScheduleDispositionTimer()
            {
                if (this.session.State != AmqpObjectState.Opened)
                {
                    return;
                }

                if (Interlocked.Exchange(ref this.dispositionScheduled, 1) == 1)
                {
                    return;
                }

                this.dispositionTimer.Set(AmqpConstants.DefaultDispositionTimeout);
                Utils.Trace(TraceLevel.Verbose, "{0}: Started the disposition timer.", this);
            }

            void SendDisposition(bool sendNow)
            {
                if (!sendNow)
                {
                    this.ScheduleDispositionTimer();
                    return;
                }

                if (Interlocked.Exchange(ref this.sendingDisposition, 1) == 1)
                {
                    this.ScheduleDispositionTimer();
                    return;
                }

                //ThreadPool.QueueUserWorkItem(dispositionWaitCallback, this);
                try
                {
                    this.DisposeAndSettle();
                }
                finally
                {
                    Interlocked.Exchange(ref this.sendingDisposition, 0);
                }
            }

            void DisposeAndSettle()
            {
                if (this.session.State != AmqpObjectState.Opened)
                {
                    return;
                }

                bool needSettle = false;
                SequenceNumber start;
                SequenceNumber end;
                lock (this.syncRoot)
                {
                    start = this.unsettledLwm;
                    end = this.nextDeliveryId;
                }

                Disposition disposition = null;
                for (; start != end; start.Increment())
                {
                    Delivery firstDelivery = this.GetDelivery(start);
                    if (firstDelivery == null || !firstDelivery.StateChanged)
                    {
                        continue;
                    }

                    firstDelivery.StateChanged = false;
                    if (firstDelivery.Settled)
                    {
                        needSettle = true;
                    }

                    disposition = new Disposition();
                    disposition.First = start.Value;
                    disposition.Settled = firstDelivery.Settled;
                    disposition.State = firstDelivery.State;
                    disposition.Role = this.IsReceiver;

                    SequenceNumber last = start;
                    int count = 0;
                    for (last.Increment(); last != end; last.Increment())
                    {
                        Delivery lastDelivery = this.GetDelivery(last);
                        if (lastDelivery == null)
                        {
                            ++count;
                            continue;
                        }

                        if (!lastDelivery.StateChanged ||
                            lastDelivery.Settled != firstDelivery.Settled ||
                            !IsEqual(lastDelivery.State as Outcome, firstDelivery.State as Outcome))
                        {
                            break;
                        }

                        lastDelivery.StateChanged = false;
                        ++count;
                    }

                    if (count > 0)
                    {
                        disposition.Last = start + count;
                        start = disposition.Last.Value;
                    }

                    this.session.SendCommand(disposition);
                }

                if (needSettle)
                {
                    this.Settle();
                }
            }
        }

        sealed class OutgoingSessionChannel : SessionChannel
        {
            readonly Action<object> onSettledDeliveryComplete;
            readonly string name;
            readonly SerializedWorker<Delivery> inflightDeliveries;
            readonly uint maxFrameSize;
            SequenceNumber nextOutgoingId;
            uint outgoingWindow;
            uint remoteIncomingWindow;

            public OutgoingSessionChannel(AmqpSession session)
                : base(session, session.settings.OutgoingBufferSize)
            {
                this.name = session.ToString() + "(out)";
                this.maxFrameSize = session.connection.Settings.MaxFrameSize();
                this.onSettledDeliveryComplete = this.OnSettledDeliveryComplete;
                this.inflightDeliveries = new SerializedWorker<Delivery>(this.OnSendDelivery, null, false);
                this.nextOutgoingId = session.settings.NextOutgoingId.Value;
                this.outgoingWindow = session.settings.OutgoingWindow.Value;
                this.remoteIncomingWindow = session.settings.OutgoingWindow.Value;
                this.IsReceiver = false;
            }

            public void SendDelivery(Delivery delivery)
            {
                this.inflightDeliveries.DoWork(delivery);
            }

            public void SendFlow(Flow flow)
            {
                // Once the session flow state is added to flow, the flow frame has to be sent
                // before any additional transfers are sent; otherwise the implicit count on
                // the other side will be wrong. This is for outgoing only. Incoming seems ok.
                lock (this.SyncRoot)
                {
                    this.AddFlowState(flow);
                    this.Session.incomingChannel.AddFlowState(flow);
                    this.Session.SendCommand(flow);
                }
            }

            public void OnBegin(Begin begin)
            {
                lock (this.SyncRoot)
                {
                    if (begin.IncomingWindow.Value == uint.MaxValue)
                    {
                        this.outgoingWindow = this.remoteIncomingWindow = uint.MaxValue;
                    }
                    else
                    {
                        // Can happen in pipeline mode
                        uint alreadySent = this.Session.settings.OutgoingWindow.Value - this.outgoingWindow;
                        if (alreadySent > begin.IncomingWindow.Value)
                        {
                            this.remoteIncomingWindow = 0;
                            this.outgoingWindow = 0;
                        }
                        else
                        {
                            this.remoteIncomingWindow = begin.IncomingWindow.Value - alreadySent;
                            this.outgoingWindow = this.remoteIncomingWindow;
                        }
                    }
                }
            }

            public void OnFlow(Flow flow)
            {
                uint newWindow = 0;
                lock (this.SyncRoot)
                {
                    uint flowNextIncomingId = flow.NextIncomingId.HasValue ? flow.NextIncomingId.Value : 0;
                    if (flow.IncomingWindow.Value == uint.MaxValue)
                    {
                        this.outgoingWindow = this.remoteIncomingWindow = uint.MaxValue;
                    }
                    else
                    {
                        this.remoteIncomingWindow = flowNextIncomingId + flow.IncomingWindow.Value - this.nextOutgoingId.Value;
                        this.outgoingWindow = this.remoteIncomingWindow;
                    }

                    newWindow = this.remoteIncomingWindow;
                }

                if (newWindow > 0)
                {
                    this.inflightDeliveries.ContinueWork();
                }
            }

            public void AddFlowState(Flow flow)
            {
                lock (this.SyncRoot)
                {
                    flow.OutgoingWindow = this.outgoingWindow;
                    flow.NextOutgoingId = this.nextOutgoingId.Value;
                }
            }

            public override string ToString()
            {
                return this.name;
            }

            protected override void OnSettle(int count)
            {
                this.inflightDeliveries.ContinueWork();
            }

            void OnSettledDeliveryComplete(object state)
            {
                Delivery delivery = (Delivery)state;
                delivery.Link.OnDisposeDelivery(delivery);
                this.Settle();
            }

            bool OnSendDelivery(Delivery delivery)
            {
                // TODO: need lock here?
                if (!delivery.DeliveryId.HasValue && !this.TryAddDelivery(delivery))
                {
                    Utils.Trace(TraceLevel.Verbose, "{0}: Buffer full", this);
                    return false;
                }

                bool more = true;
                while (more)
                {
                    lock (this.SyncRoot)
                    {
                        if (this.remoteIncomingWindow == 0)
                        {
                            Utils.Trace(TraceLevel.Verbose, "{0}: Window closed", this);
                            return false;
                        }

                        this.nextOutgoingId.Increment();
                        if (this.remoteIncomingWindow != uint.MaxValue)
                        {
                            --this.remoteIncomingWindow;
                            --this.outgoingWindow;
                        }
                    }

                    Transfer transfer = delivery.GetTransfer(this.maxFrameSize, delivery.Link.LocalHandle.Value, out more);
                    transfer.DeliveryId = delivery.DeliveryId;

                    if (delivery.Settled && !more)
                    {
                        // We want to settle the delivery after network write completes
                        this.Session.SendCommand(transfer, this.onSettledDeliveryComplete, delivery);
                    }
                    else
                    {
                        this.Session.SendCommand(transfer);
                    }
                }

                return true;
            }
        }

        sealed class IncomingSessionChannel : SessionChannel
        {
            readonly string name;
            readonly SerializedWorker<Tuple<AmqpLink, Transfer>> pendingTransfers;
            SequenceNumber nextIncomingId;  // implicit next transfer id
            uint incomingWindow;
            uint flowThreshold;
            uint needFlowCount;
            bool transferEverReceived;

            public IncomingSessionChannel(AmqpSession session)
                : base(session, session.settings.IncomingBufferSize)
            {
                this.name = session.ToString() + "(in)";
                this.pendingTransfers = new SerializedWorker<Tuple<AmqpLink, Transfer>>(this.OnTransfer, null, false);
                this.incomingWindow = session.settings.IncomingWindow();
                this.flowThreshold = this.incomingWindow == uint.MaxValue ? uint.MaxValue : this.incomingWindow * 2 / 3;
                this.IsReceiver = true;
            }

            public void OnReceiveTransfer(AmqpLink link, Transfer transfer)
            {
                if (!transferEverReceived)
                {
                    this.OnReceiveFirstTransfer(transfer);
                    this.transferEverReceived = true;
                }

                lock (this.SyncRoot)
                {
                    if (this.incomingWindow == 0)
                    {
                        Utils.Trace(TraceLevel.Verbose, "{0}: Window closed", this);
                        throw new AmqpException(AmqpError.WindowViolation);
                    }

                    this.nextIncomingId.Increment();
                    --this.incomingWindow;
                }

                this.pendingTransfers.DoWork(new Tuple<AmqpLink, Transfer>(link, transfer));
            }

            public void OnBegin(Begin begin)
            {
                lock (this.SyncRoot)
                {
                    this.nextIncomingId = begin.NextOutgoingId.Value;
                }
            }

            public void OnFlow(Flow flow)
            {
                lock (this.SyncRoot)
                {
                }
            }

            public void OnAcceptSubsequentTransfer(int count)
            {
                this.OnSettle(count);
            }

            public void AddFlowState(Flow flow)
            {
                lock (this.SyncRoot)
                {
                    flow.NextIncomingId = this.nextIncomingId.Value;
                    flow.IncomingWindow = this.incomingWindow;
                    this.needFlowCount = 0;
                }
            }

            public override string ToString()
            {
                return this.name;
            }

            protected override void OnSettle(int count)
            {
                lock (this.SyncRoot)
                {
                    if (this.incomingWindow != uint.MaxValue)
                    {
                        this.incomingWindow += (uint)count;
                        this.needFlowCount += (uint)count;
                    }
                }

                if (this.needFlowCount >= this.flowThreshold)
                {
                    this.needFlowCount = 0;
                    this.Session.SendFlow();
                }

                this.pendingTransfers.ContinueWork();
            }

            bool OnTransfer(Tuple<AmqpLink, Transfer> tuple)
            {
                AmqpLink link = tuple.Item1;
                Transfer transfer = tuple.Item2;
                Delivery newDelivery = null;
                if (transfer.DeliveryId.HasValue &&
                    transfer.DeliveryId.Value == this.NextDeliveryId.Value)
                {
                    newDelivery = link.CreateDelivery();
                    if (!this.TryAddDelivery(newDelivery))
                    {
                        Utils.Trace(TraceLevel.Verbose, "{0}: Buffer full", this);
                        return false;
                    }
                }

                Utils.Trace(TraceLevel.Debug, "{0}: Receive a transfer (id:{1}, settled:{2}) in-win:{3}", this, transfer.DeliveryId, transfer.Settled(), this.incomingWindow);
                link.ProcessTransfer(newDelivery, transfer);
                return true;
            }
        }

        class Diagnostics
        {
            long transferSent;
            long transferReceived;
            long sessionFlowSent;
            long allFlowSent;
            long sessionFlowReceived;
            long linkFlowReceived;
            long dispositionSent;
            long dispositionReceived;

            SequenceNumber lastDispositionId;

            public void SendTransfer()
            {
                Interlocked.Increment(ref this.transferSent);
            }

            public void ReceiveTransfer()
            {
                Interlocked.Increment(ref this.transferReceived);
            }

            public void SendSessionFlow()
            {
                Interlocked.Increment(ref this.sessionFlowSent);
            }

            public void SendFlow()
            {
                Interlocked.Increment(ref this.allFlowSent);
            }

            public void ReceiveFlow(bool linkFlow)
            {
                if (linkFlow)
                {
                    Interlocked.Increment(ref this.linkFlowReceived);
                }
                else
                {
                    Interlocked.Increment(ref this.sessionFlowReceived);
                }
            }

            public void SendDisposition()
            {
                Interlocked.Increment(ref this.dispositionSent);
            }

            public void ReceiveDisposition()
            {
                Interlocked.Increment(ref this.dispositionReceived);
            }

            public void SetLastDisposition(SequenceNumber lastDispositionId)
            {
                this.lastDispositionId = lastDispositionId;
            }

            public override string ToString()
            {
                return string.Format(
                    "s({0},{1},{2},{3}), r({4},{5},{6},{7}), l({8})",
                    this.transferSent,
                    this.sessionFlowSent,
                    this.allFlowSent - this.sessionFlowSent,
                    this.dispositionSent,
                    this.transferReceived,
                    this.sessionFlowReceived,
                    this.linkFlowReceived,
                    this.dispositionReceived,
                    this.lastDispositionId.Value
                    );
            }
        }
    }
}
