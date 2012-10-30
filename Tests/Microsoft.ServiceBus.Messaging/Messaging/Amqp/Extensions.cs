//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;
    using Microsoft.ServiceBus.Messaging.Amqp.Transaction;
    using Microsoft.ServiceBus.Common;

    /// <summary>
    /// make it convenient to deal with nullable types
    /// also set the default value if the field is null
    /// </summary>
    static class Extensions
    {
        // open
        public static uint MaxFrameSize(this Open open)
        {
            return open.MaxFrameSize == null ? uint.MaxValue : open.MaxFrameSize.Value;
        }

        public static ushort ChannelMax(this Open open)
        {
            return open.ChannelMax == null ? ushort.MaxValue : open.ChannelMax.Value;
        }

        public static uint IdleTimeOut(this Open open)
        {
            return open.IdleTimeOut == null || open.IdleTimeOut.Value == 0 ? uint.MaxValue : open.IdleTimeOut.Value;
        }

        // begin
        public static ushort RemoteChannel(this Begin begin)
        {
            return begin.RemoteChannel == null ? (ushort)0 : begin.RemoteChannel.Value;
        }

        public static uint HandleMax(this Begin begin)
        {
            return begin.HandleMax == null ? uint.MaxValue : begin.HandleMax.Value;
        }

        public static uint OutgoingWindow(this Begin begin)
        {
            return begin.OutgoingWindow == null ? uint.MaxValue : begin.OutgoingWindow.Value;
        }

        public static uint IncomingWindow(this Begin begin)
        {
            return begin.IncomingWindow == null ? uint.MaxValue : begin.IncomingWindow.Value;
        }

        // attach
        public static bool IsReceiver(this Attach attach)
        {
            return attach.Role.Value;
        }

        public static bool IncompleteUnsettled(this Attach attach)
        {
            return attach.IncompleteUnsettled == null ? false : attach.IncompleteUnsettled.Value;
        }

        public static ulong MaxMessageSize(this Attach attach)
        {
            return attach.MaxMessageSize == null ? ulong.MaxValue : attach.MaxMessageSize.Value;
        }

        public static Terminus Terminus(this Attach attach)
        {
            if (attach.IsReceiver())
            {
                Source source = attach.Source as Source;
                return source == null ? null : new Terminus(source);
            }
            else
            {
                Target target = attach.Target as Target;
                return target == null ? null : new Terminus(target);
            }
        }

        public static Address Address(this Attach attach)
        {
            if (attach.IsReceiver())
            {
                Fx.Assert(attach.Source != null && attach.Source is Source, "Source is not valid.");
                return ((Source)attach.Source).Address;
            }
            else
            {
                Fx.Assert(attach.Target != null && attach.Target is Target, "Target is not valid.");
                return ((Target)attach.Target).Address;
            }
        }

        public static bool Dynamic(this Attach attach)
        {
            if (attach.IsReceiver())
            {
                Fx.Assert(attach.Source != null && attach.Source is Source, "Source is not valid.");
                return ((Source)attach.Source).Dynamic();
            }
            else
            {
                Fx.Assert(attach.Target != null && attach.Target is Target, "Target is not valid.");
                return ((Target)attach.Target).Dynamic();
            }
        }

        public static SettleMode SettleType(this Attach attach)
        {
            SenderSettleMode ssm = attach.SndSettleMode.HasValue ? (SenderSettleMode)attach.SndSettleMode.Value : SenderSettleMode.Mixed;
            ReceiverSettleMode rsm = attach.RcvSettleMode.HasValue ? (ReceiverSettleMode)attach.RcvSettleMode.Value : ReceiverSettleMode.First;

            if (ssm == SenderSettleMode.Settled)
            {
                return Amqp.SettleMode.SettleOnSend;
            }
            else
            {
                if (rsm == ReceiverSettleMode.First)
                {
                    return Amqp.SettleMode.SettleOnReceive;
                }
                else
                {
                    return Amqp.SettleMode.SettleOnDispose;
                }
            }
        }

        public static Attach Clone(this Attach attach)
        {
            Attach clone = new Attach();
            clone.LinkName = attach.LinkName;
            clone.Role = attach.Role;
            clone.SndSettleMode = attach.SndSettleMode;
            clone.RcvSettleMode = attach.RcvSettleMode;
            clone.Source = attach.Source;
            clone.Target = attach.Target;
            clone.Unsettled = attach.Unsettled;
            clone.IncompleteUnsettled = attach.IncompleteUnsettled;
            clone.InitialDeliveryCount = attach.InitialDeliveryCount;
            clone.MaxMessageSize = attach.MaxMessageSize;
            clone.OfferedCapabilities = attach.OfferedCapabilities;
            clone.DesiredCapabilities = attach.DesiredCapabilities;
            clone.Properties = attach.Properties;

            return clone;
        }

        public static Source Clone(this Source source)
        {
            Source clone = new Source();
            clone.Address = source.Address;
            clone.Durable = source.Durable;
            clone.ExpiryPolicy = source.ExpiryPolicy;
            clone.Timeout = source.Timeout;
            clone.DistributionMode = source.DistributionMode;
            clone.FilterSet = source.FilterSet;
            clone.DefaultOutcome = source.DefaultOutcome;
            clone.Outcomes = source.Outcomes;
            clone.Capabilities = source.Capabilities;

            return clone;
        }

        public static Target Clone(this Target target)
        {
            Target clone = new Target();
            clone.Address = target.Address;
            clone.Durable = target.Durable;
            clone.ExpiryPolicy = target.ExpiryPolicy;
            clone.Timeout = target.Timeout;
            clone.Capabilities = target.Capabilities;

            return clone;
        }

        // transfer
        public static bool Settled(this Transfer transfer)
        {
            return transfer.Settled == null ? false : transfer.Settled.Value;
        }

        public static bool More(this Transfer transfer)
        {
            return transfer.More == null ? false : transfer.More.Value;
        }

        public static bool Resume(this Transfer transfer)
        {
            return transfer.Resume == null ? false : transfer.Resume.Value;
        }

        public static bool Aborted(this Transfer transfer)
        {
            return transfer.Aborted == null ? false : transfer.Aborted.Value;
        }

        public static bool Batchable(this Transfer transfer)
        {
            return transfer.Batchable == null ? false : transfer.Batchable.Value;
        }

        // disposition
        public static bool Settled(this Disposition disposition)
        {
            return disposition.Settled == null ? false : disposition.Settled.Value;
        }

        public static bool Batchable(this Disposition disposition)
        {
            return disposition.Batchable == null ? false : disposition.Batchable.Value;
        }

        // flow
        public static uint LinkCredit(this Flow flow)
        {
            return flow.LinkCredit.HasValue ? flow.LinkCredit.Value : uint.MaxValue;
        }

        public static bool Echo(this Flow flow)
        {
            return flow.Echo == null ? false : flow.Echo.Value;
        }

        // detach
        public static bool Closed(this Detach detach)
        {
            return detach.Closed == null ? false : detach.Closed.Value;
        }

        // message header
        public static bool Durable(this Header header)
        {
            return header.Durable.HasValue && header.Durable.Value;
        }

        public static byte Priority(this Header header)
        {
            return header.Priority == null ? (byte)0 : header.Priority.Value;
        }

        public static uint Ttl(this Header header)
        {
            return header.Ttl == null ? (uint)0 : header.Ttl.Value;
        }

        public static bool FirstAcquirer(this Header header)
        {
            return header.FirstAcquirer == null ? false : header.FirstAcquirer.Value;
        }

        public static uint DeliveryCount(this Header header)
        {
            return header.DeliveryCount == null ? (uint)0 : header.DeliveryCount.Value;
        }

        // message property
        public static DateTime AbsoluteExpiryTime(this Properties properties)
        {
            return properties.AbsoluteExpiryTime == null ? default(DateTime) : properties.AbsoluteExpiryTime.Value;
        }

        public static DateTime CreationTime(this Properties properties)
        {
            return properties.CreationTime == null ? default(DateTime) : properties.CreationTime.Value;
        }

        public static SequenceNumber GroupSequence(this Properties properties)
        {
            return properties.GroupSequence == null ? 0 : properties.GroupSequence.Value;
        }

        // delivery
        public static bool Transactional(this Delivery delivery)
        {
            return delivery.State != null && delivery.State.DescriptorCode == TransactionalState.Code;
        }

        // Source and Target
        public static bool Dynamic(this Source source)
        {
            return source.Dynamic == null ? false : source.Dynamic.Value;
        }

        public static bool Dynamic(this Target target)
        {
            return target.Dynamic == null ? false : target.Dynamic.Value;
        }

        public static bool Durable(this Source source)
        {
            return source.Durable == null ? false : (TerminusDurability)source.Durable.Value == TerminusDurability.None;
        }

        public static bool Durable(this Target target)
        {
            return target.Durable == null ? false : (TerminusDurability)target.Durable.Value == TerminusDurability.None;
        }
    }
}
