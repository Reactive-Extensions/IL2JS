using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.LiveLabs.Extras
{
    public interface IImSet<T> : IEnumerable<T>
    {
        int Count { get; }
        bool IsReadOnly { get; }
        bool Contains(T value);
        void CopyTo(T[] array, int arrayIndex);
        T this[int i] { get; }
    }

    public interface IMSet<T> : IImSet<T>
    {
        // Re-implement from above
        new bool IsReadOnly { get; }

        bool Add(T value);
    }

    [DebuggerDisplay("Count = {Count}"), DebuggerTypeProxy(typeof(SetDebugView<>))]
    public class Set<T> : IMSet<T>
    {
        private class Entry
        {
            public T Value;

            public Entry(T value)
            {
                Value = value;
            }
        }

        // up to 31 bits
        private static readonly int[] primes = {
                                                   1, 3, 7, 13, 31, 61, 127, 251, 509, 1021, 2039, 4093, 8191, 16381,
                                                   32749, 65521, 131071, 262139, 524287, 1048573, 2097143, 4194301,
                                                   8388593, 16777213, 33554393, 67108859, 134217689, 268435399,
                                                   536870909, 1073741789, 2147483647
                                               };

        private const int threshold = 3;

        private int c;
        private Entry[] entries;

        private static bool Full(int n, int c)
        {
            return c > 0 && n / c < 2;
        }

        private static int NextNonFull(int c)
        {
            for (var i = 0; i < primes.Length; i++)
            {
                var p = primes[i];
                if (!Full(p, c))
                    return p;
            }
            throw new InvalidOperationException("map is too large");
        }

        public Set()
        {
            c = 0;
            entries = null;
        }

        public Set(int capacity)
        {
            c = 0;
            if (capacity == 0)
                entries = null;
            else if (capacity < threshold)
                entries = new Entry[capacity];
            else
                entries = new Entry[NextNonFull(capacity)];
        }

        private static bool Insert(Entry[] entries, T value)
        {
            var n = (uint)entries.Length;
            var baseHash = (uint)value.GetHashCode();
            var interval = 1 + baseHash % (n - 1);
            var hash = baseHash % n;

            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[hash] == null)
                {
                    entries[hash] = new Entry(value);
                    return true;
                }
                else if (entries[hash].Value.Equals(value))
                    return false;
                hash = (uint)(((ulong)hash + interval) % n);
            }
            throw new InvalidOperationException("set full");
        }

        public int Count { get { return c; } }

        public bool IsReadOnly { get { return false; } }

        bool IImSet<T>.IsReadOnly { get { return true; } }

        public bool Contains(T value)
        {
            if (entries != null)
            {
                if (c < threshold)
                {
                    for (var i = 0; i < c; i++)
                    {
                        if (entries[i].Value.Equals(value))
                            return true;
                    }
                }
                else
                {
                    var n = (uint)entries.Length;
                    var baseHash = (uint)value.GetHashCode();
                    var interval = 1 + baseHash % (n - 1);
                    var hash = baseHash % n;

                    for (var i = 0; i < entries.Length; i++)
                    {
                        if (entries[hash] == null)
                            break;
                        else if (entries[hash].Value.Equals(value))
                            return true;
                        hash = (uint)(((ulong)hash + interval) % n);
                    }
                }
            }
            return false;
        }

        public bool Add(T value)
        {
            if (entries == null)
            {
                entries = new Entry[1];
                entries[0] = new Entry(value);
            }
            else if (c + 1 < threshold)
            {
                for (var i = 0; i < c; i++)
                {
                    if (entries[i].Value.Equals(value))
                        return false;
                }
                if (c < entries.Length)
                    entries[c] = new Entry(value);
                else
                {
                    var newEntries = new Entry[entries.Length * 2];
                    for (var i = 0; i < c; i++)
                        newEntries[i] = entries[i];
                    newEntries[c] = new Entry(value);
                    entries = newEntries;
                }
            }
            else if (c < threshold)
            {
                var newEntries = new Entry[NextNonFull(c + 1)];
                for (var i = 0; i < c; i++)
                    Insert(newEntries, entries[i].Value);
                if (!Insert(newEntries, value))
                    return false;
                entries = newEntries;
            }
            else if (Full(entries.Length, c + 1))
            {
                var newEntries = new Entry[NextNonFull(c + 1)];
                foreach (var e in entries)
                {
                    if (e != null)
                        Insert(newEntries, e.Value);
                }
                if (!Insert(newEntries, value))
                    return false;
                entries = newEntries;
            }
            else
            {
                if (!Insert(entries, value))
                    return false;
            }
            c++;
            return true;
        }

        public T this[int i]
        {
            get
            {
                if (i < 0 || i >= c)
                    throw new IndexOutOfRangeException();
                if (c < threshold)
                    return entries[i].Value;
                else
                {
                    var j = 0;
                    foreach (var entry in entries)
                    {
                        if (entry != null)
                        {
                            if (j == i)
                                return entry.Value;
                            j++;
                        }
                    }
                    throw new InvalidOperationException("count is out of sync with entries");
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (entries != null)
            {
                foreach (var e in entries)
                {
                    if (e != null)
                        yield return e.Value;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (entries != null)
            {
                var i = arrayIndex;
                foreach (var e in entries)
                {
                    if (e != null)
                        array[i++] = e.Value;
                }
            }
        }
    }

    internal sealed class SetDebugView<T>
    {
        private IImSet<T> set;

        public SetDebugView(IImSet<T> set)
        {
            this.set = set;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                var array = new T[set.Count];
                set.CopyTo(array, 0);
                return array;
            }
        }
    }

    public static class SetExtensions
    {
        public static Set<T> ToSet<T>(this IEnumerable<T> ts)
        {
            var res = new Set<T>();
            foreach (var t in ts)
                res.Add(t);
            return res;
        }

        public static Set<U> ToSet<T, U>(this IEnumerable<T> ts, Func<T, U> f)
        {
            var res = new Set<U>();
            foreach (var t in ts)
                res.Add(f(t));
            return res;
        }
    }
}
