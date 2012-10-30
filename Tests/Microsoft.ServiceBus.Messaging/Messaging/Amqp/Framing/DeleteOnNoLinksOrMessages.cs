//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    sealed class DeleteOnNoLinksOrMessages : LifeTimePolicy
    {
        public static readonly string Name = "amqp:delete-on-no-links-or-messages:list";
        public static readonly ulong Code = 0x000000000000002e;

        public DeleteOnNoLinksOrMessages() : base(Name, Code) { }

        public override string ToString()
        {
            return "delete-on-no-links-or-messages()";
        }
    }
}
