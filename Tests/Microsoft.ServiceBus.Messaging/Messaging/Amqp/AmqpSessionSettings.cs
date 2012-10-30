//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    sealed class AmqpSessionSettings : Begin
    {
        public AmqpSessionSettings()
        {
            this.NextOutgoingId = AmqpConstants.DefaultNextTransferId;
            this.IncomingWindow = AmqpConstants.DefaultWindowSize;
            this.OutgoingWindow = AmqpConstants.DefaultWindowSize;
            this.HandleMax = AmqpConstants.DefaultMaxLinkHandles;
            this.OutgoingBufferSize = AmqpConstants.DefaultSessionBufferSize;
            this.IncomingBufferSize = AmqpConstants.DefaultSessionBufferSize;
        }

        public int DispositionThreshold
        {
            get;
            set;
        }

        public SequenceNumber InitialDeliveryId
        {
            get;
            set;
        }

        public int OutgoingBufferSize
        {
            get;
            set;
        }

        public int IncomingBufferSize
        {
            get;
            set;
        }

        public static AmqpSessionSettings Create(Begin begin)
        {
            AmqpSessionSettings settings = new AmqpSessionSettings();
            settings.IncomingWindow = begin.OutgoingWindow;
            settings.OutgoingWindow = begin.IncomingWindow;
            settings.HandleMax = Math.Min(settings.HandleMax.Value, begin.HandleMax());

            return settings;
        }

        public AmqpSessionSettings Clone()
        {
            AmqpSessionSettings settings = new AmqpSessionSettings();
            settings.DispositionThreshold = this.DispositionThreshold;
            settings.IncomingBufferSize = this.IncomingBufferSize;
            settings.OutgoingBufferSize = this.OutgoingBufferSize;
            settings.InitialDeliveryId = this.InitialDeliveryId;
            settings.NextOutgoingId = this.NextOutgoingId;
            settings.IncomingWindow = this.IncomingWindow;
            settings.OutgoingWindow = this.OutgoingWindow;
            settings.HandleMax = this.HandleMax;
            settings.OfferedCapabilities = this.OfferedCapabilities;
            settings.DesiredCapabilities = this.DesiredCapabilities;
            settings.Properties = this.Properties;

            return settings;
        }
    }
}
