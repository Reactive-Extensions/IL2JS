

namespace System.Collections.ObjectModel
{
    using System.Collections.Generic;

    public class Collection<T> : IList<T>, ICollection<T>, IEnumerable<T>, IList, ICollection, IEnumerable
    {
        // Fields
        private IList<T> items;

        // Methods
        public Collection() : this(new List<T>()) { }
        public Collection(IList<T> list) { this.items = list; }
        public void Add(T item) { this.items.Add(item); }
        public void Clear() { this.items.Clear(); }
        public bool Contains(T item) { return this.items.Contains(item); }
        public void CopyTo(T[] array, int index) { this.items.CopyTo(array, index); }
        public IEnumerator<T> GetEnumerator() { return this.items.GetEnumerator(); }
        public int IndexOf(T item) { return this.IndexOf(item); }
        public void Insert(int index, T item) { this.items.Insert(index, item); }
        public bool Remove(T item) { return this.items.Remove(item); }
        public void RemoveAt(int index) { this.items.RemoveAt(index); }
        void ICollection.CopyTo(Array array, int index) { throw new NotSupportedException(); }
        IEnumerator IEnumerable.GetEnumerator() { return items.GetEnumerator(); }

        int IList.Add(object value) 
        {
            throw new NotSupportedException();
            //items.Add((object)value); 
            //return items.Count; 
        }

        bool IList.Contains(object value) { throw new NotSupportedException(); }
        int IList.IndexOf(object value) { throw new NotSupportedException(); }
        void IList.Insert(int index, object value) { throw new NotSupportedException(); }
        void IList.Remove(object value) { throw new NotSupportedException(); }

        // Properties
        public int Count { get { return items.Count; } }
        public T this[int index]
        {
            get { return items[index]; }
            set { items[index] = value; }
        }
        bool ICollection<T>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        bool ICollection.IsSynchronized { get { return false; } }
        object ICollection.SyncRoot { get { return items; } }
        bool IList.IsFixedSize { get { return false; } }
        bool IList.IsReadOnly { get { return false; } }
        object IList.this[int index]
        {
            get
            {
                return items[index];
            }
            set
            {
                items[index] = (T)value;
            }
        }
    }
}
