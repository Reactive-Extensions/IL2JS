using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.LiveLabs.Extras
{
    [DebuggerDisplay("Count = {Count}"), DebuggerTypeProxy(typeof(MapDebugView<,>))]
    public class OrdMap<K, V> : IMap<K, V>
    {
        [NotNull]
        private readonly Seq<K> keys;
        [NotNull]
        private readonly Map<K, V> map;

        public OrdMap()
        {
            keys = new Seq<K>();
            map = new Map<K, V>();
        }

        public void Add(K key, V value)
        {
            if (map.ContainsKey(key))
            {
                keys.Remove(key);
                map[key] = value;
            }
            else
                map.Add(key, value);
            keys.Add(key);
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return keys.Select(key => new KeyValuePair<K, V>(key, map[key])).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool IsReadOnly { get { return false; } }

        bool IImMap<K, V>.IsReadOnly { get { return true; } }

        public K GetKey(int index)
        {
            return keys[index];
        }

        public V this[K key]
        {
            get { return map[key]; }
            set { map[key] = value; }
        }

        V IImMap<K, V>.this[K key] { get { return map[key]; } }

        public int Count { get { return map.Count; } }

        public ISeq<K> Keys { get { return keys; } }

        public ISeq<V> Values { get { return map.Values; } }

        public bool ContainsKey(K key) { return map.ContainsKey(key); }

        public bool TryGetValue(K key, out V value) { return map.TryGetValue(key, out value); }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            var i = arrayIndex;
            foreach (var key in keys)
                array[i++] = new KeyValuePair<K, V>(key, map[key]);
        }
    }

    public static class OrMapExtensions
    {
        public static OrdMap<K, V> ToOrdMap<K, V>(this IEnumerable<KeyValuePair<K, V>> kvs)
        {
            var res = new OrdMap<K, V>();
            foreach (var kv in kvs)
                res.Add(kv.Key, kv.Value);
            return res;
        }

        public static OrdMap<U, W> ToOrdMap<K, V, U, W>(this IEnumerable<KeyValuePair<K, V>> kvs, Func<KeyValuePair<K, V>, U> f, Func<KeyValuePair<K, V>, W> g)
        {
            var res = new OrdMap<U, W>();
            foreach (var kv in kvs)
                res.Add(f(kv), g(kv));
            return res;
        }
    }

}