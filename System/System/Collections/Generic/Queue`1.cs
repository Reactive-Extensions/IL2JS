namespace System.Collections.Generic
{
    public class Queue<T> : IEnumerable<T>, ICollection
    {
        private T[] _array;
        private const int _DefaultCapacity = 4;
        private static T[] _emptyArray;
        private const int _GrowFactor = 200;
        private int _head;
        private const int _MinimumGrow = 4;
        private const int _ShrinkThreshold = 0x20;
        private int _size;
        private object _syncRoot;
        private int _tail;

        static Queue()
        {
            _emptyArray = new T[0];
        }

        public Queue()
        {
            _array = _emptyArray;
        }

        public Queue(IEnumerable<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException();
            }
            _array = new T[4];
            _size = 0;
            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Enqueue(enumerator.Current);
                }
            }
        }

        public Queue(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            _array = new T[capacity];
            _head = 0;
            _tail = 0;
            _size = 0;
        }

        public void Clear()
        {
            if (_head < _tail)
            {
                Array.Clear(_array, _head, _size);
            }
            else
            {
                Array.Clear(_array, _head, _array.Length - _head);
                Array.Clear(_array, 0, _tail);
            }
            _head = 0;
            _tail = 0;
            _size = 0;
        }

        public bool Contains(T item)
        {
            int index = _head;
            int num2 = _size;
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            while (num2-- > 0)
            {
                if (item == null)
                {
                    if (_array[index] == null)
                    {
                        return true;
                    }
                }
                else if ((_array[index] != null) && comparer.Equals(_array[index], item))
                {
                    return true;
                }
                index = (index + 1) % _array.Length;
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException();
            }
            if ((arrayIndex < 0) || (arrayIndex > array.Length))
            {
                throw new ArgumentOutOfRangeException();
            }
            int length = array.Length;
            if ((length - arrayIndex) < _size)
            {
                throw new ArgumentException();
            }
            int num2 = ((length - arrayIndex) < _size) ? (length - arrayIndex) : _size;
            if (num2 != 0)
            {
                int num3 = ((_array.Length - _head) < num2) ? (_array.Length - _head) : num2;
                Array.Copy(_array, _head, array, arrayIndex, num3);
                num2 -= num3;
                if (num2 > 0)
                {
                    Array.Copy(_array, 0, array, (arrayIndex + _array.Length) - _head, num2);
                }
            }
        }

        public T Dequeue()
        {
            if (_size == 0)
            {
                throw new InvalidOperationException();
            }
            T local = _array[_head];
            _array[_head] = default(T);
            _head = (_head + 1) % _array.Length;
            _size--;
            return local;
        }

        public void Enqueue(T item)
        {
            if (_size == _array.Length)
            {
                int capacity = _array.Length * 2;
                if (capacity < (_array.Length + 4))
                {
                    capacity = _array.Length + 4;
                }
                SetCapacity(capacity);
            }
            _array[_tail] = item;
            _tail = (_tail + 1) % _array.Length;
            _size++;
        }

        internal T GetElement(int i)
        {
            return _array[(_head + i) % _array.Length];
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public T Peek()
        {
            if (_size == 0)
            {
                throw new InvalidOperationException();
            }
            return _array[_head];
        }

        private void SetCapacity(int capacity)
        {
            T[] destinationArray = new T[capacity];
            if (_size > 0)
            {
                if (_head < _tail)
                {
                    Array.Copy(_array, _head, destinationArray, 0, _size);
                }
                else
                {
                    Array.Copy(_array, _head, destinationArray, 0, _array.Length - _head);
                    Array.Copy(_array, 0, destinationArray, _array.Length - _head, _tail);
                }
            }
            _array = destinationArray;
            _head = 0;
            _tail = (_size == capacity) ? 0 : _size;
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException();
            }
            if (array.Rank != 1)
            {
                throw new ArgumentException();
            }
            if (array.GetLowerBound(0) != 0)
            {
                throw new ArgumentException();
            }
            int length = array.Length;
            if ((index < 0) || (index > length))
            {
                throw new ArgumentOutOfRangeException();
            }
            if ((length - index) < _size)
            {
                throw new ArgumentException();
            }
            int num2 = ((length - index) < _size) ? (length - index) : _size;
            if (num2 != 0)
            {
                try
                {
                    int num3 = ((_array.Length - _head) < num2) ? (_array.Length - _head) : num2;
                    Array.Copy(_array, _head, array, index, num3);
                    num2 -= num3;
                    if (num2 > 0)
                    {
                        Array.Copy(_array, 0, array, (index + _array.Length) - _head, num2);
                    }
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new ArgumentException();
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public T[] ToArray()
        {
            T[] destinationArray = new T[_size];
            if (_size != 0)
            {
                if (_head < _tail)
                {
                    Array.Copy(_array, _head, destinationArray, 0, _size);
                    return destinationArray;
                }
                Array.Copy(_array, _head, destinationArray, 0, _array.Length - _head);
                Array.Copy(_array, 0, destinationArray, _array.Length - _head, _tail);
            }
            return destinationArray;
        }

        public void TrimExcess()
        {
            int num = (int)(_array.Length * 0.9);
            if (_size < num)
            {
                SetCapacity(_size);
            }
        }

        // Properties
        public int Count
        {
            get
            {
                return _size;
            }
        }

        // Nested Types
        public struct Enumerator : IEnumerator<T>
        {
            private Queue<T> _q;
            private int _index;
            private T _currentElement;
            internal Enumerator(Queue<T> q)
            {
                _q = q;
                _index = -1;
                _currentElement = default(T);
            }

            public void Dispose()
            {
                _index = -2;
                _currentElement = default(T);
            }

            public bool MoveNext()
            {
                if (_index == -2)
                {
                    return false;
                }
                _index++;
                if (_index == _q._size)
                {
                    _index = -2;
                    _currentElement = default(T);
                    return false;
                }
                _currentElement = _q.GetElement(_index);
                return true;
            }

            public T Current
            {
                get
                {
                    if (_index < 0)
                    {
                        if (_index == -1)
                        {
                            throw new InvalidOperationException();
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }
                    return _currentElement;
                }
            }
            object IEnumerator.Current
            {
                get
                {
                    if (_index < 0)
                    {
                        if (_index == -1)
                        {
                            throw new InvalidOperationException();
                        }
                        else
                        {
                            throw new InvalidOperationException();
                        }
                    }
                    return _currentElement;
                }
            }
            void IEnumerator.Reset()
            {
                _index = -1;
                _currentElement = default(T);
            }
        }


        public bool IsSynchronized
        {
            get { throw new NotImplementedException(); }
        }

        public object SyncRoot
        {
            get { throw new NotImplementedException(); }
        }
    }
}