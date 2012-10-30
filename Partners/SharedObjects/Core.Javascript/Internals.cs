// <copyright file="Internals.cs" company="Microsoft">
// Copyright © 2010 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using Microsoft.Csa.SharedObjects.Utilities;

    internal class ParentEntry : ISharedObjectSerializable, INotifyPropertyChanged
    {
        private Guid id;

        public Guid Id
        {
            get { return this.id; }
            set { this.id = value; this.NotifyPropertyChanged("Id"); }
        }

        private int index;

        public int Index
        {
            get { return this.index; }
            set { this.index = value; this.NotifyPropertyChanged("Index"); }
        }

        public ParentEntry()
        {
        }

        public ParentEntry(Guid id, int index)
        {
            this.Id = id;
            this.Index = index;
        }

        #region Implementation of INotifyPropertyChanged
        // NotifyPropertyChanged will raise the PropertyChanged event, passing the source property that is being updated.
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region Implementation of ISharedObjectSerializable
        public void Serialize(IPayloadWriter writer)
        {
            writer.Write("Id", this.Id);
            writer.Write("Index", this.Index);
        }

        public void Deserialize(IPayloadReader reader)
        {
            this.Id = reader.ReadGuid("Id");
            this.Index = reader.ReadInt32("Index");
        }
        #endregion
    }

    internal class SharedProperty : ISharedObjectSerializable, INotifyPropertyChanged
    {
        public string Name { get; set; }
        public short Index { get; set; }
        public string Value { get; set; }
        public SharedAttributes Attributes { get; set; }

        // TODO: workaround for js bridge. Remove when bridge is removed
        public byte PropertyType { get; set; }

        internal List<PropertyUpdateOperation> LocalUpdates { get; private set; }

        private ETag etag;

        public ETag ETag
        {
            get { return this.etag; }
            set
            {
                // Remove property changed handler from old ETag
                if (this.etag != null)
                {
                    this.etag.PropertyChanged -= this.ETagChanged;
                }

                // Set new ETag and apply PropertyChanged
                this.etag = value;
                this.etag.PropertyChanged += this.ETagChanged;
                this.NotifyPropertyChanged("ETag");
            }
        }

        private void ETagChanged(object sender, PropertyChangedEventArgs e)
        {
            this.NotifyPropertyChanged("ETag");
        }

        public SharedProperty()
        {
            this.Attributes = new SharedAttributes();
            this.ETag = new ETag();
            this.LocalUpdates = new List<PropertyUpdateOperation>();
        }

        public SharedProperty(Guid clientId)
            : this()
        {
            this.ETag = new ETag(clientId);
        }

        public bool IsServerAppliedProperty()
        {
            return (this.Attributes.TimestampAttribute != UpdateFrequency.Never)
                    || (this.Attributes.EditorAttribute != EditorOption.None);
        }

        internal bool WaitingForAcks()
        {
            return LocalUpdates.Any();
        }

        internal void OnDisconnect()
        {
            LocalUpdates.Clear();
        }

        #region INotifyPropertyChanged Members
        // NotifyPropertyChanged will raise the PropertyChanged event, passing the source property that is being updated.
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region ISharedObjectSerializable Members
        public virtual void Serialize(IPayloadWriter writer)
        {
            writer.Write("Name", this.Name);
            writer.Write("Index", this.Index);
            writer.Write("ETag", this.ETag);
            writer.Write("Attributes", this.Attributes);
            writer.Write("PropertyType", this.PropertyType);
            writer.Write("Value", this.Value);
        }

        public virtual void Deserialize(IPayloadReader reader)
        {
            this.Name = reader.ReadString("Name");
            this.Index = reader.ReadInt16("Index");
            this.ETag = reader.ReadObject<ETag>("ETag");
            this.Attributes = reader.ReadObject<SharedAttributes>("Attributes", ReadObjectOption.Create);
            this.PropertyType = reader.ReadByte("PropertyType");
            this.Value = reader.ReadString("Value");
        }
        #endregion
    }

    internal class PropertyUpdateOperation
    {
        public PropertyChangedPayload Payload { get; set; }
        public bool ReapplyUpdate { get; set; }

        #region Equality Operators
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            PropertyUpdateOperation other = obj as PropertyUpdateOperation;
            if (other == null)
            {
                return false;
            }
            return other.Payload.Equals(this.Payload);
        }
        #endregion
    }

    internal class DynamicTypeMapping
    {
        private Dictionary<byte, Type> Mapping { get; set; }
        public static readonly DynamicTypeMapping Instance = new DynamicTypeMapping();
        private DynamicTypeMapping() { }

        public Type GetTypeFromValue(byte value)
        {
            if (this.Mapping == null)
            {
                InitTypeMappings();
            }
            return this.Mapping[value];
        }
        public byte GetValueFromType(Type type)
        {
            if (this.Mapping == null)
            {
                this.InitTypeMappings();
            }
            return (this.Mapping.Where(kv => kv.Value.FullName.Equals(type.FullName))).FirstOrDefault().Key;
        }

        private void InitTypeMappings()
        {
            this.Mapping = new Dictionary<byte, Type>();
            this.Mapping.Add(0, typeof(System.String));
            this.Mapping.Add(1, typeof(System.Int32));
        }
    }

    /// <summary>
    /// A Custom Dictionary for SharedProperty to allow accessing an entry based on SharedProperty Name
    /// </summary>
    internal class SharedPropertyDictionary : ISharedObjectSerializable, IDictionary<short, SharedProperty>
    {
        private IDictionary<short, SharedProperty> dictionary;

        public SharedPropertyDictionary()
        {
            this.dictionary = new Dictionary<short, SharedProperty>();
        }

        public SharedPropertyDictionary(IDictionary<short, SharedProperty> dictionary)            
        {
            this.dictionary = dictionary;
        }

        public void Add(short index, SharedProperty property)
        {
            this.dictionary.Add(index, property);
        }

        public SharedProperty this[string propertyName]
        {
            get
            {
                return this.dictionary.Values.Where(x => x.Name.Equals(propertyName)).FirstOrDefault();
            }
        }

        public ICollection<SharedProperty> Values
        {
            get
            {
                return this.dictionary.Values;
            }
        }

        public bool TryGetValue(string propertyName, out SharedProperty property)
        {
            property = this.dictionary.Values.Where(x => x.Name.Equals(propertyName)).FirstOrDefault();
            return property != null;
        }

        #region Implementation of ISharedObjectSerializable
        public void Serialize(IPayloadWriter writer)
        {
            writer.Write("Values", this.dictionary.Values);
        }

        public void Deserialize(IPayloadReader reader)
        {
            this.dictionary.Clear();
            reader.ReadList("Values", r =>
                {
                    SharedProperty prop = new SharedProperty();
                    prop.Deserialize(r);
                    this.dictionary.Add(prop.Index, prop);
                });
        }
        #endregion

        #region IDictionary<short,SharedProperty> Members


        public bool ContainsKey(short key)
        {
            throw new NotImplementedException();
        }

        public ICollection<short> Keys
        {
            get { throw new NotImplementedException(); }
        }

        public bool Remove(short key)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(short key, out SharedProperty value)
        {
            throw new NotImplementedException();
        }

        public SharedProperty this[short key]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region ICollection<KeyValuePair<short,SharedProperty>> Members

        public void Add(KeyValuePair<short, SharedProperty> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<short, SharedProperty> item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<short, SharedProperty>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public bool Remove(KeyValuePair<short, SharedProperty> item)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable<KeyValuePair<short,SharedProperty>> Members

        public IEnumerator<KeyValuePair<short, SharedProperty>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    internal class SharedAsyncResult<T> : AsyncResult<T>, ISharedAsyncResult
    {
        public Action<T> SharedAction { get; set; }

        public SharedAsyncResult(AsyncCallback asyncCallback, object state)
            : base(asyncCallback, state)
        {
        }

        public SharedAsyncResult(AsyncCallback asyncCallback, object state, Action<T> action)
            : base(asyncCallback, state)
        {
            SharedAction = action;
        }
    }

}
