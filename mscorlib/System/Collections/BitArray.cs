namespace System.Collections
{
    public sealed class BitArray : ICollection, ICloneable
    {
        private const int _ShrinkThreshold = 256;
        private object _syncRoot;
        private int _version;
        private const int BitsPerByte = 8;
        private const int BitsPerInt32 = 32;
        private const int BytesPerInt32 = 4;
        private int[] m_array;
        private int m_length;

        public BitArray(int length)
            : this(length, false)
        {
        }

        public BitArray(bool[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            this.m_array = new int[GetArrayLength(values.Length, 32)];
            this.m_length = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i])
                {
                    this.m_array[i / 32] |= ((int)1) << (i % 32);
                }
            }
            this._version = 0;
        }

        public BitArray(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }
            if (bytes.Length > 268435455)
            {
                throw new ArgumentException("bytes");
            }
            this.m_array = new int[GetArrayLength(bytes.Length, 4)];
            this.m_length = bytes.Length*8;
            int index = 0;
            int num2 = 0;
            while ((bytes.Length - num2) >= 4)
            {
                m_array[index++] = (((bytes[num2] & 255) | ((bytes[num2 + 1] & 255) << 8)) |
                                    ((bytes[num2 + 2] & 255) << 16)) | ((bytes[num2 + 3] & 255) << 24);
                num2 += 4;
            }
            switch ((bytes.Length - num2))
            {
            case 1:
                m_array[index] |= bytes[num2] & 255;
                break;
            case 2:
                m_array[index] |= (bytes[num2 + 1] & 255) << 8;
                m_array[index] |= bytes[num2] & 255;
                break;
            case 3:
                m_array[index] = (bytes[num2 + 2] & 255) << 16;
                m_array[index] |= (bytes[num2 + 1] & 255) << 8;
                m_array[index] |= bytes[num2] & 255;
                break;

            default:
                break;
            }
            this._version = 0;
        }

        public BitArray(int[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }
            if (values.Length > 67108863)
            {
                throw new ArgumentException("values");
            }
            this.m_array = new int[values.Length];
            this.m_length = values.Length * 32;
            Array.Copy(values, this.m_array, values.Length);
            this._version = 0;
        }

        public BitArray(BitArray bits)
        {
            if (bits == null)
            {
                throw new ArgumentNullException("bits");
            }
            int arrayLength = GetArrayLength(bits.m_length, 32);
            this.m_array = new int[arrayLength];
            this.m_length = bits.m_length;
            Array.Copy(bits.m_array, this.m_array, arrayLength);
            this._version = bits._version;
        }

        public BitArray(int length, bool defaultValue)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length");
            }
            this.m_array = new int[GetArrayLength(length, 32)];
            this.m_length = length;
            int num = defaultValue ? -1 : 0;
            for (int i = 0; i < this.m_array.Length; i++)
            {
                this.m_array[i] = num;
            }
            this._version = 0;
        }

        public BitArray And(BitArray value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (this.m_length != value.m_length)
            {
                throw new ArgumentException("value");
            }
            int arrayLength = GetArrayLength(this.m_length, 32);
            for (int i = 0; i < arrayLength; i++)
            {
                this.m_array[i] &= value.m_array[i];
            }
            this._version++;
            return this;
        }

        public object Clone()
        {
            BitArray array = new BitArray(this.m_array);
            array._version = this._version;
            array.m_length = this.m_length;
            return array;
        }

        public void CopyTo(Array array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (array is int[])
            {
                Array.Copy(this.m_array, 0, array, index, GetArrayLength(this.m_length, 32));
            }
            else if (array is byte[])
            {
                int arrayLength = GetArrayLength(this.m_length, 8);
                if ((array.Length - index) < arrayLength)
                {
                    throw new ArgumentException();
                }
                byte[] buffer = (byte[])array;
                for (int i = 0; i < arrayLength; i++)
                {
                    buffer[index + i] = (byte)((this.m_array[i / 4] >> ((i % 4) * 8)) & 255);
                }
            }
            else
            {
                if (!(array is bool[]))
                {
                    throw new ArgumentException("array");
                }
                if ((array.Length - index) < this.m_length)
                {
                    throw new ArgumentException();
                }
                bool[] flagArray = (bool[])array;
                for (int j = 0; j < this.m_length; j++)
                {
                    flagArray[index + j] = ((this.m_array[j / 32] >> (j % 32)) & 1) != 0;
                }
            }
        }

        public bool Get(int index)
        {
            if ((index < 0) || (index >= this.m_length))
            {
                throw new ArgumentOutOfRangeException("index");
            }
            return ((this.m_array[index / 32] & (((int)1) << (index % 32))) != 0);
        }

        private static int GetArrayLength(int n, int div)
        {
            if (n <= 0)
            {
                return 0;
            }
            return (((n - 1) / div) + 1);
        }

        public IEnumerator GetEnumerator()
        {
            return new BitArrayEnumeratorSimple(this);
        }

        public BitArray Not()
        {
            int arrayLength = GetArrayLength(this.m_length, 32);
            for (int i = 0; i < arrayLength; i++)
            {
                this.m_array[i] = ~this.m_array[i];
            }
            this._version++;
            return this;
        }

        public BitArray Or(BitArray value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (this.m_length != value.m_length)
            {
                throw new ArgumentException();
            }
            int arrayLength = GetArrayLength(this.m_length, 32);
            for (int i = 0; i < arrayLength; i++)
            {
                this.m_array[i] |= value.m_array[i];
            }
            this._version++;
            return this;
        }

        public void Set(int index, bool value)
        {
            if ((index < 0) || (index >= this.m_length))
            {
                throw new ArgumentOutOfRangeException("index");
            }
            if (value)
            {
                this.m_array[index / 32] |= ((int)1) << (index % 32);
            }
            else
            {
                this.m_array[index / 32] &= ~(((int)1) << (index % 32));
            }
            this._version++;
        }

        public void SetAll(bool value)
        {
            int num = value ? -1 : 0;
            int arrayLength = GetArrayLength(this.m_length, 32);
            for (int i = 0; i < arrayLength; i++)
            {
                this.m_array[i] = num;
            }
            this._version++;
        }

        public BitArray Xor(BitArray value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (this.m_length != value.m_length)
            {
                throw new ArgumentException();
            }
            int arrayLength = GetArrayLength(this.m_length, 32);
            for (int i = 0; i < arrayLength; i++)
            {
                this.m_array[i] ^= value.m_array[i];
            }
            this._version++;
            return this;
        }

        public int Count
        {
            get
            {
                return this.m_length;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public bool this[int index]
        {
            get
            {
                return this.Get(index);
            }
            set
            {
                this.Set(index, value);
            }
        }

        public int Length
        {
            get
            {
                return this.m_length;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                int arrayLength = GetArrayLength(value, 32);
                if ((arrayLength > this.m_array.Length) || ((arrayLength + 256) < this.m_array.Length))
                {
                    int[] destinationArray = new int[arrayLength];
                    Array.Copy(this.m_array, destinationArray, (arrayLength > this.m_array.Length) ? this.m_array.Length : arrayLength);
                    this.m_array = destinationArray;
                }
                if (value > this.m_length)
                {
                    int index = GetArrayLength(this.m_length, 32) - 1;
                    int num3 = this.m_length % 32;
                    if (num3 > 0)
                    {
                        this.m_array[index] &= (((int)1) << num3) - 1;
                    }
                    Array.Clear(this.m_array, index + 1, (arrayLength - index) - 1);
                }
                this.m_length = value;
                this._version++;
            }
        }

        public object SyncRoot
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

        private class BitArrayEnumeratorSimple : IEnumerator, ICloneable
        {
            private BitArray bitarray;
            private bool currentElement;
            private int index;
            private int version;

            internal BitArrayEnumeratorSimple(BitArray bitarray)
            {
                this.bitarray = bitarray;
                this.index = -1;
                this.version = bitarray._version;
            }

            public object Clone()
            {
                return base.MemberwiseClone();
            }

            public virtual bool MoveNext()
            {
                if (this.version != this.bitarray._version)
                {
                    throw new InvalidOperationException("bit array changed during enumeration");
                }
                if (this.index < (this.bitarray.Count - 1))
                {
                    this.index++;
                    this.currentElement = this.bitarray.Get(this.index);
                    return true;
                }
                this.index = this.bitarray.Count;
                return false;
            }

            public void Reset()
            {
                if (this.version != this.bitarray._version)
                {
                    throw new InvalidOperationException("bit array changed during enumeration");
                }
                this.index = -1;
            }

            public virtual object Current
            {
                get
                {
                    if (this.index == -1)
                    {
                        throw new InvalidOperationException("enumeration not started");
                    }
                    if (this.index >= this.bitarray.Count)
                    {
                        throw new InvalidOperationException("enumeration ended");
                    }
                    return this.currentElement;
                }
            }
        }
    }
}
