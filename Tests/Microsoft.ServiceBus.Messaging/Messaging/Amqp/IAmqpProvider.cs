//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Threading;
    using Microsoft.ServiceBus.Common;
    using Microsoft.ServiceBus.Messaging.Amqp.Encoding;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;
    using Microsoft.ServiceBus.Messaging.Amqp.Transport;

    interface IConnectionFactory
    {
        AmqpConnection CreateConnection(TransportBase transport, ProtocolHeader protocolHeader, bool isInitiator, AmqpSettings amqpSettings, AmqpConnectionSettings connectionSettings);
    }

    interface ISessionFactory
    {
        AmqpSession CreateSession(AmqpConnection connection, AmqpSessionSettings settings);
    }

    interface ILinkFactory
    {
        AmqpLink CreateLink(AmqpSession session, AmqpLinkSettings settings);

        IAsyncResult BeginOpenLink(AmqpLink link, TimeSpan timeout, AsyncCallback callback, object state);

        void EndOpenLink(IAsyncResult result);
    }

    interface IRuntimeProvider : IConnectionFactory, ISessionFactory, ILinkFactory
    {
    }

    interface INodeFactory
    {
        IAsyncResult BeginCreateNode(string address, Fields properties, TimeSpan timeout, AsyncCallback callback, object state);

        void EndCreateNode(IAsyncResult result);

        IAsyncResult BeginDeleteNode(string address, TimeSpan timeout, AsyncCallback callback, object state);

        void EndDeleteNode(IAsyncResult result);
    }

    interface IAmqpProvider : IRuntimeProvider, INodeFactory
    {
    }
}
