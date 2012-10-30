//----------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//----------------------------------------------------------------

namespace Microsoft.ServiceBus.Messaging.Amqp
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.ServiceBus.Common;
    using Microsoft.ServiceBus.Messaging.Amqp.Framing;
    using Microsoft.ServiceBus.Messaging.Amqp.Transport;

    public interface IPublisher
    {
        void Send(object value, Action<object> callback, object state);

        void Close();
    }

    public interface ISubscriber
    {
        object Receive();

        void Close();
    }

    public sealed class Container
    {
        readonly Uri addressUri;
        readonly string id;
        readonly object syncRoot;
        Listener listener;
        AmqpConnection connection;

        public Container(string address)
        {
            this.addressUri = new Uri(address);
            this.id = "C" + System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
            this.syncRoot = new object();
        }

        public Uri Address
        {
            get { return this.addressUri; }
        }

        public IPublisher CreatePublisher(string node)
        {
            return new Publisher(this, node);
        }

        /*public*/ ISubscriber CreateSubscriber(string node)
        {
            return new Subscriber(this, node);
        }

        /*public*/ IPublisher AcceptPublisher(string node)
        {
            this.EnsureListenerOpen();
            return this.listener.AcceptPublisher(node);
        }

        public ISubscriber AcceptSubscriber(string node)
        {
            this.EnsureListenerOpen();
            return this.listener.AcceptSubscriber(node);
        }

        public void Close()
        {
            if (this.connection != null)
            {
                this.connection.Close();
            }

            if (this.listener != null)
            {
                this.listener.Close();
            }
        }

        void EnsureConnectionOpen()
        {
            if (this.connection == null)
            {
                lock (this.syncRoot)
                {
                    if (this.connection == null)
                    {
                        this.connection = OpenContainerAsyncResult.End(new OpenContainerAsyncResult(this, null, null));
                    }
                }
            }
        }

        void EnsureListenerOpen()
        {
            if (this.listener == null)
            {
                lock (this.syncRoot)
                {
                    if (this.listener == null)
                    {
                        this.listener = new Listener(this);
                        this.listener.Open();
                    }
                }
            }
        }

        sealed class OpenContainerAsyncResult : AsyncResult
        {
            static readonly Action<TransportAsyncCallbackArgs> onTransport = OnTransport;
            static readonly AsyncCompletion onConnectionOpen = OnConnectionOpen;
            readonly Container parent;
            AmqpConnection connection;

            public OpenContainerAsyncResult(Container parent, AsyncCallback callback, object state)
                : base(callback, state)
            {
                this.parent = parent;

                int port = this.parent.addressUri.Port;
                if (port == -1)
                {
                    port = AmqpConstants.DefaultPort;
                }

                TcpTransportSettings tcpSettings = new TcpTransportSettings();
                tcpSettings.TcpBacklog = 20;
                tcpSettings.TcpBufferSize = 4096;
                tcpSettings.SetEndPoint(this.parent.addressUri.Host, port, false);
                TransportSettings transportSettings = tcpSettings;

                TransportInitiator initiator = transportSettings.CreateInitiator();
                TransportAsyncCallbackArgs args = new TransportAsyncCallbackArgs();
                args.CompletedCallback = onTransport;
                args.UserToken = this;
                if (!initiator.ConnectAsync(TimeSpan.MaxValue, args))
                {
                    OnTransport(args);
                }
            }

            public static AmqpConnection End(IAsyncResult result)
            {
                return AsyncResult.End<OpenContainerAsyncResult>(result).connection;
            }

            static void OnTransport(TransportAsyncCallbackArgs args)
            {
                OpenContainerAsyncResult thisPtr = (OpenContainerAsyncResult)args.UserToken;
                AmqpSettings settings = new AmqpSettings();
                TransportProvider provider = new AmqpTransportProvider();
                provider.Versions.Add(new AmqpVersion(1, 0, 0));
                settings.TransportProviders.Add(provider);
                thisPtr.connection = new AmqpConnection(args.Transport, settings, new AmqpConnectionSettings() { ContainerId = thisPtr.parent.id });
                thisPtr.connection.BeginOpen(TimeSpan.MaxValue, thisPtr.PrepareAsyncCompletion(onConnectionOpen), thisPtr);
            }

            static bool OnConnectionOpen(IAsyncResult result)
            {
                OpenContainerAsyncResult thisPtr = (OpenContainerAsyncResult)result.AsyncState;
                thisPtr.connection.EndOpen(result);
                return true;
            }
        }
        
        abstract class Terminus
        {
            static readonly WaitCallback establishLink = EstablishLink;
            static readonly AsyncCallback onCreateLink = OnCreateLink;
            readonly Container container;
            readonly string node;
            bool creatingLink;

            protected Terminus(Container container, string node)
            {
                this.container = container;
                this.node = node;
            }

            public AmqpLink Link
            {
                get;
                set;
            }

            protected string Node
            {
                get { return this.node;}
            }

            public void Close()
            {
                if (this.Link != null)
                {
                    this.Link.Session.Close();
                }
            }

            protected abstract AmqpLink CreateLink(AmqpSession session);

            protected abstract void OnLinkCreated();

            protected void EnsureLink()
            {
                if (this.Link == null)
                {
                    lock (this.node)
                    {
                        if (this.Link == null && !this.creatingLink)
                        {
                            this.creatingLink = true;
                            ThreadPool.QueueUserWorkItem(establishLink, this);
                        }
                    }
                }
            }

            static void EstablishLink(object state)
            {
                Terminus thisPtr = (Terminus)state;
                new OpenTerminusAsyncResult(thisPtr, onCreateLink, thisPtr);
            }

            static void OnCreateLink(IAsyncResult result)
            {
                Terminus thisPtr = (Terminus)result.AsyncState;
                thisPtr.Link = OpenTerminusAsyncResult.End(result);
                thisPtr.creatingLink = false;

                thisPtr.OnLinkCreated();
            }

            sealed class OpenTerminusAsyncResult : AsyncResult
            {
                readonly AsyncCallback onObjectOpen;
                readonly Terminus parent;
                AmqpSession session;
                AmqpLink link;

                public OpenTerminusAsyncResult(Terminus parent, AsyncCallback callback, object state)
                    : base(callback, state)
                {
                    this.parent = parent;
                    this.onObjectOpen = this.OnObjectOpen;

                    this.parent.container.EnsureConnectionOpen();
                    this.session = this.parent.container.connection.CreateSession(new AmqpSessionSettings());
                    this.link = this.parent.CreateLink(this.session);
                    this.session.BeginOpen(TimeSpan.MaxValue, this.onObjectOpen, this.session);
                    this.link.BeginOpen(TimeSpan.MaxValue, this.onObjectOpen, this.link);
                }

                public static AmqpLink End(IAsyncResult result)
                {
                    return AsyncResult.End<OpenTerminusAsyncResult>(result).link;
                }

                void OnObjectOpen(IAsyncResult result)
                {
                    AmqpObject amqpObject = (AmqpObject)result.AsyncState;
                    amqpObject.EndOpen(result);
                    if (amqpObject is AmqpLink)
                    {
                        this.Complete(false);
                    }
                }
            }
        }

        sealed class Publisher : Terminus, IPublisher
        {
            readonly SerializedWorker<AmqpMessage> sender;

            public Publisher(Container container, string node)
                : base(container, node)
            {
                this.sender = new SerializedWorker<AmqpMessage>(this.OnSend, null, false);
            }

            public void Send(object value, Action<object> callback, object state)
            {
                this.EnsureLink();
                AmqpMessage message = AmqpMessage.Create(new AmqpValue() { Value = value });
                message.CompleteCallback = callback;
                message.UserToken = state;
                this.sender.DoWork(message);
            }

            protected override AmqpLink CreateLink(AmqpSession session)
            {
                AmqpLinkSettings settings = new AmqpLinkSettings();
                settings.LinkName = Guid.NewGuid().ToString("N");
                settings.Target = new Target() { Address = this.Node };
                settings.Role = false;
                settings.SettleType = SettleMode.SettleOnSend;
                return new SendingAmqpLink(session, settings);
            }

            protected override void OnLinkCreated()
            {
                this.sender.ContinueWork();
            }

            bool OnSend(AmqpMessage message)
            {
                if (this.Link == null)
                {
                    return false;
                }

                this.Link.SendDelivery(message);
                return true;
            }
        }

        sealed class Subscriber : Terminus, ISubscriber
        {
            readonly InputQueue<AmqpMessage> messages;

            public Subscriber(Container container, string node)
                : base(container, node)
            {
                this.messages = new InputQueue<AmqpMessage>();
            }

            internal Action<AmqpMessage> MessageListener
            {
                get { return this.OnMessage; }
            }

            public object Receive()
            {
                AmqpMessage message = this.messages.Dequeue(TimeSpan.MaxValue);
                if (message.BodyType == SectionFlag.AmqpValue)
                {
                    return message.ValueBody.Value;
                }
                else if (message.BodyType == SectionFlag.Data)
                {
                    return message.BodyStream;
                }

                throw new NotSupportedException("message.body");
            }

            protected override AmqpLink CreateLink(AmqpSession session)
            {
                AmqpLinkSettings settings = new AmqpLinkSettings();
                settings.LinkName = Guid.NewGuid().ToString("N");
                settings.Source = new Source() { Address = this.Node };
                settings.Role = true;
                ReceivingAmqpLink link = new ReceivingAmqpLink(session, settings);
                link.RegisterMessageListener(this.OnMessage);
                return link;
            }

            protected override void OnLinkCreated()
            {
            }

            void OnMessage(AmqpMessage message)
            {
                this.messages.EnqueueAndDispatch(message, null, false);
                this.Link.DisposeDelivery(message, true, message.State);
            }
        }

        sealed class Listener : IRuntimeProvider
        {
            readonly Container container;
            readonly List<AmqpConnection> connections;
            readonly AmqpSettings settings;
            readonly Dictionary<string, List<LinkAsyncResult>> nodes;
            readonly object syncRoot;
            TransportListener listener;

            public Listener(Container container)
            {
                this.container = container;
                this.connections = new List<AmqpConnection>();
                this.settings = new AmqpSettings() { RuntimeProvider = this };
                TransportProvider provider = new AmqpTransportProvider();
                provider.Versions.Add(new AmqpVersion(1, 0, 0));
                this.settings.TransportProviders.Add(provider);
                this.nodes = new Dictionary<string, List<LinkAsyncResult>>(StringComparer.OrdinalIgnoreCase);
                this.syncRoot = new object();
            }

            public void Open()
            {
                int port = this.container.addressUri.Port;
                if (port == -1)
                {
                    port = AmqpConstants.DefaultPort;
                }

                TcpTransportSettings tcpSettings = new TcpTransportSettings();
                tcpSettings.TcpBacklog = 20;
                tcpSettings.TcpBufferSize = 4096;
                tcpSettings.SetEndPoint(this.container.addressUri.Host, port, true);
                TransportSettings transportSettings = tcpSettings;

                this.listener = transportSettings.CreateListener();
                this.listener.Listen(this.OnAcceptTransport);
            }

            public void Close()
            {
                if (this.listener != null)
                {
                    this.listener.Close();
                }
            }

            public IPublisher AcceptPublisher(string node)
            {
                Publisher publisher = new Publisher(this.container, node);
                publisher.Link = this.AcceptLink(node, false, null);
                return publisher;
            }

            public ISubscriber AcceptSubscriber(string node)
            {
                Subscriber subscriber = new Subscriber(this.container, node);
                subscriber.Link = this.AcceptLink(node, true, subscriber.MessageListener);
                return subscriber;
            }

            void OnAcceptTransport(TransportAsyncCallbackArgs args)
            {
                if (args.Exception != null)
                {
                    return;
                }

                AmqpConnection connection = new AmqpConnection(args.Transport, this.settings, new AmqpConnectionSettings() { ContainerId = this.container.id });
                connection.Closed += new EventHandler(connection_Closed);
                lock (this.syncRoot)
                {
                    this.connections.Add(connection);
                }

                connection.Open();
            }

            void connection_Closed(object sender, EventArgs e)
            {
                lock (this.syncRoot)
                {
                    this.connections.Remove((AmqpConnection)sender);
                }
            }

            bool TryMatchLink(string node, bool fromClient, bool isReceiver, Func<bool, LinkAsyncResult> requestCreator, out LinkAsyncResult result)
            {
                result = null;
                bool matched = false;
                lock (this.syncRoot)
                {
                    List<LinkAsyncResult> requests;
                    if (!this.nodes.TryGetValue(node, out requests))
                    {
                        requests = new List<LinkAsyncResult>();
                        this.nodes.Add(node, requests);
                    }

                    for (int i = 0; i < requests.Count; ++i)
                    {
                        result = requests[i];
                        if (result.Match(fromClient, isReceiver))
                        {
                            requests.RemoveAt(i);
                            matched = true;
                            break;
                        }
                    }

                    if (!matched)
                    {
                        result = requestCreator(isReceiver);
                        requests.Add(result);
                    }
                }

                return matched;
            }

            // Find a matching client side link; otherwise wait
            AmqpLink AcceptLink(string node, bool isReceiver, Action<AmqpMessage> messageListener)
            {
                LinkAsyncResult result = null;
                if (this.TryMatchLink(node, true, isReceiver, (r) => { return new AcceptLinkAsyncResult(r, messageListener, null, null); }, out result))
                {
                    if (isReceiver)
                    {
                        ReceivingAmqpLink link = (ReceivingAmqpLink)result.Link;
                        link.RegisterMessageListener(messageListener);
                    }

                    result.Signal(null);
                    return result.Link;
                }

                return AcceptLinkAsyncResult.End(result);
            }

            AmqpConnection IConnectionFactory.CreateConnection(TransportBase transport, ProtocolHeader protocolHeader, bool isInitiator, AmqpSettings amqpSettings, AmqpConnectionSettings connectionSettings)
            {
                throw new NotImplementedException();
            }

            AmqpSession ISessionFactory.CreateSession(AmqpConnection connection, AmqpSessionSettings settings)
            {
                throw new NotImplementedException();
            }

            AmqpLink ILinkFactory.CreateLink(AmqpSession session, AmqpLinkSettings settings)
            {
                bool isReceiver = settings.Role.Value;
                if (isReceiver)
                {
                    settings.TransferLimit = uint.MaxValue;
                    return new ReceivingAmqpLink(session, settings);
                }
                else
                {
                    return new SendingAmqpLink(session, settings);
                }
            }

            IAsyncResult ILinkFactory.BeginOpenLink(AmqpLink link, TimeSpan timeout, AsyncCallback callback, object state)
            {
                return new OpenLinkAsyncResult(this, link, callback, state);
            }

            void ILinkFactory.EndOpenLink(IAsyncResult result)
            {
                OpenLinkAsyncResult.End(result);
            }

            abstract class LinkAsyncResult : AsyncResult
            {
                protected LinkAsyncResult(AsyncCallback callback, object state)
                    : base(callback, state)
                {
                }

                public AmqpLink Link
                {
                    get;
                    protected set;
                }

                public static AmqpLink End(IAsyncResult result)
                {
                    return AsyncResult.End<LinkAsyncResult>(result).Link;
                }

                public void Signal(AmqpLink link)
                {
                    this.OnSignal(link);
                    this.Complete(false);
                }

                public abstract bool Match(bool fromClient, bool isReceiver);

                protected abstract void OnSignal(AmqpLink link);
            }

            sealed class AcceptLinkAsyncResult : LinkAsyncResult
            {
                readonly bool isReceiver;
                readonly Action<AmqpMessage> messageListener;

                public AcceptLinkAsyncResult(bool isReceiver, Action<AmqpMessage> messageListener, AsyncCallback callback, object state)
                    : base(callback, state)
                {
                    this.isReceiver = isReceiver;
                    this.messageListener = messageListener;
                }

                public override bool Match(bool fromClient, bool isReceiver)
                {
                    return !fromClient && this.isReceiver == isReceiver;
                }

                protected override void OnSignal(AmqpLink link)
                {
                    Fx.Assert(link != null, "Accept cannot be completed with a null link");
                    this.Link = link;
                    if (this.isReceiver)
                    {
                        ((ReceivingAmqpLink)link).RegisterMessageListener(this.messageListener);
                    }
                }
            }

            sealed class OpenLinkAsyncResult : LinkAsyncResult
            {
                public OpenLinkAsyncResult(Listener listener, AmqpLink link, AsyncCallback callback, object state)
                    : base(callback, state)
                {
                    this.Link = link;

                    string node = link.IsReceiver ? ((Target)link.Settings.Target).Address.ToString() : ((Source)link.Settings.Source).Address.ToString();
                    LinkAsyncResult result = null;
                    if (listener.TryMatchLink(node, false, link.IsReceiver, (r) => { return this; }, out result))
                    {
                        result.Signal(link);

                        this.Complete(true);
                    }
                }

                public override bool Match(bool fromClient, bool isReceiver)
                {
                    return fromClient && this.Link.IsReceiver == isReceiver;
                }

                protected override void OnSignal(AmqpLink link)
                {
                }
            }
        }
    }
}
