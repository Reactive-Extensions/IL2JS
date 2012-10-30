// <copyright file="SharedObjectsMap.cs" company="Microsoft">
// Copyright © 2009 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    internal class SharedEntryMap<T> : IDictionary<Guid, T> where T : ISharedObjectEntry
    {
        private IDictionary<Guid, T> byId;
        private IDictionary<string, T> byName;

        public SharedEntryMap()
        {
            this.byId = new Dictionary<Guid, T>();
            this.byName = new Dictionary<string, T>();
        }

        #region Implementation of IEnumerable
        public IEnumerator<KeyValuePair<Guid, T>> GetEnumerator()
        {
            return this.byId.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.byId.GetEnumerator();
        }
        #endregion

        #region Implementation of ICollection
        public void Add(KeyValuePair<Guid, T> item)
        {
            this.Add(item.Value);
        }

        public void Add(KeyValuePair<string, T> item)
        {
            this.Add(item.Value);
        }

        public virtual void Add(T item)
        {
            this.byId.Add(item.Id, item);
            this.byName.Add(item.Name, item);
        }

        public virtual void Clear()
        {
            this.byId.Clear();
            this.byName.Clear();
        }

        public bool Contains(KeyValuePair<Guid, T> item)
        {
            return this.Contains(item.Value);
        }

        public bool Contains(KeyValuePair<string, T> item)
        {
            return this.Contains(item.Value);
        }

        public virtual bool Contains(T item)
        {
            return this.byId.ContainsKey(item.Id) && this.byName.ContainsKey(item.Name);
        }

        public void CopyTo(KeyValuePair<string, T>[] array, int arrayIndex)
        {
            this.CopyTo(array.Select(x => x.Value).ToArray(), arrayIndex);
        }

        public void CopyTo(KeyValuePair<Guid, T>[] array, int arrayIndex)
        {
            this.CopyTo(array.Select(x => x.Value).ToArray(), arrayIndex);
        }

        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            this.byId.CopyTo(array.Select(x => new KeyValuePair<Guid, T>(x.Id, x)).ToArray(), arrayIndex);
            this.byName.CopyTo(array.Select(x => new KeyValuePair<string, T>(x.Name, x)).ToArray(), arrayIndex);
        }

        public bool Remove(KeyValuePair<string, T> item)
        {
            return this.Remove(item.Value);
        }

        public bool Remove(KeyValuePair<Guid, T> item)
        {
            return this.Remove(item.Value);
        }

        public virtual bool Remove(T item)
        {
            return this.byId.Remove(item.Id) && this.byName.Remove(item.Name);
        }

        public int Count
        {
            get { return this.byId.Count; }
        }

        public bool IsReadOnly
        {
            get { return this.byId.IsReadOnly; }
        }
        #endregion

        #region Implementation of IDictionary
        public bool ContainsKey(Guid key)
        {
            return this.byId.ContainsKey(key);
        }

        public bool ContainsKey(string key)
        {
            return this.byName.ContainsKey(key);
        }

        public void Add(Guid key, T value)
        {
            if (key != value.Id)
            {
                throw new ArgumentException("Cannot index obejct with different ID than object", "key");
            }
            this.Add(value);
        }

        public void Add(string key, T value)
        {
            if (key != value.Name)
            {
                throw new ArgumentException("Cannot index obejct with different name than object", "key");
            }
            this.Add(value);
        }

        public bool Remove(Guid key)
        {
            T value;
            if (this.byId.TryGetValue(key, out value))
            {
                return this.byId.Remove(key) && this.byName.Remove(value.Name);
            }
            return false;
        }

        public bool Remove(string key)
        {
            T value;
            if (this.byName.TryGetValue(key, out value))
            {
                return this.byName.Remove(key) && this.byId.Remove(value.Id);
            }
            return false;
        }

        public bool TryGetValue(Guid key, out T value)
        {
            return this.byId.TryGetValue(key, out value);
        }

        public bool TryGetValue(string key, out T value)
        {
            if (key == null)
            {
                value = default(T);
                return false;
            }

            return this.byName.TryGetValue(key, out value);
        }

        public T this[Guid key]
        {
            get { return this.byId[key]; }
            set
            {
                if (key != value.Id)
                {
                    throw new ArgumentException("Cannot index obejct with different ID than object", "key");
                }
                this.Put(value);
            }
        }

        public T this[string key]
        {
            get { return this.byName[key]; }
            set
            {
                if (key != value.Name)
                {
                    throw new ArgumentException("Cannot index obejct with different name than object", "key");
                }
                this.Put(value);
            }
        }

        protected virtual void Put(T value)
        {
            this.byId[value.Id] = value;
            this.byName[value.Name] = value;
        }

        public ICollection<Guid> Keys
        {
            get { return this.byId.Keys; }
        }

        public ICollection<T> Values
        {
            get { return this.byId.Values; }
        }
        #endregion
    }
}
