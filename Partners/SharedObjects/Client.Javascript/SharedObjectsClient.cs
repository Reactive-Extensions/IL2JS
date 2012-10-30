// <copyright file="SharedObjectsClient.cs" company="Microsoft">
// Copyright © 2009 Microsoft Corporation.  All rights reserved.
// </copyright>

using System.Runtime.Serialization.Json;

namespace Microsoft.Csa.SharedObjects.Client
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using System.Windows.Threading;
    using Microsoft.Csa.EventLink.Client;
    using Microsoft.Csa.SharedObjects;
    using Microsoft.Csa.SharedObjects.Utilities;

    public partial class SharedObjectsClient : ISharedObjectEntry
    {
        //TODO document all public members

        #region Variables & Properties
        public Dispatcher Dispatcher
        {
            get;
            private set;
        }

        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event EventHandler MessageReceived;
        public event EventHandler<ObjectDeletedEventArgs> ObjectDeleted;
        public event EventHandler<CollectionDeletedEventArgs> CollectionDeleted;

        public Guid ClientId { get; private set; }

        private INotifyPropertyChanged principalObject;
        public INotifyPropertyChanged PrincipalObject
        {
            get
            {
                return principalObject;
            }
            set
            {
                if (principalObject != value)
                {
                    principalObject = value;
                    Debug.Assert(!this.Connecting, "Principal can't be set while connecting");
                    if (this.IsConnected)
                    {
                        RegisterPrincipalPayload payload = new RegisterPrincipalPayload(this.ClientId, this.principalObject);
                        this.Channel.SendEvent(this.publishUpdatesChannelName, payload);
                    }
                }
            }
        }

        public string Namespace { get; private set; }
        public NamespaceLifetime NamespaceLifetime { get; private set; }
        public SharedInterlocked SharedInterlocked { get; private set; }

        private bool connected = false;
        public bool IsConnected { get { return connected; } }
        internal ProtocolVersion ServerVersion { get; private set; }

        public SharedObjectSecurity ObjectSecurity { get; set; }
        /// <summary>
        /// Gets the Id of the Namespace that this SharedObjectsClient is connected to.
        /// </summary>
        public Guid Id { get { return Constants.RootId; } }
        /// <summary>
        /// Gets the name of the Namespace that this SharedObjectsClient is connected to.
        /// </summary>
        public string Name { get { return this.Namespace; } }

        internal bool Connecting { get; set; }
        internal bool IgnoreUpdates { get; private set; }
        internal CollectionsManager CollectionsManager { get; private set; }
        internal ObjectsManager ObjectsManager { get; private set; }

        internal BaseEventLinkChannel Channel { get; set; }
        private string globalListeningChannelName;
        private string publishUpdatesChannelName;
        private long ticksLastMessageReceived;

        /// <summary>
        /// Used to ensure that at most one incoming change is being applied at any
        /// given time.  Guarantees that all changes are applied in order.
        /// </summary>
        private object processingLock = new object();
        #endregion

        #region Initialization
        public SharedObjectsClient(string namespaceUri, INotifyPropertyChanged principalObject, Dispatcher dispatcher)
            : this(namespaceUri, NamespaceLifetime.Default, principalObject, dispatcher)
        {
        }

        public SharedObjectsClient(string namespaceUri, NamespaceLifetime namespaceLifetime, INotifyPropertyChanged principalObject, Dispatcher dispatcher)
        {
            Uri uri = new Uri(namespaceUri);

            if (dispatcher == null)
            {
#if IL2JS
                this.Dispatcher = null;
#elif SILVERLIGHT
                // The Silverlight Client will extract the curernt dispatcher by accessing the RootVisual
                if (Application.Current != null && Application.Current.RootVisual != null && Application.Current.RootVisual.Dispatcher != null)
                {
                    this.Dispatcher = Application.Current.RootVisual.Dispatcher;
                }
                else
                {
                    // if we did not get the Dispatcher throw an exception
                    throw new InvalidOperationException("The SharedObject client must be initialized after the RootVisual has been loaded");
                }
#else
                this.Dispatcher = Dispatcher.CurrentDispatcher;
#endif
            }
            else
            {
                this.Dispatcher = dispatcher;
            }

            // The Namespace is the path
            string namespaceName = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
            string endPoint = uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);

            if (string.IsNullOrEmpty(namespaceName))
            {
                throw new ArgumentException("namespaceUri", "Invalid namespace Uri specified");
            }

            this.Namespace = namespaceName;
            this.NamespaceLifetime = namespaceLifetime;

            this.CollectionsManager = new CollectionsManager(this);
            this.ObjectsManager = new ObjectsManager(this);
            this.SharedInterlocked = new SharedInterlocked(this);

            this.principalObject = principalObject;

            this.ticksLastMessageReceived = DateTime.Now.Ticks;

            this.globalListeningChannelName = Channels.GetGlobalListeningName();
            this.publishUpdatesChannelName = Channels.GetPublishUpdatesName(this.Namespace);

            this.Channel = new EventLinkChannel(new Uri(endPoint), namespaceName, OnErrorStateChanged);
        }

        public SharedObjectsClient(string namespaceUri, NamespaceLifetime namespaceLifetime)
            : this(namespaceUri, namespaceLifetime, null, null)
        {
        }

        public SharedObjectsClient(string namespaceUri, NamespaceLifetime namespaceLifetime, INotifyPropertyChanged principalObject)
            : this(namespaceUri, namespaceLifetime, principalObject, null)
        {
        }

        /// <summary>
        /// Connect the client to the Shared Object Server
        /// </summary>
        public void Connect()
        {
            if (this.Connecting)
            {
                throw new InvalidOperationException("Client is already connecting");
            }

            this.Connecting = true;
            this.ClientId = Guid.NewGuid();

            if (string.IsNullOrEmpty(this.Namespace))
            {
                throw new Exception("Namespace name cannot be null or empty");
            }

            Debug.Assert(this.CollectionsManager.Values.All(e => e.IsConnected == false));
            this.InitializeEventLinkChannels();
        }

        internal void StartIgnoreUpdates()
        {
            Debug.WriteLine("[Client] Begin ignoring updates on client {0}", this.ClientId);
            this.IgnoreUpdates = true;
        }

        internal void StopIgnoreUpdates()
        {
            Debug.WriteLine("[Client] End ignoring updates on client {0}", this.ClientId);
            this.IgnoreUpdates = false;
        }

        public void Disconnect(Action completionCallback)
        {
            bool needToCallback = true;
            try
            {
                if (this.IsConnected)
                {
                    this.CollectionsManager.OnDisconnect();
                    if (this.CollectionsManager.HeartbeatTimer != null)
                        this.CollectionsManager.HeartbeatTimer.Stop();
                    this.ObjectsManager.OnDisconnect();

                    if (this.Channel != null)
                    {
                        this.Channel.SubscriptionInitialized -= OnIncomingChannelsInitialized;
                        this.Channel.EventsReceived -= OnIncomingChannelsDataReceived;
                        this.Channel.UnsubscribeAsync(completionCallback);
                        needToCallback = false;
                    }
                    this.connected = false;

                    RaiseDisconnected();
                }
                this.Connecting = false;
            }
            finally
            {
                if (needToCallback && completionCallback != null)
                    completionCallback();
            }
        }

        private void InitializeEventLinkChannels()
        {
            // Make sure that we aren't already registered for the events so that we don't receive duplicates.
            this.Channel.SubscriptionInitialized -= OnIncomingChannelsInitialized;
            this.Channel.EventsReceived -= OnIncomingChannelsDataReceived;

            this.Channel.SubscriptionInitialized += OnIncomingChannelsInitialized;
            this.Channel.EventsReceived += OnIncomingChannelsDataReceived;
            this.Channel.SubscribeAsync(Channels.GetClientName(this.ClientId));
        }

        private void OnIncomingChannelsInitialized()
        {
            string clientSubscriptionId = this.Channel.ChannelName;

            // TODO: the ClientId could probably just be the clientSubscriptionId
            ClientConnectPayload payload = new ClientConnectPayload(clientSubscriptionId, this.ClientId, this.Namespace, this.NamespaceLifetime, this.PrincipalObject);
            this.Channel.SendEvent(this.globalListeningChannelName, payload);
        }

        internal void RaiseConnected(ClientConnectPayload payload)
        {
            if (!this.IsConnected && this.Connecting)
            {
                this.Connecting = false;
                this.connected = true;

                if (payload.SenderVersion.Major != Payload.ProtocolVersion.Major)
                    throw new InvalidOperationException("Mismatched server major version");
                ServerVersion = payload.SenderVersion;

                this.CollectionsManager.OnConnect();
                this.ObjectsManager.OnConnect();

                if (this.Connected != null)
                {
                    this.Connected(this, EventArgs.Empty);
                }
            }
        }


        private void RaiseDisconnected()
        {
            if (this.Disconnected != null)
            {
                this.Disconnected(this, EventArgs.Empty);
            }
        }

        internal void VerifyConnected()
        {
            if (!this.IsConnected)
            {
                throw new ClientDisconnectedException("Changes not allowed until client is online");
            }
        }
        #endregion

        /// <summary>
        /// Sends a string to the Server for it to Trace out
        /// </summary>
        /// <param name="message"></param>
        public void Trace(string message)
        {
            //var payload = new TracePayload(message, this.ClientId);
            //this.Channel.SendEvent(this.globalListeningChannelName, payload);
        }

