using System;
using System.Collections.Generic;

namespace Microsoft.LiveLabs.Extras
{
    public class IntSet64 : IEnumerable<int>
    {
        private ulong set;

        public IntSet64()
        {
        }

        public IntSet64(ulong set)
        {
            this.set = set;
        }

        public bool this[int index]
        {
            get { return GetBit(index); }
            set { SetBit(index, value); }
        }

        private void SetBit(int index, bool value)
        {
            if (index < 0 || index >= 64)
                throw new IndexOutOfRangeException();
            if (value)
                set |= ((ulong)1 << index);
            else
                set &= ~((ulong)1 << index);
        }

        private bool GetBit(int index)
        {
            if (index < 0 || index >= 64)
                throw new IndexOutOfRangeException();
            return (set & (((ulong)1 << index))) != 0;
        }

        public IEnumerator<int> GetEnumerator()
        {
            var v = set;
            for (var i = 0; i < 64; i++)
            {
                if ((v & 0x1) != 0)
                    yield return i;
                v >>= 1;
            }
        }

        public int Count
        {
            get
            {
                var n = 0;
                var v = set;
                for (var i = 0; i < 64; i++)
                {
                    if (v == 0)
                        break;
                    if ((v & 0x1) != 0)
                        n++;
                    v >>= 1;
                }
                return n;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public byte ToByte()
        {
            return (byte)(set & 0xFF);
        }

        public ulong ToUInt64()
        {
            return set;
        }
    }
}
