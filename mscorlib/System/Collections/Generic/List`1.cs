using System.Collections.ObjectModel;

namespace System.Collections.Generic
{
    public class List<T> : IList<T>, IList
    {
        private const int _defaultCapacity = 4;
        private static readonly T[] _emptyArray;
        private T[] _items;
        private int _size;
        private int _version;

        static List()
        {
            _emptyArray = new T[0];
        }

        public List()
        {
            _items = _emptyArray;
        }

        public List(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }
            ICollection<T> is2 = collection as ICollection<T>;
            if (is2 != null)
            {
                int count = is2.Count;
                this._items = new T[count];
                is2.CopyTo(this._items, 0);
                this._size = count;
            }
            else
            {
                this._size = 0;
                this._items = new T[4];
                using (IEnumerator<T> enumerator = collection.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        this.Add(enumerator.Current);
                    }
                }
            }
        }

        public List(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException("capacity");
            }
            this._items = new T[capacity];
        }

        public void Add(T item)
        {
            if (this._size == this._items.Length)
            {
                this.EnsureCapacity(this._size + 1);
            }
            this._items[this._size++] = item;
            this._version++;
        }

        public void AddRange(IEnumerable<T> collection)
        {
            this.InsertRange(this._size, collection);
        }

        public ReadOnlyCollection<T> AsReadOnly()
        {
            return new ReadOnlyCollection<T>(this);
        }

        public int BinarySearch(T item)
        {
            return this.BinarySearch(0, this.Count, item, null);
        }

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return this.BinarySearch(0, this.Count, item, comparer);
        }

        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            if ((this._size - index) < count)
            {
                throw new ArgumentException();
            }
            return Array.BinarySearch<T>(this._items, index, count, item, comparer);
        }

        public void Clear()
        {
            if (this._size > 0)
            {
                Array.Clear(this._items, 0, this._size);
                this._size = 0;
            }
            this._version++;
        }

        public bool Contains(T item)
        {
            if (item == null)
            {
                for (int j = 0; j < this._size; j++)
                {
                    if (this._items[j] == null)
                    {
                        return true;
                    }
                }
                return false;
            }
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < this._size; i++)
            {
                if (comparer.Equals(this._items[i], item))
                {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(T[] array)
        {
            this.CopyTo(array, 0);
        }

        public void CopyTo(T[] array, int index)
        {
            Array.Copy(this._items, 0, array, index, this._size);
        }

        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            if ((this._size - index) < count)
            {
                throw new ArgumentException();
            }
            Array.Copy(this._items, index, array, arrayIndex, count);
        }

        private void EnsureCapacity(int min)
        {
            if (this._items.Length < min)
            {
                int num = (this._items.Length == 0) ? 4 : (this._items.Length * 2);
                if (num < min)
                {
                    num = min;
                }
                this.Capacity = num;
            }
        }

        public void ForEach(Action<T> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException("action");
            }
            for (int i = 0; i < this._size; i++)
            {
                action(this._items[i]);
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator((List<T>)this);
        }

        public List<T> GetRange(int index, int count)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            if ((this._size - index) < count)
            {
                throw new ArgumentException();
            }
            List<T> list = new List<T>(count);
            Array.Copy(this._items, index, list._items, 0, count);
            list._size = count;
            return list;
        }

        public int IndexOf(T item)
        {
            return Array.IndexOf<T>(this._items, item, 0, this._size);
        }

        public int IndexOf(T item, int index)
        {
            if (index > this._size)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            return Array.IndexOf<T>(this._items, item, index, this._size - index);
        }

        public int IndexOf(T item, int index, int count)
        {
            if (index > this._size)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if ((count < 0) || (index > (this._size - count)))
            {
                throw new ArgumentOutOfRangeException("count");
            }
            return Array.IndexOf<T>(this._items, item, index, count);
        }

        public void Insert(int index, T item)
        {
            if (index > this._size)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (this._size == this._items.Length)
            {
                this.EnsureCapacity(this._size + 1);
            }
            if (index < this._size)
            {
                Array.Copy(this._items, index, this._items, index + 1, this._size - index);
            }
            this._items[index] = item;
            this._size++;
            this._version++;
        }

        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("collection");
            }
            if (index > this._size)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            ICollection<T> is2 = collection as ICollection<T>;
            if (is2 != null)
            {
                int count = is2.Count;
                if (count > 0)
                {
                    this.EnsureCapacity(this._size + count);
                    if (index < this._size)
                    {
                        Array.Copy(this._items, index, this._items, index + count, this._size - index);
                    }
                    if (this == is2)
                    {
                        Array.Copy(this._items, 0, this._items, index, index);
                        Array.Copy(this._items, index + count, this._items, index * 2, this._size - index);
                    }
                    else
                    {
                        T[] array = new T[count];
                        is2.CopyTo(array, 0);
                        array.CopyTo(this._items, index);
                    }
                    this._size += count;
                }
            }
            else
            {
                using (IEnumerator<T> enumerator = collection.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        this.Insert(index++, enumerator.Current);
                    }
                }
            }
            this._version++;
        }

        private static bool IsCompatibleObject(object value)
        {
            return ((value is T) || ((value == null) && (default(T) == null)));
        }

        public int LastIndexOf(T item)
        {
            return this.LastIndexOf(item, this._size - 1, this._size);
        }

        public int LastIndexOf(T item, int index)
        {
            if (index >= this._size)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            return this.LastIndexOf(item, index, index + 1);
        }

        public int LastIndexOf(T item, int index, int count)
        {
            if (this._size == 0)
            {
                return -1;
            }
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            if (index >= this._size)
                throw new ArgumentOutOfRangeException("index");
            if (count > (index + 1))
                throw new ArgumentOutOfRangeException("count");
            return Array.LastIndexOf<T>(this._items, item, index, count);
        }

        public bool Remove(T item)
        {
            int index = this.IndexOf(item);
            if (index >= 0)
            {
                this.RemoveAt(index);
                return true;
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            if (index >= this._size)
            {
                throw new ArgumentOutOfRangeException();
            }
            this._size--;
            if (index < this._size)
            {
                Array.Copy(this._items, index + 1, this._items, index, this._size - index);
            }
            this._items[this._size] = default(T);
            this._version++;
        }

        public void RemoveRange(int index, int count)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            if ((this._size - index) < count)
            {
                throw new ArgumentException();
            }
            if (count > 0)
            {
                this._size -= count;
                if (index < this._size)
                {
                    Array.Copy(this._items, index + count, this._items, index, this._size - index);
                }
                Array.Clear(this._items, this._size, count);
                this._version++;
            }
        }

        public void Reverse()
        {
            this.Reverse(0, this.Count);
        }

        public void Reverse(int index, int count)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            if ((this._size - index) < count)
            {
                throw new ArgumentException();
            }
            Array.Reverse(this._items, index, count);
            this._version++;
        }

        public void Sort()
        {
            this.Sort(0, this.Count, null);
        }

        public void Sort(IComparer<T> comparer)
        {
            this.Sort(0, this.Count, comparer);
        }

        public void Sort(Comparison<T> comparison)
        {
            if (comparison == null)
            {
                throw new ArgumentNullException("comparison");
            }
            if (this._size > 0)
            {
                IComparer<T> comparer = new Array.FunctorComparer<T>(comparison);
                Array.Sort<T>(this._items, 0, this._size, comparer);
            }
        }

        public void Sort(int index, int count, IComparer<T> comparer)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            if ((this._size - index) < count)
            {
                throw new ArgumentException();
            }
            Array.Sort<T>(this._items, index, count, comparer);
            this._version++;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator((List<T>)this);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            try
            {
                Array.Copy(this._items, 0, array, index, this._size);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException("array");
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator((List<T>)this);
        }

        int IList.Add(object item)
        {
            if (item == null && default(T) != null)
                throw new ArgumentNullException("item");
            try
            {
                this.Add((T)item);
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException("item");
            }
            return (this.Count - 1);
        }

        bool IList.Contains(object item)
        {
            return (List<T>.IsCompatibleObject(item) && this.Contains((T)item));
        }

        int IList.IndexOf(object item)
        {
            if (List<T>.IsCompatibleObject(item))
            {
                return this.IndexOf((T)item);
            }
            return -1;
        }

        void IList.Insert(int index, object item)
        {
            if (item == null && default(T) != null)
                throw new ArgumentNullException("item");
            try
            {
                this.Insert(index, (T)item);
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException("item");
            }
        }

        void IList.Remove(object item)
        {
            if (List<T>.IsCompatibleObject(item))
            {
                this.Remove((T)item);
            }
        }

        public T[] ToArray()
        {
            T[] destinationArray = new T[this._size];
            Array.Copy(this._items, 0, destinationArray, 0, this._size);
            return destinationArray;
        }

        public void TrimExcess()
        {
            int num = (int)(this._items.Length * 0.9);
            if (this._size < num)
            {
                this.Capacity = this._size;
            }
        }

        public int Capacity
        {
            get
            {
                return this._items.Length;
            }
            set
            {
                if (value != this._items.Length)
                {
                    if (value < this._size)
                    {
                        throw new ArgumentOutOfRangeException("value");
                    }
                    if (value > 0)
                    {
                        T[] destinationArray = new T[value];
                        if (this._size > 0)
                        {
                            Array.Copy(this._items, 0, destinationArray, 0, this._size);
                        }
                        this._items = destinationArray;
                    }
                    else
                    {
                        this._items = List<T>._emptyArray;
                    }
                }
            }
        }

        public int Count
        {
            get
            {
                return this._size;
            }
        }

        public T this[int index]
        {
            get
            {
                if (index >= this._size)
                {
                    throw new ArgumentOutOfRangeException();
                }
                return this._items[index];
            }
            set
            {
                if (index >= this._size)
                {
                    throw new ArgumentOutOfRangeException();
                }
                this._items[index] = value;
                this._version++;
            }
        }

        bool ICollection<T>.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        bool IList.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        bool IList.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        object IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                if (value == null && default(T) != null)
                    throw new ArgumentNullException("value");
                try
                {
                    this[index] = (T)value;
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException("value");
                }
            }
        }

        public struct Enumerator : IEnumerator<T>
        {
            private List<T> list;
            private int index;
            private int version;
            private T current;
            internal Enumerator(List<T> list)
            {
                this.list = list;
                this.index = 0;
                this.version = list._version;
                this.current = default(T);
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (this.version != this.list._version)
                {
                    throw new InvalidOperationException("list changed during enumeration");
                }
                if (this.index < this.list._size)
                {
                    this.current = this.list._items[this.index];
                    this.index++;
                    return true;
                }
                this.index = this.list._size + 1;
                this.current = default(T);
                return false;
            }

            public T Current
            {
                get
                {
                    return this.current;
                }
            }
            object IEnumerator.Current
            {
                get
                {
                    if ((this.index == 0) || (this.index == (this.list._size + 1)))
                    {
                        throw new InvalidOperationException("invalid enemeration state");
                    }
                    return this.Current;
                }
            }
            void IEnumerator.Reset()
            {
                if (this.version != this.list._version)
                {
                    throw new InvalidOperationException("list changed during enumeration");
                }
                this.index = 0;
                this.current = default(T);
            }
        }


        public object SyncRoot
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsSynchronized
        {
            get { throw new NotImplementedException(); }
        }
    }
}
