//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    sealed class DeleteOnNoMessages : LifeTimePolicy
    {
        public static readonly string Name = "amqp:delete-on-no-messages:list";
        public static readonly ulong Code = 0x000000000000002d;

        public DeleteOnNoMessages() : base(Name, Code) { }

        public override string ToString()
        {
            return "delete-on-no-messages()";
        }
    }
}
