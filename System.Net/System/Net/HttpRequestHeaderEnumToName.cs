
namespace System.Net
{
    internal class HttpRequestHeaderEnumToName
    {
        // Fields
        private static readonly string[] HeaderStrings = new string[] { 
        "Cache-Control", "Connection", "Date", "Keep-Alive", "Pragma", "Trailer", "Transfer-Encoding", "Upgrade", "Via", "Warning", "Allow", "Content-Length", "Content-Type", "Content-Encoding", "Content-Language", "Content-Location", 
        "Content-MD5", "Content-Range", "Expires", "Last-Modified", "Accept", "Accept-Charset", "Accept-Encoding", "Accept-Language", "Authorization", "Cookie", "Expect", "From", "Host", "If-Match", "If-Modified-Since", "If-None-Match", 
        "If-Range", "If-Unmodified-Since", "Max-Forwards", "Proxy-Authorization", "Referer", "Range", "TE", "Translate", "User-Agent"
        };

        // Properties
        public string this[HttpRequestHeader reqHeader]
        {
            get
            {
                return HeaderStrings[(int)reqHeader];
            }
        }
    }
}