#if USING_SERVER_COMMAND
        private void SendServerCommand(ISharedObjectSerializable command)
        {
            SendPublishEvent(new ServerCommandPayload(command));
        }
#endif

        /// <summary>
        /// Sends a string to the Server for it to Trace out
        /// </summary>
        /// <param name="message"></param>
        public void Trace(string format, params object[] args)
        {
            Trace(string.Format(format, args));
        }

        #region Shared Objects


        /// <summary>
        /// Opens an object with the specified name
        /// </summary>
        /// <param name="name">The object to open</param>
        /// <param name="mode">An ObjectMode value that specifies whether an object is created if one does not exist, and determines whether the contents of existing object is retained or overwritten.</param>
        /// <returns>A shared object with the specified name</returns>  
        public T OpenObject<T>(string name, ObjectMode mode) where T : INotifyPropertyChanged, new()
        {
            return this.ObjectsManager.Open<T>(name, mode, null);
        }

        /// <summary>
        /// Opens an object with the specified name and provides a callback for notification when the object is connected to the server
        /// </summary>
        /// <param name="name">The object to open</param>
        /// <param name="mode">An ObjectMode value that specifies whether an object is created if one does not exist, and determines whether the contents of existing object is retained or overwritten.</param>
        /// <param name="callback">Method to execute when the object is connected to the server</param>
        /// <returns>A shared object with the specified name</returns>  
        public T OpenObject<T>(string name, ObjectMode mode, Action<ObjectConnectedEventArgs> callback) where T : INotifyPropertyChanged, new()
        {
            return this.ObjectsManager.Open<T>(name, mode, callback);
        }

        /// <summary>
        /// Creates or overwrites an object with the specified name
        /// </summary>
        /// <param name="name">The collection to create</param>        
        /// <returns>A SharedObservableCollection with the specified name</returns>  
        public T CreateObject<T>(string name) where T : INotifyPropertyChanged, new()
        {
            if (name == Constants.PresenceCollectionName)
            {
                throw new ArgumentException("The name you specified is a reserved system name. To connect to the presence collection call OpenPresenceCollection", "name");
            }
            return OpenObject<T>(name, ObjectMode.Create);
        }

        public void CloseObject(INotifyPropertyChanged value)
        {
            this.ObjectsManager.Close(value);
        }

        public void DeleteObject(string name)
        {
            this.ObjectsManager.Delete(name);
        }

        public void DeleteObject(INotifyPropertyChanged value)
        {
            this.ObjectsManager.Delete(value);
        }

        #endregion

        #region Shared Collections

        /// <summary>
        /// Opens a Collection with the specified name
        /// </summary>
        /// <param name="name">The collection to open</param>
        /// <param name="mode">An ObjectMode value that specifies whether a collection is created if one does not exist, and determines whether the contents of existing collections are retained or overwritten.</param>
        /// <returns>A SharedObservableCollection with the specified name</returns>  
        public SharedObservableCollection<INotifyPropertyChanged> OpenCollection(string name, ObjectMode mode, Action<CollectionConnectedEventArgs> onConnected)
        {
            return this.OpenCollection<INotifyPropertyChanged>(name, mode, onConnected);
        }

        /// <summary>
        /// Opens a strongly typed Collection with the specified name
        /// </summary>
        /// <param name="name">The collection to open</param>
        /// <param name="mode">An ObjectMode value that specifies whether a collection is created if one does not exist, and determines whether the contents of existing collections are retained or overwritten.</param>
        /// <returns>A SharedObservableCollection with the specified name</returns>  
        public SharedObservableCollection<T> OpenCollection<T>(string name, ObjectMode mode, Action<CollectionConnectedEventArgs> onConnected) where T : INotifyPropertyChanged
        {
            if (name == Constants.PresenceCollectionName)
            {
                throw new ArgumentException("The name you specified is a reserved system name. To connect to the presence collection call OpenPresenceCollection", "name");
            }
            return this.CollectionsManager.OpenCollection<T>(name, mode, onConnected);
        }

        /// <summary>
        /// Creates or overwrites a Collection with the specified name
        /// </summary>
        /// <param name="name">The collection to create</param>        
        /// <returns>A SharedObservableCollection with the specified name</returns>  
        public SharedObservableCollection<INotifyPropertyChanged> CreateCollection(string name, Action<CollectionConnectedEventArgs> onConnected)
        {
            return this.CreateCollection<INotifyPropertyChanged>(name, onConnected);
        }

        /// <summary>
        /// Creates or overwrites a strongly typed Collection with the specified name
        /// </summary>
        /// <param name="name">The collection to open</param>
        /// <returns>A SharedObservableCollection with the specified name</returns>  
        public SharedObservableCollection<T> CreateCollection<T>(string name, Action<CollectionConnectedEventArgs> onConnected) where T : INotifyPropertyChanged
        {
            return OpenCollection<T>(name, ObjectMode.Create, onConnected);
        }

        /// <summary>
        /// Opens a Bag with the specified name
        /// </summary>
        /// <param name="name">The bag to open</param>
        /// <param name="mode">An ObjectMode value that specifies whether a bag is created if one does not exist, and determines whether the contents of existing bags are retained or overwritten.</param>
        /// <returns>A SharedObservableBag with the specified name</returns>  
        public SharedObservableBag<INotifyPropertyChanged> OpenBag(string name, ObjectMode mode, Action<CollectionConnectedEventArgs> onConnected)
        {
            return this.OpenBag<INotifyPropertyChanged>(name, mode, onConnected);
        }

        /// <summary>
        /// Opens a strongly typed Bag with the specified name
        /// </summary>
        /// <param name="name">The bag to open</param>
        /// <param name="mode">An ObjectMode value that specifies whether a bag is created if one does not exist, and determines whether the contents of existing bags are retained or overwritten.</param>
        /// <returns>A SharedObservableBag with the specified name</returns>  
        public SharedObservableBag<T> OpenBag<T>(string name, ObjectMode mode, Action<CollectionConnectedEventArgs> onConnected) where T : INotifyPropertyChanged
        {
            if (name == Constants.PresenceCollectionName)
            {
                throw new ArgumentException("The name you specified is a reserved system name. To connect to the presence bag call OpenPresenceCollection", "name");
            }
            return this.CollectionsManager.OpenBag<T>(name, mode, onConnected);
        }

        /// <summary>
        /// Creates or overwrites a Bag with the specified name
        /// </summary>
        /// <param name="name">The bag to create</param>        
        /// <returns>A SharedObservableBag with the specified name</returns>  
        public SharedObservableBag<INotifyPropertyChanged> CreateBag(string name, Action<CollectionConnectedEventArgs> onConnected)
        {
            return this.CreateBag<INotifyPropertyChanged>(name, onConnected);
        }

        /// <summary>
        /// Creates or overwrites a strongly typed Bag with the specified name
        /// </summary>
        /// <param name="name">The bag to open</param>
        /// <returns>A SharedObservableBag with the specified name</returns>  
        public SharedObservableBag<T> CreateBag<T>(string name, Action<CollectionConnectedEventArgs> onConnected) where T : INotifyPropertyChanged
        {
            return OpenBag<T>(name, ObjectMode.Create, onConnected);
        }

        public SharedObservableBag<T> OpenPresenceBag<T>(Action<CollectionConnectedEventArgs> onConnected) where T : INotifyPropertyChanged
        {
            return this.CollectionsManager.OpenBag<T>(Constants.PresenceCollectionName, ObjectMode.Open, onConnected);
        }

        public void CloseCollection(SharedCollection collection)
        {
            this.CollectionsManager.Close(collection.Id);
        }

        public void DeleteCollection(string name)
        {
            this.CollectionsManager.Delete(name);
        }

        #endregion

        #region Incoming Events
        private void OnIncomingChannelsDataReceived(IEnumerable<Payload> payloads)
        {
            ticksLastMessageReceived = DateTime.Now.Ticks;

            if (this.IgnoreUpdates)
            {
                Debug.WriteLine("Ignored all objects or event list is empty");
                return;
            }

            lock (processingLock)
            {
                this.ProcessEvents(payloads.ToList());
            }
        }

        /// <summary>
        /// Process the events, batching sequential events of the same type together when possible.
        /// Since processing might be dispatched to UI thread, wait for signal before proceeding to next batch. 
        /// </summary>
        /// <param name="elData"></param>
        private void ProcessEvents(IList<Payload> elData)
        {
            List<Payload> currentBatch = new List<Payload>();

            while (elData.Count > 0)
            {
                Payload eld = elData[0];
                if (currentBatch.Count == 0 || currentBatch[0].PayloadType == eld.PayloadType)
                {
                    currentBatch.Add(eld);
                    elData.RemoveAt(0);
                }
                else
                {
                    this.ProcessBatch(currentBatch);
                    currentBatch.Clear();
                }
            }

            if (currentBatch.Count > 0)
            {
                this.ProcessBatch(currentBatch);
                currentBatch.Clear();
            }
        }

        private void ProcessBatch(IEnumerable<Payload> payloads)
        {
            // All incoming changes must be applied on the Dispatcher thread
            this.RunOnDispatcher(() =>
            {
                this.OnIncomingPayload(payloads);
                this.ObjectsManager.OnIncomingPayload(payloads);
                this.CollectionsManager.OnIncomingPayload(payloads);
            });
        }

        private void OnIncomingPayload(IEnumerable<Payload> payloads)
        {
            // All incoming changes must be processed on the Dispatcher thread. The invokation onto that
            // thread happens in the layers above this one for greater centralization
            Debug.Assert(this.CheckAccess());

            switch (payloads.First().PayloadType)
            {
                case PayloadType.RegisterClient:
                    Debug.Assert(payloads.Count() == 1);
                    RaiseConnected((ClientConnectPayload)payloads.First());
                    break;

                case PayloadType.ObjectSecurity:
                    NotifyObjectSecurityPayload(payloads);
                    break;

                case PayloadType.EvictionPolicy:
                    NotifyEvictionPolicyPayload(payloads);
                    break;

                case PayloadType.AtomicOperation:
                    NotifyAtomicPayload(payloads);
                    break;

                case PayloadType.DirectMessage:
                    RaiseMessageReceived(payloads);
                    break;
                case PayloadType.Error:
                case PayloadType.ObjectError:
                case PayloadType.ObjectPropertyError:
                case PayloadType.UnauthorizedError:
                    NotifyErrors(payloads);
                    break;
            }
        }

        private void RaiseMessageReceived(IEnumerable<Payload> events)
        {
            foreach (Payload payload in events)
            {
                MessagePayload messagePayload = (MessagePayload)payload;
                if (this.MessageReceived == null)
                {
                    var error = new ErrorPayload(SharedObjects.Error.General, "Message ignored.",
                        "Your client received a message.  The message was ignored because the MessageReceived handler is not registered.", messagePayload.ClientId, Payload.NextId);
                    RaiseError(error);
                }
                else
                {
                    this.MessageReceived(this, new MessageEventArgs(messagePayload.ClientId, messagePayload.Message));
                }
            }
        }

        internal void RaiseObjectDeleted(ObjectDeletedEventArgs e)
        {
            if (ObjectDeleted != null)
            {
                ObjectDeleted(this, e);
            }
        }

        internal void RaiseCollectionDeleted(CollectionDeletedEventArgs e)
        {
            if (CollectionDeleted != null)
            {
                CollectionDeleted(this, e);
            }
        }

        #endregion

        #region Atomic Interlocked
        /// <summary>
        /// Received the atomic update information for an object, set it internally
        /// </summary>
        /// <param name="payload"></param>
        private void ReceivedAtomicValue(AtomicPayload payload)
        {
            // Check if the data payload is a response to a request we have pending in our Async queue
            ISharedAsyncResult ar;
            if (!activeAsyncOperations.TryGetValue(payload.PayloadId, out ar))
            {
                return;
            }

            Type targetType = Type.GetType(payload.PropertyType);

            // Since target types always match to action types, we simply need to catch this and set the result properly
            if (targetType == typeof(int) || targetType == typeof(long))
            {
                var result = (SharedAsyncResult<long>)ar;
                this.CompleteAsyncResult(result, (long)Json.ReadObject(typeof(long), payload.Parameters[0]), payload.PayloadId);
            }
            else if (targetType == typeof(string))
            {
                var result = (SharedAsyncResult<string>)ar;
                this.CompleteAsyncResult(result, (string)Json.ReadObject(typeof(string), payload.Parameters[0]), payload.PayloadId);
            }
        }

        private void NotifyAtomicPayload(IEnumerable<Payload> events)
        {
            foreach (AtomicPayload payload in events)
            {
                this.ReceivedAtomicValue(payload);
            }
        }
        #endregion

        internal void SendPublishEvent(Payload data)
        {
            this.Channel.SendEvent(this.publishUpdatesChannelName, data);
        }

        private void SendPublishEvent(Payload[] data)
        {
            this.Channel.SendEvent(this.publishUpdatesChannelName, data);
        }

        public void SendMessage(string identity, string message)
        {
            Payload data = new MessagePayload(identity, message, this.ClientId);
            this.Channel.SendEvent(this.publishUpdatesChannelName, data);
        }

        public long LastActivity
        {
            get
            {
                return ticksLastMessageReceived;
            }
        }

        internal bool WaitingForAcks()
        {
            return activeAsyncOperations.Count > 0 || CollectionsManager.WaitingForAcks() || ObjectsManager.WaitingForAcks();
        }

        internal void VerifyAccess()
        {
            this.Dispatcher.VerifyAccess();
        }

        internal bool CheckAccess()
        {
            return this.Dispatcher.CheckAccess();
        }

        /// <summary>
        /// Incoming changes from the Server are all dispatched using BeginInvoke or directly if we are already on the correct thread
        /// Changes applied directly by the client are checked for access and an exception thrown if the wrong thread is used
        /// </summary>
        /// <param name="action"></param>
        internal void RunOnDispatcher(Action action)
        {
            if (this.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                this.Dispatcher.Invoke(action);
            }
        }

        #region Async Operation Support

        private Dictionary<ushort, ISharedAsyncResult> activeAsyncOperations = new Dictionary<ushort, ISharedAsyncResult>();

        internal void EnqueueAsyncResult(ISharedAsyncResult result, ushort payloadId)
        {
            if (result.CompletedSynchronously || result.IsCompleted)
            {
                return;
            }

            activeAsyncOperations[payloadId] = result;
        }

        private void CompleteAsyncResult<T>(SharedAsyncResult<T> result, T value, ushort payloadId)
        {
            result.SetAsCompleted(value, false);
            activeAsyncOperations.Remove(payloadId);
        }

        /// <summary>
        /// Complete the provided SharedAsyncResult which will translate the error into an Exception
        /// </summary>
        /// <param name="result"></param>
        /// <param name="payload"></param>
        private void CompleteAsyncResult(ISharedAsyncResult result, ushort payloadId)
        {
            if (result != null)
            {
                // TODO: once we have custom exceptions for our operations we would throw the correct exception
                result.SetAsCompleted(new Exception("GetAccessControl returned with SharedObjectSecurity information for object we are not aware of"), false);
            }
            activeAsyncOperations.Remove(payloadId);
        }

        /// <summary>
        /// Get the ISharedObjectEntry matching the Id
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        internal ISharedObjectEntry GetSharedEntry(bool isContainer, Guid id)
        {
            if (id == Constants.RootId)
            {
                return this;
            }
            else if (isContainer)
            {
                CollectionEntry colEntry;
                this.CollectionsManager.TryGetValue(id, out colEntry);
                return colEntry;
            }
            else
            {
                ObjectEntry objEntry;
                this.ObjectsManager.TryGetValue(id, out objEntry);
                return objEntry;
            }
        }

        #endregion

        #region EvictionPolicy

        private void NotifyEvictionPolicyPayload(IEnumerable<Payload> events)
        {
            foreach (EvictionPolicyPayload payload in events)
            {
                if (payload.Action == PayloadAction.Get)
                {
                    GotEvictionPolicy(payload);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }

        /// <summary>
        /// Received the SharedObjectSecurity information for an object, set it internally
        /// </summary>
        /// <param name="payload"></param>
        private void GotEvictionPolicy(EvictionPolicyPayload payload)
        {
            var data = payload.Policy;

            // Check if the data payload is a response to a request we have pending in our Async queue
            ISharedAsyncResult ar;
            if (!activeAsyncOperations.TryGetValue(payload.PayloadId, out ar))
            {
                Debug.Assert(false, "No matching asyncresult operation for the GetEvictionPolicy call");
                return;
            }

            // We know that the async results for GotEvictionPolicy will be a SharedObjectSecurity object
            var result = (SharedAsyncResult<EvictionPolicy>)ar;

            ISharedObjectEntry sharedObjectEntry = GetSharedEntry(true, payload.EntryId);
            if (sharedObjectEntry == null)
            {
                // Object/Collection does not exist, cannot set the AccessControl for it                
                var error = new ObjectErrorPayload(SharedObjects.Error.ObjectNotFound, payload.EntryId, "", "EvictionPolicy was retrieved for object that is no longer in scope on the client", payload.ClientId, payload.PayloadId);
                this.CompleteAsyncResult(result, payload.PayloadId);
                RaiseError(error);
                return;
            }

            this.CompleteAsyncResult<EvictionPolicy>(result, payload.Policy, payload.PayloadId);
        }

        #endregion

    }
}
