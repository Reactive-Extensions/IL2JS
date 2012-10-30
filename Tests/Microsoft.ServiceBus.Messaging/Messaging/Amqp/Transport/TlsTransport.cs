//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
    using System;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.ServiceBus.Common;

    sealed class TlsTransport : TransportBase
    {
        readonly TransportBase innerTransport;
        readonly string targetHost;
        readonly bool isInitiator;
        readonly SslStream sslStream;
        X509Certificate2 serverCert;

        readonly AsyncCallback onReadComplete;
        readonly AsyncCallback onOpenComplete;
        readonly AsyncCallback onWriteComplete;

        public TlsTransport(TransportBase innerTransport, TlsTransportSettings tlsSettings)
            : base()
        {
            Fx.Assert((tlsSettings.IsInitiator && tlsSettings.TargetHost != null) || (!tlsSettings.IsInitiator && tlsSettings.Certificate != null),
                tlsSettings.IsInitiator ? "Must have a target host for the client." : "Must have a certificate for the server.");
            this.innerTransport = innerTransport;
            this.isInitiator = tlsSettings.IsInitiator;
            this.targetHost = tlsSettings.TargetHost;
            this.serverCert = tlsSettings.Certificate;
            this.sslStream = tlsSettings.CertificateValidationCallback == null ?
                new SslStream(new TransportStream(this.innerTransport), false) :
                new SslStream(new TransportStream(this.innerTransport), false, tlsSettings.CertificateValidationCallback);

            this.onReadComplete = this.OnReadComplete;
            this.onOpenComplete = this.OnOpenComplete;
            this.onWriteComplete = this.OnWriteComplete;
        }

        public override bool IsSecure
        {
            get { return true; }
        }

        protected override string Type
        {
            get { return "Tls"; }
        }

        public override void Shutdown(TransportShutdownOption how)
        {
            this.innerTransport.Shutdown(how);
        }

        public sealed override bool WriteAsync(TransportAsyncCallbackArgs args)
        {
            IAsyncResult result = this.sslStream.BeginWrite(args.Buffer, args.Offset, args.Count, this.onWriteComplete, args);
            if (result.CompletedSynchronously)
            {
                this.HandleWriteComplete(result);
                return false;
            }
            else
            {
                return true;
            }
        }

        public sealed override bool ReadAsync(TransportAsyncCallbackArgs args)
        {
            Fx.Assert(args.Buffer != null, "must have buffer to read");
            IAsyncResult result = this.sslStream.BeginRead(args.Buffer, args.Offset, args.Count, this.onReadComplete, args);
            if (result.CompletedSynchronously)
            {
                this.HandleReadComplete(result);
                return false;
            }
            else
            {
                return true;
            }
        }

        protected override bool OpenInternal()
        {
            IAsyncResult result = null;
            if (this.isInitiator)
            {
                result = this.sslStream.BeginAuthenticateAsClient(this.targetHost, this.onOpenComplete, null);
            }
            else
            {
                result = this.sslStream.BeginAuthenticateAsServer(this.serverCert, this.onOpenComplete, null);
            }

            if (result.CompletedSynchronously)
            {
                this.HandleOpenComplete(result);
                return true;
            }
            else
            {
                return false;
            }
        }

        protected override bool CloseInternal()
        {
            this.sslStream.Close();
            this.innerTransport.Close();
            return true;
        }

        protected override void AbortInternal()
        {
            this.sslStream.Close();
            this.innerTransport.Abort();
        }

        void OnOpenComplete(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            Exception exception = null;
            try
            {
                this.HandleOpenComplete(result);
            }
            catch (Exception exp)
            {
                exception = exp;
            }

            this.CompleteOpen(false, exception);
        }

        void OnReadComplete(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            TransportAsyncCallbackArgs args = this.HandleReadComplete(result);
            args.CompletedCallback(args);
        }

        void HandleOpenComplete(IAsyncResult result)
        {
            this.serverCert = null;
            if (this.isInitiator)
            {
                this.sslStream.EndAuthenticateAsClient(result);
            }
            else
            {
                this.sslStream.EndAuthenticateAsServer(result);
            }
        }

        TransportAsyncCallbackArgs HandleReadComplete(IAsyncResult result)
        {
            TransportAsyncCallbackArgs args = (TransportAsyncCallbackArgs)result.AsyncState;
            args.CompletedSynchronously = result.CompletedSynchronously;

            try
            {
                args.BytesTransfered = this.sslStream.EndRead(result);
                args.Exception = null;
            }
            catch (Exception exception)
            {
                args.Exception = exception;
            }

            return args;
        }

        void OnWriteComplete(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            TransportAsyncCallbackArgs args = this.HandleWriteComplete(result);
            args.CompletedCallback(args);
        }

        TransportAsyncCallbackArgs HandleWriteComplete(IAsyncResult result)
        {
            TransportAsyncCallbackArgs args = (TransportAsyncCallbackArgs)result.AsyncState;
            args.CompletedSynchronously = result.CompletedSynchronously;

            try
            {
                this.sslStream.EndWrite(result);
                args.Exception = null;
            }
            catch (Exception exception)
            {
                args.Exception = exception;
            }

            return args;
        }
    }
}
