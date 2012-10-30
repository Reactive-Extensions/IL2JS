//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
    enum TransportShutdownOption : byte
    {
        Read = 1,
        Write = 2,
        ReadWrite = 3
    }
}