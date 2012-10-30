using System.IO;

namespace System.Net
{
	public abstract class HttpWebResponse : WebResponse
	{               
        protected HttpWebResponse()
        {
        }

        public virtual CookieCollection Cookies { get { throw new NotImplementedException(); } }
        public virtual string Method { get { throw new NotImplementedException(); } }
        public virtual HttpStatusCode StatusCode { get { throw new NotImplementedException(); } }
        public virtual string StatusDescription { get { throw new NotImplementedException(); } }
    }
}
