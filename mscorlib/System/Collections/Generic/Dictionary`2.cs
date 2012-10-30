using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System.Collections.Generic
{
    [Runtime(true)]
    [NoInterop(true)]
    public class Dictionary<K, V> : IDictionary<K, V>
    {
        private int c;
        private Entry[] entries;
        // null => standard comparer
        private IEqualityComparer<K> comparer;

        // ----------------------------------------------------------------------
        // Direct vs Hash implementation redirector
        // ----------------------------------------------------------------------

        [Import(@"function(root, inst, noComparer) {
                      var keyType = inst.T.L[0];
                      if (noComparer && (keyType == root.StringType || keyType == root.Int32Type)) {
                          inst.Dict = {};
                          inst.Get = function(key)  {
                              var v = inst.Dict[key];
                              if (v === undefined)
                                  throw root.InvalidOperationExceptionWithMessage(""no such key"");
                              return v;
                          };
                          inst.AddOrSet = function(key, value, addOnly) {
                              var res = inst.Dict[key] === undefined;
                              if (addOnly && !res)
                                  throw root.InvalidOperationExceptionWithMessage(""duplicate key"");
                              inst.Dict[key] = value;
                              return res;
                          };
                          inst.TryGetValue = function(key, ptr) {
                              var v = inst.Dict[key];
                              if (v === undefined)
                                  return false;
                              ptr.W(v);
                              return true;
                          };
                          inst.ContainsKey = function(key) {
                              return inst.Dict[key] !== undefined;
                          };
                      }
                      else {
                          inst.Get = inst.GetHash;
                          inst.AddOrSet = inst.AddOrSetHash;
                          inst.TryGetValue = inst.TryGetValueHash;
                          inst.ContainsKey = inst.ContainsKeyHash;
                      }
                  }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern private void SetupDirect(bool noComparer);

        [Import("function(inst) { return inst.Dict != null; }", PassInstanceAsArgument = true)]
        extern private bool IsDirect();

        [Import("Get")]
        extern private V Get(K key);

        [Import("AddOrSet")]
        extern private bool AddOrSet(K key, V value, bool addOnly);

        [Import("ContainsKey")]
        extern public bool ContainsKey(K key);

        [Import("TryGetValue")]
        extern public bool TryGetValue(K key, out V value);

        // ----------------------------------------------------------------------
        // Direct implementation
        // ----------------------------------------------------------------------

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
        private const int mask = 0x7fffffff;

        private delegate KeyValuePair<K, V> WithKeyValue(K key, V value);

        [Import(@"function(inst, array, index, f) {
                      var valType = inst.T.L[1];
                      var i = index;
                      for (var p in inst.Dict)
                          array[i++] = f(p, valType.C(inst.Dict[p]));
                  }", PassInstanceAsArgument = true)]
        extern private void PrimCopyToDirect(KeyValuePair<K, V>[] array, int index, WithKeyValue f);

        [Import(@"function(inst, array, index) {
                      var i = index;
                      for (var p in inst.Dict)
                          array[i++] = p;
                  }", PassInstanceAsArgument = true)]
        extern private void PrimCopyToKeysDirect(K[] array, int index);

        [Import(@"function(inst, array, index) {
                      var valType = inst.T.L[1];
                      var i = index;
                      for (var p in inst.Dict)
                          array[i++] = valType.C(inst.Dict[p]);
                  }", PassInstanceAsArgument = true)]
        extern private void PrimCopyToValuesDirect(V[] array, int index);

        [Import(@"function(inst, key) {
                      if (inst.Dict[key] === undefined)
                          return false;
                      delete inst.Dict[key];
                      return true;
                  }", PassInstanceAsArgument = true)]
        extern private bool PrimRemoveDirect(K key);

        [Import("function(inst) { inst.Dict = {}; }", PassInstanceAsArgument = true)]
        extern private void ClearDirect();

        // ----------------------------------------------------------------------
        // Hash implementation
        // ----------------------------------------------------------------------

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

        private static bool InsertHash(Entry[] entries, K key, V value, bool insertOnly, IEqualityComparer<K> comparer)
        {
            var n = entries.Length;
            var baseHash = (comparer == null ? key.GetHashCode() : comparer.GetHashCode(key)) & mask;
            var interval = 1 + (baseHash%(n - 1));
            var hash = baseHash%n;

            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[hash] == null)
                {
                    entries[hash] = new Entry(key, value);
                    return true;
                }
                else if (comparer == null ? entries[hash].Key.Equals(key) : comparer.Equals(entries[hash].Key, key))
                {
                    if (insertOnly)
                        throw new InvalidOperationException("duplicate key");
                    entries[hash].Value = value;
                    return false;
                }
                hash = (hash + interval)%n;
            }
            throw new InvalidOperationException("map full");
        }

        [Export("AddOrSetHash")]
        private bool AddOrSetHash(K key, V value, bool addOnly)
        {
            if (entries == null)
            {
                entries = new Entry[1];
                entries[0] = new Entry(key, value);
                return true;
            }
            else if (c + 1 < threshold)
            {
                for (var i = 0; i < c; i++)
                {
                    if (comparer == null ? entries[i].Key.Equals(key) : comparer.Equals(entries[i].Key, key))
                    {
                        if (addOnly)
                            throw new InvalidOperationException("duplicate key");
                        else
                        {
                            entries[i].Value = value;
                            return false;
                        }
                    }
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
                return true;
            }
            else if (c < threshold)
            {
                var newEntries = new Entry[NextNonFull(c + 1)];
                for (var i = 0; i < c; i++)
                {
                    if (comparer == null ? entries[i].Key.Equals(key) : comparer.Equals(entries[i].Key, key))
                    {
                        if (addOnly)
                            throw new InvalidOperationException("duplicate key");
                        entries[i].Value = value;
                        return false;
                    }
                    InsertHash(newEntries, entries[i].Key, entries[i].Value, true, comparer);
                }
                InsertHash(newEntries, key, value, true, comparer);
                entries = newEntries;
                return true;
            }
            else if (Full(entries.Length, c + 1))
            {
                var newEntries = new Entry[NextNonFull(c + 1)];
                foreach (var e in entries)
                {
                    if (e != null)
                    {
                        if (comparer == null ? e.Key.Equals(key) : comparer.Equals(e.Key, key))
                        {
                            if (addOnly)
                                throw new InvalidOperationException("duplicate key");
                            e.Value = value;
                            return false;
                        }
                        InsertHash(newEntries, e.Key, e.Value, true, comparer);
                    }
                }
                InsertHash(newEntries, key, value, true, comparer);
                entries = newEntries;
                return true;
            }
            else
                return InsertHash(entries, key, value, addOnly, comparer);
        }

        [Export("GetHash")]
        private V GetHash(K key)
        {
            var res = default(V);
            if (!TryGetValueHash(key, out res))
                throw new InvalidOperationException("no such key");
            return res;
        }

        [Export("ContainsKeyHash")]
        private bool ContainsKeyHash(K key)
        {
            var dummy = default(V);
            return TryGetValueHash(key, out dummy);
        }

        [Export("TryGetValueHash")]
        private bool TryGetValueHash(K key, out V value)
        {
            if (entries != null)
            {
                if (c < threshold)
                {
                    for (var i = 0; i < c; i++)
                    {
                        if (comparer == null ? entries[i].Key.Equals(key) : comparer.Equals(entries[i].Key, key))
                        {
                            value = entries[i].Value;
                            return true;
                        }
                    }
                }
                else
                {
                    var n = entries.Length;
                    var baseHash = (comparer == null ? key.GetHashCode() : comparer.GetHashCode(key)) & mask;
                    var interval = 1 + baseHash % (n - 1);
                    var hash = baseHash % n;

                    for (var i = 0; i < entries.Length; i++)
                    {
                        if (entries[hash] == null)
                            break;
                        else if (comparer == null ? entries[hash].Key.Equals(key) : comparer.Equals(entries[hash].Key, key))
                        {
                            value = entries[hash].Value;
                            return true;
                        }
                        hash = (hash + interval) % n;
                    }
                }
            }
            value = default(V);
            return false;
        }

        private void PrimCopyToHash(KeyValuePair<K, V>[] array, int index)
        {
            if (entries != null)
            {
                var i = index;
                foreach (var e in entries)
                {
                    if (e != null)
                        array[i++] = new KeyValuePair<K, V>(e.Key, e.Value);
                }
            }
        }

        private void PrimCopyToKeysHash(K[] array, int index)
        {
            if (entries != null)
            {
                var i = index;
                foreach (var e in entries)
                {
                    if (e != null)
                        array[i++] = e.Key;
                }
            }
        }

        private void PrimCopyToValuesHash(V[] array, int index)
        {
            if (entries != null)
            {
                var i = index;
                foreach (var e in entries)
                {
                    if (e != null)
                        array[i++] = e.Value;
                }
            }
        }

        private bool PrimRemoveHash(K key)
        {
            if (ContainsKey(key))
            {
                var res = new Dictionary<K, V>(c - 1);
                foreach (var e in entries)
                {
                    if (e != null && !(comparer == null ? e.Key.Equals(key) : comparer.Equals(e.Key, key)))
                        res.Add(e.Key, e.Value);
                }
                entries = res.entries;
                return true;
            }
            else
                return false;
        }

        // ----------------------------------------------------------------------
        // Public interface
        // ----------------------------------------------------------------------

        public Dictionary() : this(0, null)
        {
        }

        public Dictionary(IDictionary<K, V> dict) : this(dict, null)
        {
        }

        public Dictionary(IEqualityComparer<K> comparer) : this(0, comparer)
        {
        }

        public Dictionary(int capacity) : this(capacity, null)
        {
        }

        public Dictionary(IDictionary<K, V> dict, IEqualityComparer<K> comparer)
            : this(dict.Count, comparer)
        {
            foreach (var kv in dict)
                Add(kv.Key, kv.Value);
        }

        public Dictionary(int capacity, IEqualityComparer<K> comparer)
        {
            c = 0;
            this.comparer = comparer;
            SetupDirect(comparer == null);
            if (!IsDirect() && capacity > 0)
            {
                if (capacity < threshold)
                    entries = new Entry[capacity];
                else
                    entries = new Entry[NextNonFull(capacity)];
            }
        }

        public int Count { get { return c; } }

        public bool IsReadOnly { get { return false; } }

        public V this[K key]
        {
            get
            {
                return Get(key);
            }
            set
            {
                if (AddOrSet(key, value, false))
                    c++;
            }
        }

        public KeyCollection Keys
        {
            get
            {
                return new KeyCollection(this);
            }
        }

        public ValueCollection Values
        {
            get
            {
                return new ValueCollection(this);
            }
        }

        ICollection<K> IDictionary<K, V>.Keys
        {
            get
            {
                return Keys;
            }
        }

        ICollection<V> IDictionary<K, V>.Values
        {
            get
            {
                return Values;
            }
        }

        public void Add(K key, V value)
        {
            AddOrSet(key, value, true);
            c++;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator()
        {
            return GetEnumerator();
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        public void CopyTo(KeyValuePair<K, V>[] array, int index)
        {
            if (IsDirect())
                PrimCopyToDirect(array, index, (k, v) => new KeyValuePair<K, V>(k, v));
            else
                PrimCopyToHash(array, index);
        }

        public bool Remove(K key)
        {
            if (IsDirect() ? PrimRemoveDirect(key) : PrimRemoveHash(key))
            {
                c--;
                return true;
            }
            return false;
        }

        public void Add(KeyValuePair<K, V> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            c = 0;
            if (IsDirect())
                ClearDirect();
            else
                entries = null;
        }

        public bool Contains(KeyValuePair<K, V> item)
        {
            throw new NotSupportedException();
        }

        public bool Remove(KeyValuePair<K, V> item)
        {
            throw new NotSupportedException();
        }

        // ----------------------------------------------------------------------
        // Enumerators
        // ----------------------------------------------------------------------

        // NOTE: These could be implemented much simpler but we must maintain the existing signatures.

        public struct Enumerator : IEnumerator<KeyValuePair<K, V>>
        {
            private Dictionary<K, V> dictionary;
            private int index;
            private K[] keys;

            internal Enumerator(Dictionary<K, V> dictionary)
            {
                this.dictionary = dictionary;
                index = -1;
                if (dictionary.IsDirect())
                {
                    keys = new K[dictionary.Count];
                    dictionary.PrimCopyToKeysDirect(keys, 0);
                }
                else
                    keys = null;
            }

            public bool MoveNext()
            {
                if (keys == null)
                {
                    if (dictionary.entries == null)
                        return false;
                    index++;
                    while (index < dictionary.entries.Length)
                    {
                        if (dictionary.entries[index] != null)
                            return true;
                        index++;
                    }
                    return false;
                }
                else
                {
                    index++;
                    return index < keys.Length;
                }
            }

            public KeyValuePair<K, V> Current
            {
                get
                {
                    if (keys == null)
                    {
                        if (dictionary.entries != null && index >= 0 && index < dictionary.entries.Length)
                            return new KeyValuePair<K, V>
                                (dictionary.entries[index].Key, dictionary.entries[index].Value);
                    }
                    else
                    {
                        if (index >= 0 && index < keys.Length)
                            return new KeyValuePair<K, V>(keys[index], dictionary[keys[index]]);
                    }
                    throw new InvalidOperationException("invalid enemeration");
                }
            }

            public void Dispose()
            {
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                index = -1;
            }
        }

        public sealed class KeyCollection : ICollection<K>
        {
            private Dictionary<K, V> dictionary;

            public KeyCollection(Dictionary<K, V> dictionary)
            {
                if (dictionary == null)
                    throw new ArgumentNullException("dictionary");
                this.dictionary = dictionary;
            }

            public void CopyTo(K[] array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException("array");
                if (index < 0 || index > array.Length)
                    throw new ArgumentOutOfRangeException("index");
                if (array.Length - index < dictionary.Count)
                    throw new ArgumentException();
                if (dictionary.IsDirect())
                    dictionary.PrimCopyToKeysDirect(array, index);
                else
                    dictionary.PrimCopyToKeysHash(array, index);
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            void ICollection<K>.Add(K item)
            {
                throw new NotSupportedException();
            }

            void ICollection<K>.Clear()
            {
                throw new NotSupportedException();
            }

            bool ICollection<K>.Contains(K item)
            {
                return dictionary.ContainsKey(item);
            }

            bool ICollection<K>.Remove(K item)
            {
                throw new NotSupportedException();
            }

            IEnumerator<K> IEnumerable<K>.GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            public int Count { get { return dictionary.Count; } }

            bool ICollection<K>.IsReadOnly { get { return true; } }

            public struct Enumerator : IEnumerator<K>
            {
                private Dictionary<K, V> dictionary;
                private int index;
                private K[] keys;

                internal Enumerator(Dictionary<K, V> dictionary)
                {
                    this.dictionary = dictionary;
                    index = -1;
                    if (dictionary.IsDirect())
                    {
                        keys = new K[dictionary.Count];
                        dictionary.PrimCopyToKeysDirect(keys, 0);
                    }
                    else
                        keys = null;
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    if (keys == null)
                    {
                        if (dictionary.entries == null)
                            return false;
                        index++;
                        while (index < dictionary.entries.Length)
                        {
                            if (dictionary.entries[index] != null)
                                return true;
                            index++;
                        }
                        return false;
                    }
                    else
                    {
                        index++;
                        return index < keys.Length;
                    }
                }

                public K Current
                {
                    get
                    {
                        if (keys == null)
                        {
                            if (dictionary.entries != null && index >= 0 && index < dictionary.entries.Length)
                                return dictionary.entries[index].Key;
                        }
                        else
                        {
                            if (index >= 0 && index < keys.Length)
                                return keys[index];
                        }
                        throw new InvalidOperationException("invalid enemeration");
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return Current;
                    }
                }

                public void Reset()
                {
                    index = -1;
                }
            }
        }

        public sealed class ValueCollection : ICollection<V>
        {
            private Dictionary<K, V> dictionary;

            public ValueCollection(Dictionary<K, V> dictionary)
            {
                if (dictionary == null)
                    throw new ArgumentNullException("dictionary");
                this.dictionary = dictionary;
            }

            public void CopyTo(V[] array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException("array");
                if (index < 0 || index > array.Length)
                    throw new ArgumentOutOfRangeException("index");
                if (array.Length - index < dictionary.Count)
                    throw new ArgumentException();
                if (dictionary.IsDirect())
                    dictionary.PrimCopyToValuesDirect(array, index);
                else
                    dictionary.PrimCopyToValuesHash(array, index);
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            void ICollection<V>.Add(V item)
            {
                throw new NotSupportedException();
            }

            void ICollection<V>.Clear()
            {
                throw new NotSupportedException();
            }

            bool ICollection<V>.Contains(V item)
            {
                throw new NotSupportedException();
            }

            bool ICollection<V>.Remove(V item)
            {
                throw new NotSupportedException();
            }

            IEnumerator<V> IEnumerable<V>.GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new Enumerator(dictionary);
            }

            public int Count { get { return dictionary.Count; } }

            bool ICollection<V>.IsReadOnly { get { return true; } }

            public struct Enumerator : IEnumerator<V>
            {
                private Dictionary<K, V> dictionary;
                private int index;
                private K[] keys;

                internal Enumerator(Dictionary<K, V> dictionary)
                {
                    this.dictionary = dictionary;
                    index = -1;
                    if (dictionary.IsDirect())
                    {
                        keys = new K[dictionary.Count];
                        dictionary.PrimCopyToKeysDirect(keys, 0);
                    }
                    else
                        keys = null;
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    if (keys == null)
                    {
                        if (dictionary.entries == null)
                            return false;
                        index++;
                        while (index < dictionary.entries.Length)
                        {
                            if (dictionary.entries[index] != null)
                                return true;
                            index++;
                        }
                        return false;
                    }
                    else
                    {
                        index++;
                        return index < keys.Length;
                    }
                }

                public V Current
                {
                    get
                    {
                        if (keys == null)
                        {
                            if (dictionary.entries != null && index >= 0 && index < dictionary.entries.Length)
                                return dictionary.entries[index].Value;
                        }
                        else
                        {
                            if (index >= 0 && index < keys.Length)
                                return dictionary[keys[index]];
                        }
                        throw new InvalidOperationException("invalid enemeration");
                    }
                }

                object IEnumerator.Current
                {
                    get
                    {
                        return Current;
                    }
                }

                public void Reset()
                {
                    index = -1;
                }
            }
        }
    }
}