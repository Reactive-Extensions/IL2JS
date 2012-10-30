//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    sealed class DeleteOnClose : LifeTimePolicy
    {
        public static readonly string Name = "amqp:delete-on-close:list";
        public static readonly ulong Code = 0x000000000000002b;

        public DeleteOnClose() : base(Name, Code) { }

        public override string ToString()
        {
            return "deleted-on-close()";
        }
    }
}
