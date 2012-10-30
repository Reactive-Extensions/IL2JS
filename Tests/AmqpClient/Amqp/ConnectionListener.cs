//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Collections.Concurrent;
    using Microsoft.ServiceBus.Common;
    using Microsoft.ServiceBus.Messaging.Amqp;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;
    using Microsoft.ServiceBus.Messaging.Amqp.Transport;

    interface IConnectionHandler
    {
        void HandleConnection(AmqpConnection connection);
    }

    abstract class ConnectionListener : AmqpObject, IConnectionFactory
    {
        readonly Uri listenAddress;
        readonly TransportListener transportListener;
        readonly AmqpSettings amqpSettings;
        readonly AmqpConnectionSettings connectionSettings;
        readonly Action<TransportAsyncCallbackArgs> onAcceptTransport;
        readonly AsyncCallback onConnectionOpenComplete;

        protected ConnectionListener(
            Uri addressUri,
            AmqpSettings amqpSettings,
            AmqpConnectionSettings connectionSettings)
        {
            amqpSettings.ValidateListenerSettings();
            this.listenAddress = addressUri;
            this.amqpSettings = amqpSettings;
            this.connectionSettings = connectionSettings;
            this.onAcceptTransport = this.OnAcceptTransport;

            TcpTransportSettings tcpSettings = new TcpTransportSettings();
            tcpSettings.SetEndPoint(addressUri.Host, addressUri.Port, true);
            TransportListener tpListener = null;
            if (addressUri.Scheme.Equals(AmqpConstants.SchemeAmqps, StringComparison.OrdinalIgnoreCase))
            {
                TlsTransportProvider tlsProvider = this.amqpSettings.GetTransportProvider<TlsTransportProvider>();
                if (tlsProvider == null)
                {
                    throw Fx.Exception.ArgumentNull("TlsSecurityProvider");
                }

                Fx.Assert(tlsProvider.Settings.Certificate != null, "Must have a valid certificate.");
                TlsTransportSettings tlsSettings = new TlsTransportSettings(tcpSettings, false);
                tlsSettings.Certificate = tlsProvider.Settings.Certificate;
                tpListener = tlsSettings.CreateListener();
            }
            else
            {
                tpListener = tcpSettings.CreateListener();
            }

            this.transportListener = new AmqpTransportListener(new TransportListener[] { tpListener }, this.amqpSettings);
            this.onConnectionOpenComplete = new AsyncCallback(this.OnConnectionOpenComplete);
        }

        public Uri ListenAddress
        {
            get { return this.listenAddress; }
        }

        public AmqpSettings AmqpSettings
        {
            get { return this.amqpSettings; }
        }

        public AmqpConnectionSettings ConnectionSettings
        {
            get { return this.connectionSettings; }
        }

        public static ConnectionListener CreateSharedListener(
            Uri addressUri,
            AmqpSettings amqpSettings,
            AmqpConnectionSettings connectionSettings)
        {
            return new SharedConnectionListener(addressUri, amqpSettings, connectionSettings);
        }

        public static ConnectionListener CreateExclusiveListener(
            Uri addressUri,
            AmqpSettings amqpSettings,
            AmqpConnectionSettings connectionSettings,
            IConnectionHandler connectionHandler)
        {
            return new ExclusiveConnectionListener(addressUri, amqpSettings, connectionSettings, connectionHandler);
        }

        public AmqpConnection CreateConnection(TransportBase transport, ProtocolHeader protocolHeader, bool isInitiator, AmqpSettings amqpSettings, AmqpConnectionSettings connectionSettings)
        {
            if (this.amqpSettings.RequireSecureTransport && !transport.IsSecure)
            {
                throw new AmqpException(AmqpError.NotAllowed, SR.AmqpTransportNotSecure);
            }

            AmqpConnection connection = new AmqpConnection(transport, protocolHeader, isInitiator, amqpSettings, connectionSettings);
            Utils.Trace(TraceLevel.Info, "{0}: Created {1}", this, connection);
            return connection;
        }

        protected override string  Type
        {
            get { return "connection-listener"; }
        }

        protected virtual Action<Open> OnReceiveConnectionOpen
        {
            get { return null; }
        }

        public void RegisterHandler(string virtualHost, IConnectionHandler handler)
        {
            this.OnRegisterHandler(virtualHost, handler);
        }

        public void UnregisterHandler(string virtualHost)
        {
            this.OnUnregisterHandler(virtualHost);
        }

        protected override bool OpenInternal()
        {
            this.transportListener.Listen(this.onAcceptTransport);
            return true;
        }

        protected override bool CloseInternal()
        {
            this.transportListener.Close();
            return true;
        }

        protected override void AbortInternal()
        {
            this.transportListener.Abort();
        }

        protected abstract void OnRegisterHandler(string virtualHost, IConnectionHandler handler);

        protected abstract void OnUnregisterHandler(string virtualHost);

        protected abstract void HandleConnection(AmqpConnection connection);

        void OnAcceptTransport(TransportAsyncCallbackArgs args)
        {
            Fx.Assert(args.Exception == null, "Should not be failed.");
            Fx.Assert(args.Transport != null, "Should have a transport");

            AmqpConnectionSettings settings = this.connectionSettings.Clone();
            settings.OnOpenCallback = this.OnReceiveConnectionOpen;
            AmqpConnection connection = null;
            try
            {
                connection = this.CreateConnection(
                    args.Transport, 
                    (ProtocolHeader)args.UserToken, 
                    false, 
                    this.amqpSettings.Clone(), 
                    settings);
                connection.BeginOpen(connection.DefaultOpenTimeout, this.onConnectionOpenComplete, connection);
            }
            catch (Exception ex)
            {
                if (Fx.IsFatal(ex))
                {
                    throw;
                }

                if (connection != null)
                {
                    connection.TryClose(ExceptionHelper.ToAmqpException(ex));
                }
            }
        }

        void OnConnectionOpenComplete(IAsyncResult result)
        {
            AmqpConnection connection = (AmqpConnection)result.AsyncState;
            try
            {
                connection.EndOpen(result);

                this.HandleConnection(connection);
            }
            catch (Exception exception)
            {
                if (Fx.IsFatal(exception))
                {
                    throw;
                }

                connection.TryClose(ExceptionHelper.ToAmqpException(exception));
            }
        }

        sealed class ExclusiveConnectionListener : ConnectionListener
        {
            IConnectionHandler connectionHandler;

            public ExclusiveConnectionListener(
                Uri addressUri,
                AmqpSettings amqpSettings,
                AmqpConnectionSettings connectionSettings,
                IConnectionHandler connectionHandler)
                : base(addressUri, amqpSettings, connectionSettings)
            {
                Fx.Assert(connectionHandler != null, "Connection handler cannot be null.");
                this.connectionHandler = connectionHandler;
            }

            protected override void OnRegisterHandler(string virtualHost, IConnectionHandler handler)
            {
                throw new InvalidOperationException();
            }

            protected override void OnUnregisterHandler(string virtualHost)
            {
                throw new InvalidOperationException();
            }

            protected override void HandleConnection(AmqpConnection connection)
            {
                this.connectionHandler.HandleConnection(connection);
            }
        }

        sealed class SharedConnectionListener : ConnectionListener
        {
            readonly ConcurrentDictionary<string, IConnectionHandler> connectionHandlers;
            readonly Action<Open> connectionOpenCallback;

            public SharedConnectionListener(
                Uri addressUri,
                AmqpSettings amqpSettings,
                AmqpConnectionSettings connectionSettings)
                : base(addressUri, amqpSettings, connectionSettings)
            {
                this.connectionHandlers = new ConcurrentDictionary<string, IConnectionHandler>(StringComparer.OrdinalIgnoreCase);
                this.connectionOpenCallback = this.OnConnectionOpen;
            }

            protected override Action<Open> OnReceiveConnectionOpen
            {
                get
                {
                    return this.connectionOpenCallback;
                }
            }

            protected override void OnRegisterHandler(string virtualHost, IConnectionHandler handler)
            {
                Utils.Trace(TraceLevel.Info, "{0}: Register handler {1}", this, virtualHost);
                this.connectionHandlers.AddOrUpdate(virtualHost, handler, (k, v) => { return handler; });
            }

            protected override void OnUnregisterHandler(string virtualHost)
            {
                Utils.Trace(TraceLevel.Info, "{0}: Unregister handler {1}", this, virtualHost);
                IConnectionHandler unused = null;
                this.connectionHandlers.TryRemove(virtualHost, out unused);
            }

            protected override void HandleConnection(AmqpConnection connection)
            {
                try
                {
                    Fx.Assert(connection.Settings.RemoteHostName != null, "RemoteHostName cannot be null. It has been validated in OnConnectionOpen.");
                    IConnectionHandler handler = null;
                    if (!this.connectionHandlers.TryGetValue(connection.Settings.RemoteHostName, out handler))
                    {
                        throw new AmqpException(AmqpError.NotFound, SR.AmqpConnectionHandlerNotFound(connection.Settings.RemoteHostName));
                    }

                    handler.HandleConnection(connection);
                }
                catch (Exception exception)
                {
                    if (Fx.IsFatal(exception))
                    {
                        throw;
                    }

                    throw ExceptionHelper.ToAmqpException(exception);
                }
            }

            void OnConnectionOpen(Open open)
            {
                if (open.HostName == null)
                {
                    throw new AmqpException(AmqpError.InvalidField, "open.hostname");
                }
                else if (!this.connectionHandlers.ContainsKey(open.HostName))
                {
                    throw new AmqpException(AmqpError.NotFound, SR.AmqpConnectionHandlerNotFound(open.HostName));
                }
            }
        }
    }
}
