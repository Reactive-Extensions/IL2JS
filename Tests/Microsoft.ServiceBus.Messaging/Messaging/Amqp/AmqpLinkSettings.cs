//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    sealed class AmqpLinkSettings : Attach
    {
        uint linkCredit;

        public AmqpLinkSettings()
        {
        }

        public uint TransferLimit
        {
            get
            {
                return this.linkCredit;
            }

            set
            {
                this.linkCredit = value;
                this.FlowThreshold = Math.Min(100, (int)(this.linkCredit * 2 / 3));
            }
        }

        public int FlowThreshold
        {
            get;
            set;
        }

        public bool AutoSendFlow
        {
            get;
            set;
        }

        public SettleMode SettleType
        {
            get 
            {
                return this.SettleType(); 
            }

            set
            {
                switch (value)
                {
                    case Amqp.SettleMode.SettleOnSend:
                        this.SndSettleMode = (byte)SenderSettleMode.Settled;
                        break;
                    case Amqp.SettleMode.SettleOnReceive:
                        break;
                    case Amqp.SettleMode.SettleOnDispose:
                        this.RcvSettleMode = (byte)ReceiverSettleMode.Second;
                        break;
                }
            }
        }

        public static AmqpLinkSettings Create(Attach attach)
        {
            AmqpLinkSettings settings = new AmqpLinkSettings();
            settings.LinkName = attach.LinkName;
            settings.Role = !attach.Role.Value;
            settings.Source = attach.Source;
            settings.Target = attach.Target;
            settings.SndSettleMode = attach.SndSettleMode;
            settings.RcvSettleMode = attach.RcvSettleMode;
            settings.MaxMessageSize = attach.MaxMessageSize;
            settings.Properties = attach.Properties;
            if (settings.Role.Value)
            {
                settings.TransferLimit = AmqpConstants.DefaultLinkCredit;
            }
            else
            {
                settings.InitialDeliveryCount = 0;
            }

            return settings;
        }

        public override bool Equals(object obj)
        {
            AmqpLinkSettings other = obj as AmqpLinkSettings;
            if (other == null || other.LinkName == null)
            {
                return false;
            }

            return this.LinkName.Equals(other.LinkName, StringComparison.CurrentCultureIgnoreCase) &&
                this.Role == other.Role;
        }

        public override int GetHashCode()
        {
            return this.LinkName.GetHashCode() * 397 + this.Role.GetHashCode();
        }
    }
}
