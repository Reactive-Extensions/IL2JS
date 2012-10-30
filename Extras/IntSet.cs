//
// Replacement for System.Collections.BitArray with fast equality
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.LiveLabs.Extras {

    public class IntSet : IEquatable<IntSet>, IEnumerable<int>
    {
        private int n;
        private uint[] v;
        private int k;

        public IntSet(int n)
        {
            this.n = n;
            v = new uint[(n + 31) / 32];
            k = -1;
        }

        public IntSet Clone()
        {
            var res = new IntSet(n);
            v.CopyTo(res.v, 0);
            res.k = k;
            return res;
        }

        public bool this[int i]
        {
            get
            {
                if (i < 0 || i >= n)
                    throw new ArgumentOutOfRangeException("i");
                var j = i / 32;
                if (j >= v.Length)
                    return false;
                else
                    return (v[j] & (1u << (i % 32))) != 0u;
            }
            set
            {
                if (i < 0 || i >= n)
                    throw new ArgumentOutOfRangeException("i");
                k = -1;
                var j = i / 32;
                if (value)
                    v[j] |= (1u << (i % 32));
                else
                    v[j] &= (~(1u << (i % 32)));
            }
        }

        public void SetAll(bool b)
        {
            k = -1;
            if (b)
            {
                if (v.Length > 0)
                {
                    for (var i = 0; i < v.Length - 1; i++)
                        v[i] = 0xffffffffu;
                    var rem = n % 32;
                    if (rem == 0)
                        v[v.Length - 1] = 0xffffffffu;
                    else
                        v[v.Length - 1] = (1u << rem) - 1u;
                }
            }
            else
            {
                for (var i = 0; i < v.Length; i++)
                    v[i] = 0u;
            }
        }

        public IntSet Intersect(IntSet other)
        {
            var res = new IntSet(Math.Min(n, other.n));
            for (var i = 0; i < res.v.Length; i++)
                res.v[i] = v[i] & other.v[i];
            return res;
        }

        public void IntersectInPlace(IntSet other)
        {
            k = -1;
            n = Math.Min(n, other.n);
            if (v.Length > other.v.Length)
            {
                var newv = new uint[other.v.Length];
                other.v.CopyTo(newv, 0);
                for (var i = 0; i < newv.Length; i++)
                    newv[i] &= v[i];
                v = newv;
            }
            else
            {
                for (var i = 0; i < v.Length; i++)
                    v[i] &= other.v[i];
            }
        }

        public IntSet Union(IntSet other)
        {
            var res = new IntSet(Math.Max(n, other.n));
            for (var i = 0; i < Math.Min(v.Length, other.v.Length); i++)
                res.v[i] = v[i] | other.v[i];
            if (v.Length < other.v.Length)
            {
                for (var i = v.Length; i < other.v.Length; i++)
                    res.v[i] = other.v[i];
            }
            else
            {
                for (var i = other.v.Length; i < v.Length; i++)
                    res.v[i] = v[i];
            }
            return res;
        }

        public IntSet Union(IntSet other, BoolRef changed)
        {
            var res = new IntSet(Math.Max(n, other.n));
            for (var i = 0; i < Math.Min(v.Length, other.v.Length); i++)
            {
                var origv = v[i];
                res.v[i] = v[i] | other.v[i];
                if (res.v[i] != origv)
                    changed.Set();
            }
            if (v.Length < other.v.Length)
            {
                for (var i = v.Length; i < other.v.Length; i++)
                {
                    res.v[i] = other.v[i];
                    if (other.v[i] != 0u)
                        changed.Set();
                }
            }
            else
            {
                for (var i = other.v.Length; i < v.Length; i++)
                    res.v[i] = v[i];
            }
            return res;
        }

        public void UnionInPlace(IntSet other)
        {
            k = -1;
            n = Math.Max(n, other.n);
            if (v.Length < other.v.Length)
            {
                var newv = new uint[other.v.Length];
                other.v.CopyTo(newv, 0);
                for (var i = 0; i < v.Length; i++)
                    newv[i] |= v[i];
                v = newv;
            }
            else
            {
                for (var i = 0; i < other.v.Length; i++)
                    v[i] |= other.v[i];
            }
        }

        public void UnionInPlace(IntSet other, BoolRef changed)
        {
            k = -1;
            n = Math.Max(n, other.n);
            if (v.Length < other.v.Length)
            {
                var newv = new uint[other.v.Length];
                other.v.CopyTo(newv, 0);
                for (var i = 0; i < v.Length; i++)
                {
                    var origv = newv[i];
                    newv[i] |= v[i];
                    if (newv[i] != origv)
                        changed.Set();
                }
                for (var i = v.Length; i < other.v.Length; i++)
                {
                    if (newv[i] != 0u)
                        changed.Set();
                }
                v = newv;
            }
            else
            {
                for (var i = 0; i < other.v.Length; i++)
                {
                    var origv = v[i];
                    v[i] |= other.v[i];
                    if (v[i] != origv)
                        changed.Set();
                }
            }
        }

        public bool IsSubset(IntSet other)
        {
            var lim = Math.Min(v.Length, other.v.Length);
            for (var i = 0; i < lim; i++)
            {
                var x = v[i] & ~(other.v[i]);
                if (x != 0u)
                    return false;
            }
            if (v.Length > other.v.Length)
            {
                for (var i = other.v.Length; i < v.Length; i++)
                {
                    if (v[i] != 0u)
                        return false;
                }
            }
            return true;
        }

        public bool IsDisjoint(IntSet other)
        {
            var lim = Math.Min(v.Length, other.v.Length);
            for (var i = 0; i < lim; i++)
            {
                var x = v[i] & other.v[i];
                if (x != 0u)
                    return false;
            }
            return true;
        }

        public int Capacity
        {
            get { return n; }
        }

        public bool IsEmpty
        {
            get
            {
                for (var i = 0; i < v.Length; i++)
                {
                    if (v[i] != 0u)
                        return false;
                }
                return true;
            }
        }

        public bool IsFull
        {
            get
            {
                for (var i = 0; i < v.Length - 1; i++)
                {
                    if (v[i] != 0xffffffffu)
                        return false;
                }
                var rem = n % 32;
                if (rem == 0)
                    return v[v.Length - 1] == 0xffffffffu;
                else
                    return v[v.Length - 1] == (1u << rem) - 1u;
            }
        }

        private static int[] nbits = { 0, 1, 1, 2, 1, 2, 2, 3, 1, 2, 2, 3, 2, 3, 3, 4 };

        public int Count
        {
            get
            {
                if (k < 0)
                {
                    k = 0;
                    for (var i = 0; i < v.Length; i++)
                    {
                        var x = v[i];
                        for (var j = 0; j < 8; j++)
                        {
                            k += nbits[x % 16];
                            x >>= 4;
                        }
                    }
                }
                return k;
            }
        }

        public override bool Equals(object obj)
        {
            var vector = obj as IntSet;
            return vector != null && Equals(vector);
        }

        public bool Equals(IntSet other)
        {
            if (k >= 0 && other.k >= 0 && k != other.k)
                return false;
            var common = Math.Min(v.Length, other.v.Length);
            for (var i = 0; i < common; i++)
            {
                if (v[i] != other.v[i])
                    return false;
            }
            for (var i = other.v.Length; i < v.Length; i++)
            {
                if (v[i] != 0u)
                    return false;
            }
            for (var i = v.Length; i < other.v.Length; i++)
            {
                if (other.v[i] != 0u)
                    return false;
            }
            return true;
        }

        protected static int Rot7(int v)
        {
            return (int)(((uint)v << 7) | ((uint)v >> 25));
        }

        public override int GetHashCode()
        {
            var res = 0;
            var j = v.Length - 1;
            while (j >= 0 && v[j] == 0u)
                j--;
            for (var i = 0; i <= j; i++)
                res = Rot7(res) ^ (int)v[i];
            return res;
        }

        public IEnumerator<int> GetEnumerator()
        {
            if (k != 0)
            {
                for (var i = 0; i < v.Length; i++)
                {
                    var x = v[i];
                    var j = 0;
                    while (x != 0u)
                    {
                        if ((x % 2) != 0u)
                            yield return i * 32 + j;
                        j++;
                        x >>= 1;
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Append(StringBuilder sb)
        {
            sb.Append('{');
            var first = false;
            for (var i = 0; i < v.Length; i++)
            {
                var x = v[i];
                var j = 0;
                while (x != 0u)
                {
                    if ((x % 2) != 0u)
                    {
                        if (first)
                            first = false;
                        else
                            sb.Append(',');
                        sb.Append(i*32 + j);
                    }
                    j++;
                    x >>= 1;
                }
            }
            sb.Append('}');
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            Append(sb);
            return sb.ToString();
        }
    }

}