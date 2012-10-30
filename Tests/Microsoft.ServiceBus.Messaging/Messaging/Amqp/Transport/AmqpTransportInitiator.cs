//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
    using System;
    using Microsoft.ServiceBus.Common;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    sealed class AmqpTransportInitiator : TransportInitiator
    {
        AmqpSettings settings;
        TransportSettings transportSettings;
        AsyncIO.AsyncBufferWriter writer;
        AsyncIO.AsyncBufferReader reader;
        TimeoutHelper timeoutHelper;
        int providerIndex;
        ProtocolHeader sentHeader;

        /// <summary>
        /// This initiator establishes a base transport using the transport settings
        /// Then it iterates through the security provider list in the settings to upgrade
        /// the transport (e.g. tcp -> tls -> sasl).
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="transportSettings"></param>
        public AmqpTransportInitiator(AmqpSettings settings, TransportSettings transportSettings)
        {
            settings.ValidateInitiatorSettings();
            this.settings = settings;
            this.transportSettings = transportSettings;
        }

        public override bool ConnectAsync(TimeSpan timeout, TransportAsyncCallbackArgs callbackArgs)
        {
            TransportInitiator innerInitiator = this.transportSettings.CreateInitiator();
            Utils.Trace(TraceLevel.Info, "{0}: Connecting to {1}...", this, this.transportSettings);
            TransportAsyncCallbackArgs args = new TransportAsyncCallbackArgs();
            args.CompletedCallback = this.OnConnectComplete;
            args.UserToken = callbackArgs;
            callbackArgs.CompletedSynchronously = false;
            this.timeoutHelper = new TimeoutHelper(timeout);
            innerInitiator.ConnectAsync(timeout, args);
            return true;
        }

        public override string ToString()
        {
            return "tp-initiator";
        }

        void OnConnectComplete(TransportAsyncCallbackArgs args)
        {
            if (args.Exception != null)
            {
                this.Complete(args);
                return;
            }

            TransportProvider provider = this.settings.TransportProviders[this.providerIndex];
            if (provider.ProtocolId == ProtocolId.Amqp)
            {
                this.Complete(args);
                return;
            }

            Utils.Trace(TraceLevel.Info, "{0}: Connected. Start security negotiation...", this);
            this.writer = new AsyncIO.AsyncBufferWriter(args.Transport);
            this.reader = new AsyncIO.AsyncBufferReader(args.Transport);
            this.WriteSecurityHeader(args);
        }

        void WriteSecurityHeader(TransportAsyncCallbackArgs args)
        {
            // secure transport: header negotiation
            TransportProvider provider = this.settings.TransportProviders[this.providerIndex];
            this.sentHeader = new ProtocolHeader(provider.ProtocolId, provider.DefaultVersion);
            Utils.Trace(TraceLevel.Info, "{0}: Sending header {1}", this, this.sentHeader);
            Utils.Trace(TraceLevel.Frame, "SEND  {0}", this.sentHeader);

            args.SetBuffer(this.sentHeader.Buffer);
            args.CompletedCallback = this.OnWriteHeaderComplete;
            this.writer.WriteBuffer(args);
        }

        void OnWriteHeaderComplete(TransportAsyncCallbackArgs args)
        {
            if (args.Exception != null)
            {
                this.Complete(args);
                return;
            }

            Utils.Trace(TraceLevel.Verbose, "{0}: Write header complete. Reading header...", this);
            byte[] headerBuffer = new byte[ProtocolHeader.Size];
            args.SetBuffer(headerBuffer, 0, headerBuffer.Length);
            args.CompletedCallback = this.OnReadHeaderComplete;
            this.reader.ReadBuffer(args);
        }

        void OnReadHeaderComplete(TransportAsyncCallbackArgs args)
        {
            if (args.Exception != null)
            {
                this.Complete(args);
                return;
            }

            try
            {
                ProtocolHeader receivedHeader = ProtocolHeader.Decode(ByteBuffer.Wrap(args.Buffer, args.Offset, args.Count));
                Utils.Trace(TraceLevel.Info, "{0}: Received header {1}", this, receivedHeader);
                Utils.Trace(TraceLevel.Frame, "RECV  {0}", receivedHeader);

                if (!receivedHeader.Equals(this.sentHeader))
                {
                    // TODO: need to reconnect with the reply version if supported
                    throw new AmqpException(AmqpError.NotImplemented, SRClient.ProtocolVersionNotSupported(this.sentHeader, receivedHeader));
                }

                // upgrade transport
                TransportBase secureTransport = this.settings.TransportProviders[this.providerIndex].CreateTransport(args.Transport, true);
                Utils.Trace(TraceLevel.Info, "{0}: Upgrade transport {1} -> {2}.", this, args.Transport, secureTransport);
                args.Transport = secureTransport;
                IAsyncResult result = args.Transport.BeginOpen(this.timeoutHelper.RemainingTime(), this.OnTransportOpenCompete, args);
                if (result.CompletedSynchronously)
                {
                    this.HandleTransportOpened(result);
                }
            }
            catch (Exception exp)
            {
                if (Fx.IsFatal(exp))
                {
                    throw;
                }

                Utils.Trace(TraceLevel.Info, "{0}: exception: {1}", this, exp.Message);
                args.Exception = exp;
                this.Complete(args);
            }
        }

        void OnTransportOpenCompete(IAsyncResult result)
        {
            if (result.CompletedSynchronously)
            {
                return;
            }

            try
            {
                this.HandleTransportOpened(result);
            }
            catch (Exception exception)
            {
                if (Fx.IsFatal(exception))
                {
                    throw;
                }

                TransportAsyncCallbackArgs args = (TransportAsyncCallbackArgs)result.AsyncState;
                args.Exception = exception;
                this.Complete(args);
            }
        }

        void HandleTransportOpened(IAsyncResult result)
        {
            TransportAsyncCallbackArgs args = (TransportAsyncCallbackArgs)result.AsyncState;
            args.Transport.EndOpen(result);

            ++this.providerIndex;
            if (this.providerIndex == this.settings.TransportProviders.Count ||
                this.settings.TransportProviders[this.providerIndex].ProtocolId == ProtocolId.Amqp)
            {
                this.writer = null;
                this.reader = null;
                this.providerIndex = 0;
                this.Complete(args);
            }
            else
            {
                this.writer = new AsyncIO.AsyncBufferWriter(args.Transport);
                this.reader = new AsyncIO.AsyncBufferReader(args.Transport);
                this.WriteSecurityHeader(args);
            }
        }

        void Complete(TransportAsyncCallbackArgs args)
        {
            if (args.Exception != null && args.Transport != null)
            {
                args.Transport.TryClose(args.Exception);
                args.Transport = null;
            }

            TransportAsyncCallbackArgs innerArgs = (TransportAsyncCallbackArgs)args.UserToken;
            innerArgs.Transport = args.Transport;
            innerArgs.Exception = args.Exception;
            innerArgs.CompletedCallback(innerArgs);
        }
    }
}
