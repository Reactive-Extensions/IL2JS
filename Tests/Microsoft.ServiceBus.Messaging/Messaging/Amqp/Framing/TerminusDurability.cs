//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    enum TerminusDurability : uint
    {
        None = 0,
        Configuration = 1,
        UnsettledState = 2
    }
}