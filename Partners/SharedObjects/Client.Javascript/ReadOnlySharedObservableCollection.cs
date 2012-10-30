// <copyright file="ReadOnlySharedObservableCollection.cs" company="Microsoft">
// Copyright © 2009 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects.Client
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;

    /// <summary>
    /// Provides an immutable wrapper to a SharedObjectCollection
    /// </summary>
    public class ReadOnlySharedObservableCollection<T> : INotifyCollectionChanged,
                                                     ICollection<T> where T : INotifyPropertyChanged
    {
        /// <summary>
        /// Construct an instance that wraps an existing SharedObjectCollection
        /// </summary>
        /// <param name="collection">The mutable collection</param>
        public ReadOnlySharedObservableCollection(SharedObservableCollection<T> collection)
        {
            this.Items = collection;
        }

        protected SharedObservableCollection<T> Items { get; private set; }

        public T this[int idx]
        {
            get { return Items[idx]; }
        }

        #region ICollection<T> Members

        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException();
        }

        void ICollection<T>.Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(T item)
        {
            return this.Items.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.Items.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return this.Items.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException();
        }

        public int IndexOf(T item)
        {
            return this.Items.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }
        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return this.Items.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Items.GetEnumerator();
        }

        #endregion

        #region INotifyCollectionChanged Members
        private NotifyCollectionChangedEventHandler collectionChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                // if this is the first time a subscription is made, subscribe
                // to the underlying collection
                if (this.collectionChanged == null)
                {
                    this.Items.CollectionChanged += OnSharedObjectCollectionChanged;
                }
                this.collectionChanged += value;
            }

            remove
            {
                this.collectionChanged -= value;

                // if there are no more subscribers, unsubscribe from the underlying 
                // collection
                if (this.collectionChanged == null)
                {
                    this.Items.CollectionChanged -= OnSharedObjectCollectionChanged;
                }
            }
        }

        private void OnSharedObjectCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // called when the underlying member list changes. Here we can just forward the notification
            // to our subscribers because the object in the event args is the same object.
            this.collectionChanged(this, e);
        }
        #endregion
    }
}
