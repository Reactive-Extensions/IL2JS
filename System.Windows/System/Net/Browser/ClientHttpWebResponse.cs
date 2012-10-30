////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////

namespace System.Net.Browser
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Interop;

    public class ClientHttpWebResponse : HttpWebResponse
    {
        private string method;
        private Uri url;
        private HttpStatusCode status;
        private Stream stream;

        public ClientHttpWebResponse(string method, Uri url, HttpStatusCode status, Stream stream)
        {
            this.method = method;
            this.url = url;
            this.status = status;
            this.stream = stream;
        }

        public override CookieCollection Cookies
        {
            get
            {
                return null;
            }
        }

        public override string Method
        {
            get
            {
                return base.Method;
            }
        }

        public override HttpStatusCode StatusCode
        {
            get
            {
                return status;
            }
        }

        public override Stream GetResponseStream()
        {
            return this.stream;
        }

        public override void Close()
        {
            // Do nothing
        }

        public override long ContentLength
        {
            get { throw new NotImplementedException(); }
        }

        public override string ContentType
        {
            get { throw new NotImplementedException(); }
        }

        public override Uri ResponseUri
        {
            get { throw new NotImplementedException(); }
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
        }
    }
}
