//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    enum ProtocolId : byte
    {
        Amqp = 0,
        AmqpTls = 2,
        AmqpSasl = 3
    }
}
