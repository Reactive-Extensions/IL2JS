// <copyright file="ObjectsManager.cs" company="Microsoft">
// Copyright © 2010 Microsoft Corporation.  All rights reserved.
// </copyright>


namespace Microsoft.Csa.SharedObjects.Client
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.IO;
    using Microsoft.Csa.SharedObjects.Utilities;

    internal class ObjectsManager : SharedEntryMap<ObjectEntry>, IDictionary<INotifyPropertyChanged, ObjectEntry>
    {
        private SharedObjectsClient client;
        private IDictionary<INotifyPropertyChanged, ObjectEntry> byRef;
        private Dictionary<string, OpenOperation> pendingOpenOperations;

        internal struct OpenOperation
        {
            public ObjectEntry Entry { get; set; }
            public Action<ObjectConnectedEventArgs> Callback { get; set; }
        }

        public ObjectsManager(SharedObjectsClient client)
        {
            this.client = client;
            this.byRef = new Dictionary<INotifyPropertyChanged, ObjectEntry>();
            this.pendingOpenOperations = new Dictionary<string, OpenOperation>();
        }

        /// <summary>
        /// Will attempt to add this object to the ObjectsManager. If the object is already
        /// being tracked or is in the ignore list we will return false. 
        /// </summary>
        /// <param name="addedObj"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        public bool TryAdd(INotifyPropertyChanged addedObj, out ObjectEntry entry)
        {
            if (this.TryGetValue(addedObj, out entry))
            {
                // Entry already exists
                return false;
            }

            Type sharedObjectType = addedObj.GetType();

            if (sharedObjectType.GetConstructor(Type.EmptyTypes) == null)
            {
                throw new MissingMethodException("You must provide an empty constructor for your object to be initialized");
            }
            
            entry = new ObjectEntry(this.client, addedObj);
            this.Add(entry);

            // Send this object to the server
            Debug.WriteLine(string.Format("[ObjectsManager] - Client={0}: Creating {1}, {2}", this.client.ClientId, entry.Id, entry.Name));
            var data = entry.ToPayload();
            this.client.SendPublishEvent(data);

            return true;
        }

        public bool TryRemove(INotifyPropertyChanged removedObj, out ObjectEntry entry)
        {
            if (!this.TryGetValue(removedObj, out entry) || entry.IgnoreChanges)
            {
                // Either object does not exist, or avoid the feedback loop
                return false;
            }

            this.Remove(entry);
            return true;
        }
        
        #region Named Shared Objects
        /// <summary>
        /// Opens an object with the specified name
        /// </summary>
        /// <param name="name">The object to open</param>
        /// <param name="mode">An ObjectMode value that specifies whether an object is created if one does not exist, and determines whether the contents of existing object is retained or overwritten.</param>
        /// <returns>A shared object with the specified name</returns>  
        public T Open<T>(string name, ObjectMode mode, Action<ObjectConnectedEventArgs> callback) where T : INotifyPropertyChanged, new()
        {
            client.VerifyAccess();

            if (mode == ObjectMode.Create || mode == ObjectMode.Truncate)
            {
                throw new ArgumentException("mode", "Create and Truncate are not supported ObjectModes for named objects");
            }

            if (!this.client.IsConnected)
            {
                throw new InvalidOperationException("Cannot open object before the Client is connected");
            }

            if (this.pendingOpenOperations.ContainsKey(name))
            {
                throw new InvalidOperationException("The specified named object is already in the process of being opened");
            }

            ObjectEntry entry = null;
            if (this.ContainsKey(name))
            {
                if (mode != ObjectMode.Open && mode != ObjectMode.OpenOrCreate)
                {
                    throw new ArgumentException("Invalid ObjectMode. Specified object has already been opened on this client. Try using ObjectMode.Open", "mode");
                }

                entry = this[name];
                // Make sure type of object matches T
                if (entry.Type != typeof(T))
                {
                    throw new ArgumentException("Specified type does not match that of existing collection");
                }

                if (callback != null)
                {
                    // If callback was provided, call the opened callback
                    callback(new ObjectConnectedEventArgs(entry.Object, entry.Name, false));
                }
            }
            else
            {
                entry = new ObjectEntry(this.client, name, typeof(T));
                // Add to internal dictionary tracking all the pending open operations
                this.pendingOpenOperations[entry.Name] = new OpenOperation() { Entry = entry, Callback = callback };

                Payload eventData = new ObjectOpenedPayload(entry.ToPayload(), mode, this.client.ClientId);
                this.client.SendPublishEvent(eventData);
                
                entry.AddParent(new ParentEntry(this.client.Id, 0));
            }

            return (T)entry.Object;
        }

        internal void OnConnect()
        {
            client.VerifyAccess();

            if (!this.client.IsConnected)
            {
                throw new InvalidOperationException("Cannot open object before the Client is connected");
            }

            foreach (var pair in pendingOpenOperations)
            {
                Payload eventData = new ObjectOpenedPayload(pair.Value.Entry.ToPayload(), ObjectMode.Open, this.client.ClientId); // TODO ransomr may need to create pending collection
                this.client.SendPublishEvent(eventData);
            }
            foreach (var pair in this)
            {
                this.pendingOpenOperations[pair.Value.Name] = new OpenOperation() { Entry = pair.Value };

                Payload eventData = new ObjectOpenedPayload(pair.Value.ToPayload(), ObjectMode.Open, this.client.ClientId);
                this.client.SendPublishEvent(eventData);
            }
            Clear();
        }

        internal void OnDisconnect()
        {
            foreach (var pair in this)
            {
                pair.Value.OnDisconnect();
            }
        }

        public void Close(INotifyPropertyChanged value)
        {
            client.VerifyAccess();

            if (!this.client.IsConnected)
            {
                throw new InvalidOperationException("Cannot close object before the Client is connected");
            }

            ObjectEntry entry = null;
            if (!this.TryGetValue(value, out entry))
            {
                throw new InvalidOperationException("Unable to close object. Object not found.");
            }

            // If we the parentId is the client itself indicates that the user is 'Closing' an
            // opened named object, when this happens we need to tell the server that the client is no longer interested in this
            // named object, this won't necessarily disconnect the object entirely, since it may be referenced by an open collection
            var data = new ObjectClosedPayload(entry.Id, this.client.ClientId);
            this.client.SendPublishEvent(data);

            // Closing an object simply closes the local 
            entry.RemoveParent(this.client.Id);
        }

        public void Delete(string name)
        {
            client.VerifyAccess();

            ObjectEntry entry = null;
            if (this.TryGetValue(name, out entry))
            {
                Payload eventData = new ObjectDeletedPayload(entry.Id, this.client.ClientId);
                client.SendPublishEvent(eventData);

                OnObjectDeleted(entry, Source.Client);
            }
            else
            {
                // If we have no local record of an object it may still exist. Send Delete payload
                Payload eventData = new ObjectDeletedPayload(name, this.client.ClientId);
                client.SendPublishEvent(eventData);
            }
        }

        public void Delete(INotifyPropertyChanged value)
        {
            client.VerifyAccess();

            if (!this.client.IsConnected)
            {
                throw new InvalidOperationException("Cannot delete object before the Client is connected");
            }
            
            ObjectEntry entry = null;
            if (!this.TryGetValue(value, out entry))
            {
                throw new InvalidOperationException("Unable to delete object. Object not found.");
            }

            Payload eventData = new ObjectDeletedPayload(entry.Id, this.client.ClientId);
            client.SendPublishEvent(eventData);

            this.OnObjectDeleted(entry, Source.Client);
        }

        #endregion

        #region Outgoing Change Handlers (Used for Singleton objects)



        #endregion

        #region Incoming Change Handlers

        internal void OnIncomingPayload(IEnumerable<Payload> payloads)
        {
            // All incoming changes must be processed on the Dispatcher thread. The invokation onto that
            // thread happens in the layers above this one for greater centralization
            Debug.Assert(this.client.CheckAccess());

            foreach (var payload in payloads)
            {
                switch (payload.PayloadType)
                {
                    case PayloadType.ObjectConnected:
                        OnObjectConnected(payload as ObjectConnectedPayload);
                        break;

                    case PayloadType.Object:
                        OnObjectPayload(payload as ObjectPayload);
                        break;

                    case PayloadType.PropertyUpdated:
                        UpdateSharedObject(payload as PropertyChangedPayload);
                        break;

                    case PayloadType.ObjectDeleted:
                        OnObjectDeleted(payload as ObjectDeletedPayload);
                        break;
                }
            }
        }

        private void OnObjectDeleted(ObjectDeletedPayload payload)
        {
            ObjectEntry entry;
            if (this.TryGetValue(payload.Id, out entry))
            {
                OnObjectDeleted(entry, Source.Server);
                this.client.RaiseObjectDeleted(new ObjectDeletedEventArgs(entry.Object, entry.Name));
            }
            else
            {
                // Object is not in client. Should only receive this event if we called for the object to be deleted ourselves
                this.client.RaiseObjectDeleted(new ObjectDeletedEventArgs(null, payload.Name));
            }
        }

        /// <summary>
        /// Raise the deleted event and remove the namespace as a parent for this object
        /// </summary>
        /// <param name="entry"></param>
        private void OnObjectDeleted(ObjectEntry entry, Source source)
        {            
            // Remove the object as parented by the Root. If the object is not in any collections it 
            // will be removed entirely, but otherwise we will wait until all the removal messages come from the server
            entry.RemoveParent(this.client.Id);
        }

        private void OnObjectConnected(ObjectConnectedPayload data)
        {
            Debug.Assert(this.client.CheckAccess());

            OpenOperation operation;
            ObjectPayload objectPayload = data.ObjectPayload;
            if (this.pendingOpenOperations.TryGetValue(objectPayload.Name, out operation))
            {
                // This will occur when a client has called OpenObject for an object
                // We must verify that the object the user
                // is registering for is of the same type on the server as on the client
                if (objectPayload.Type != operation.Entry.Type.AssemblyQualifiedName)
                {
                    throw new Exception("The type of the object on the client does not match with the server");
                }
                this.pendingOpenOperations.Remove(objectPayload.Name);

                // Update all the properties on the local copy of the object
                operation.Entry.Update(objectPayload);
                operation.Entry.IsConnected = true;
                this.Add(operation.Entry);

                if (operation.Callback != null)
                {
                    // If callback was provided, call the opened callback
                    operation.Callback(new ObjectConnectedEventArgs(operation.Entry.Object, objectPayload.Name, data.Created));
                }
            }
            else
            {
                Debug.Assert(false, "Received ObjectConnected payload for object we did not open");
            }
        }

        private void OnObjectPayload(ObjectPayload data)
        {
            Debug.Assert(this.client.CheckAccess());

            // Check if this is a locally pending object creation
            if (data.ClientId == this.client.ClientId)
            {
                // Nothing
                return;
            }
            else
            {
                // Create object from payload. Object will be set to IgnoreChanges
                ObjectEntry entry = new ObjectEntry(this.client, data);
                entry.IsConnected = true;
                this.Add(entry);
                entry.IgnoreChanges = false;
            }
        }

        private void UpdateSharedObject(PropertyChangedPayload data)
        {
            Debug.Assert(this.client.CheckAccess());

            ObjectEntry entry = null;
            try
            {

                if (!this.TryGetValue(data.ObjectId, out entry))
                {
                    // Received update for an object we are not tracking
                    Debug.WriteLine("[PropertyChanged] Missing shared object for update in incoming event.");
                    return;
                }

                SharedProperty property = entry.IsDynamic ? entry.Properties[data.PropertyName] : entry.Properties[data.PropertyIndex];

                // In case of dynamic properties, this could be the first time this property is seen
                if (property == null && entry.IsDynamic)
                {
                    property = new SharedProperty()
                                   {
                                       Attributes = new SharedAttributes(),
                                       Index = (short)entry.Properties.Count,
                                       Name = data.PropertyName,
                                       ETag = new ETag(this.client.ClientId),
                                       PropertyType = data.PropertyType
                                   };
                    entry.Properties.Add(property.Index, property);
                }

                // Ignore changes for given object, add to list
                entry.IgnoreChanges = true;

                // Don't apply update if we made it locally
                bool applyUpdate = data.ClientId != this.client.ClientId;
                bool conflictDetected = false;

                PropertyUpdateOperation matchingUpdate = property.LocalUpdates.Where(x => x.Payload.Equals(data)).FirstOrDefault();
                if (matchingUpdate != null)
                {
                    // Remove update from pending list
                    Debug.WriteLine("[PropertyChanged] Received acknowledgement of matching property update");
                    if (matchingUpdate.ReapplyUpdate)
                    {
                        applyUpdate = true;
                    }
                    property.LocalUpdates.Remove(matchingUpdate);
                }
                // Check for conflict, but won't stop us from applying update
                conflictDetected = data.IsConflict(property.LocalUpdates) &&
                    !property.IsServerAppliedProperty() && (property.Attributes.ConcurrencyAttribute != ConcurrencyPolicy.Overwrite);

                if (applyUpdate)
                {
                    // Mark pending updates to re-apply if we are applying a change from another endpoint
                    if (data.ClientId != this.client.ClientId)
                    {
                        property.LocalUpdates.ForEach(x => x.ReapplyUpdate = true);
                    }
                    if (entry.IsDynamic)
                    {
                        var dictionary = entry.Object as IDictionary<string, object>;
                        if (dictionary == null)
                        {
                            throw new ArgumentException("Dictionary is null");
                        }

                        // Insert up-to-date value for property
                        dictionary[data.PropertyName] = Json.ReadObject(DynamicTypeMapping.Instance.GetTypeFromValue(data.PropertyType), data.PropertyValue);
                    }
                    else
                    {
                        Json.AssignProperty(entry.Object, property.Name, data.PropertyValue);
                    }
                    property.Value = data.PropertyValue;
                }
                property.ETag = data.ETag;

                if (conflictDetected)
                {
                    Debug.WriteLine("[PropertyChanged] Conflict detected on property update");
                    IEnumerable<PropertyChangedPayload> rejectedUpdates = property.LocalUpdates.Select(x => x.Payload);
                    if (property.Attributes.ConcurrencyAttribute == ConcurrencyPolicy.RejectAndNotify)
                    {
                        var notify = new ConcurrencyUpdateRejectedEventArgs(entry.Object, property.Name, rejectedUpdates.Select(x => Json.ReadProperty(entry.Object, property.Name, x.PropertyValue)));
                        this.client.RaiseError(notify);
                    }
                    property.LocalUpdates.Clear();
                }
            }
            finally
            {
                if (entry != null)
                {
                    entry.IgnoreChanges = false;
                }
            }
        }

        internal bool WaitingForAcks()
        {
            client.VerifyAccess();
            var properties = from namedObject in this.Values
                             from property in namedObject.Properties
                             where property.Value.WaitingForAcks()
                             select property;
            return properties.Any();
        }
       
        #endregion

        #region Implementation of IEnumerable<out KeyValuePair<INotifyPropertyChanged,ObjectEntry>>
        IEnumerator<KeyValuePair<INotifyPropertyChanged, ObjectEntry>> IEnumerable<KeyValuePair<INotifyPropertyChanged, ObjectEntry>>.GetEnumerator()
        {
            return this.byRef.GetEnumerator();
        }
        #endregion

        #region Implementation of ICollection<KeyValuePair<INotifyPropertyChanged,ObjectEntry>>
        public void Add(KeyValuePair<INotifyPropertyChanged, ObjectEntry> item)
        {
            this.Add(item.Value);
        }

        public override void Add(ObjectEntry item)
        {
            base.Add(item);
            this.byRef.Add(item.Object, item);
        }

        public bool Contains(KeyValuePair<INotifyPropertyChanged, ObjectEntry> item)
        {
            return this.Contains(item.Value);
        }

        public override bool Contains(ObjectEntry item)
        {
            return base.Contains(item) && this.byRef.ContainsKey(item.Object);
        }

        public void CopyTo(KeyValuePair<INotifyPropertyChanged, ObjectEntry>[] array, int arrayIndex)
        {
            this.CopyTo(array.Select(x => x.Value).ToArray(), arrayIndex);
        }

        public override void CopyTo(ObjectEntry[] array, int arrayIndex)
        {
            base.CopyTo(array, arrayIndex);
            this.byRef.CopyTo(array.Select(x => new KeyValuePair<INotifyPropertyChanged, ObjectEntry>(x.Object, x)).ToArray(), arrayIndex);
        }

        public bool Remove(KeyValuePair<INotifyPropertyChanged, ObjectEntry> item)
        {
            return this.Remove(item.Value);
        }

        public override bool Remove(ObjectEntry item)
        {
            return base.Remove(item) && this.byRef.Remove(item.Object);
        }

        public override void Clear()
        {
            base.Clear();
            this.byRef.Clear();
        }
        #endregion

        #region Implementation of IDictionary<INotifyPropertyChanged,ObjectEntry>
        public bool ContainsKey(INotifyPropertyChanged key)
        {
            return this.byRef.ContainsKey(key);
        }

        public void Add(INotifyPropertyChanged key, ObjectEntry value)
        {
            if (key != value.Object)
            {
                throw new ArgumentException("Cannot index obejct with different object than object", "key");
            }
            this.Add(value);
        }

        public bool Remove(INotifyPropertyChanged key)
        {
            ObjectEntry value;
            if (this.byRef.TryGetValue(key, out value))
            {
                return this.Remove(value);
            }
            return false;
        }

        public bool TryGetValue(INotifyPropertyChanged key, out ObjectEntry value)
        {
            return this.byRef.TryGetValue(key, out value);
        }

        public ObjectEntry this[INotifyPropertyChanged key]
        {
            get { return this.byRef[key]; }
            set
            {
                if (key != value.Object)
                {
                    throw new ArgumentException("Cannot index obejct with different object than object", "key");
                }
                this.Put(value);
            }
        }

        protected override void Put(ObjectEntry value)
        {
            base.Put(value);
            this.byRef[value.Object] = value;
        }

        ICollection<INotifyPropertyChanged> IDictionary<INotifyPropertyChanged, ObjectEntry>.Keys
        {
            get { return this.byRef.Keys; }
        }
        #endregion
    }
}
