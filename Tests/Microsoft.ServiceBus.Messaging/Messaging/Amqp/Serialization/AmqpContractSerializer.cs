//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    public sealed class AmqpContractSerializer
    {
        static readonly SerializableTypeCache Cache = new SerializableTypeCache();
        readonly SerializableType type;

        public AmqpContractSerializer(Type type)
        {
            this.type = GetType(type);
        }

        public void WriteObject(Stream stream, object graph)
        {
            this.type.WriteObject(stream, graph);
        }

        public object ReadObject(Stream stream)
        {
            return this.type.ReadObject(stream);
        }

        internal static SerializableType GetType(Type type)
        {
            SerializableType serialiableType = Cache.GetSerializableType(type);
            if (serialiableType == null)
            {
                serialiableType = CompileType(type);
                Cache.AddSerializableType(type, serialiableType);
            }

            return serialiableType;
        }

        static SerializableType CompileType(Type type)
        {
            // at this point, type is a composite type
            object[] typeAttributes = type.GetCustomAttributes(typeof(AmqpContractAttribute), true);
            if (typeAttributes.Length != 1)
            {
                throw new NotSupportedException(type.FullName);
            }

            AmqpContractAttribute contractAttribute = (AmqpContractAttribute)typeAttributes[0];
            string descriptorName = contractAttribute.Name;
            ulong? descriptorCode = contractAttribute.InternalCode;
            if (descriptorName == null && descriptorCode == null)
            {
                descriptorName = type.FullName;
            }

            int lastOrder = 0;
            SortedList<int, SerialiableMember> memberList = new SortedList<int, SerialiableMember>();
            MemberInfo[] memberInfos = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (MemberInfo memberInfo in memberInfos)
            {
                if (memberInfo.DeclaringType != type ||
                    (memberInfo.MemberType != MemberTypes.Field &&
                    memberInfo.MemberType != MemberTypes.Property))
                {
                    continue;
                }

                object[] memberAttributes = memberInfo.GetCustomAttributes(typeof(AmqpMemberAttribute), true);
                if (memberAttributes.Length != 1)
                {
                    continue;
                }

                AmqpMemberAttribute attribute = (AmqpMemberAttribute)memberAttributes[0];

                SerialiableMember member = new SerialiableMember();
                member.Name = attribute.Name ?? memberInfo.Name;
                member.Order = attribute.InternalOrder ?? lastOrder;
                member.Mandatory = attribute.Mandatory;
                member.Accessor = MemberAccessor.Create(memberInfo);

                // This will recursively resolve member types
                Type memberType = memberInfo.MemberType == MemberTypes.Field ? ((FieldInfo)memberInfo).FieldType : ((PropertyInfo)memberInfo).PropertyType;
                member.Type = GetType(memberType);

                memberList.Add(member.Order, member);
                lastOrder = member.Order >= lastOrder ? member.Order + 1 : lastOrder + 1;
            }

            SerialiableMember[] members = new SerialiableMember[memberList.Count];
            for (int i = 0; i < memberList.Count; ++i)
            {
                members[i] = memberList[i];
            }

            SerializableType serializableType = SerializableType.Create(type, descriptorName, descriptorCode, members);
            return serializableType;
        }
    }
}
