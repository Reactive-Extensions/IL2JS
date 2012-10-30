// <copyright file="CollectionsManager.cs" company="Microsoft">
// Copyright © 2010 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects.Client
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows.Threading;

    internal class CollectionsManager : SharedEntryMap<CollectionEntry>
    {
        private SharedObjectsClient client;
        internal DispatcherTimer HeartbeatTimer;
        internal long LastHeartbeat;

        private Dictionary<string, CollectionEntry> pendingCollections;

        public CollectionsManager(SharedObjectsClient client)
        {
            this.client = client;

            this.pendingCollections = new Dictionary<string, CollectionEntry>();
        }

        public void Delete(string name)
        {
            client.VerifyAccess();

            CollectionEntry entry = null;
            if (this.TryGetValue(name, out entry))
            {
                DeleteCollection(entry, Source.Client);
            }
            else
            {
                // If we have no local record of a collection it may still exist. Send Delete payload
                Payload eventData = new CollectionDeletedPayload(name, this.client.ClientId);
                client.SendPublishEvent(eventData);
            }
        }

        /// <summary>
        /// Open the specified collection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name">The name of the collection to open</param>
        /// <param name="mode">The mode used to open the collection</param>
        /// <param name="collectionType">The type of the Collection (e.g. Ordered, Unordered)</param>
        /// <param name="connectedCallback">Callback to call when the collection is connected</param>
        /// <returns></returns>
        public SharedCollection Open<T>(string name, ObjectMode mode, CollectionType collectionType) where T : INotifyPropertyChanged
        {
            client.VerifyAccess();

            if (!this.client.IsConnected)
            {
                throw new InvalidOperationException("Cannot open collection before the Client is connected");
            }

            if (this.pendingCollections.ContainsKey(name))
            {
                throw new InvalidOperationException("The specified collection is already in the process of being opened");
            }

            CollectionEntry entry = null;
            if (this.ContainsKey(name))
            {
                if (mode != ObjectMode.Open && mode != ObjectMode.OpenOrCreate)
                {
                    throw new ArgumentException("Invalid ObjectMode. Specified collection has already been opened on this client. Try using ObjectMode.Open", "mode");
                }

                entry = this[name];
                // Make sure type of collection matches T
                if (entry.Type != typeof(T))
                {
                    throw new ArgumentException("Specified type does not match that of existing collection");
                }
            }
            else
            {
                switch (collectionType)
                {
                    case CollectionType.Ordered:
                        entry = new OrderedCollectionEntry(this.client, name, typeof(T));

                        // Start the heartbeat timer when an ordered collection is created
                        // TODO ransomr optimize heartbeats so only sent when there are ordered collections with changes, make thread safe
                        if (this.HeartbeatTimer == null)
                        {
                            this.HeartbeatTimer = new DispatcherTimer();
                            this.HeartbeatTimer.Tick += this.SendCollectionHeartbeat;
                            this.HeartbeatTimer.Interval = TimeSpan.FromSeconds(Constants.HeartbeatIntervalSeconds);
                            this.HeartbeatTimer.Start();
                        }
                        break;
                    case CollectionType.Unordered:
                        entry = new UnorderedCollectionEntry(this.client, name, typeof(T));
                        break;
                }
                AddCollection(entry, mode, Source.Client);
            }

            // Register a new reference to the SharedObservableCollection
            return entry.Register<T>();
        }

        public SharedObservableCollection<T> OpenCollection<T>(string name, ObjectMode mode, Action<CollectionConnectedEventArgs> onConnected) where T : INotifyPropertyChanged
        {
            var result = (SharedObservableCollection<T>)Open<T>(name, mode, CollectionType.Ordered);
            result.RegisterConnectedCallback(onConnected);
            
            return result;
        }

        public SharedObservableBag<T> OpenBag<T>(string name, ObjectMode mode, Action<CollectionConnectedEventArgs> onConnected) where T : INotifyPropertyChanged
        {
            var result = (SharedObservableBag<T>)Open<T>(name, mode, CollectionType.Unordered);
            result.RegisterConnectedCallback(onConnected);

            return result;
        }

        public void Close(Guid id)
        {
            if (!this.client.IsConnected)
            {
                throw new InvalidOperationException("Cannot close collection before the Client is connected");
            }

            CollectionEntry entry = null;
            if (!this.TryGetValue(id, out entry))
            {
                throw new InvalidOperationException("Unable to close collection. Collection not found.");
            }

            // Create close collection payload
            var data = new CollectionClosedPayload(entry.Id, this.client.ClientId);
            this.client.SendPublishEvent(data);

            this.Remove(entry.Name);
            entry.OnClose();
        }

        public void OnConnect()
        {
            client.VerifyAccess();

            if (!this.client.IsConnected)
            {
                throw new InvalidOperationException("Cannot open collection before the Client is connected");
            }

            foreach (var entry in pendingCollections)
            {
                AddCollection(entry.Value, ObjectMode.Open, Source.Client); // TODO ransomr may need to create pending collection
            }
            foreach (var entry in this)
            {
                AddCollection(entry.Value, ObjectMode.Open, Source.Client);
            }
            Clear();
        }

        #region Outgoing Change Handlers
        private void AddCollection(CollectionEntry entry, ObjectMode mode, Source source)
        {
            // Client created a new collection - send create payload to server
            if (source == Source.Client)
            {
                if (this.client.IsConnected)
                {
                    Payload eventData = new CollectionOpenedPayload(entry.Name, entry.Id, entry.Type.AssemblyQualifiedName, mode, entry.CollectionType, this.client.ClientId);
                    this.client.SendPublishEvent(eventData);
                    Debug.Assert(entry.Items.Count == 0);
                }
                else
                {
                    // Client is not connected throw exception
                    throw new InvalidOperationException("Cannot Add Collection before Client is connected");
                }
            }

            // Add to internal dictionary tracking all the known collections
            this.pendingCollections[entry.Name] = entry;
        }


        /// <summary>
        /// Delete the collection from the namespace. Unhook all the local data objects
        /// </summary>
        /// <param name="entry"></param>
        private void DeleteCollection(CollectionEntry entry, Source source)
        {
            this.VerifyCollectionConnected(entry);

            // Alert the server that the collection has been deleted
            if (source == Source.Client)
            {
                // Specify both the name and id since we have this collection mapped into our local client
                Payload eventData = new CollectionDeletedPayload(entry.Id, this.client.ClientId);
                client.SendPublishEvent(eventData);
            }

            this.Remove(entry.Name);
            entry.OnDelete();
        }

        #endregion

        #region Incoming Changes Handler
        internal void OnIncomingPayload(IEnumerable<Payload> payloads)
        {
            // All incoming changes must be processed on the Dispatcher thread. The invokation onto that
            // thread happens in the layers above this one for greater centralization
            Debug.Assert(this.client.CheckAccess());

            foreach (var payload in payloads)
            {
                switch (payload.PayloadType)
                {
                    case PayloadType.CollectionConnected:
                        {
                            OnCollectionConnected(payload as CollectionConnectedPayload);
                            break;
                        }
                    case PayloadType.CollectionOpened:
                        {
                            this.OnCollectionOpened(payload as CollectionOpenedPayload);
                            break;
                        }
                    case PayloadType.CollectionDeleted:
                        {
                            OnCollectionDeleted(payload as CollectionDeletedPayload);
                            break;
                        }
                    case PayloadType.ObjectInserted:
                        {
                            OnObjectInserted(payload as ObjectInsertedPayload);
                            break;
                        }
                    case PayloadType.ObjectRemoved:
                        {
                            OnObjectRemoved(payload as ObjectRemovedPayload);
                            break;
                        }
                }
            }
        }

        private void OnObjectRemoved(ObjectRemovedPayload data)
        {
            CollectionEntry collectionEntry;
            if(!this.TryGetValue(data.Parent.Id, out collectionEntry))
            {
                Debug.WriteLine("Missing collection for incoming object removal");
                return;
            }
            
            // Route removal request to the collection entry
            collectionEntry.OnObjectRemoved(data);            
        }

        private void OnObjectInserted(ObjectInsertedPayload data)
        {
            CollectionEntry collectionEntry;
            if (!this.client.CollectionsManager.TryGetValue(data.Parent.Id, out collectionEntry))
            {
                Debug.WriteLine("Missing collection for incoming object insertion");
                return;
            }

            // Route insert request to the collection entry
            collectionEntry.OnObjectInserted(data);
        }

        private void OnCollectionConnected(CollectionConnectedPayload payload)
        {
            Debug.Assert(payload != null);
            // Alert the collection that is now "Online"
            var entry = this[payload.Name];
            entry.Id = payload.Id;
            entry.SetConnected(true, payload.Created);
        }

        private void OnCollectionOpened(CollectionOpenedPayload payload)
        {
            CollectionEntry entry;

            // Collection already exists locally
            if (this.pendingCollections.TryGetValue(payload.Name, out entry))
            {
                // This will occur when a client has called OpenCollection for a collection
                // We must verify that the collection the user
                // is registering for is of the same type on the server as on the client
                if (payload.Type != entry.Type.AssemblyQualifiedName)
                {
                    throw new Exception("The type of the registered collection on the client does not match with the server");
                }

                this.pendingCollections.Remove(payload.Name);
                entry.Id = payload.Id;
                this.Add(entry);                
            }
            else
            {
                Debug.Assert(false, "CollectionOpened received for collection we are not aware of");
            }
        }

        private void OnCollectionDeleted(CollectionDeletedPayload payload)
        {
            CollectionEntry entry;
            if (this.TryGetValue(payload.Id, out entry))
            {
                DeleteCollection(entry, Source.Server);
                this.client.RaiseCollectionDeleted(new CollectionDeletedEventArgs(entry.MasterCollection as SharedCollection, entry.Name));
            }
            else
            {
                this.client.RaiseCollectionDeleted(new CollectionDeletedEventArgs(null, payload.Name));
            }
        }

        #endregion

        private void VerifyCollectionConnected(CollectionEntry entry)
        {
            if (!entry.IsConnected)
            {
                throw new NotSupportedException("Changes not allowed until Collection is online");
            }
        }

        private void SendCollectionHeartbeat(object sender, EventArgs e)
        {
            this.client.Dispatcher.VerifyAccess();

            IEnumerable<OrderedCollectionEntry> OrderedCollectionEntries = this.Values.OfType<OrderedCollectionEntry>();
            // Only send heartbeat if we have converged from client's perspective (no pending ops)
            if (OrderedCollectionEntries.Any(x => x.WaitingForAcks()))
            {
                return;
            }
            Dictionary<Guid, int> collectionStates = OrderedCollectionEntries.ToDictionary(x => x.Id, x => x.OperationSequence);
            Dictionary<Guid, byte[]> collectionChecksums = OrderedCollectionEntries.ToDictionary(x => x.Id, x => OperationalTransform.GetChecksum(x.CollectionIndices));

            CollectionHeartbeatPayload data = new CollectionHeartbeatPayload(this.client.ClientId, collectionStates, collectionChecksums);
            try
            {
                client.SendPublishEvent(data);
            }
            catch (ClientDisconnectedException)
            {
                // This can happen as a result of a race condition between the collection manager being made disconnected and the
                // heartbeat being sent.
            }

            this.LastHeartbeat = DateTime.Now.Ticks;
        }

        public void OnDisconnect()
        {
            foreach (CollectionEntry entry in this.Values)
            {
                entry.OnClose();
            }
        }

        internal bool WaitingForAcks()
        {
            foreach (CollectionEntry entry in this.Values)
            {
                if (entry.WaitingForAcks())
                    return true;
            }
            return false;
        }
    }
}


