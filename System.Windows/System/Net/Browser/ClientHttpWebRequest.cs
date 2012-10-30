////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////

using Microsoft.LiveLabs.JavaScript.Interop;

namespace System.Net.Browser
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Interop;

    internal sealed class XmlHttpRequest
    {
        [Import(@"function() {
                      if (typeof(ActiveXObject) != ""undefined"")  
                          return new ActiveXObject(""MSXML2.XMLHTTP"");
                      else if (typeof(XMLHttpRequest) != ""undefined"")
                          return new XMLHttpRequest();
                      else
                          return null;
                  }")]
        extern public XmlHttpRequest();

        extern public Action OnReadyStateChange {
            [Import("function(inst, value) { inst.onreadystatechange = value; }", PassInstanceAsArgument = true)]
            set;
        }

        public void Open(string method, string url)
        {
            Open(method, url, false, null, null);
        }

        public void Open(string method, string url, bool async)
        {
            Open(method, url, async, null, null);
        }

        public void Open(string method, string url, bool async, string user)
        {
            Open(method, url, async, user);
        }

        extern public int ReadyState { get; }
        extern public string ResponseText { get; }
       //  extern public XmlDocument ResponseXML { get; }
        extern public int Status { get; }
        extern public string StatusText { get; }
        extern public void Abort();
        extern public string GetAllResponseHeaders();
        extern public string GetResponseHeader(string headerName);
        extern public void Open(string method, string url, bool async, string user, string password);
        extern public void Send(string body);
        extern public void SetRequestHeader(string name, string value);
    }

    public class ClientHttpWebRequest : HttpWebRequest
    {
        private XmlHttpRequest request;
        private Uri requestUri;
        private WebHeaderCollection headers;
        private bool hasResponse;

        public override string Method { get; set; }
        public override string ContentType { get; set; }
        /// <summary>
        /// Gets or sets the value of the Connection HTTP header.
        /// </summary>
        public string Connection { get; set; }
        public string UserAgent { get; set; }
        public override Uri RequestUri { get { return requestUri; } }

        /// <summary>
        /// Gets the Uniform Resource Identifier (URI) of the Internet resource that actually responds to the request.
        /// </summary>
        public Uri Address { get; private set; }

        AsyncResult<HttpWebResponse> resultGetResponse = null;
        Stream requestStream;

        public ClientHttpWebRequest(Uri uri)
        {
            this.requestUri = uri;
            this.request = new XmlHttpRequest();
            this.Method = "GET"; // Default method is GET            
            this.request.OnReadyStateChange = this.OnStateChanged;
        }

        public override IAsyncResult BeginGetRequestStream(AsyncCallback callback, object state)
        {
            var result = new AsyncResultNoResult(callback, state);
            result.SetAsCompleted(null, true);
            return result;
        }

        public override Stream EndGetRequestStream(IAsyncResult asyncResult)
        {
            // Trick in IL2JS MSCORLIB...passing null will create an internal JScriptStream
            StreamWriter sw = new StreamWriter((Stream)null);
            // So we must save off the auto-generated stream so we can then send it up...tricky tricky
            requestStream = sw.BaseStream;
            return requestStream;
        }

        public override IAsyncResult BeginGetResponse(AsyncCallback callback, object state)
        {
            var result = new AsyncResult<HttpWebResponse>(callback, state);
            resultGetResponse = result;

            this.request.Open(this.Method, this.RequestUri.AbsolutePath, true);

            // Add applicable request headers
            if (!string.IsNullOrEmpty(this.ContentType))
            {
                this.request.SetRequestHeader("Content-Type", this.ContentType);
            }
            if (!string.IsNullOrEmpty(this.Accept))
            {
                this.request.SetRequestHeader("Accept", this.Accept);
            }
            if (!string.IsNullOrEmpty(UserAgent))
            {
                this.request.SetRequestHeader("User-agent", this.UserAgent);
            }
            if (!string.IsNullOrEmpty(Connection))
            {
                this.request.SetRequestHeader("Connection", this.Connection);
            }

            foreach (var key in this.Headers.AllKeys)
            {
                var value = this.Headers[key];
                this.request.SetRequestHeader(key, value);
            }


            if (requestStream != null)
            {
                using (StreamReader sr = new StreamReader(requestStream))
                {
                    string requestString = sr.ReadToEnd();
                    // Call send with the connectStream information
                    request.Send(requestString);
                }
            }
            else
            {
                request.Send("");
            }
            requestStream = null;

            return result;
        }

        public override WebResponse EndGetResponse(IAsyncResult asyncResult)
        {
            var result = asyncResult as AsyncResult<HttpWebResponse>;
            if (result == null)
            {
                throw new ArgumentException("result", "asyncResult was not returned by the current instance from a call to BeginGetResponse.");
            }
            return result.EndInvoke();
        }

        public override void Abort()
        {
            request.Abort();
        }

        private void OnStateChanged()
        {
            switch (request.ReadyState)
            {
                #region Debugging Information
                //case 0:
                //    Console.WriteLine("HttpWebRequest.State 0 - Initialized");
                //    break;                      
                //case 1:
                //    Console.WriteLine("HttpWebRequest.State 1 - Opened");
                //    break;  
                //case 2:
                //    Console.WriteLine("HttpWebRequest.State 2 - Send called");
                //    break;
                //case 3:
                //    Console.WriteLine("HttpWebRequest.State 3 - Receiving data");
                //    break;
                #endregion
                case 4:
                    hasResponse = true;

                    //Console.WriteLine("=========================================================");
                    //Console.WriteLine("==================RESPONSE RECEIVED======================"); 
                    //Console.WriteLine("{0} - Code:{1} Text:{2}", this.RequestUri.GetComponents(UriComponents.Path, UriFormat.SafeUnescaped), request.Status, request.StatusText);
                    //Console.WriteLine("HEADERS: {0}", request.GetAllResponseHeaders());                    
                    //Console.WriteLine("RESPONSE: {0}", request.ResponseText);                    

                    StreamWriter sw = new StreamWriter((Stream)null);
                    sw.Write(request.ResponseText);                                       

                    var httpWebResponse = new ClientHttpWebResponse(this.Method, this.RequestUri, (HttpStatusCode)request.Status, sw.BaseStream);
                    string responseHeaders = request.GetAllResponseHeaders();
                    
                    if (responseHeaders != null)
                    {
                        var lines = responseHeaders.Split(new char[] { '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var headervalue in lines)
                        {
                            
                            int splitter = headervalue.IndexOf(": ");

                            // We expect the first character to be ASCII 10
                            if ((int)headervalue[0] != 10 || splitter == -1)
                            {
                                continue;
                            }
                            else
                            {
                                string key = headervalue.Substring(1, splitter - 1);
                                string value = headervalue.Substring(splitter + 2);
                                httpWebResponse.Headers[key] = value;
                                //Console.WriteLine("Key>[{0}] Value>[{1}]", key, value);
                            }                            
                        }
                    }

                    //Console.WriteLine("==================END-RESPONSE RECEIVED======================"); 

                    if (this.request.Status >= 300) // Error or Warning Range
                    {
                        resultGetResponse.SetAsCompleted(new WebException("Request failure code:" + request.Status, null, WebExceptionStatus.UnknownError, httpWebResponse), false);
                    }
                    else
                    {
                        resultGetResponse.SetAsCompleted(httpWebResponse, false);
                    }

                    break;
            }
        }

        private WebHeaderCollection webheadCollection;
        public override WebHeaderCollection Headers
        {
            get
            {
                if (webheadCollection == null)
                {
                    webheadCollection = new WebHeaderCollection();
                }
                return webheadCollection;
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }

}
