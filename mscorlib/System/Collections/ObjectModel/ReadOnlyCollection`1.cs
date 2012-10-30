using System.Collections.Generic;

namespace System.Collections.ObjectModel
{
    public class ReadOnlyCollection<T> : IList<T>, IList
    {
        private IList<T> list;

        public ReadOnlyCollection(IList<T> list)
        {
            if (list == null)
            {
                throw new ArgumentNullException("list");
            }
            this.list = list;
        }

        public bool Contains(T value)
        {
            return this.list.Contains(value);
        }

        public void CopyTo(T[] array, int index)
        {
            this.list.CopyTo(array, index);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.list.GetEnumerator();
        }

        public int IndexOf(T value)
        {
            return this.list.IndexOf(value);
        }

        private static bool IsCompatibleObject(object value)
        {
            return ((value is T) || ((value == null) && (default(T) == null)));
        }

        void ICollection<T>.Add(T value)
        {
            throw new NotSupportedException("collection is read-only");
        }

        void ICollection<T>.Clear()
        {
            throw new NotSupportedException("collection is read-only");
        }

        bool ICollection<T>.Remove(T value)
        {
            throw new NotSupportedException("collection is read-only");
        }

        void IList<T>.Insert(int index, T value)
        {
            throw new NotSupportedException("collection is read-only");
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new NotSupportedException("collection is read-only");
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if ((array.Length - index) < this.Count)
            {
                throw new ArgumentException();
            }
            T[] localArray = array as T[];
            if (localArray != null)
            {
                this.list.CopyTo(localArray, index);
            }
            else
            {
                Type elementType = array.GetType().GetElementType();
                Type c = typeof(T);
                if (!elementType.IsAssignableFrom(c) && !c.IsAssignableFrom(elementType))
                {
                    throw new ArgumentException("invalid array type");
                }
                object[] objArray = array as object[];
                if (objArray == null)
                {
                    throw new ArgumentException("invalid array type");
                }
                int count = this.list.Count;
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        objArray[index++] = this.list[i];
                    }
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException("invalid array type");
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.list.GetEnumerator();
        }

        int IList.Add(object value)
        {
            throw new NotSupportedException("collection is read-only");
        }

        void IList.Clear()
        {
            throw new NotSupportedException("collection is read-only");
        }

        bool IList.Contains(object value)
        {
            return (ReadOnlyCollection<T>.IsCompatibleObject(value) && this.Contains((T)value));
        }

        int IList.IndexOf(object value)
        {
            if (ReadOnlyCollection<T>.IsCompatibleObject(value))
            {
                return this.IndexOf((T)value);
            }
            return -1;
        }

        void IList.Insert(int index, object value)
        {
            throw new NotSupportedException("collection is read-only");
        }

        void IList.Remove(object value)
        {
            throw new NotSupportedException("collection is read-only");
        }

        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException("collection is read-only");
        }

        // Properties
        public int Count
        {
            get
            {
                return this.list.Count;
            }
        }

        public T this[int index]
        {
            get
            {
                return this.list[index];
            }
        }

        protected IList<T> Items
        {
            get
            {
                return this.list;
            }
        }

        bool ICollection<T>.IsReadOnly
        {
            get
            {
                return true;
            }
        }

        T IList<T>.this[int index]
        {
            get
            {
                return this.list[index];
            }
            set
            {
                throw new NotSupportedException("collection is read-only");
            }
        }

        bool IList.IsFixedSize
        {
            get
            {
                return true;
            }
        }

        bool IList.IsReadOnly
        {
            get
            {
                return true;
            }
        }

        object IList.this[int index]
        {
            get
            {
                return this.list[index];
            }
            set
            {
                throw new NotSupportedException("collection is read-only");
            }
        }

        int ICollection.Count
        {
            get { throw new NotImplementedException(); }
        }

        object ICollection.SyncRoot
        {
            get { throw new NotImplementedException(); }
        }

        bool ICollection.IsSynchronized
        {
            get { throw new NotImplementedException(); }
        }
    }
}
