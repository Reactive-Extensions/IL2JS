// <copyright file="EventLinkChannel.cs" company="Microsoft">
// Copyright © 2009 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects.Client
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using Microsoft.Csa.EventLink;
    using Microsoft.Csa.EventLink.Client;
    using System.Text;
    using Microsoft.Csa.SharedObjects.Utilities;

    using ErrorStateChangeCallback = System.Action<Microsoft.Csa.EventLink.Client.EventLinkClient.ErrorState, System.Exception>;

    internal class EventLinkChannel : BaseEventLinkChannel
    {
        internal EventLinkClient EventLinkClient { get; private set; }
        private Uri baseUri;
        private string partitionId;
        private ErrorStateChangeCallback callback;

        public EventLinkChannel(Uri baseUri, string partitionId, ErrorStateChangeCallback callback)
        {
            this.baseUri = baseUri;
            this.partitionId = partitionId;
            this.callback = callback;
        }

        public override void SubscribeAsync(string channelName)
        {
            this.ChannelName = channelName;
            this.EventLinkClient = new EventLinkClient(baseUri, partitionId, callback);
            this.EventLinkClient.Subscribe(this.ChannelName, EventLinkEventsReceived, this.OnSubscriptionInitialized);
        }

        public override void UnsubscribeAsync(Action completionCallback)
        {
            this.EventLinkClient.Unsubscribe(completionCallback);
            this.EventLinkClient = null;
        }

        public override void SendEvent(string channelName, Payload data)
        {
            this.EventLinkClient.Publish(channelName, new Payload[] { data });
            Debug.WriteLine("EventLinkChannel.SendEvent Payload: {0} on channel {1} from {2}: {3}", data.PayloadType, channelName, data.ClientId, data.ToJsonString());
        }

        public override void SendEvent(string channelName, Payload[] data)
        {
            for (int i = 0; i < data.Length; ++i)
            {
                Debug.WriteLine("EventLinkChannel.SendEvent[] Payload: {0} on channel {1} from {2}: {3}", data[i].PayloadType, channelName, data[i].ClientId, data[i].ToJsonString());
            }
            this.EventLinkClient.Publish(channelName, data);
        }

        private void EventLinkEventsReceived(EventSet[] eventSets)
        {
            if (eventSets == null)
            {
                return;
            }

            var payloads = from es in eventSets
                           from payload in es.Payloads
                           select payload;
            foreach (Payload p in payloads)
            {
                Debug.WriteLine("Receiving Payload: {0} on channel {1}: {2}", p.PayloadType, this.ChannelName, p.ToJsonString());
            }
            this.OnEventsReceived(payloads);
        }
    }
}
