//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    sealed class MessageAnnotations : DescribedAnnotations
    {
        public static readonly string Name = "amqp:message-annotations:map";
        public static readonly ulong Code = 0x0000000000000072;

        public MessageAnnotations() : base(Name, Code) { }
    }
}
