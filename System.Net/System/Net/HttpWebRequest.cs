using System;
using System.IO;

namespace System.Net
{
    public abstract class HttpWebRequest : WebRequest
    {
        // Methods
        protected HttpWebRequest()
        {
        }

        public override void Abort() { throw new NotImplementedException(); }
        public override IAsyncResult BeginGetRequestStream(AsyncCallback callback, object state) { throw new NotImplementedException(); }
        public override IAsyncResult BeginGetResponse(AsyncCallback callback, object state) { throw new NotImplementedException(); }
        public override Stream EndGetRequestStream(IAsyncResult asyncResult) { throw new NotImplementedException(); }
        public override WebResponse EndGetResponse(IAsyncResult asyncResult) { throw new NotImplementedException(); }

        // Properties
        public string Accept
        {
            get;
            set;
        }

        public virtual bool AllowReadStreamBuffering
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public virtual bool AllowWriteStreamBuffering
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override string ContentType
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public virtual bool HaveResponse
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override WebHeaderCollection Headers
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override string Method
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override Uri RequestUri
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual bool SupportsCookieContainer
        {
            get
            {
                return false;
            }
        }
    }
}
