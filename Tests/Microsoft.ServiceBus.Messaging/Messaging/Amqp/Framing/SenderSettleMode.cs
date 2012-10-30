//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Framing
{
    enum SenderSettleMode : byte
    {
        Unsettled = 0,
        Settled = 1,
        Mixed = 2
    }
}