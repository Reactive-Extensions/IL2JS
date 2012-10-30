//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    using System;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;

    sealed class Footer : DescribedAnnotations
    {
        public static readonly string Name = "amqp:footer:map";
        public static readonly ulong Code = 0x0000000000000078;

        public Footer() : base(Name, Code) { }

        public override string ToString()
        {
            return "footer";
        }
    }
}
