//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Serialization
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    sealed class SerializableTypeCache
    {
        static readonly Dictionary<Type, SerializableType> builtInTypes = new Dictionary<Type, SerializableType>()
        {
            { typeof(bool),     SerializableType.Create(typeof(bool)) },
            { typeof(byte),     SerializableType.Create(typeof(byte)) },
            { typeof(ushort),   SerializableType.Create(typeof(ushort)) },
            { typeof(uint),     SerializableType.Create(typeof(uint)) },
            { typeof(ulong),    SerializableType.Create(typeof(ulong)) },
            { typeof(sbyte),    SerializableType.Create(typeof(sbyte)) },
            { typeof(short),    SerializableType.Create(typeof(short)) },
            { typeof(int),      SerializableType.Create(typeof(int)) },
            { typeof(long),     SerializableType.Create(typeof(long)) },
            { typeof(float),    SerializableType.Create(typeof(float)) },
            { typeof(double),   SerializableType.Create(typeof(double)) },
            { typeof(decimal),  SerializableType.Create(typeof(decimal)) },
            { typeof(char),     SerializableType.Create(typeof(char)) },
            { typeof(DateTime), SerializableType.Create(typeof(DateTime)) },
            { typeof(Guid),     SerializableType.Create(typeof(Guid)) },
            { typeof(ArraySegment<byte>), SerializableType.Create(typeof(ArraySegment<byte>)) },
            { typeof(string),   SerializableType.Create(typeof(string)) },
        };

        readonly ConcurrentDictionary<Type, SerializableType> cache;

        public SerializableTypeCache()
        {
            this.cache = new ConcurrentDictionary<Type, SerializableType>();
        }

        public SerializableType GetSerializableType(Type type)
        {
            SerializableType serializableType = null;
            if (builtInTypes.TryGetValue(type, out serializableType))
            {
                return serializableType;
            }

            if (this.cache.TryGetValue(type, out serializableType))
            {
                return serializableType;
            }

            return null;
        }

        public void AddSerializableType(Type type, SerializableType serializableType)
        {
            this.cache.TryAdd(type, serializableType);
        }
    }
}
