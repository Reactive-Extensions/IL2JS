////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////

//// parseUri 1.2.2
//// (c) Steven Levithan <stevenlevithan.com>
//// MIT License

//function parseUri (str) {
//    var	o   = parseUri.options,
//        m   = o.parser[o.strictMode ? "strict" : "loose"].exec(str),
//        uri = {},
//        i   = 14;

//    while (i--) uri[o.key[i]] = m[i] || "";

//    uri[o.q.name] = {};
//    uri[o.key[12]].replace(o.q.parser, function ($0, $1, $2) {
//        if ($1) uri[o.q.name][$1] = $2;
//    });

//    return uri;
//};

//parseUri.options = {
//    strictMode: false,
//    key: ["source","protocol","authority","userInfo","user","password","host","port","relative","path","directory","file","query","anchor"],
//    q:   {
//        name:   "queryKey",
//        parser: /(?:^|&)([^&=]*)=?([^&]*)/g
//    },
//    parser: {
//        strict: /^(?:([^:\/?#]+):)?(?:\/\/((?:(([^:@]*)(?::([^:@]*))?)?@)?([^:\/?#]*)(?::(\d*))?))?((((?:[^?#\/]*\/)*)([^?#]*))(?:\?([^#]*))?(?:#(.*))?)/,
//        loose:  /^(?:(?![^:@]+:[^:@\/]*@)([^:\/?#.]+):)?(?:\/\/)?((?:(([^:@]*)(?::([^:@]*))?)?@)?([^:\/?#]*)(?::(\d*))?)(((\/(?:[^?#](?![^?#\/]*\.[^?#\/.]+(?:[?#]|$)))*\/?)?([^?#\/]*))(?:\?([^#]*))?(?:#(.*))?)/
//    }
//};


using Microsoft.LiveLabs.JavaScript.IL2JS;

namespace System
{
    [Flags]
    public enum UriComponents
    {
        AbsoluteUri = 0x7f,
        Fragment = 0x40,
        Host = 4,
        HostAndPort = 0x84,
        HttpRequestUrl = 0x3d,
        KeepDelimiter = 0x40000000,
        Path = 0x10,
        PathAndQuery = 0x30,
        Port = 8,
        Query = 0x20,
        Scheme = 1,
        SchemeAndServer = 13,
        StrongAuthority = 0x86,
        StrongPort = 0x80,
        UserInfo = 2
    }

    public class Uri
    {
        private string path;

        public Uri(string uri)
        {
            // Super naive implementation

            // Find the //
            int schemeIdentifier = uri.IndexOf("://");
            if (schemeIdentifier == -1) { throw new ArgumentException(); }

            this.Scheme = uri.Substring(0, schemeIdentifier);
            int beginHost = schemeIdentifier += 3;

            int endHost = uri.IndexOf("/", beginHost);
            if (endHost == -1)
            {
                // No path provided
                this.Host = uri.Substring(beginHost);                
            }
            else
            {
                this.Host = uri.Substring(beginHost, endHost - beginHost);
                this.path = uri.Substring(endHost + 1);
            }
            this.AbsolutePath = uri;
        }

        public Uri(Uri baseUri, string relativeUri)
            : this(string.Format("{0}/{1}", baseUri.AbsolutePath, relativeUri))
        {         
        }

        public string GetComponents(UriComponents components, UriFormat format)
        {
            switch (components)
            {
                case UriComponents.PathAndQuery:
                    return AbsolutePath + Query;

                case UriComponents.SchemeAndServer:
                    return string.Format("{0}://{1}", this.Scheme, this.Host);

                case UriComponents.Path:
                    return path;

                default:
                    throw new Exception("Not supported");
            }
        }

        public string Scheme { get; private set; }
        public string Host { get; private set; }
        public string Hostname { get; private set; }
        public int Port { get; private set; }
        public string AbsolutePath { get; private set; }
        public string Bookmark { get; private set; }
        public string Query { get; private set; }

        public override string ToString()
        {
            return this.AbsolutePath;
        }
    }
}
