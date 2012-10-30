using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.LiveLabs.Extras
{
    public interface IImMap<K, V> : IEnumerable<KeyValuePair<K, V>>
    {
        int Count { get; }
        bool IsReadOnly { get; }
        V this[K key] { get; }
        ISeq<K> Keys { get; }
        ISeq<V> Values { get; }
        void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex);
        bool ContainsKey(K key);
        bool TryGetValue(K key, out V value);
    }

    public interface IMap<K, V> : IImMap<K, V>
    {
        // Re-implement from above
        new bool IsReadOnly { get; }
        new V this[K key] { get; set; }

        void Add(K key, V value);
    }

    [DebuggerDisplay("Count = {Count}"), DebuggerTypeProxy(typeof(MapDebugView<,>))]
    public class Map<K, V> : IMap<K, V>
    {
        private class Entry
        {
            public K Key;
            public V Value;

            public Entry(K key, V value)
            {
                Key = key;
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

        public Map()
        {
            c = 0;
            entries = null;
        }

        public Map(int capacity)
        {
            c = 0;
            if (capacity == 0)
                entries = null;
            else if (capacity < threshold)
                entries = new Entry[capacity];
            else
                entries = new Entry[NextNonFull(capacity)];
        }

        public Map(IImMap<K, V> dict)
            : this(dict.Count)
        {
            foreach (var kv in dict)
                Add(kv.Key, kv.Value);
        }

        public Map(IDictionary<K, V> dict)
            : this(dict.Count)
        {
            foreach (var kv in dict)
                Add(kv.Key, kv.Value);
        }

        private static void Insert(Entry[] entries, K key, V value)
        {
            var n = (uint)entries.Length;
            var baseHash = (uint)key.GetHashCode();
            var interval = 1 + baseHash % (n - 1);
            var hash = baseHash % n;

            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[hash] == null)
                {
                    entries[hash] = new Entry(key, value);
                    return;
                }
                else if (entries[hash].Key.Equals(key))
                    throw new InvalidOperationException("duplicate key");
                hash = (uint)(((ulong)hash + interval) % n);
            }
            throw new InvalidOperationException("map full");
        }

        public int Count { get { return c; } }

        public bool IsReadOnly { get { return false; } }

        bool IImMap<K, V>.IsReadOnly { get { return true; } }

        private V Get(K key)
        {
            var res = default(V);
            if (!TryGetValue(key, out res))
                throw new InvalidOperationException("no such key");
            return res;
        }

        private void Set(K key, V value)
        {
            if (entries == null)
                throw new InvalidOperationException("no such key");
            else if (c < threshold)
            {
                for (var i = 0; i < c; i++)
                {
                    if (entries[i].Key.Equals(key))
                    {
                        entries[i].Value = value;
                        return;
                    }
                }
                throw new InvalidOperationException("no such key");
            }
            else
            {
                var n = (uint)entries.Length;
                var baseHash = (uint)key.GetHashCode();
                var interval = 1 + baseHash % (n - 1);
                var hash = baseHash % n;

                for (var i = 0; i < entries.Length; i++)
                {
                    if (entries[hash] == null)
                        throw new InvalidOperationException("no such key");
                    else if (entries[hash].Key.Equals(key))
                    {
                        entries[hash].Value = value;
                        return;
                    }
                    hash = (uint)(((ulong)hash + interval) % n);
                }
                throw new InvalidOperationException("no such key");
            }
        }

        public V this[K key]
        {
            get { return Get(key); }
            set { Set(key, value); }
        }

        V IImMap<K, V>.this[K key] { get { return Get(key); } }

        public ISeq<K> Keys
        {
            get
            {
                var res = new Seq<K>(c);
                if (entries != null)
                {
                    foreach (var e in entries)
                        res.Add(e.Key);
                }
                return res;
            }
        }

        public ISeq<V> Values
        {
            get
            {
                var res = new Seq<V>(c);
                if (entries != null)
                {
                    foreach (var e in entries)
                        res.Add(e.Value);
                }
                return res;
            }
        }

        public bool ContainsKey(K key)
        {
            var dummy = default(V);
            return TryGetValue(key, out dummy);
        }

        public bool TryGetValue(K key, out V value)
        {
            if (entries != null)
            {
                if (c < threshold)
                {
                    for (var i = 0; i < c; i++)
                    {
                        if (entries[i].Key.Equals(key))
                        {
                            value = entries[i].Value;
                            return true;
                        }
                    }
                }
                else
                {
                    var n = (uint)entries.Length;
                    var baseHash = (uint)key.GetHashCode();
                    var interval = 1 + baseHash % (n - 1);
                    var hash = baseHash % n;

                    for (var i = 0; i < entries.Length; i++)
                    {
                        if (entries[hash] == null)
                            break;
                        else if (entries[hash].Key.Equals(key))
                        {
                            value = entries[hash].Value;
                            return true;
                        }
                        hash = (uint)(((ulong)hash + interval) % n);
                    }
                }
            }
            value = default(V);
            return false;
        }

        public void Add(K key, V value)
        {
            if (entries == null)
            {
                entries = new Entry[1];
                entries[0] = new Entry(key, value);
            }
            else if (c + 1 < threshold)
            {
                for (var i = 0; i < c; i++)
                {
                    if (entries[i].Key.Equals(key))
                        throw new InvalidOperationException("duplicate key");
                }
                if (c < entries.Length)
                    entries[c] = new Entry(key, value);
                else
                {
                    var newEntries = new Entry[entries.Length * 2];
                    for (var i = 0; i < c; i++)
                        newEntries[i] = entries[i];
                    newEntries[c] = new Entry(key, value);
                    entries = newEntries;
                }
            }
            else if (c < threshold)
            {
                var newEntries = new Entry[NextNonFull(c + 1)];
                for (var i = 0; i < c; i++)
                    Insert(newEntries, entries[i].Key, entries[i].Value);
                Insert(newEntries, key, value);
                entries = newEntries;
            }
            else if (Full(entries.Length, c + 1))
            {
                var newEntries = new Entry[NextNonFull(c + 1)];
                foreach (var e in entries)
                {
                    if (e != null)
                        Insert(newEntries, e.Key, e.Value);
                }
                Insert(newEntries, key, value);
                entries = newEntries;
            }
            else
                Insert(entries, key, value);
            c++;
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            if (entries != null)
            {
                foreach (var e in entries)
                {
                    if (e != null)
                        yield return new KeyValuePair<K, V>(e.Key, e.Value);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            if (entries != null)
            {
                var i = arrayIndex;
                foreach (var e in entries)
                {
                    if (e != null)
                        array[i++] = new KeyValuePair<K, V>(e.Key, e.Value);
                }
            }
        }

        public bool DisjointKeys(IImMap<K, V> other)
        {
            if (entries == null || other.Count == 0)
                return true;
            foreach (var e in entries)
            {
                if (e != null)
                {
                    if (other.ContainsKey(e.Key))
                        return false;
                }
            }
            return true;
        }
    }

    internal sealed class MapDebugView<K, V>
    {
        private IImMap<K, V> map;

        public MapDebugView(IImMap<K, V> map)
        {
            this.map = map;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<K, V>[] Items
        {
            get
            {
                var array = new KeyValuePair<K, V>[map.Count];
                map.CopyTo(array, 0);
                return array;
            }
        }
    }

    public static class MapExtensions
    {
        public static Map<K, V> ToMap<K, V>(this IEnumerable<KeyValuePair<K, V>> kvs)
        {
            var res = new Map<K, V>();
            foreach (var kv in kvs)
                res.Add(kv.Key, kv.Value);
            return res;
        }

        public static Map<U, W> ToMap<K, V, U, W>(this IEnumerable<KeyValuePair<K, V>> kvs, Func<KeyValuePair<K, V>, U> f, Func<KeyValuePair<K, V>, W> g)
        {
            var res = new Map<U, W>();
            foreach (var kv in kvs)
                res.Add(f(kv), g(kv));
            return res;
        }
    }
}