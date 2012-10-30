//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    enum SettleMode : byte
    {
        SettleOnSend,
        SettleOnReceive,
        SettleOnDispose
    }
}
