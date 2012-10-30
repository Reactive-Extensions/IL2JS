//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using Microsoft.ServiceBus.Common;

    sealed class TcpTransportListener : TransportListener
    {
        Socket listenSocket;
        TcpTransportSettings transportSettings;

        public TcpTransportListener(TcpTransportSettings transportSettings)
        {
            this.transportSettings = transportSettings;
        }

        protected override string Type
        {
            get { return "tcp-listener"; }
        }

        protected override bool CloseInternal()
        {
            if (this.listenSocket != null)
            {
                this.listenSocket.Close();
                this.listenSocket = null;
            }

            return true;
        }

        protected override void AbortInternal()
        {
            if (this.listenSocket != null)
            {
                this.listenSocket.Close(0);
                this.listenSocket = null;
            }
        }

        protected override void OnListen()
        {
            IPEndPoint endPoint = this.transportSettings.EndPoint as IPEndPoint;
            if (endPoint == null)
            {
                throw new InvalidOperationException(SRClient.ListenOnIPEndpoint);
            }

            this.listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            this.listenSocket.Bind(endPoint);
            this.listenSocket.Listen(this.transportSettings.TcpBacklog);

            for (int i = 0; i < this.transportSettings.ListenerAcceptorCount; ++i)
            {
                SocketAsyncEventArgs listenEventArgs = new SocketAsyncEventArgs();
                listenEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(this.OnAcceptComplete);
                listenEventArgs.UserToken = this;

                ActionItem.Schedule(AcceptTransportLoop, listenEventArgs);
            }
        }

        static void AcceptTransportLoop(object state)
        {
            SocketAsyncEventArgs args = (SocketAsyncEventArgs)state;
            TcpTransportListener thisPtr = (TcpTransportListener)args.UserToken;

            while (thisPtr.State != AmqpObjectState.End)
            {
                try
                {
                    args.AcceptSocket = null;
                    if (!thisPtr.listenSocket.AcceptAsync(args))
                    {
                        thisPtr.HandleAcceptComplete(args, true);
                    }
                    else
                    {
                        break;
                    }
                }
                catch (SocketException socketException)
                {
                    if (!thisPtr.ShouldRetryAccept(socketException.SocketErrorCode))
                    {
                        args.Dispose();
                        thisPtr.TryClose(socketException);
                        break;
                    }
                }
                catch (Exception exception)
                {
                    if (Fx.IsFatal(exception))
                    {
                        throw;
                    }

                    args.Dispose();
                    thisPtr.TryClose(exception);
                    break;
                }
            }
        }

        void OnAcceptComplete(object sender, SocketAsyncEventArgs e)
        {
            if (this.HandleAcceptComplete(e, false))
            {
                AcceptTransportLoop(e);
            }
        }

        bool HandleAcceptComplete(SocketAsyncEventArgs e, bool completedSynchronously)
        {
            if (e.SocketError == SocketError.Success)
            {
                TcpTransport transport = new TcpTransport(e.AcceptSocket, this.transportSettings);
                transport.Open();

                TransportAsyncCallbackArgs args = new TransportAsyncCallbackArgs();
                args.Transport = transport;
                args.CompletedSynchronously = completedSynchronously;
                this.OnTransportAccepted(args);
                return true;
            }
            else
            {
                e.Dispose();
                this.TryClose(new SocketException((int)e.SocketError));
                return false;
            }
        }

        bool ShouldRetryAccept(SocketError error)
        {
            return error == SocketError.ConnectionReset ||
                   error == SocketError.NoBufferSpaceAvailable ||
                   error == SocketError.TimedOut;
        }
    }
}
