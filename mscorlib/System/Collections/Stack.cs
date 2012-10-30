namespace System.Collections
{
    internal class Stack : ICollection, ICloneable
    {
        private object[] _array;
        private const int _defaultCapacity = 10;
        private int _size;
        private object _syncRoot;
        private int _version;

        public Stack()
        {
            this._array = new object[10];
            this._size = 0;
            this._version = 0;
        }

        public Stack(int initialCapacity)
        {
            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException("initialCapacity");
            }
            if (initialCapacity < 10)
            {
                initialCapacity = 10;
            }
            this._array = new object[initialCapacity];
            this._size = 0;
            this._version = 0;
        }

        public virtual object Clone()
        {
            Stack stack = new Stack(this._size);
            stack._size = this._size;
            Array.Copy(this._array, 0, stack._array, 0, this._size);
            stack._version = this._version;
            return stack;
        }

        public virtual bool Contains(object obj)
        {
            int index = this._size;
            while (index-- > 0)
            {
                if (obj == null)
                {
                    if (this._array[index] == null)
                    {
                        return true;
                    }
                }
                else if ((this._array[index] != null) && this._array[index].Equals(obj))
                {
                    return true;
                }
            }
            return false;
        }

        public virtual void CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if ((array.Length - index) < this._size)
            {
                throw new ArgumentException();
            }
            int num = 0;
            if (array is object[])
            {
                object[] objArray = (object[])array;
                while (num < this._size)
                {
                    objArray[num + index] = this._array[(this._size - num) - 1];
                    num++;
                }
            }
            else
            {
                while (num < this._size)
                {
                    array.SetValue(this._array[(this._size - num) - 1], (int)(num + index));
                    num++;
                }
            }
        }

        public virtual IEnumerator GetEnumerator()
        {
            return new StackEnumerator(this);
        }

        public virtual object Pop()
        {
            if (this._size == 0)
            {
                throw new InvalidOperationException("empty stack");
            }
            this._version++;
            object obj2 = this._array[--this._size];
            this._array[this._size] = null;
            return obj2;
        }

        public virtual void Push(object obj)
        {
            if (this._size == this._array.Length)
            {
                object[] destinationArray = new object[2 * this._array.Length];
                Array.Copy(this._array, 0, destinationArray, 0, this._size);
                this._array = destinationArray;
            }
            this._array[this._size++] = obj;
            this._version++;
        }

        public virtual int Count
        {
            get
            {
                return this._size;
            }
        }

        public virtual bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public virtual object SyncRoot
        {
            get
            {
                if (this._syncRoot == null)
                {
                    this._syncRoot = new object();
                }
                return this._syncRoot;
            }
        }

        private class StackEnumerator : IEnumerator, ICloneable
        {
            private int _index;
            private Stack _stack;
            private int _version;
            private object currentElement;

            internal StackEnumerator(Stack stack)
            {
                this._stack = stack;
                this._version = this._stack._version;
                this._index = -2;
                this.currentElement = null;
            }

            public object Clone()
            {
                return base.MemberwiseClone();
            }

            public virtual bool MoveNext()
            {
                bool flag;
                if (this._version != this._stack._version)
                {
                    throw new InvalidOperationException("stack changed during enumeration");
                }
                if (this._index == -2)
                {
                    this._index = this._stack._size - 1;
                    flag = this._index >= 0;
                    if (flag)
                    {
                        this.currentElement = this._stack._array[this._index];
                    }
                    return flag;
                }
                if (this._index == -1)
                {
                    return false;
                }
                flag = --this._index >= 0;
                if (flag)
                {
                    this.currentElement = this._stack._array[this._index];
                    return flag;
                }
                this.currentElement = null;
                return flag;
            }

            public virtual void Reset()
            {
                if (this._version != this._stack._version)
                {
                    throw new InvalidOperationException("stack changed during enumeration");
                }
                this._index = -2;
                this.currentElement = null;
            }

            public virtual object Current
            {
                get
                {
                    if (this._index == -2)
                    {
                        throw new InvalidOperationException("enumeration not started");
                    }
                    if (this._index == -1)
                    {
                        throw new InvalidOperationException("enumeration ended");
                    }
                    return this.currentElement;
                }
            }
        }
    }
}
