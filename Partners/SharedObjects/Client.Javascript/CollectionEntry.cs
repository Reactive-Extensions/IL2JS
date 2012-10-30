// <copyright file="CollectionEntry.cs" company="Microsoft">
// Copyright © 2010 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects.Client
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Manages a single shared collection
    /// </summary>
    internal abstract class CollectionEntry : ISharedObjectEntry
    {
        internal SharedObjectsClient client { get; private set; }

        public Guid Id { get; internal set; }
        public string Name { get; private set; }

        public Dictionary<Guid, ObjectEntry> Items { get; private set; }

        public Type Type { get; private set; }
        public ETag ETag { get; private set; }
        public SharedObjectSecurity ObjectSecurity { get; set; }

        internal SharedCollection MasterCollection { get; set; }
        internal SharedCollection TypedCollection { get; set; }

        abstract public CollectionType CollectionType { get; }

        protected bool ignoreChanges = false;

        public bool IsConnected
        {
            get { return MasterCollection.IsConnected; }
        }

        public CollectionEntry(SharedObjectsClient client, CollectionOpenedPayload payload)
            : this(client, payload.Name, Type.GetType(payload.Type))
        {
            this.Id = payload.Id;
        }

        public CollectionEntry(SharedObjectsClient client, string name, Type type)
        {
            this.client = client;
            this.Name = name;
            this.Type = type;

            this.Items = new Dictionary<Guid, ObjectEntry>();
        }

        internal void SetConnected(bool value, bool created)
        {
            if (value == true)
            {
                ForEachCollection(c =>
                    {
                        c.Id = this.Id;
                        c.OnConnected(created);
                    });        
            }
            else
            {
                OnDisconnect();
                ForEachCollection(c => c.RaiseDisconnected());
            }
        }

        internal void SetFaulted(bool value)
        {
            ForEachCollection(c => c.IsFaulted = value);
        }


        abstract internal void OnObjectRemoved(ObjectRemovedPayload data);
        abstract protected void RemoveObjectFromCollection (ObjectEntry objectEntry);

        protected void BaseOnObjectRemoved(Guid objectId)
        {
            ObjectEntry objectEntry;
            if (!this.client.ObjectsManager.TryGetValue(objectId, out objectEntry))
            {
                // This can happen if two clients remove the same object concurrently.
                Debug.WriteLine(string.Format("Object removed from collection that is not in client - Client={0}: ObjectId {1}", this.client.ClientId, objectId));
                return;
            }

            Debug.Assert(client.CheckAccess());

            try
            {
                objectEntry.IgnoreChanges = true;

                if (!this.Items.ContainsKey(objectEntry.Id))
                {
                    Debug.Assert(false, "Trying to remove object from collection it does not contain");
                }
                else
                {
                    this.Items.Remove(objectEntry.Id);
                    RemoveObjectFromCollection (objectEntry);
                    Debug.WriteLine(string.Format("[OrderedCollection] - Client={0}: REMOTE Deleting {1}", this.client.ClientId, objectEntry.Object));

                    // Remove this object from this parent
                    objectEntry.RemoveParent(this.Id);
                }
            }
            finally
            {
                objectEntry.IgnoreChanges = false;
                objectEntry.Dispose();
            }
        }

        abstract internal void OnObjectInserted(ObjectInsertedPayload data);
        abstract protected void AddObjectToCollection(ObjectInsertedPayload data, ObjectEntry objectEntry);
        
        abstract internal SharedCollection Register<T>() where T : INotifyPropertyChanged;

        protected void BaseOnObjectInserted(ObjectInsertedPayload data)
        {
            ObjectEntry objectEntry;
            if (!this.client.ObjectsManager.TryGetValue(data.ObjectId, out objectEntry))
            {
                Debug.Assert(false, "Missing object for incoming object insertion");
                return;
            }

            Debug.Assert(client.CheckAccess());
            Debug.Assert(!this.Items.ContainsKey(data.ObjectId), "Cannot re-insert object with same ID as existing object");

            objectEntry.AddParent(data.Parent);

            try
            {
                objectEntry.IgnoreChanges = true;

                if (this.Items.ContainsKey(objectEntry.Id))
                {
                    // TODO: this can happen due to a race condition
                    Debug.Assert(false, "Trying to add object to collection it already contains");
                }
                else
                {
                    this.Items.Add(objectEntry.Id, objectEntry);
                    AddObjectToCollection(data, objectEntry);
                    Debug.WriteLine(string.Format("[OrderedCollection] - Client={0}: REMOTE Inserting {1} in position {2}", this.client.ClientId, objectEntry.Object, data.Parent.Index));
                }
            }
            finally
            {
                objectEntry.IgnoreChanges = false;
            }
        }


        abstract internal bool WaitingForAcks();
        abstract protected void OnDisconnect();

        private void ForEachCollection(Action<SharedCollection> action)
        {
            action(MasterCollection);
            if (TypedCollection != null)
                action(TypedCollection);
        }

        /// <summary>
        /// Called when the collection is being closed
        /// </summary>
        internal void OnClose()
        {
            MasterCollection.Dispatcher.VerifyAccess();
            MasterCollection.RaiseDisconnected();

            foreach (ObjectEntry entry in this.Items.Values)
            {
                entry.RemoveParent(this.Id);
            }
            this.Items.Clear();
            this.SetConnected(false, false);

            try
            {
                ignoreChanges = true;
                MasterCollection.ClearSharedCollection();
            }
            finally
            {
                ignoreChanges = false;
            }
        }

        internal void OnDelete()
        {
            OnClose();
            SetFaulted(true);
        }

        protected bool AddSharedObject(INotifyPropertyChanged addedObj, int index, out ObjectEntry objectEntry)
        {            
            // First determine if the added object is being tracked by the SharedObjects System
            // if it is then we don't need to send the objects content payload, just
            // the insertion information to put this object into this collection.
            this.client.ObjectsManager.TryAdd(addedObj, out objectEntry);
            Debug.Assert(objectEntry != null);

            if (objectEntry.IgnoreChanges)
            {
                // Avoid feedback loop from applying incoming insertions
                return false;
            }

            this.MasterCollection.VerifyConnected();

            var parent = new ParentEntry()
            {
                Index = index,
                Id = this.Id
            };

            objectEntry.AddParent(parent);
            this.Items.Add(objectEntry.Id, objectEntry);
            return true;
        }

        protected bool RemoveSharedObject(INotifyPropertyChanged removedObj, out ObjectEntry objectEntry)
        {
            if (!this.client.ObjectsManager.TryGetValue(removedObj, out objectEntry))
            {
                Debug.Assert(false, "Removed object from shared collection that was not tracked by ObjectManager");
            }

            if (objectEntry.IgnoreChanges)
            {
                //"Ignoring object removal because we are ignoring changes and don't want feedback loop");
                return false;
            }

            objectEntry.RemoveParent(this.Id);

            this.Items.Remove(objectEntry.Id);

            return true;
        }

    }

    internal class UnorderedCollectionEntry : CollectionEntry, ISharedObjectEntry
    {
        private int UnackedMessages;

        override public CollectionType CollectionType
        {
            get { return CollectionType.Unordered; }
        }

        internal SharedObservableBag<INotifyPropertyChanged> GetMasterCollection()
        {
            return (SharedObservableBag<INotifyPropertyChanged>) MasterCollection;
        }

        private void Init()
        {
            this.MasterCollection = new SharedObservableBag<INotifyPropertyChanged>(Name, Type, this);
            this.GetMasterCollection().CollectionChanged += OnMasterCollectionChanged;
        }

        public UnorderedCollectionEntry(SharedObjectsClient client, CollectionOpenedPayload payload)
            : base(client, payload)
        {
            Init();
        }

        public UnorderedCollectionEntry(SharedObjectsClient client, string name, Type type)
            : base(client, name, type)
        {
            Init();
        }

        internal override SharedCollection Register<T>()
        {
            if (TypedCollection == null)
            {
                TypedCollection = new SharedObservableBag<T>(GetMasterCollection(), this);
            }
            return (SharedCollection)TypedCollection;
        }

        #region Outoging Change Handlers
        private void OnMasterCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ignoreChanges)
                return;

            Debug.Assert(client.CheckAccess());

            this.MasterCollection.VerifyNotFaulted();

            switch (e.Action)
            {
                // For each new SharedObject, start listening to property change events, add to the all objects table,
                // and send an EventLink message.
                case NotifyCollectionChangedAction.Add:
                    AddSharedObjects(e.NewItems);
                    break;

                // For each removed SharedObject, stop listening to property change events, remove from the all objects table,
                // and send an EventLink message.
                case NotifyCollectionChangedAction.Remove:
                    RemoveSharedObjects(e.OldItems);
                    break;

                // For each new SharedObject, start listening to property change events, add to the all objects table,
                // and send an EventLink message.
                // For each replaced SharedObject, stop listening to property change events, remove from the all objects table,
                // and send an EventLink message.
                case NotifyCollectionChangedAction.Replace:
                    RemoveSharedObjects(e.OldItems);
                    AddSharedObjects(e.NewItems);
                    break;

                // For each removed SharedObject, stop listening to property change events, remove from the all objects table,
                // and send an EventLink message.
                case NotifyCollectionChangedAction.Reset:
                    RemoveSharedObjects(this.Items.Values.Select(x => x.Object).ToList());
                    break;

                default:
                    throw new NotSupportedException("Action not supported");
            }
        }

        private void AddSharedObjects(IList items)
        {
            Debug.Assert(client.CheckAccess());

            foreach (INotifyPropertyChanged addedObj in items)
            {
                ObjectEntry objectEntry;
                if (AddSharedObject(addedObj, -1, out objectEntry))
                {
                    Debug.WriteLine(string.Format("[UnorderedCollection] - Client={0}: LOCAL Inserting {1}", this.client.ClientId, objectEntry.Object));

                    var payload = new ObjectInsertedPayload(this.Id, objectEntry.Id, this.client.ClientId);
                    SendObjectInsertedPayload(payload);
                }
            }
        }

        private void RemoveSharedObjects(IList items)
        {
            Debug.Assert(client.CheckAccess());
            this.MasterCollection.VerifyConnected();

            foreach (INotifyPropertyChanged removedObj in items)
            {
                ObjectEntry objectEntry;
                if (RemoveSharedObject(removedObj, out objectEntry))
                {
                    Debug.WriteLine(string.Format("[UnorderedCollection] - Client={0}: LOCAL Deleting {1}", this.client.ClientId, objectEntry.Object));

                    var data = new ObjectRemovedPayload(this.Id, objectEntry.Id, this.client.ClientId);
                    this.SendObjectRemovedPayload(data);
                }
            }
        }

        #endregion

        #region Incoming Change Handlers

        override internal void OnObjectRemoved(ObjectRemovedPayload data)
        {
            // Check if this is a locally pending delete, if so remove the delete from the list of LocalOperations
            if (data.ClientId == this.client.ClientId)
            {
                Debug.Assert(UnackedMessages > 0);
                --UnackedMessages;
                return;
            }

            BaseOnObjectRemoved(data.ObjectId);
        }

        override protected void RemoveObjectFromCollection(ObjectEntry objectEntry)
        {
            this.GetMasterCollection().IncomingRemove(objectEntry.Object);
        }

        override internal void OnObjectInserted(ObjectInsertedPayload data)
        {
            if (data.ClientId == this.client.ClientId)
            {
                Debug.Assert(UnackedMessages > 0);
                --UnackedMessages;
                return;
            }

            BaseOnObjectInserted (data);
        }

        protected override void AddObjectToCollection(ObjectInsertedPayload data, ObjectEntry objectEntry)
        {
            this.GetMasterCollection().IncomingAdd(objectEntry.Object);
        }

        override internal bool WaitingForAcks()
        {
            Debug.Assert(UnackedMessages >= 0);
            return UnackedMessages > 0;
        }

        override protected void OnDisconnect()
        {
            UnackedMessages = 0;
        }

        #endregion

        private void SendObjectInsertedPayload(ObjectInsertedPayload data)
        {
            this.client.SendPublishEvent(data);
            ++UnackedMessages;
        }

        private void SendObjectRemovedPayload(ObjectRemovedPayload data)
        {
            this.client.SendPublishEvent(data);
            ++UnackedMessages;
        }
    }

    internal class OrderedCollectionEntry : CollectionEntry, ISharedObjectEntry
    {
        override public CollectionType CollectionType
        {
            get { return CollectionType.Ordered; }
        }

        // Highest operation sequence recieved by the client
        internal int OperationSequence { get; private set; }

        // Store an ordered list of the objects stored in the collection for propert transformations
        internal List<Guid> CollectionIndices { get; private set; }

        // Store list of pending local operations which have NOT been acknowledged by the server
        private List<CollectionOperation> LocalOperations { get; set; }

        internal SharedObservableCollection<INotifyPropertyChanged> GetMasterCollection()
        {
            return (SharedObservableCollection<INotifyPropertyChanged>) MasterCollection;
        }

        private void Init()
        {
            this.LocalOperations = new List<CollectionOperation>();
            this.CollectionIndices = new List<Guid>();
            this.MasterCollection = new SharedObservableCollection<INotifyPropertyChanged>(Name, Type, this);
            this.GetMasterCollection().CollectionChanged += OnMasterCollectionChanged;
        }

        public OrderedCollectionEntry(SharedObjectsClient client, CollectionOpenedPayload payload)
            : base(client, payload)
        {
            Init();
        }

        public OrderedCollectionEntry(SharedObjectsClient client, string name, Type type)
            : base(client, name, type)
        {
            Init();
        }


        internal override SharedCollection Register<T>()
        {
            if (TypedCollection == null)
            {
                TypedCollection = new SharedObservableCollection<T>(GetMasterCollection(), this);
            }
            return (SharedCollection)TypedCollection;
        }

        #region Outoging Change Handlers
        private void OnMasterCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ignoreChanges)
                return;

            Debug.Assert(client.CheckAccess());

            this.MasterCollection.VerifyNotFaulted();

            switch (e.Action)
            {
                // For each new SharedObject, start listening to property change events, add to the all objects table,
                // and send an EventLink message.
                case NotifyCollectionChangedAction.Add:
                    AddSharedObjects(e.NewItems);
                    break;

                // For each removed SharedObject, stop listening to property change events, remove from the all objects table,
                // and send an EventLink message.
                case NotifyCollectionChangedAction.Remove:
                    RemoveSharedObjects(e.OldItems);
                    break;

                // For each new SharedObject, start listening to property change events, add to the all objects table,
                // and send an EventLink message.
                // For each replaced SharedObject, stop listening to property change events, remove from the all objects table,
                // and send an EventLink message.
                case NotifyCollectionChangedAction.Replace:
                    RemoveSharedObjects(e.OldItems);
                    AddSharedObjects(e.NewItems);
                    break;

                // For each removed SharedObject, stop listening to property change events, remove from the all objects table,
                // and send an EventLink message.
                case NotifyCollectionChangedAction.Reset:
                    RemoveSharedObjects(this.Items.Values.Select(x => x.Object).ToList());
                    break;

                default:
                    throw new NotSupportedException("Action not supported");
            }
        }

        private void AddSharedObjects(IList items)
        {
            Debug.Assert(client.CheckAccess());
            
            foreach (INotifyPropertyChanged addedObj in items)
            {
                int index = this.GetMasterCollection().IndexOf(addedObj);
                ObjectEntry objectEntry;
                if (AddSharedObject (addedObj, index, out objectEntry))
                {
                    this.CollectionIndices.Insert(index, objectEntry.Id);

                    Debug.WriteLine(string.Format("[OrderedCollection] - Client={0}: LOCAL Inserting {1} in position {2}", this.client.ClientId, objectEntry.Object, index));

                    var payload = new ObjectInsertedPayload(this.Id, objectEntry.Id, index, this.OperationSequence, this.client.ClientId);
                    SendObjectInsertedPayload(payload);
                }
            }
        }

        private void RemoveSharedObjects(IList items)
        {
            Debug.Assert(client.CheckAccess());
            this.MasterCollection.VerifyConnected();

            foreach (INotifyPropertyChanged removedObj in items)
            {
                ObjectEntry objectEntry;
                if (RemoveSharedObject(removedObj, out objectEntry))
                {
                    // Get index before removing
                    int index = this.CollectionIndices.IndexOf(objectEntry.Id);

                    // Now we can remove from index list
                    this.CollectionIndices.Remove(objectEntry.Id);

                    Debug.WriteLine(string.Format("[OrderedCollection] - Client={0}: LOCAL Deleting {1} Index {2}", this.client.ClientId, objectEntry.Object, index));

                    var data = new ObjectRemovedPayload(this.Id, objectEntry.Id, index, this.OperationSequence, this.client.ClientId);
                    this.SendObjectRemovedPayload(data);
                }
            }
        }

        #endregion

        #region Incoming Change Handlers

        override internal void OnObjectRemoved(ObjectRemovedPayload data)
        {
            Debug.Assert(this.IsConnected == false || this.OperationSequence == 0 || data.OperationSequence == this.OperationSequence + 1);
            this.OperationSequence = data.OperationSequence;

            // Check if this is a locally pending delete, if so remove the delete from the list of LocalOperations
            if (data.ClientId == this.client.ClientId)
            {
                this.ProcessAck(OperationAction.Remove, data.ObjectId);
                return;
            }

            // Apply transformation, update index
            CollectionOperation operation = new CollectionOperation(data);
            this.ApplyTransform(this, ref operation);
            data.Parent.Index = operation.ObjectIndex;

            if (operation.ApplyOperation)
            {
                Debug.Assert(CollectionIndices[operation.ObjectIndex] == data.ObjectId);
                BaseOnObjectRemoved(data.ObjectId);
            }
            else
            {
                Debug.WriteLine(string.Format("[OrderedCollection] - Client={0}: REMOTE NOT Deleting {1} Index {2}", this.client.ClientId, data.ObjectId, operation.ObjectIndex));
            }
        }

        protected override void RemoveObjectFromCollection(ObjectEntry objectEntry)
        {
            this.GetMasterCollection().IncomingRemove(objectEntry.Object);
            this.CollectionIndices.Remove(objectEntry.Id);
        }

        override internal void OnObjectInserted(ObjectInsertedPayload data)
        {
            Debug.Assert(this.IsConnected == false || this.OperationSequence == 0 || data.OperationSequence == this.OperationSequence + 1);
            this.OperationSequence = data.OperationSequence;

            // Check if this is a locally pending insert, if so remove the insert from the list of LocalOperations
            if (data.ClientId == this.client.ClientId) 
            {
                this.ProcessAck(OperationAction.Insert, data.ObjectId);
                return;
            }

            // Apply transformation, update index
            CollectionOperation operation = new CollectionOperation(data);
            this.ApplyTransform(this, ref operation);
            data.Parent.Index = operation.ObjectIndex;

            if (operation.ApplyOperation)
            {
                BaseOnObjectInserted(data);
            }
        }

        protected override void AddObjectToCollection(ObjectInsertedPayload data, ObjectEntry objectEntry)
        {
            this.GetMasterCollection().IncomingInsert(data.Parent.Index, objectEntry.Object);
            this.CollectionIndices.Insert(data.Parent.Index, objectEntry.Id);
        }

        override internal bool WaitingForAcks()
        {
            return LocalOperations.Count > 0;
        }

        override protected void OnDisconnect()
        {
            LocalOperations.Clear();
        }

        internal void ProcessAck(OperationAction action, Guid objectId)
        {
            CollectionOperation firstOp = this.LocalOperations[0];
            if (firstOp.Action == action && firstOp.ObjectId == objectId)
            {
                this.LocalOperations.RemoveAt(0);
            }
            else
            {
                Debug.Assert(false, "Received ack for unknown or out of order operation");
            }
        }

        #endregion

        private void SendObjectInsertedPayload(ObjectInsertedPayload data)
        {
            // Add to pending list
            CollectionOperation change = new CollectionOperation(data);
            this.LocalOperations.Add(change);

            this.client.SendPublishEvent(data);
        }

        private void SendObjectRemovedPayload(ObjectRemovedPayload data)
        {
            // Add to pending list
            CollectionOperation change = new CollectionOperation(data);
            this.LocalOperations.Add(change);

            this.client.SendPublishEvent(data);
        }


        private void ApplyTransform(OrderedCollectionEntry collection, ref CollectionOperation incoming)
        {
            List<CollectionOperation> updatedOperations = new List<CollectionOperation>();
            foreach (var operation in collection.LocalOperations)
            {
                CollectionOperation newOperation;
                CollectionOperation.ApplyTransform(operation, ref incoming, out newOperation);
                updatedOperations.Add(newOperation);
            }

            // Replace local operations with updated, transformed versions
            collection.LocalOperations = updatedOperations;
        }
    }
}
