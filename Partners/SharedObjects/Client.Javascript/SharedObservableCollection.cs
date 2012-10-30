// <copyright file="SharedObservableCollection.cs" company="Microsoft">
// Copyright © 2009 Microsoft Corporation.  All rights reserved.
// </copyright>

namespace Microsoft.Csa.SharedObjects.Client
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Threading;
    using System.Reflection;
    using Microsoft.Csa.SharedObjects;

    /// <summary>
    /// The non-generic SharedCollection is only used for internal purposes. In certain scenarios we need to perform
    /// operations on a set of collections whose generic Type we do not know. In those cases we cast the collections to this class
    /// which enables modification without knowing the generic type. 
    /// </summary>
    public abstract partial class SharedCollection : INotifyPropertyChanged
    {
        private bool isConnected = false;
        private List<Action<CollectionConnectedEventArgs>> connectedCallbacks;

        public string Name { get; private set; }

        public Guid Id { get; internal set; }

        internal CollectionEntry Entry { get; set; }
        public Type Type { get; protected set; }

        
        /// <summary>
        /// Gets the Dispatcher this object is associated with. 
        /// </summary>
        public Dispatcher Dispatcher { get; private set; }
 
        internal SharedCollection(string name, Type type, CollectionEntry entry)
        {
            this.Name = name;
            this.Type = type;
            this.Entry = entry;
            this.Dispatcher = entry.client.Dispatcher;
        }


        /// <summary>
        /// Is true if the collection is in a faulted state (has been deleted / has experienced an unrecoverable error)
        /// </summary>
        public bool IsFaulted { get; internal set; }

        public bool IsConnected
        {
            get
            {
                return isConnected;
            }
            protected set
            {
                if (this.isConnected != value)
                {
                    this.isConnected = value;
                    RaisePropertyChanged("IsConnected");
                }
            }
        }

        internal void OnConnected(bool created)
        {
            this.IsConnected = true;
            if(connectedCallbacks != null)
            {
                var e = new CollectionConnectedEventArgs(this, this.Name, created);
                connectedCallbacks.ForEach(a => a(e));
                connectedCallbacks = null;
            }
        }

        internal void RegisterConnectedCallback(Action<CollectionConnectedEventArgs> callback)
        {
            if (callback == null)
            {
                return;
            }
            if (this.IsConnected)
            {
                // Collection was already connected when open was called
                callback(new CollectionConnectedEventArgs(this, this.Name, false));
                return;
            }
            if(connectedCallbacks == null)
            {
                connectedCallbacks = new List<Action<CollectionConnectedEventArgs>>(1);
            }
            connectedCallbacks.Add(callback);
        }

        internal void RaiseDisconnected()
        {
            this.IsConnected = false;
        }

        internal void VerifyNotFaulted()
        {
            if (this.IsFaulted)
            {
                throw new InvalidOperationException("No changes can be made to this collection because it is in a faulted state. It may have been deleted or fallen out of sync with the server");
            }
        }

        internal void VerifyConnected()
        {
            if (!this.IsConnected || this.IsFaulted)
            {
                throw new ClientDisconnectedException("No changes can be made to this collection because it is not in a connected state.");
            }
        }

        abstract internal void ClearSharedCollection();

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        internal void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }

    public class SharedObservableBag<T> : SharedCollection, ICollection<T>, ICollection, INotifyCollectionChanged where T : INotifyPropertyChanged
    {
        protected ObservableCollection<INotifyPropertyChanged> Items;

        internal SharedObservableBag(string name, Type type, ObservableCollection<INotifyPropertyChanged> items, CollectionEntry entry)
            : base(name, type, entry)
        {
            this.Items = items;
        }

        internal SharedObservableBag(string name, Type type, CollectionEntry entry)
            : this(name, type, new ObservableCollection<INotifyPropertyChanged>(), entry)
        {
        }

        internal SharedObservableBag(string name, CollectionEntry entry)
            : this(name, typeof(T), entry)
        {
        }

        internal SharedObservableBag(SharedObservableBag<INotifyPropertyChanged> collection, CollectionEntry entry)
            : this(collection.Name, collection.Type, collection.Items, entry)
        {
            this.IsConnected = collection.IsConnected;
        }

        internal override void ClearSharedCollection()
        {
            this.Items.Clear();
        }

        internal void IncomingAdd(T item)
        {
            this.Dispatcher.VerifyAccess();
            this.VerifyNotFaulted();
            this.Items.Add(item);
        }

        public bool IncomingRemove(T item)
        {
            this.Dispatcher.VerifyAccess();
            this.VerifyNotFaulted();
            return this.Items.Remove(item);
        }

        #region INotifyCollectionChanged Members

        private NotifyCollectionChangedEventHandler collectionChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged
        {
            add
            {
                if (this.collectionChanged == null)
                {
                    this.Items.CollectionChanged += this.OnItemsCollectionChanged;
                }
                this.collectionChanged += value;
            }
            remove
            {
                this.collectionChanged -= value;
                if (this.collectionChanged == null)
                {
                    this.Items.CollectionChanged -= this.OnItemsCollectionChanged;
                }
            }
        }

        private void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.collectionChanged(this, e);
        }

        #endregion

        #region ICollection<T> Members

        public void Add(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            this.Dispatcher.VerifyAccess();
            this.VerifyConnected();

            this.Items.Add(item);
        }

        public void Clear()
        {
            this.Dispatcher.VerifyAccess();
            this.VerifyConnected();
            this.Items.Clear();
        }

        public bool Contains(T item)
        {
            return this.Items.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            this.Dispatcher.VerifyAccess();
            this.VerifyConnected();

            this.Items.Cast<T>().ToList().CopyTo(array, arrayIndex);
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            this.Dispatcher.VerifyAccess();
            this.VerifyConnected();
            return this.Items.Remove(item);
        }

        public int Count
        {
            get { return this.Items.Count; }
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return this.Items.Cast<T>().GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region ICollection Members

        void ICollection.CopyTo(Array array, int index)
        {
            this.CopyTo((T[])array, index);
        }

        bool ICollection.IsSynchronized
        {
            get { return false; }
        }

        object ICollection.SyncRoot
        {
            get { return null; }
        }

        #endregion
    }

    public class SharedObservableCollection<T> : SharedObservableBag<T>, INotifyCollectionChanged, IList,
                                           IList<T>, ICollection<T> where T : INotifyPropertyChanged
    {
        private SharedObservableCollection(string name, Type type, ObservableCollection<INotifyPropertyChanged> items, CollectionEntry entry)
            : base(name, type, items, entry)
        {
        }

        internal SharedObservableCollection(string name, Type type, CollectionEntry entry)
            : this(name, type, new ObservableCollection<INotifyPropertyChanged>(), entry)
        {
        }

        internal SharedObservableCollection(string name, CollectionEntry entry)
            : this(name, typeof(T), entry)
        {
        }

        internal SharedObservableCollection(SharedObservableCollection<INotifyPropertyChanged> collection, CollectionEntry entry)
            : this(collection.Name, collection.Type, collection.Items, entry)
        {
            this.IsConnected = collection.IsConnected;
        }

        public void IncomingInsert(int index, T item)
        {
            this.Dispatcher.VerifyAccess();
            this.VerifyNotFaulted();
            this.Items.Insert(index, item);
        }

        #region IList<T> Members

        public T this[int index]
        {
            get
            {
                return (T)this.Items[index];
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("item");
                }

                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException("index");
                }

                this.Dispatcher.VerifyAccess();
                this.VerifyConnected();
                this.Items[index] = value;
            }
        }

        public int IndexOf(T item)
        {
            return this.Items.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            this.Dispatcher.VerifyAccess();
            this.VerifyConnected();
            this.Items.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            this.Dispatcher.VerifyAccess();
            this.VerifyConnected();
            this.Items.RemoveAt(index);
        }

        #endregion

        #region IList Members

        int IList.Add(object value)
        {
            this.Add((T)value);
            return this.Count - 1;
        }

        bool IList.Contains(object value)
        {
            return this.Contains((T)value);
        }

        int IList.IndexOf(object value)
        {
            return this.IndexOf((T)value);
        }

        void IList.Insert(int index, object value)
        {
            this.Insert(index, (T)value);
        }

        bool IList.IsFixedSize
        {
            get { return false; }
        }

        void IList.Remove(object value)
        {
            this.Remove((T)value);
        }

        object IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                this[index] = (T)value;
            }
        }

        #endregion
    }
}
