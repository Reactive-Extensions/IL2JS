//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    enum AmqpObjectState
    {
        // these states indicate that nothing has been received
        Start,
        HeaderSent,
        OpenPipe,
        OpenClosePipe,

        HeaderReceived,
        HeaderExchanged,
        OpenSent,
        OpenReceived,
        ClosePipe,
        Opened,
        CloseSent,
        CloseReceived,
        End,
        Faulted,
    }
}