// <copyright file="ObjectEntry.cs" company="Microsoft">
// Copyright © 2010 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects.Client
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Reflection;
    using System.Threading;
    using Microsoft.Csa.SharedObjects.Utilities;

    internal class ObjectEntry : ISharedObjectEntry
    {        
        private INotifyPropertyChanged objectReference;
        private SharedObjectsClient client;
        internal bool IgnoreChanges { get; set; }

        public string Name { get; private set; }
        public Guid Id { get; private set; }
        public SharedObjectSecurity ObjectSecurity { get; set; }
        public SharedAttributes Attributes { get; private set; }
        public SharedPropertyDictionary Properties { get; private set; }
        public bool IsDynamic { get; private set; }
        public bool IsConnected { get; set; }
        public Dictionary<Guid, ParentEntry> Parents { get; private set; }
        public Type Type { get; set; }

        public void AddParent(ParentEntry entry)
        {
            if (Parents.ContainsKey(entry.Id))
            {
                throw new ObjectAlreadyExistsException("Object is already parented by this parent");
            }
            Parents.Add(entry.Id, entry);
        }

        public void RemoveParent(Guid parentId)
        {
            if (!Parents.Remove(parentId))
            {
                throw new Exception("Object is not parented by this parent");
            }

            // If the object has no parents remove and dispose of it
            if (this.Parents.Count == 0)
            {
                this.client.ObjectsManager.Remove(this);
                this.Dispose();
            }
        }

        public INotifyPropertyChanged Object
        {
            get
            {
                return objectReference;
            }
            private set
            {
                Debug.Assert(value != null);
                value.PropertyChanged += OnPropertyChanged;
                this.objectReference = value;
            }
        }

        /// <summary>
        /// Constructor used for outgoing codepaths
        /// </summary>
        /// <param name="client"></param>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <param name="obj"></param>
        private ObjectEntry(SharedObjectsClient client, string name, Guid id, INotifyPropertyChanged obj)
        {
            this.client = client;
            this.IgnoreChanges = false;
            
            this.Object = obj;
            this.Id = id;
            this.Name = name ?? this.Id.ToString();
            IJsObject jsObj = obj as IJsObject; // do this here so we only do the cast once
            this.IsDynamic = jsObj != null;
            this.Parents = new Dictionary<Guid, ParentEntry>();
            this.Type = obj.GetType();

            this.Attributes = new SharedAttributes(obj.GetType());

            if(jsObj != null)
            {
                this.Properties = new SharedPropertyDictionary(jsObj.GetSharedProperties(this.client.ClientId));
            }
            else
            {
                this.Properties = new SharedPropertyDictionary(obj.GetSharedProperties(this.client.ClientId));
            }
        }

        /// <summary>
        /// Constructor used for Outgoing 'Open' requests
        /// </summary>
        /// <param name="client"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        public ObjectEntry(SharedObjectsClient client, string name, Type type)
            : this(client, name, Guid.Empty, ConstructSharedType(type))
        {
        }

        /// <summary>
        /// Constructor used for Outgoing operation of inserting an un-named object into a collection
        /// </summary>
        /// <param name="client"></param>
        /// <param name="obj"></param>
        public ObjectEntry(SharedObjectsClient client, INotifyPropertyChanged obj)
            : this(client, null, Guid.NewGuid(), obj)
        {
            // Un-named objects are set to connected immediately because from the clients perspective they are already live objects
            this.IsConnected = true;
        }

        /// <summary>
        /// Constructor used for Incoming Objects
        /// </summary>
        /// <param name="client"></param>
        public ObjectEntry(SharedObjectsClient client, ObjectPayload payload)
        {
            this.client = client;
            this.IgnoreChanges = true;

            this.Object = this.GenerateSharedObject(payload);
            this.Name = payload.Name;
            this.Id = payload.Id;
            this.Attributes = payload.Attributes;
            this.IsDynamic = payload.IsDynamic;
            this.Properties = new SharedPropertyDictionary(payload.SharedProperties);
            this.Parents = new Dictionary<Guid, ParentEntry>();
            this.Type = Type.GetType(payload.Type);
        }

        private static INotifyPropertyChanged ConstructSharedType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            return (INotifyPropertyChanged)Activator.CreateInstance(type);
        }

        private static INotifyPropertyChanged ConstructSharedType(string typeName)
        {
            Type type = Type.GetType(typeName);
            if(type == null)
            {
                throw new ArgumentException("Unable to resolve type " + typeName, "typeName");
            }
            return ConstructSharedType(type);
        }

        private INotifyPropertyChanged GenerateSharedObject(ObjectPayload data)
        {
            ManualResetEvent waitInit = new ManualResetEvent(false);
            INotifyPropertyChanged sharedObject = null;

            // Create the object on the Dispatcher thread
            this.client.RunOnDispatcher(() =>
            {
                sharedObject = ConstructSharedType(data.Type);
                UpdateProperties(sharedObject, data);
                waitInit.Set();
            });

            waitInit.WaitOne();
            return sharedObject;
        }

        /// <summary>
        /// Update the properties of the target object with the contents of the ObjectPayload
        /// </summary>
        /// <param name="target"></param>
        /// <param name="data"></param>
        private void UpdateProperties(INotifyPropertyChanged target, ObjectPayload data)
        {
            this.client.VerifyAccess();

            Type targetType = target.GetType();

            IJsObject jsDictionary = target as IJsObject;
            if (jsDictionary != null)
            {
                // In case of JsObservableDictionary, we want to treat the properties in this payload as items of the dictionary
                foreach (SharedProperty sharedProp in data.SharedProperties.Values)
                {
                    jsDictionary[sharedProp.Name] = Json.ReadObject(DynamicTypeMapping.Instance.GetTypeFromValue(sharedProp.PropertyType), sharedProp.Value);
                }
            }
            else
            {
                foreach (SharedProperty sharedProp in data.SharedProperties.Values)
                {
                    Json.AssignProperty(target, sharedProp.Name, sharedProp.Value);
                }
            }
        }

        /// <summary>
        /// Updates the local copy of any object based upon the contents of an ObjectPayload. This codepath
        /// is exercised when an existing named object is opened and the actual contents of the object are
        /// pushed down to the client which needs to then update the local object
        /// </summary>
        /// <param name="payload"></param>
        internal void Update(ObjectPayload payload)
        {
            try
            {
                this.IgnoreChanges = true;
                this.Id = payload.Id;
                UpdateProperties(this.Object, payload);
            }
            finally
            {
                this.IgnoreChanges = false;
            }
        }

        #region Outgoing Change Handlers
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.client.VerifyAccess();
            this.client.VerifyConnected();

            // Check if object is ignored
            if (this.IgnoreChanges)
            {
                return;
            }

            if (!this.IsDynamic && this.IsIgnoredProperty(e.PropertyName))
            {
                return;
            }

            // Check if object is connected
            if (!this.IsConnected)
            {
                throw new InvalidOperationException("Changes cannot be made to this named object until it is connected with the server");
            }

            // Try to extract property information
            SharedProperty property;

            // If dynamic, pull value from dictionary
            object dynamicVal = null;
            if (this.IsDynamic)
            {
                var dictionary = this.Object as IDictionary<string, object>;
                if (!dictionary.TryGetValue(e.PropertyName, out dynamicVal))
                {
                    throw new ArgumentException("Dictionary doesn't contains the value: " + e.PropertyName);
                }
            }
            if (!this.Properties.TryGetValue(e.PropertyName, out property))
            {
                if (this.IsDynamic)
                {
                    // Add new property if dynamic
                    if (dynamicVal != null)
                    {
                        property = new SharedProperty()
                                       {
                                           Attributes = new SharedAttributes(),
                                           Index = -1,
                                           ETag = new ETag(this.client.ClientId),
                                           Name = e.PropertyName,
                                           PropertyType = DynamicTypeMapping.Instance.GetValueFromType(dynamicVal.GetType())
                                       };
                    }
                }
                if (property == null)
                {
                    throw new ArgumentException("Dictionary doesn't contains the value: " + e.PropertyName);
                }
            }
            // Throw exception if property cannot be updated by the client
            if (property.IsServerAppliedProperty())
            {
                 throw new InvalidOperationException("Client cannot modify properties with server-applied attributes");
            }

            // Create payload
            PropertyChangedPayload data = new PropertyChangedPayload(this.client.ClientId, property.Index, this.Id, property.ETag, this.IsDynamic ? dynamicVal : this.Object.GetPropertyValue(e.PropertyName));
            if (this.IsDynamic)
            {
                data.PropertyName = e.PropertyName;
                // Property type might change, we need to re-get each time
                data.PropertyType = DynamicTypeMapping.Instance.GetValueFromType(dynamicVal.GetType());
            }

            // Add to pending update list
            property.LocalUpdates.Add(new PropertyUpdateOperation() { Payload = data, ReapplyUpdate = false });

            // Send to server
            this.client.SendPublishEvent(data);
        }

        private bool IsIgnoredProperty(string propertyName)
        {
            if (this.IgnoreChanges)
            {
                return true;
            }

            Type objectType = this.Object.GetType();
            PropertyInfo propertyInfo = objectType.GetProperty(propertyName);
            return propertyInfo.IsIgnored();
        }

        #endregion

        /// <summary>
        /// Construct an ObjectPayload for this ObjectEntry
        /// </summary>
        /// <returns></returns>
        internal ObjectPayload ToPayload()
        {
            return new ObjectPayload(this.client.ClientId, this.Object.GetType().AssemblyQualifiedName, this.Id,
                                     this.Name, this.Properties, this.Attributes, this.IsDynamic);
        }
        
        public void Dispose()
        {
            INotifyPropertyChanged obj = this.objectReference;
            if (obj != null)
            {
                obj.PropertyChanged -= OnPropertyChanged;
            }
        }

        internal void OnDisconnect()
        {
            foreach (var pair in Properties)
            {
                pair.Value.OnDisconnect();
            }
        }
    }
}
