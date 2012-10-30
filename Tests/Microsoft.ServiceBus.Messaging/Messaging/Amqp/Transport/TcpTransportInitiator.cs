//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using Microsoft.ServiceBus.Common;

    sealed class TcpTransportInitiator : TransportInitiator
    {
        TcpTransportSettings transportSettings;
        TransportAsyncCallbackArgs callbackArgs;

        internal TcpTransportInitiator(TcpTransportSettings transportSettings)
        {
            this.transportSettings = transportSettings;
        }

        public override bool ConnectAsync(TimeSpan timeout, TransportAsyncCallbackArgs callbackArgs)
        {
            // TODO: set socket connect timeout to timeout
            this.callbackArgs = callbackArgs;

            // TODO: IPv6 support for DnsEndPoint
            EndPoint endPoint = this.transportSettings.EndPoint;
            AddressFamily addressFamily = endPoint is DnsEndPoint ? AddressFamily.InterNetwork : endPoint.AddressFamily;
            Socket socket = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
            SocketAsyncEventArgs connectEventArgs = new SocketAsyncEventArgs();
            connectEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnectComplete);
            connectEventArgs.AcceptSocket = socket;
            connectEventArgs.RemoteEndPoint = endPoint;
            connectEventArgs.UserToken = this;
            if (socket.ConnectAsync(connectEventArgs))
            {
                return true;
            }
            else
            {
                this.Complete(connectEventArgs, true);
                return false;
            }
        }

        static void OnConnectComplete(object sender, SocketAsyncEventArgs e)
        {
            TcpTransportInitiator thisPtr = (TcpTransportInitiator)e.UserToken;
            thisPtr.Complete(e, false);
        }

        void Complete(SocketAsyncEventArgs e, bool completeSynchronously)
        {
            TransportBase transport = null;
            Exception exception = null;
            if (e.SocketError != SocketError.Success)
            {
                exception = new SocketException((int)e.SocketError);
                if (e.AcceptSocket != null)
                {
                    e.AcceptSocket.Close(0);
                }
            }
            else
            {
                Fx.Assert(e.AcceptSocket != null, "Must have a valid socket accepted.");
                transport = new TcpTransport(e.AcceptSocket, this.transportSettings);
                transport.Open();
            }

            e.Dispose();
            this.callbackArgs.CompletedSynchronously = completeSynchronously;
            this.callbackArgs.Exception = exception;
            this.callbackArgs.Transport = transport;

            if (!completeSynchronously)
            {
                this.callbackArgs.CompletedCallback(this.callbackArgs);
            }
        }
    }
}
