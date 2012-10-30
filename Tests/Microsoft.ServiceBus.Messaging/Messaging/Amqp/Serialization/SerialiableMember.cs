//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Serialization
{
    sealed class SerialiableMember
    {
        public string Name
        {
            get;
            set;
        }

        public int Order
        {
            get;
            set;
        }

        public bool Mandatory
        {
            get;
            set;
        }

        public MemberAccessor Accessor
        {
            get;
            set;
        }

        public SerializableType Type
        {
            get;
            set;
        }
    }
}
