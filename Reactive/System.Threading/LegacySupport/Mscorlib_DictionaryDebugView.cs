using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace System.Collections.Generic
{
    /// <summary>
    /// This internal class from mscorlib.dll is used by ConcurrentDictionary.
    /// </summary>
    internal sealed class Mscorlib_DictionaryDebugView<K, V>
    {
        private IDictionary<K, V> dict;

        internal Mscorlib_DictionaryDebugView(IDictionary<K, V> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            this.dict = dictionary;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        internal KeyValuePair<K, V>[] Items
        {
            get
            {
                KeyValuePair<K, V>[] items = new KeyValuePair<K, V>[dict.Count];
                dict.CopyTo(items, 0);
                return items;
            }
        }
    }  
}
