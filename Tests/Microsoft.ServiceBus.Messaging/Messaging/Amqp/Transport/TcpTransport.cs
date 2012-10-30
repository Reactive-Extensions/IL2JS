//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
    using System;
    using System.Net.Sockets;
    using Microsoft.ServiceBus.Common;
    using Microsoft.ServiceBus.Messaging.Amqp;

    sealed class TcpTransport : TransportBase
    {
        Socket socket;
        SocketAsyncEventArgs sendEventArgs;
        SocketAsyncEventArgs receiveEventArgs;

        public TcpTransport(Socket socket, TcpTransportSettings transportSettings) :
            base()
        {
            this.socket = socket;
            this.socket.SendBufferSize = transportSettings.TcpBufferSize;
            this.socket.ReceiveBufferSize = transportSettings.TcpBufferSize;

            this.sendEventArgs = new SocketAsyncEventArgs();
            this.sendEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(this.OnAsyncComplete);
            this.receiveEventArgs = new SocketAsyncEventArgs();
            this.receiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(this.OnAsyncComplete);
        }

        protected override string Type
        {
            get { return "Tcp"; }
        }

        public override void Shutdown(TransportShutdownOption how)
        {
            if (how == TransportShutdownOption.Read)
            {
                this.socket.Shutdown(SocketShutdown.Receive);
            }
            else if (how == TransportShutdownOption.Write)
            {
                this.socket.Shutdown(SocketShutdown.Send);
            }
            else if (how == TransportShutdownOption.ReadWrite)
            {
                this.socket.Shutdown(SocketShutdown.Both);
            }
        }

        public sealed override bool WriteAsync(TransportAsyncCallbackArgs args)
        {
            Fx.Assert(args.Buffer != null, "must have a buffer to write");
            Fx.Assert(args.CompletedCallback != null, "must have a valid callback");

            this.sendEventArgs.SetBuffer(args.Buffer, args.Offset, args.Count);
            this.sendEventArgs.UserToken = args;
            if (!this.socket.SendAsync(this.sendEventArgs))
            {
                this.OperationComplete(this.sendEventArgs, true);
                return false;
            }

            return true;
        }

        public sealed override bool ReadAsync(TransportAsyncCallbackArgs args)
        {
            Fx.Assert(args.Buffer != null, "must have buffer(s) to read");
            Fx.Assert(args.CompletedCallback != null, "must have a valid callback");

            this.receiveEventArgs.SetBuffer(args.Buffer, args.Offset, args.Count);
            this.receiveEventArgs.UserToken = args;
            if (!this.socket.ReceiveAsync(this.receiveEventArgs))
            {
                this.OperationComplete(this.receiveEventArgs, true);
                return false;
            }

            return true;
        }

        protected override bool CloseInternal()
        {
            this.sendEventArgs.Dispose();
            this.receiveEventArgs.Dispose();
            this.socket.Close();
            return true;
        }

        protected override void AbortInternal()
        {
            this.sendEventArgs.Dispose();
            this.receiveEventArgs.Dispose();
            this.socket.Close(0);
        }

        void OnAsyncComplete(object sender, SocketAsyncEventArgs args)
        {
            this.OperationComplete(args, false);
        }

        void OperationComplete(SocketAsyncEventArgs socketArgs, bool completedSynchronously)
        {
            TransportAsyncCallbackArgs args = (TransportAsyncCallbackArgs)socketArgs.UserToken;
            if (socketArgs.SocketError == SocketError.Success)
            {
                args.BytesTransfered = socketArgs.BytesTransferred;
                args.Exception = null;
                args.CompletedSynchronously = completedSynchronously;
            }
            else
            {
                args.CompletedSynchronously = completedSynchronously;
                args.Exception = new SocketException((int)socketArgs.SocketError);
            }

            if (!completedSynchronously)
            {
                args.CompletedCallback(args);
            }
        }
    }
}
