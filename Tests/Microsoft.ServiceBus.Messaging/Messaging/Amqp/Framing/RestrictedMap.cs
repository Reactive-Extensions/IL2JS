//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System.Collections;
    using System.Collections.Generic;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    abstract class RestrictedMap : IEnumerable<KeyValuePair<MapKey, object>>
    {
        AmqpMap innerMap;

        protected AmqpMap InnerMap
        {
            get 
            {
                if (this.innerMap == null)
                {
                    this.innerMap = new AmqpMap();
                }

                return this.innerMap; 
            }
        }

        public void SetMap(AmqpMap map)
        {
            this.innerMap = map;
        }

        public override string ToString()
        {
            return this.InnerMap.ToString();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            IEnumerable map = this.InnerMap;
            return map.GetEnumerator();
        }

        IEnumerator<KeyValuePair<MapKey, object>> IEnumerable<KeyValuePair<MapKey, object>>.GetEnumerator()
        {
            IEnumerable<KeyValuePair<MapKey, object>> map = this.InnerMap;
            return map.GetEnumerator();
        }
    }

    abstract class RestrictedMap<TKey> : RestrictedMap
    {
        public static implicit operator AmqpMap(RestrictedMap<TKey> restrictedMap)
        {
            return restrictedMap == null ? null : restrictedMap.InnerMap;
        }

        public object this[TKey key]
        {
            get { return this.InnerMap[new MapKey(key)]; }
            set { this.InnerMap[new MapKey(key)] = value; }
        }

        public object this[MapKey key]
        {
            get { return this.InnerMap[key]; }
            set { this.InnerMap[key] = value; }
        }

        public bool TryGetValue<TValue>(TKey key, out TValue value)
        {
            return this.InnerMap.TryGetValue(new MapKey(key), out value);
        }

        public bool TryGetValue<TValue>(MapKey key, out TValue value)
        {
            return this.InnerMap.TryGetValue(key, out value);
        }

        public void Add(TKey key, object value)
        {
            this.InnerMap.Add(new MapKey(key), value);
        }

        public void Add(MapKey key, object value)
        {
            this.InnerMap.Add(key, value);
        }
    }

    sealed class Fields : RestrictedMap<AmqpSymbol>
    {
    }

    sealed class FilterSet : RestrictedMap<AmqpSymbol>
    {
    }

    sealed class PropertiesMap : RestrictedMap<string>
    {
    }

    sealed class Annotations : RestrictedMap<AmqpSymbol>
    {
    }
}
