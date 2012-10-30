//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    sealed class DeleteOnNoLinks : LifeTimePolicy
    {
        public static readonly string Name = "amqp:delete-on-no-links:list";
        public static readonly ulong Code = 0x000000000000002c;

        public DeleteOnNoLinks() : base(Name, Code) { }

        public override string ToString()
        {
            return "delete-on-no-links()";
        }
    }
}
