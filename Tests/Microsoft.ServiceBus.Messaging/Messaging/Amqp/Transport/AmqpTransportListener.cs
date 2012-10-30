//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp.Transport
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ServiceBus.Common;
    using Microsoft.ServiceBus.Messaging.Amqp;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;

    /// <summary>
    /// This listener supports protocol upgrade (e.g. tcp -> tls -> sasl)
    /// </summary>
    sealed class AmqpTransportListener : TransportListener
    {
        readonly List<TransportListener> innerListeners;
        readonly AmqpSettings settings;

        public AmqpTransportListener(IEnumerable<TransportListener> listeners, AmqpSettings settings)
        {
            this.innerListeners = new List<TransportListener>(listeners);
            this.settings = settings;
        }

        public AmqpSettings AmqpSettings
        {
            get { return this.settings; }
        }

        protected override string Type
        {
            get { return "tp-listener"; }
        }

        protected override void OnListen()
        {
            Action<TransportAsyncCallbackArgs> onTransportAccept = this.OnAcceptTransport;
            EventHandler onListenerClose = this.OnListenerClosed;
            foreach (TransportListener listener in this.innerListeners)
            {
                listener.Closed += onListenerClose;
                listener.Listen(onTransportAccept);
            }
        }

        protected override bool CloseInternal()
        {
            foreach (TransportListener listener in this.innerListeners.ToArray())
            {
                listener.Close();
            }

            return true;
        }

        protected override void AbortInternal()
        {
            foreach (TransportListener listener in this.innerListeners.ToArray())
            {
                listener.Abort();
            }
        }

        void OnListenerClosed(object sender, EventArgs e)
        {
            this.innerListeners.Remove((TransportListener)sender);
        }

        void OnAcceptTransport(TransportAsyncCallbackArgs args)
        {
            Utils.Trace(TraceLevel.Info, "{0}: Accepted a transport. Spawning a handler.", this);
            TransportHandler.SpawnHandler(this, args);
        }

        void OnHandleTransportComplete(TransportAsyncCallbackArgs args)
        {
            args.SetBuffer(null, 0, 0);
            args.CompletedCallback = null;

            if (args.Exception != null)
            {
                args.Transport.TryClose(args.Exception);
            }
            else
            {
                this.OnTransportAccepted(args);
            }
        }

        sealed class TransportHandler
        {
            readonly static AsyncCallback onTransportOpened = OnTransportOpened;

            readonly AmqpTransportListener parent;
            readonly TransportAsyncCallbackArgs args;
            readonly Action<TransportAsyncCallbackArgs> readCompleteCallback;
            readonly Action<TransportAsyncCallbackArgs> writeCompleteCallback;
            AsyncIO.AsyncBufferReader bufferReader;
            AsyncIO.AsyncBufferWriter bufferWriter;
            byte[] buffer;
            TimeoutHelper timeoutHelper;

            TransportHandler(AmqpTransportListener parent, TransportAsyncCallbackArgs args)
            {
                this.parent = parent;
                this.args = args;
                this.buffer = new byte[ProtocolHeader.Size];
                this.bufferReader = new AsyncIO.AsyncBufferReader(args.Transport);
                this.bufferWriter = new AsyncIO.AsyncBufferWriter(args.Transport);
                this.readCompleteCallback = this.OnReadHeaderComplete;
                this.writeCompleteCallback = this.OnWriteHeaderComplete;
                this.timeoutHelper = new TimeoutHelper(TimeSpan.FromSeconds(AmqpConstants.DefaultTimeout));
            }

            public static void SpawnHandler(AmqpTransportListener parent, TransportAsyncCallbackArgs args)
            {
                TransportHandler handler = new TransportHandler(parent, args);
                ActionItem.Schedule(Start, handler);
            }

            public override string ToString()
            {
                return "tp-handler";
            }

            static void Start(object state)
            {
                TransportHandler thisPtr = (TransportHandler)state;
                thisPtr.ReadProtocolHeader();
            }

            static void OnTransportOpened(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                TransportHandler thisPtr = (TransportHandler)result.AsyncState;
                try
                {
                    thisPtr.HandleTransportOpened(result);
                }
                catch (Exception exp)
                {
                    thisPtr.args.Exception = exp;
                    thisPtr.parent.OnHandleTransportComplete(thisPtr.args);
                }
            }

            void ReadProtocolHeader()
            {
                Utils.Trace(TraceLevel.Verbose, "{0}: reading protocol header from the transport", this);
                this.args.SetBuffer(this.buffer, 0, this.buffer.Length);
                this.args.CompletedCallback = this.readCompleteCallback;
                this.bufferReader.ReadBuffer(this.args);
            }

            void OnReadHeaderComplete(TransportAsyncCallbackArgs args)
            {
                if (args.Exception != null)
                {
                    this.parent.OnHandleTransportComplete(args);
                    return;
                }

                Utils.Trace(TraceLevel.Verbose, "{0}: Read protocol header completed", this);
                ByteBuffer buffer = ByteBuffer.Wrap(this.buffer, 0, this.buffer.Length);
                try
                {
                    this.OnProtocolHeader(buffer);
                }
                catch (Exception exp)
                {
                    args.Exception = exp;
                    this.parent.OnHandleTransportComplete(args);
                }
            }

            void OnProtocolHeader(ByteBuffer buffer)
            {
                ProtocolHeader header = ProtocolHeader.Decode(buffer);
                Utils.Trace(TraceLevel.Info, "{0}: Received a protocol header {1}.", this, header);

                // Protocol id negotiation
                TransportProvider provider = null;
                if (!this.parent.settings.TryGetTransportProvider(header, out provider))
                {
                    Fx.Assert(provider != null, "At least on provider should be configured.");
                    this.WriteReplyHeader(new ProtocolHeader(provider.ProtocolId, provider.DefaultVersion), true);
                    return;
                }

                // Protocol version negotiation
                AmqpVersion version;
                if (!provider.TryGetVersion(header.Version, out version))
                {
                    this.WriteReplyHeader(new ProtocolHeader(provider.ProtocolId, version), true);
                    return;
                }

                TransportBase newTransport = provider.CreateTransport(this.args.Transport, false);
                if (object.ReferenceEquals(newTransport, this.args.Transport))
                {
                    if ((this.parent.settings.RequireSecureTransport && !newTransport.IsSecure) ||
                        (!this.parent.settings.AllowAnonymousConnection && !newTransport.IsAuthenticated))
                    {
                        Utils.Trace(TraceLevel.Warning, 
                            "{0}: Transport {1} does not meet the security requirement (isSecure:{2}, isAuthenticated:{3}).", 
                            this.parent, 
                            newTransport,
                            newTransport.IsSecure,
                            newTransport.IsAuthenticated);
                        this.WriteReplyHeader(this.parent.settings.GetDefaultHeader(), true);
                    }
                    else
                    {
                        this.args.UserToken = header;
                        this.parent.OnHandleTransportComplete(this.args);
                    }
                }
                else
                {
                    Utils.Trace(TraceLevel.Frame, "RECV  {0}", header);
                    Utils.Trace(TraceLevel.Verbose, "{0}: Upgrade transport {1} -> {2}.", this, this.args.Transport, newTransport);
                    this.args.Transport = newTransport;
                    this.WriteReplyHeader(header, false);
                }
            }

            void HandleTransportOpened(IAsyncResult result)
            {
                this.args.Transport.EndOpen(result);
                this.bufferReader = new AsyncIO.AsyncBufferReader(this.args.Transport);
                this.bufferWriter = new AsyncIO.AsyncBufferWriter(this.args.Transport);
                this.ReadProtocolHeader();
            }

            void WriteReplyHeader(ProtocolHeader header, bool fail)
            {
                Utils.Trace(TraceLevel.Verbose, "{0}: Write reply protocol header {1}", this, header);
                Utils.Trace(TraceLevel.Frame, "SEND  {0}", header);
                this.args.SetBuffer(header.Buffer);
                this.args.CompletedCallback = fail ? null : this.writeCompleteCallback;
                this.bufferWriter.WriteBuffer(this.args);

                if (fail)
                {
                    this.args.Exception = new NotSupportedException(header.ToString());
                    this.parent.OnHandleTransportComplete(this.args);
                }
            }

            void OnWriteHeaderComplete(TransportAsyncCallbackArgs args)
            {
                Utils.Trace(TraceLevel.Verbose, "{0}: Write protocol header completed", this);
                if (args.Exception != null)
                {
                    this.parent.OnHandleTransportComplete(args);
                    return;
                }

                try
                {
                    IAsyncResult result = this.args.Transport.BeginOpen(this.timeoutHelper.RemainingTime(), onTransportOpened, this);
                    if (result.CompletedSynchronously)
                    {
                        this.HandleTransportOpened(result);
                    }
                }
                catch (Exception exp)
                {
                    args.Exception = exp;
                    this.parent.OnHandleTransportComplete(args);
                }
            }
        }
    }
}
