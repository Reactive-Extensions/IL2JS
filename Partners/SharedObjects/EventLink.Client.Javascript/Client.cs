namespace Microsoft.Csa.EventLink.Client
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Diagnostics;
    using System.Runtime.Serialization;
    using System.Net;
    using System.IO;
    using System.Threading;

#if SILVERLIGHT
    using System.Net.Browser;
#endif

    using Microsoft.Csa.EventLink;
    using Microsoft.Csa.SharedObjects.Utilities;
    using Microsoft.Csa.SharedObjects;

    using ErrorStateChangeCallback = System.Action<EventLinkClient.ErrorState, System.Exception>;

    public delegate bool ShouldRetry(int retryCount, Exception lastException, out TimeSpan delay);

    internal static class RetryPolicies
    {
        internal static readonly TimeSpan DefaultClientBackoff;
        internal static readonly int DefaultClientRetryCount;
        internal static readonly TimeSpan DefaultMaxBackoff;
        internal static readonly TimeSpan DefaultMinBackoff;

        static RetryPolicies()
        {
            DefaultMinBackoff = TimeSpan.FromSeconds(3.0);
            DefaultMaxBackoff = TimeSpan.FromSeconds(90.0);
            DefaultClientBackoff = TimeSpan.FromSeconds(30.0);
            DefaultClientRetryCount = 3;
        }

        internal static ShouldRetry NoRetry()
        {
            return delegate(int retryCount, Exception lastException, out TimeSpan retryInterval)
            {
                retryInterval = TimeSpan.Zero;
                return false;
            };
        }

        internal static ShouldRetry Retry(int retryCount, TimeSpan intervalBetweenRetries)
        {
            return delegate(int currentRetryCount, Exception lastException, out TimeSpan retryInterval)
            {
                retryInterval = intervalBetweenRetries;
                return (currentRetryCount < retryCount);
            };
        }
    }
 
    public class EventLinkClient
    {
        public enum ErrorState
        {
            None = 0,
            Temporary,
            Permanent
        }

        private class CommonClient
        {
            protected const string XmlContentType = "text/xml";
            protected const string JsonContentType = "application/json";

            protected EventLinkClient eventLinkClient;

            // Base uri to service
            private Uri baseUri;

            // the event paritition (maps to backend server)
            private string partitionId;

            protected int retryCount;
            protected Timer retryTimer;

            public CommonClient(EventLinkClient eventLinkClient, Uri baseUri, string partitionId)
            {
                this.eventLinkClient = eventLinkClient;
                this.baseUri = baseUri;
                this.partitionId = partitionId;
            }

            // Abort the request if the timer fires.
            protected static void TimeoutCallback(object state, bool timedOut)
            {
                if (timedOut)
                {
                    HttpWebRequest request = state as HttpWebRequest;
                    if (request != null)
                    {
                        request.Abort();
                    }
                }
            }

            protected void RunAsyncWriteRequest(string uri, string method, Action<Stream> writeCallback, Action<Exception> completionCallback)
            {
                HttpWebRequest req = CreateHttpWebRequest(uri, method, JsonContentType);

                IAsyncResult result = req.BeginGetRequestStream(ar =>
                {
                    try
                    {
                        using (Stream requestStream = req.EndGetRequestStream(ar))
                        {
                            writeCallback(requestStream);
                        }

                        GetAsyncResponse(req, completionCallback);
                    }
                    catch (Exception ex)
                    {
                        completionCallback(ex);
                    }
                }, null);
#if !IL2JS
                ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, TimeoutCallback, req, eventLinkClient.MillisecondsNetworkTimeout, true);
#endif
            }

            private void GetAsyncResponse(HttpWebRequest request, Action<Exception> completionCallback)
            {
                IAsyncResult result = request.BeginGetResponse(ar =>
                {
                    WebResponse response = null;
                    try
                    {
                        response = request.EndGetResponse(ar);
                        completionCallback(null);
                    }
                    catch (Exception ex)
                    {
                        completionCallback(ex);
                    }
                    finally
                    {
                        if (response != null)
                        {
                            response.Close();
                        }
                    }
                }, null);

#if !IL2JS
                ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, TimeoutCallback, request, eventLinkClient.MillisecondsNetworkTimeout, true);
#endif
            }

            protected string CreateEventsUri(string id)
            {
                return CreateServiceUri("Events", id);
            }

            protected string CreateServiceUri(string service, string id)
            {
                string relativeUri = string.Format("{0}/{1}", service, partitionId);
                if (!string.IsNullOrEmpty(id))
                {
                    relativeUri = string.Concat(relativeUri, "/", id);
                }
                Uri serviceUri = new Uri(baseUri, relativeUri);
                return serviceUri.ToString();
            }

            protected static HttpWebRequest CreateHttpWebRequest(string uri, string method, string contentType)
            {
#if SILVERLIGHT
                // For Silverlight, use the ClientHttp stack so we can support more HTTP verbs like PUT and DELETE
                HttpWebRequest req =
                    (HttpWebRequest)WebRequestCreator.ClientHttp.Create(new Uri(uri));
#else
                HttpWebRequest req =
                    (HttpWebRequest)HttpWebRequest.Create(new Uri(uri));

                req.KeepAlive = true;
#endif

                if (method == "GET")
                {
                    req.Accept = contentType;
                }
                else
                {
                    req.Method = method;
                    req.ContentType = contentType;
                }

                return req;
            }
        }

        private class SubscribeClient : CommonClient
        {
            public enum SubscribeState
            {
                Initial = 0,
                Subscribing,
                Reading,
                Unsubscribing,
                Unsubscribed,
                TempError,
                PermError
            }

            private SubscribeState state;
            public SubscribeState State {
                get { return state; }
            }

            // event link subscription id
            private string SubscriptionId { get; set; }

            // callback used to deliver events.
            private Action<EventSet[]> eventsReceived;

            // high watermark of event sequencing
            private long watermark;

            // currently running long poll request
            private HttpWebRequest currentReadRequest;

            public SubscribeClient(EventLinkClient eventLinkClient, Uri baseUri, string partitionId)
            :   base(eventLinkClient, baseUri, partitionId)
            {
            }

            private void SetState(SubscribeState newState)
            {
                state = newState;
            }

            private void OnSubscribe()
            {
                switch (State)
                {
                    case SubscribeState.Initial:
                        SetState (SubscribeState.Subscribing);
                        break;
                    default:
                        throw new InvalidOperationException("Subscribe called on EventLinkClient not in the initial state.");
                }
            }

            private void OnSubscribeComplete()
            {
                switch (State)
                {
                    case SubscribeState.Subscribing:
                        SetState(SubscribeState.Reading);
                        break;
                    default:
                        throw new InvalidOperationException("OnSubscribeComplete called on EventLinkClient in an unallowed state.");
                }
            }

            private void OnGetCompleted()
            {
                switch (State)
                {
                    case SubscribeState.Reading:
                    case SubscribeState.TempError:
                        retryCount = 0;
                        SetState(SubscribeState.Reading);
                        break;
                    case SubscribeState.Unsubscribed:
                        break;
                    default:
                        throw new InvalidOperationException("OnGetCompleted called on EventLinkClient not in a allowed state.");
                }
            }

            private void OnUnsubscribe()
            {
                switch (State)
                {
                    case SubscribeState.Subscribing:
                    case SubscribeState.Reading:
                    case SubscribeState.TempError:
                        SetState (SubscribeState.Unsubscribing);
                        break;
                    case SubscribeState.PermError:
                        break;
                    default:
                        throw new InvalidOperationException("Unsubscribe called on EventLinkClient in an unallowed state.");
                }
            }

            private void OnUnsubscribeComplete()
            {
                switch (State)
                {
                    case SubscribeState.Unsubscribing:
                        SetState(SubscribeState.Unsubscribed);
                        break;
                    case SubscribeState.PermError:
                        break;
                    default:
                        throw new InvalidOperationException("OnUnsubscribeComplete called on EventLinkClient in an unallowed state.");
                }
            }

            private void OnError(Exception ex)
            {
                switch (State)
                {
                    case SubscribeState.Subscribing:
                    case SubscribeState.Unsubscribing:
                        SetState(SubscribeState.PermError);
                        break;
                    case SubscribeState.Reading:
                    case SubscribeState.TempError:
                        TimeSpan delay;
                        if (eventLinkClient.RetryPolicy(retryCount, ex, out delay))
                        {
                            SetState(SubscribeState.TempError);
                            ++retryCount;
                            retryTimer = new Timer(RetryTimerCallback, null, delay, new TimeSpan(Timeout.Infinite));
                        }
                        else
                        {
                            SetState(SubscribeState.PermError);
                        }
                        break;
                    default:
                        throw new InvalidOperationException("OnError called on EventLinkClient in an unallowed state.");
                }
            }

            private void OnPermanentError()
            {
                SetState(SubscribeState.PermError);
            }

            public ErrorState GetErrorState()
            {
                switch (State)
                {
                    case SubscribeState.TempError:
                        return ErrorState.Temporary;
                    case SubscribeState.PermError:
                        return ErrorState.Permanent;
                    default:
                        return ErrorState.None;
                }
            }

            public void PermanentError()
            {
                lock (this)
                {
                    OnPermanentError();
                }
            }

            public void Subscribe(string id, Action<EventSet[]> eventsCallback, Action completionCallback)
            {                
                OnSubscribe();

                this.SubscriptionId = id;
                this.eventsReceived = eventsCallback;

                RunAsyncWriteRequest(
                    CreateSubscriptionUri(),
                    "PUT",
                    stream =>
                    {
                        using (StreamWriter sw = new StreamWriter(stream))
                        {
                            sw.Write(string.Format("\"{0}\"", id));
                        }
                    },
                    error =>
                    {                        
                        SubscribeCompletionCallback(error, completionCallback);
                    }
                );
            }

            private void SubscribeCompletionCallback(Exception error, Action completionCallback)
            {
                bool succeeded = false;
                bool callOnErrorStateChange = false;

                lock (this)
                {
                    if (State == SubscribeState.Subscribing)
                    {
                        if (error == null)
                        {
                            OnSubscribeComplete();
                            succeeded = true;
                        }
                        else
                        {
                            OnError(error);
                            callOnErrorStateChange = true;
                        }
                    }
                }

                if (succeeded)
                {
                    GetEvents();
                    completionCallback();
                }
                if (callOnErrorStateChange)
                {
                    eventLinkClient.SubscribeErrorStateChanged(GetErrorState(), error);
                }
            }

            public void Unsubscribe(Action completionCallback)
            {
                HttpWebRequest readRequest = null;
                bool sendUnsubscribe = false;
                lock (this)
                {
                    OnUnsubscribe();

                    if (this.currentReadRequest != null)
                    {
                        readRequest = this.currentReadRequest;
                        this.currentReadRequest = null;
                    }

                    if (State == SubscribeState.Unsubscribing)
                        sendUnsubscribe = true;
                }

                if (readRequest != null)
                    readRequest.Abort();

                if (sendUnsubscribe)
                {
                    RunAsyncWriteRequest(
                        CreateSubscriptionUri(this.SubscriptionId),
                        "DELETE",
                        stream => { }, // don't actually need to write anything to the stream
                        ex => UnsubscribeCompletionCallback(ex, completionCallback));
                }
                else if (completionCallback != null)
                {
                    completionCallback();
                }
            }

            private void UnsubscribeCompletionCallback(Exception error, Action completionCallback)
            {
                try
                {
                    bool callOnErrorStateChange = false;
                    lock (this)
                    {
                        if (error == null)
                        {
                            OnUnsubscribeComplete();
                        }
                        else
                        {
                            if (State == SubscribeState.Unsubscribing)
                            {
                                OnError(error);
                                callOnErrorStateChange = true;
                            }
                        }
                    }

                    if (callOnErrorStateChange)
                        eventLinkClient.SubscribeErrorStateChanged(GetErrorState(), error);
                }
                finally
                {
                    if (completionCallback != null)
                        completionCallback();
                }
            }

            private string CreateSubscriptionUri(string id = null)
            {
                return CreateServiceUri("Subscriptions", id);
            }

            private void GetEvents()
            {
                // Construct uri for polling events
                string uri = CreateEventsUri(this.SubscriptionId);
                bool getEvents = false;

                lock (this)
                {
                    if (State == SubscribeState.Reading || State == SubscribeState.TempError)
                        getEvents = true;
                }

                if (getEvents)
                    this.currentReadRequest = RunAsyncReadRequest(uri);
            }

            private HttpWebRequest RunAsyncReadRequest(string uri)
            {
                HttpWebRequest req = CreateHttpWebRequest(uri, "GET", JsonContentType);
                req.Headers[HttpRequestHeader.IfNoneMatch] = this.watermark.ToString();

                IAsyncResult result = req.BeginGetResponse(ar =>
                {
                    try
                    {                        
                        HttpWebResponse response = req.EndGetResponse(ar) as HttpWebResponse;                        
                        if (string.IsNullOrEmpty(response.Headers["ETag"]))
                        {
                            Console.WriteLine("NO ETag - maybe the result of an OPTIONS check for security?");
                            OnGetEventsResponse(null, null);
                        }
                        else
                        {                            
                            long watermark = long.Parse(response.Headers["ETag"]);
                            using (Stream responseStream = response.GetResponseStream())
                            {
                                OnGetEventsResponse(responseStream, null);
                            }
                            this.watermark = watermark;
                        }
                    }
                    catch (WebException wex)
                    {
                        HttpWebResponse response = wex.Response as HttpWebResponse;                        
                        if (response != null && response.StatusCode == HttpStatusCode.NotModified)
                        {
                            Console.WriteLine("Not modified");
                            OnGetEventsResponse(null, null);
                        }
                        else
                        {
                            OnGetEventsResponse(null, wex);
                        }
                    }
                    catch (Exception ex)
                    {
                        OnGetEventsResponse(null, ex);
                    }
                }, null);

#if !IL2JS
                ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, TimeoutCallback, req, eventLinkClient.MillisecondsNetworkTimeout, true);
#endif

                return req;
            }

            private void OnGetEventsResponse(Stream responseStream, Exception error)
            {
                // TODO ransomr to get optimal parallelism we should start the next read before processing these events.
                // That would require a lock to ensure the ordering of the eventRecieved calls.
                if (error == null)
                {
                    if (responseStream != null)
                    {
                        IPayloadReader reader = new JsonPayloadReader(responseStream);
                        // TODO: johnburk - avoid extra copy in ToArray
                        EventSet[] events = reader.ReadList<EventSet>(string.Empty).ToArray();

                        if (events != null && events.Length > 0)
                        {
                            VerifyEventSequencing(events);

                            this.watermark = events[events.Length - 1].Sequence;

                            if (this.eventsReceived != null)
                            {
                                this.eventsReceived(events);
                            }
                        }
                    }

                    // if still subscribed, do another get
                    lock (this)
                    {
                        OnGetCompleted();
                    }
                    GetEvents();
                }
                else
                {
                    bool callOnErrorStateChange = false;
                    lock (this)
                    {
                        if (State == SubscribeState.Reading || State == SubscribeState.TempError)
                        {
                            OnError(error);
                            callOnErrorStateChange = true;
                        }
                    }
                    if (callOnErrorStateChange)
                        eventLinkClient.SubscribeErrorStateChanged(GetErrorState(), error);
                }
            }

            private void RetryTimerCallback(Object o)
            {
                bool getEvents = false;
                lock (this)
                {
                    if (State == SubscribeState.TempError)
                        getEvents = true;
                }
                if (getEvents)
                   GetEvents();
            }

            [Conditional("DEBUG")]
            private void VerifyEventSequencing(EventSet[] events)
            {
                // make sure that events always come in ascending sequence order
                long wm = events[0].Sequence;
                Debug.Assert(wm == this.watermark + 1);
                for (int i = 1; i < events.Length; ++i)
                {
                    Debug.Assert(events[i].Sequence == events[i - 1].Sequence + 1);
                }
            }

            internal void TestOnlySetSubscriptionId(string value)
            {
                SubscriptionId = value;
            }
        }

        private class PublishClient : CommonClient
        {
            public enum PublishState
            {
                Idle = 0,
                Publishing,
                TempError,
                PermError
            }

            private PublishState state;
            public PublishState State
            {
                get { return state; }
            }

            // TODO: where does this need to be configurable?
            private const int MaxMessagePayloadSize = 1 * 1024 * 1024;

            // The outgoing queue of events
            private OutgoingEventQueue outgoingQueue;

            public PublishClient(EventLinkClient eventLinkClient, Uri baseUri, string partitionId)
                : base(eventLinkClient, baseUri, partitionId)
            {
                this.outgoingQueue = new OutgoingEventQueue(MaxMessagePayloadSize);
            }

            private void SetState(PublishState newState)
            {
                state = newState;
            }

            private void OnPublish()
            {
                switch (State)
                {
                    case PublishState.Idle:
                        SetState(PublishState.Publishing);
                        break;
                    case PublishState.Publishing:
                    case PublishState.TempError:
                        break;
                    default:
                        throw new InvalidOperationException("OnPublish called on EventLinkClient not in a allowed state.");
                }
            }

            private void OnPublishCompleted()
            {
                switch (State)
                {
                    case PublishState.Publishing:
                    case PublishState.TempError:
                        retryCount = 0;
                        if (outgoingQueue.Count > 0)
                        {
                            SetState(PublishState.Publishing);
                        }
                        else
                        {
                            OnQueueEmpty();
                        }
                        break;
                    default:
                        throw new InvalidOperationException("OnPublishCompleted called on EventLinkClient not in a allowed state.");
                }
            }

            private void OnQueueEmpty()
            {
                switch (State)
                {
                    case PublishState.Publishing:
                    case PublishState.TempError:
                        SetState(PublishState.Idle);
                        break;
                    default:
                        throw new InvalidOperationException("OnQueueEmpty called on EventLinkClient not in a allowed state.");
                }
            }

            private void OnError(Exception ex)
            {
                switch (State)
                {
                    case PublishState.Publishing:
                    case PublishState.TempError:
                        this.outgoingQueue.AbortDequeue();
                        TimeSpan delay;
                        if (eventLinkClient.RetryPolicy(retryCount, ex, out delay))
                        {
                            SetState(PublishState.TempError);
                            ++retryCount;
                            retryTimer = new Timer(RetryTimerCallback, null, delay, new TimeSpan(Timeout.Infinite));
                        }
                        else
                        {
                            SetState(PublishState.PermError);
                        }
                        break;
                    default:
                        throw new InvalidOperationException("OnError called on EventLinkClient in an unallowed state.");
                }
            }

            private void OnPermanentError()
            {
                SetState(PublishState.PermError);
            }

            public ErrorState GetErrorState()
            {
                switch (State)
                {
                    case PublishState.TempError:
                        return ErrorState.Temporary;
                    case PublishState.PermError:
                        return ErrorState.Permanent;
                    default:
                        return ErrorState.None;
                }
            }

            public void PermanentError()
            {
                lock (this)
                {
                    OnPermanentError();
                }
            }

            public void Publish(string channelName, Payload[] payloads)
            {
                EventSet eventSet = new EventSet { ChannelName = channelName, Payloads = payloads };
                bool shouldPublish = false;
                
                lock (this)
                {
                    if (State == PublishState.PermError)
                    {
                        throw new Microsoft.Csa.SharedObjects.ClientDisconnectedException("Publish called on EventLinkClient in a permanent error state.");
                    }

                    this.outgoingQueue.Enqueue(eventSet);
                    if (State == PublishState.Idle)
                    {
                        shouldPublish = true;
                    }
                    OnPublish();
                }
                
                if (shouldPublish)
                {
                    PublishEvents();
                }
            }

            private void RetryTimerCallback(Object o)
            {
                bool publishEvents = false;
                lock (this)
                {
                    if (State == PublishState.TempError)
                        publishEvents = true;
                }
                if (publishEvents)
                    PublishEvents();
            }

            private void PublishEvents()
            {
                RunAsyncWriteRequest(
                    CreateEventsUri("unused"),
                    "POST",
                    stream =>
                    {
                        try
                        {
                            WriteEvents(stream);
                        }
                        catch (Exception)
                        {
                            // TODO: error handling
                            throw;
                        }
                    },
                    PublishCompleteCallback
                );
            }

            private void PublishCompleteCallback(Exception error)
            {
                bool callOnErrorStateChange = false;
                bool shouldPublish = false;
                lock (this)
                {
                    if (error == null)
                    {
                        this.outgoingQueue.CommitDequeue();
                        OnPublishCompleted();
                    }
                    else
                    {
                        if (State == PublishState.Publishing || State == PublishState.TempError)
                        {
                            OnError(error);
                            callOnErrorStateChange = true;
                        }
                    }
                    shouldPublish = (State == PublishState.Publishing);
                }
                if (callOnErrorStateChange)
                {
                    eventLinkClient.PublishErrorStateChanged(GetErrorState(), error);
                }
                else if (shouldPublish)
                {
                    PublishEvents();
                }
            }
//#if IL2JS
//            private void WriteEvents(Stream stream)
//            {
//                string connectPayload = "[{\"ChannelName\":\"GlobalListening\",\"Payloads\":[{\"PayloadType\":8,\"ClientId\":\"99ca2aff-538e-49f2-a71c-79b7720e3f21\",\"SubscriptionId\":\"99ca2aff-538e-49f2-a71c-79b7720e3f21ClientControl\",\"SharedObjectNamespace\":\"3bb04637-af98-40e9-ad65-64fb2668a0d2\",\"PrincipalPayload\":{\"PayloadType\":14,\"PayloadId\":1,\"ClientId\":\"99ca2aff-538e-49f2-a71c-79b7720e3f21\",\"Id\":\"25a3aeaa-4771-4e91-9d62-b7ea19409c16\",\"Name\":\"25a3aeaa-4771-4e91-9d62-b7ea19409c16\",\"Attributes\":{},\"Type\":\"Microsoft.Csa.SharedObjects.PrincipalObject, Microsoft.Csa.SharedObjects.Core, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\",\"SharedProperties\":[{\"Name\":\"Id\",\"ETag\":{\"ClientId\":\"99ca2aff-538e-49f2-a71c-79b7720e3f21\"},\"Attributes\":{\"PropertyFlags\":1},\"Value\":\"\\\"ClientA\"\\\"},{\"Name\":\"Sid\",\"Index\":1,\"ETag\":{\"ClientId\":\"99ca2aff-538e-49f2-a71c-79b7720e3f21\"},\"Attributes\":{\"PropertyFlags\":2},\"Value\":\"\\\"sa\"\\\"}]},\"Version\":{\"Major\":1}}]}]";

//                using (StreamWriter sw = new StreamWriter(stream))
//                {
//                    sw.Write(connectPayload);
//                }
//            }
//#else
            private void WriteEvents(Stream stream)
            {
                // attempt to write events to stream:
                EventSet[] outgoingEvents;
                lock (this)
                {
                    outgoingEvents = this.outgoingQueue.BeginDequeue();
                }

                // This should not happen
                Debug.Assert(outgoingEvents.Length > 0, "Attempt to publish empty event set");

                using (IPayloadWriter writer = new JsonPayloadWriter(stream))
                {
                    writer.Write(string.Empty, outgoingEvents);
                }
            }
        }

        private SubscribeClient subscribeClient;
        private PublishClient publishClient;

        private ErrorStateChangeCallback onErrorStateChange;

        internal ShouldRetry RetryPolicy { get; set; }
        private TimeSpan millisecondsNetworkTimeout = TimeSpan.FromMinutes(2);
        internal TimeSpan MillisecondsNetworkTimeout { get { return millisecondsNetworkTimeout; } set { millisecondsNetworkTimeout = value; } }

        public EventLinkClient(Uri baseUri, string partitionId, ErrorStateChangeCallback callback)
        {
            subscribeClient = new SubscribeClient(this, baseUri, partitionId);
            publishClient = new PublishClient(this, baseUri, partitionId);
            onErrorStateChange = callback;
            RetryPolicy = RetryPolicies.Retry(RetryPolicies.DefaultClientRetryCount, RetryPolicies.DefaultClientBackoff);
        }

        public void Subscribe(string id, Action<EventSet[]> eventsCallback, Action completionCallback)
        {
            subscribeClient.Subscribe(id, eventsCallback, completionCallback);
        }

        public void Unsubscribe(Action completionCallback)
        {
            subscribeClient.Unsubscribe(completionCallback);
        }

        public void Publish(string channelName, Payload[] payloads)
        {
            publishClient.Publish(channelName, payloads);
        }

        internal void TestOnlySetSubscriptionId(string value)
        {
            subscribeClient.TestOnlySetSubscriptionId(value);
        }

        private ErrorState GetErrorState()
        {
            ErrorState SubscribeErrorState = subscribeClient.GetErrorState();
            ErrorState PublishErrorState = publishClient.GetErrorState();
            if (SubscribeErrorState == ErrorState.Permanent || PublishErrorState == ErrorState.Permanent)
                return ErrorState.Permanent;
            if (SubscribeErrorState == ErrorState.Temporary || PublishErrorState == ErrorState.Temporary)
                return ErrorState.Temporary;
            return ErrorState.None;
        }

        private void SubscribeErrorStateChanged(ErrorState state, Exception error)
        {
            if (state == ErrorState.Permanent)
                publishClient.PermanentError();
            onErrorStateChange(GetErrorState(), error);
        }

        private void PublishErrorStateChanged(ErrorState state, Exception error)
        {
            if (state == ErrorState.Permanent)
                subscribeClient.PermanentError();
            onErrorStateChange(GetErrorState(), error);
        }
    }
}
