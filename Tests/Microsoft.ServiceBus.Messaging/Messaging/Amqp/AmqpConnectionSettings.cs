//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    sealed class AmqpConnectionSettings : Open
    {
        public AmqpConnectionSettings()
        {
            this.MaxFrameSize = AmqpConstants.DefaultMaxFrameSize;
            this.ChannelMax = AmqpConstants.DefaultMaxConcurrentChannels;
            //this.IdleTimeOut = AmqpConstants.DefaultHeartBeatInterval;
        }

        public string RemoteHostName
        {
            get;
            set;
        }

        public Action<Open> OnOpenCallback
        {
            get;
            set;
        }

        public AmqpConnectionSettings Clone()
        {
            AmqpConnectionSettings newSettings = new AmqpConnectionSettings();

            newSettings.ContainerId = this.ContainerId;
            newSettings.HostName = this.HostName;
            newSettings.MaxFrameSize = this.MaxFrameSize;
            newSettings.ChannelMax = this.ChannelMax;
            newSettings.IdleTimeOut = this.IdleTimeOut;
            newSettings.OutgoingLocales = this.OutgoingLocales;
            newSettings.IncomingLocales = this.IncomingLocales;
            newSettings.Properties = this.Properties;
            newSettings.OfferedCapabilities = this.OfferedCapabilities;
            newSettings.DesiredCapabilities = this.DesiredCapabilities;
            newSettings.Properties = this.Properties;

            return newSettings;
        }
    }
}
