// <copyright file="Interfaces.cs" company="Microsoft">
// Copyright © 2010 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects
{
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Collections.Generic;

    internal abstract class BaseEventLinkChannel
    {
        public string ChannelName { get; protected set; }

        public event Action<IEnumerable<Payload>> EventsReceived;
        public event Action SubscriptionInitialized;

        protected void OnSubscriptionInitialized()
        {
            if (this.SubscriptionInitialized != null)
            {
                this.SubscriptionInitialized();
            }
        }

        protected void OnEventsReceived(IEnumerable<Payload> payloads)
        {
            if (this.EventsReceived != null)
            {
                this.EventsReceived(payloads);
            }
        }

        public abstract void SubscribeAsync(string channelName);
        public abstract void UnsubscribeAsync(Action completionCallback);

        public abstract void SendEvent(string channelName, Payload data);
        public abstract void SendEvent(string channelName, Payload[] data);
    }

    public interface ISharedObjectEntry
    {
        SharedObjectSecurity ObjectSecurity { get; set; }
        Guid Id { get; }
        string Name { get; }
    }

    public interface ISharedAsyncResult : IAsyncResult
    {
        void SetAsCompleted(Exception exception, bool completedSynchronously);
    }

    /// <summary>
    /// Interface for Script-Exposed ObservableDictionary
    /// </summary>
    internal interface IJsObject : IDictionary<string, object>, IDictionary, INotifyPropertyChanged
    {
        event EventHandler JsPropertyChanged;
    }
}
