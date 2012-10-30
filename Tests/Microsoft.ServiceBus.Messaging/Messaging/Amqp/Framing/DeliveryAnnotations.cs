//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    sealed class DeliveryAnnotations : DescribedAnnotations
    {
        public static readonly string Name = "amqp:delivery-annotations:map";
        public static readonly ulong Code = 0x0000000000000071;

        public DeliveryAnnotations() : base(Name, Code) { }
    }
}
