
namespace System.Net
{
    internal static class HttpHeaderToName
    {
        // Fields
        private static HttpRequestHeaderEnumToName _headerNames = new HttpRequestHeaderEnumToName();

        // Properties
        public static HttpRequestHeaderEnumToName HeaderNames
        {
            get
            {
                return _headerNames;
            }
        }
    }
}
