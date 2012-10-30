//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Encoding
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    sealed class AmqpMap : IEnumerable<KeyValuePair<MapKey, object>>
    {
        int valueSize;
        IDictionary<MapKey, object> value;

        public AmqpMap()
        {
            this.value = new Dictionary<MapKey, object>();
        }

        public AmqpMap(IDictionary<MapKey, object> value)
        {
            this.value = value;
        }

        public AmqpMap(IDictionary value)
            : this()
        {
            foreach (DictionaryEntry entry in value)
            {
                this.value.Add(new MapKey(entry.Key), entry.Value);
            }
        }

        public int Count
        {
            get { return this.value.Count; }
        }

        public int ValueSize
        {
            get
            {
                if (this.valueSize == 0)
                {
                    this.valueSize = MapEncoding.GetValueSize(this);
                }

                return this.valueSize;
            }
        }

        public object this[MapKey key]
        {
            get
            {
                object obj;
                if (this.value.TryGetValue(key, out obj))
                {
                    return obj;
                }

                return null;
            }

            set
            {
                this.value[key] = value;
            }
        }

        public bool TryGetValue<TValue>(MapKey key, out TValue value)
        {
            object obj = null;
            if (this.value.TryGetValue(key, out obj))
            {
                value = (TValue)obj;
                return true;
            }

            value = default(TValue);
            return false;
        }

        public void Add(MapKey key, object value)
        {
            this.value.Add(key, value);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            bool firstItem = true;
            foreach (KeyValuePair<MapKey, object> pair in this.value)
            {
                if (firstItem)
                {
                    firstItem = false;
                }
                else
                {
                    sb.Append(',');
                }

                sb.AppendFormat("{0}:{1}", pair.Key, pair.Value);
            }

            sb.Append(']');
            return sb.ToString();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.value.GetEnumerator();
        }

        IEnumerator<KeyValuePair<MapKey, object>> IEnumerable<KeyValuePair<MapKey, object>>.GetEnumerator()
        {
            return this.value.GetEnumerator();
        }
    }
}
