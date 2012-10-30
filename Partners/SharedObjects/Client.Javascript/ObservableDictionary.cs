// <copyright file="ObservableDictionary.cs" company="Microsoft">
// Copyright © 2009 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects.Client
{
    using System.Collections.Generic;
    using System.Collections.Specialized;

    /// <summary>
    /// This class can not be used for databinding.
    /// </summary>
    internal class ObservableDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        public event NotifyDictionaryChangedEventHandler<TKey, TValue> CollectionChanged;
        private Dictionary<TKey, TValue> dictionary;

        public ObservableDictionary()
        {
            this.dictionary = new Dictionary<TKey, TValue>();
        }

        private void DispatchChange(NotifyDictionaryChangedEventArgs<TKey, TValue> args)
        {
            if (CollectionChanged != null)
                CollectionChanged(this, args);
        }

        internal void Add(TKey key, TValue value)
        {
            this.dictionary.Add(key, value);
            var dictArgs = new NotifyDictionaryChangedEventArgs<TKey, TValue> { Action = NotifyCollectionChangedAction.Add, NewItem = value, Key = key };

            DispatchChange(dictArgs);
        }

        public bool ContainsKey(TKey key)
        {
            return this.dictionary.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get { return this.dictionary.Keys; }
        }

        internal bool Remove(TKey key)
        {
            TValue value = this.dictionary.ContainsKey(key) ? this.dictionary[key] : default(TValue);
            bool removed = this.dictionary.Remove(key);
            var dictArgs = new NotifyDictionaryChangedEventArgs<TKey, TValue> { Action = NotifyCollectionChangedAction.Remove, OldItem = value, Key = key };

            DispatchChange(dictArgs);

            return removed;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return this.dictionary.TryGetValue(key, out value);
        }

        public ICollection<TValue> Values
        {
            get { return this.dictionary.Values; }
        }

        public TValue this[TKey key]
        {
            get
            {
                return this.dictionary[key];
            }
            internal set
            {
                TValue oldValue = this.dictionary.ContainsKey(key) ? this.dictionary[key] : default(TValue);
                this.dictionary[key] = value;

                var dictArgs = new NotifyDictionaryChangedEventArgs<TKey, TValue> { Action = NotifyCollectionChangedAction.Replace, OldItem = oldValue, NewItem = value, Key = key };

                DispatchChange(dictArgs);
            }
        }

        internal void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        internal void Clear()
        {

            foreach (TKey key in this.dictionary.Keys)
            {
                TValue value = this.dictionary[key];

                var dictArgs = new NotifyDictionaryChangedEventArgs<TKey, TValue> { Action = NotifyCollectionChangedAction.Remove, OldItem = value };

                DispatchChange(dictArgs);
            }

            this.dictionary.Clear();
        }

        public int Count
        {
            get { return this.dictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        #region IEnumerable Members

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return this.dictionary.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.dictionary.GetEnumerator();
        }

        #endregion
    }

    public delegate void NotifyDictionaryChangedEventHandler<TKey, TValue>(object sender, NotifyDictionaryChangedEventArgs<TKey, TValue> args);

    public class NotifyDictionaryChangedEventArgs<TKey, TValue>
    {
        public NotifyCollectionChangedAction Action { get; set; }
        public TKey Key { get; set; }
        public TValue NewItem { get; set; }
        public TValue OldItem { get; set; }
    }
}
