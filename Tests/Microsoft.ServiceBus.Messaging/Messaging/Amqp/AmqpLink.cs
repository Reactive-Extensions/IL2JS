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
    using Microsoft.ServiceBus.Messaging.Amqp.Transaction;

    /// <summary>
    /// Implements the transport-layer link, including
    /// link command handling and link flow control.
    /// </summary>
    abstract class AmqpLink : AmqpObject
    {
        readonly AmqpLinkSettings settings;
        readonly Outcome defaultOutcome;
        readonly Dictionary<ArraySegment<byte>, Delivery> unsettledMap;
        readonly SerializedWorker<Delivery> pendingDeliveries;

        // flow control state
        SequenceNumber deliveryCount;
        uint available;
        uint linkCredit;
        bool drain;

        object syncRoot;
        uint needFlowCount;
        int sendingFlow;

        protected AmqpLink(AmqpSession session, AmqpLinkSettings linkSettings)
        {
            if (linkSettings == null)
            {
                throw new ArgumentNullException("linkSettings");
            }

            this.Session = session;
            this.settings = linkSettings;
            this.linkCredit = this.settings.TransferLimit;
            this.settings.AutoSendFlow = this.linkCredit > 0;
            this.syncRoot = new object();
            Source source = (Source)this.settings.Source;
            if (source != null)
            {
                this.defaultOutcome = source.DefaultOutcome;
            }

            if (this.defaultOutcome == null)
            {
                this.defaultOutcome = AmqpConstants.ReleasedOutcome;
            }

            this.unsettledMap = new Dictionary<ArraySegment<byte>, Delivery>(ByteArrayComparer.Instance);
            this.pendingDeliveries = new SerializedWorker<Delivery>(this.TrySendDelivery, this.AbortDelivery, false);
            session.AttachLink(this);
        }

        public string Name
        {
            get { return this.settings.LinkName; }
        }

        public uint? LocalHandle
        {
            get { return this.settings.Handle; }
            set { this.settings.Handle = value; }
        }

        public uint? RemoteHandle
        {
            get;
            set;
        }

        public AmqpSession Session
        {
            get;
            private set;
        }

        public AmqpLinkSettings Settings
        {
            get { return this.settings; }
        }

        public bool IsReceiver
        {
            get
            {
                return this.settings.Role.Value;
            }
        }

        public uint LinkCredit
        {
            get { return this.linkCredit; }
            set { this.linkCredit = value; }
        }

        protected override string Type
        {
            get { return "link"; }
        }

        public void ProcessFrame(Frame frame)
        {
            Performative command = frame.Command;

            try
            {
                if (command.DescriptorCode == Attach.Code)
                {
                    this.OnReceiveAttach((Attach)command);
                }
                else if (command.DescriptorCode == Detach.Code)
                {
                    this.OnReceiveDetach((Detach)command);
                }
                else if (command.DescriptorCode == Transfer.Code)
                {
                    // Transfer is called by incoming session directly
                    //this.OnReceiveTransfer((Transfer)command, frame.Payload);
                }
                else if (command.DescriptorCode == Flow.Code)
                {
                    this.OnReceiveFlow((Flow)command);
                }
                else
                {
                    throw new AmqpException(AmqpError.InvalidField, "descriptor-code");
                }
            }
            catch (AmqpException exception)
            {
                this.TryClose(exception);
                Utils.Trace(TraceLevel.Error, "{0}: Fault with exception: {1}", this, exception);
            }
        }

        public void OnFlow(Flow flow)
        {
            this.OnReceiveFlow(flow);
        }

        public void SendDelivery(Delivery delivery)
        {
            this.pendingDeliveries.DoWork(delivery);
        }

        // up-down: from application to link to session (to send a disposition)
        public void DisposeDelivery(Delivery delivery, bool settled, DeliveryState state)
        {
            Utils.Trace(TraceLevel.Verbose, "{0}: Dispose delivery (id={1}, settle={2}, state={3}).", this, delivery.DeliveryId, settled, state);
            if (settled && !delivery.Settled)
            {
                lock (this.syncRoot)
                {
                    if (!this.unsettledMap.Remove(delivery.DeliveryTag))
                    {
                        delivery.State = new Rejected() { Error = AmqpError.NotFound };
                        delivery.Complete();
                        return;
                    }
                }
            }

            this.Session.DisposeDelivery(this, delivery, settled, state);

            if (delivery.Settled)
            {
                delivery.Complete();
                this.CheckFlow();
            }
        }

        public bool TryGetDelivery(ArraySegment<byte> deliveryTag, out Delivery delivery)
        {
            return this.unsettledMap.TryGetValue(deliveryTag, out delivery);
        }

        public void SetTransferLimit(uint limit)
        {
            if (limit == this.settings.TransferLimit)
            {
                return;
            }

            lock (this.syncRoot)
            {
                uint currentLimit = this.settings.TransferLimit;
                this.settings.TransferLimit = limit;
                this.settings.AutoSendFlow = limit > 0;
                if (limit == uint.MaxValue)
                {
                    this.linkCredit = limit;
                }
                else if (limit > currentLimit)
                {
                    this.linkCredit += limit - currentLimit;
                }
                else
                {
                    uint reduced = currentLimit - limit;
                    this.linkCredit = this.linkCredit < reduced ? 0 : this.linkCredit - reduced;
                }
            }

            if (this.State != AmqpObjectState.Start && !this.IsClosing())
            {
                this.SendFlow(false);
            }
        }

        public void IssueCredit(uint credit, bool drain, ArraySegment<byte> txnId)
        {
            if (!this.settings.AutoSendFlow)
            {
                lock (this.syncRoot)
                {
                    this.settings.TransferLimit = credit;
                    this.linkCredit = credit;
                }

                if (this.State == AmqpObjectState.Opened)
                {
                    this.SendFlow(false, drain, txnId);
                }
            }
        }

        // bottom-up: from session disposition to link to application
        public void OnDisposeDelivery(Delivery delivery)
        {
            if (delivery.Settled)
            {
                lock (this.syncRoot)
                {
                    this.unsettledMap.Remove(delivery.DeliveryTag);
                }

                delivery.Complete();
                this.CheckFlow();
            }
        }

        public void ProcessTransfer(Delivery delivery, Transfer transfer)
        {
            Utils.Trace(TraceLevel.Verbose, "{0}: Receive a transfer(id:{1}, settled:{2}).", this, transfer.DeliveryId, transfer.Settled());
            if (delivery != null)
            {
                // handle new delivery
                bool creditAvailable = true;
                lock (this.syncRoot)
                {
                    creditAvailable = this.TryChargeCredit();
                }

                if (!creditAvailable)
                {
                    Utils.Trace(TraceLevel.Verbose, "{0}: The transfer {1} was rejected due to insufficient link credit.", this, transfer.DeliveryId.Value);
                    this.TryClose(new AmqpException(AmqpError.TransferLimitExceeded));
                }
                else
                {
                    delivery.Link = this;
                    delivery.DeliveryId = transfer.DeliveryId.Value;
                    delivery.DeliveryTag = transfer.DeliveryTag;
                    delivery.Settled = transfer.Settled();
                    delivery.Batchable = transfer.Batchable();
                    TransactionalState txnState = transfer.State as TransactionalState;
                    if (txnState != null)
                    {
                        delivery.TxnId = txnState.TxnId;
                    }

                    if (!delivery.Settled)
                    {
                        lock (this.syncRoot)
                        {
                            this.unsettledMap.Add(delivery.DeliveryTag, delivery);
                        }
                    }
                }
            }
            else
            {
                this.Session.OnAcceptIncomingTransfer();
            }

            this.OnProcessTransfer(delivery, transfer);
        }

        public abstract Delivery CreateDelivery();

        protected override bool OpenInternal()
        {
            this.SendAttach();
            return this.State == AmqpObjectState.Opened;
        }

        protected override bool CloseInternal()
        {
            this.pendingDeliveries.Abort();
            this.Session.Flush();
            this.SendDetach();
            return this.State == AmqpObjectState.End;
        }

        protected override void AbortInternal()
        {
            this.pendingDeliveries.Abort();
            // ?? what to do with unsettled map ??
            if (this.TerminalException == null)
            {
                this.TerminalException = this.Session.TerminalException;
            }

            if (this.State == AmqpObjectState.OpenReceived)
            {
                this.settings.Source = null;
                this.settings.Target = null;
                this.SendAttach();
            }
        }

        protected void AbortDelivery(Delivery delivery)
        {
            delivery.State = AmqpConstants.ReleasedOutcome;
            delivery.Complete();
        }

        protected void SendFlow(bool echo)
        {
            this.SendFlow(echo, false, AmqpConstants.EmptyBinary);
        }

        protected abstract void OnProcessTransfer(Delivery delivery, Transfer transfer);

        protected abstract void OnCreditAvailable(uint link, bool drain, ArraySegment<byte> txnId);

        bool TrySendDelivery(Delivery delivery)
        {
            Fx.Assert(delivery.DeliveryTag.Array != null, "delivery-tag must be set.");
            Fx.Assert(delivery.BytesTransfered == 0, "delivery has partially transfered.");
            Fx.Assert(delivery.Link == null, "delivery belongs to a different link.");

            // check link credit first
            bool canSend = false;
            lock (this.syncRoot)
            {
                canSend = this.TryChargeCredit();
            }

            if (!canSend)
            {
                Utils.Trace(TraceLevel.Verbose, "{0}: Insufficient link credit. credit:{1}", this, this.linkCredit);
                return false;
            }

            delivery.Link = this;
            delivery.Settled = this.settings.SettleType == SettleMode.SettleOnSend;
            delivery.State = this.defaultOutcome;
            if (!delivery.Settled)
            {
                lock (this.syncRoot)
                {
                    this.unsettledMap.Add(delivery.DeliveryTag, delivery);
                }
            }

            this.Session.SendDelivery(delivery);
            return true;
        }

        void CheckFlow()
        {
            if (this.IsReceiver && this.settings.AutoSendFlow && this.linkCredit < uint.MaxValue)
            {
                bool sendFlow = false;
                lock (this.syncRoot)
                {
                    this.RestoreCredit();
                    sendFlow = this.needFlowCount > this.settings.FlowThreshold;
                }

                if (sendFlow)
                {
                    this.needFlowCount = 0;
                    this.SendFlow(false);
                }
            }
        }

        void SendAttach()
        {
            this.TransitState("S:ATTACH", StateTransition.SendOpen);
            this.Session.SendCommand(this.settings);
        }

        void SendDetach()
        {
            this.TransitState("S:DETACH", StateTransition.SendClose);

            Detach detach = new Detach();
            detach.Handle = this.LocalHandle;
            detach.Closed = true;
            Exception exception = this.TerminalException;
            if (exception != null)
            {
                detach.Error = AmqpError.FromException(exception);
            }

            this.Session.SendCommand(detach);
        }

        void OnReceiveAttach(Attach attach)
        {
            StateTransition stateTransition;
            this.TransitState("R:ATTACH", StateTransition.ReceiveOpen, out stateTransition);

            Error error = this.Negotiate(attach);
            if (error != null)
            {
                this.OnLinkOpenFailed(new AmqpException(error));
                return;
            }

            if (stateTransition.From == AmqpObjectState.OpenSent)
            {
                if (this.IsReceiver)
                {
                    Source source = this.settings.Source as Source;
                    if (source != null && source.Dynamic())
                    {
                        source.Address = ((Source)attach.Source).Address;
                    }
                }
                else
                {
                    Target target = this.settings.Target as Target;
                    if (target != null && target.Dynamic())
                    {
                        target.Address = ((Target)attach.Target).Address;
                    }
                }
            }
            
            if (stateTransition.To == AmqpObjectState.Opened)
            {
                if ((this.IsReceiver && attach.Source == null) ||
                    (!this.IsReceiver && attach.Target == null))
                {
                    // not linkendpoint was created on the remote side
                    // a detach should be sent immediately by peer with error
                    return;
                }

                if (this.IsReceiver)
                {
                    this.deliveryCount = attach.InitialDeliveryCount.Value;
                    this.settings.Source = attach.Source;
                }
                else
                {
                    this.settings.Target = attach.Target;
                }

                this.CompleteOpen(false, null);
            }
            else if (stateTransition.To == AmqpObjectState.OpenReceived)
            {
                Utils.Trace(TraceLevel.Verbose, "{0}: opending.", this);
                try
                {
                    this.Session.Connection.AmqpSettings.RuntimeProvider.BeginOpenLink(this, this.DefaultOpenTimeout, this.OnProviderLinkOpened, null);
                }
                catch (Exception exception)
                {
                    if (Fx.IsFatal(exception))
                    {
                        throw;
                    }

                    this.OnLinkOpenFailed(exception);
                }
            }
        }

        void OnReceiveDetach(Detach detach)
        {
            if (detach.Error != null)
            {
                Utils.Trace(TraceLevel.Error, "{0}: Detach {1}", this, detach.Error);
            }

            StateTransition stateTransition;
            this.TransitState("R:DETACH", StateTransition.ReceiveClose, out stateTransition);

            if (stateTransition.To == AmqpObjectState.End)
            {
                this.CompleteClose(false, null);
            }
            else
            {
                if (detach.Error != null)
                {
                    this.CompleteOpen(false, AmqpException.FromError(detach.Error));
                }

                this.Close();
            }
        }

        void OnReceiveFlow(Flow flow)
        {
            Utils.Trace(TraceLevel.Verbose, "{0}: Receive {1}", this, flow);
            lock (this.syncRoot)
            {
                if (this.IsReceiver)
                {
                    this.available = flow.Available ?? uint.MaxValue;
                    // this.transferCount = flow.TransferCount ?? this.transferCount;
                }
                else
                {
                    this.drain = flow.Drain ?? false;
                    if (flow.LinkCredit() != uint.MaxValue)
                    {
                        if (this.linkCredit == uint.MaxValue)
                        {
                            this.linkCredit = flow.LinkCredit.Value;
                        }
                        else
                        {
                            uint oldCredit = this.linkCredit;
                            uint otherDeliveryCount = flow.DeliveryCount ?? 0;
                            this.linkCredit = unchecked(otherDeliveryCount + flow.LinkCredit.Value - this.deliveryCount.Value);
                        }
                    }
                    else
                    {
                        this.linkCredit = uint.MaxValue;
                    }
                }
            }

            this.pendingDeliveries.ContinueWork();

            if (flow.Echo())
            {
                this.SendFlow(false);
            }

            if (this.linkCredit > 0)
            {
                ArraySegment<byte> txnId = this.GetTxnIdFromFlow(flow);
                this.OnCreditAvailable(this.linkCredit, this.drain, txnId);
            }
        }

        ArraySegment<byte> GetTxnIdFromFlow(Flow flow)
        {
            object txnId;
            if (flow.Properties != null &&
                (txnId = flow.Properties["txn-id"]) != null)
            {
                return (ArraySegment<byte>)txnId;
            }

            return AmqpConstants.EmptyBinary;
        }

        Error Negotiate(Attach attach)
        {
            if (attach.MaxMessageSize.HasValue && attach.MaxMessageSize.Value != 0)
            {
                this.settings.MaxMessageSize = this.settings.MaxMessageSize.HasValue ?
                    Math.Min(this.settings.MaxMessageSize.Value, attach.MaxMessageSize.Value) :
                    attach.MaxMessageSize.Value;
            }

            return null;
        }

        void OnProviderLinkOpened(IAsyncResult result)
        {
            Utils.Trace(TraceLevel.Info, "{0}: Completing open.", this);
            Exception openException = null;

            // Capture the exception from provider first
            try
            {
                this.Session.Connection.AmqpSettings.RuntimeProvider.EndOpenLink(result);
            }
            catch (Exception exception)
            {
                if (Fx.IsFatal(exception))
                {
                    throw;
                }

                openException = exception;
            }

            // Complete the link opening. This may fail if the link state changed already
            // while the provider is opening the link
            try
            {
                if (openException != null)
                {
                    this.OnLinkOpenFailed(openException);
                }
                else
                {
                    this.Open();
                }
            }
            catch (AmqpException amqpException)
            {
                Utils.Trace(TraceLevel.Warning, "{0}: Failed to complete open {1}.", this, amqpException.Message);
                this.Abort();
            }
        }

        void OnLinkOpenFailed(Exception exception)
        {
            Utils.Trace(TraceLevel.Error, "{0}: open failed with exception {1}.", this, exception);
            if (this.State == AmqpObjectState.OpenReceived)
            {
                this.settings.Source = null;
                this.settings.Target = null;
                this.SendAttach();
            }

            this.TryClose(exception);
        }

        bool TryChargeCredit()
        {
            if (this.linkCredit == 0)
            {
                return false;
            }

            this.deliveryCount.Increment();
            if (this.linkCredit != uint.MaxValue)
            {
                --this.linkCredit;
            }

            return true;
        }

        void RestoreCredit()
        {
            if (this.linkCredit != uint.MaxValue)
            {
                ++this.needFlowCount;
                if (this.linkCredit < this.settings.TransferLimit)
                {
                    ++this.linkCredit;
                }
            }
        }

        void SendFlow(bool echo, bool drain, ArraySegment<byte> txnId)
        {
            if (this.IsClosing())
            {
                return;
            }

            if (Interlocked.Exchange(ref this.sendingFlow, 1) == 1)
            {
                return;
            }

            try
            {
                Flow flow = new Flow();
                flow.Handle = this.LocalHandle;
                lock (this.syncRoot)
                {
                    flow.LinkCredit = this.linkCredit;
                    flow.Available = this.available;
                    flow.DeliveryCount = this.deliveryCount.Value;
                }

                if (drain)
                {
                    flow.Drain = true;
                }

                if (echo)
                {
                    flow.Echo = echo;
                }

                if (txnId.Array != null)
                {
                    flow.Properties = new Fields();
                    flow.Properties["txn-id"] = txnId;
                }

                this.Session.SendFlow(flow);
            }
            finally
            {
                Interlocked.Exchange(ref this.sendingFlow, 0);
            }
        }
    }
}
