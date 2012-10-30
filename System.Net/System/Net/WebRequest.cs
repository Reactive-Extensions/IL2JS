using System;
using System.Net;
using System.IO;

namespace System.Net
{
    public abstract class WebRequest
    {
        public virtual void Abort() { throw new NotImplementedException(); }

        public virtual IAsyncResult BeginGetRequestStream(AsyncCallback callback, object state) { throw new NotImplementedException(); }
        public virtual IAsyncResult BeginGetResponse(AsyncCallback callback, object state) { throw new NotImplementedException(); }
        public static WebRequest Create(string requestUriString)
        {
            throw new NotSupportedException();
            //return new HttpWebRequest(new Uri(requestUriString));
        }
        public static WebRequest Create(Uri requestUri) 
        {
            throw new NotSupportedException();
            //return new HttpWebRequest(requestUri);
        }
        public virtual Stream EndGetRequestStream(IAsyncResult asyncResult) { throw new NotImplementedException(); }
        public virtual WebResponse EndGetResponse(IAsyncResult asyncResult) { throw new NotImplementedException(); }

        public virtual string ConnectionGroupName
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

        public virtual long ContentLength
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

        public virtual string ContentType
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

        private static object InternalSyncObject
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual string Method
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

        public virtual bool PreAuthenticate
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

        public virtual Uri RequestUri
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public virtual int Timeout
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

        public virtual bool UseDefaultCredentials
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

        public virtual WebHeaderCollection Headers
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
    }
}

 
