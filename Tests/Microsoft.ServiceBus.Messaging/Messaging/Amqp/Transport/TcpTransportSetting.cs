//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
    using System;
    using System.Net;

    sealed class TcpTransportSettings : TransportSettings
    {
        const int DefaultTcpBacklog = 200;
        const int DefaultTcpBufferSize = 4 * 1024;
        const int DefaultTcpAcceptorCount = 1;

        public TcpTransportSettings()
        {
            this.TcpBufferSize = DefaultTcpBufferSize;
            this.TcpBacklog = DefaultTcpBacklog;
            this.ListenerAcceptorCount = DefaultTcpAcceptorCount;
        }

        public EndPoint EndPoint
        {
            get;
            private set;
        }

        public int TcpBufferSize 
        { 
            get; 
            set; 
        }

        public int TcpBacklog 
        { 
            get; 
            set; 
        }

        public void SetEndPoint(string host, int port, bool listen)
        {
            IPAddress address = null;
            if (IPAddress.TryParse(host, out address))
            {
                this.EndPoint = new IPEndPoint(address, port);
            }
            else if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                this.EndPoint = new IPEndPoint(listen ? IPAddress.Any : IPAddress.Loopback, port);
            }
            else
            {
                if (listen)
                {
                    // TODO: make sure host is local machine name
                    this.EndPoint = new IPEndPoint(IPAddress.Any, port);
                }
                else
                {
                    this.EndPoint = new DnsEndPoint(host, port);
                }
            }
        }

        public override TransportInitiator CreateInitiator()
        {
            return new TcpTransportInitiator(this);
        }

        public override TransportListener CreateListener()
        {
            return new TcpTransportListener(this);
        }

        public override string ToString()
        {
            return this.EndPoint.ToString();
        }
    }
}
