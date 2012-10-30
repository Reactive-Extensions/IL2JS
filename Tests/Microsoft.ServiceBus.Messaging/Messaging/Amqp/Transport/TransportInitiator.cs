//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
    using System;

    abstract class TransportInitiator
    {
        /// <summary>
        /// Returns true if connect is pending, the callback is invoked upon completion.
        /// Returns false if connect is completed synchronously, the callback is not invoked.
        /// </summary>
        public abstract bool ConnectAsync(TimeSpan timeout, TransportAsyncCallbackArgs callbackArgs);
    }
}
