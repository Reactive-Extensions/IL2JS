////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////
////////PROVISIONAL IMPLEMENTATION////////

namespace System.Net.Browser
{
    public class WebRequestCreator
    {
        public static IWebRequestCreate BrowserHttp
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public static IWebRequestCreate ClientHttp
        {
            get
            {
                return new ClientHttpWebRequestCreator();
            }
        }
    }

    internal class ClientHttpWebRequestCreator : IWebRequestCreate
    {
        public ClientHttpWebRequestCreator() { }

        public WebRequest Create(Uri uri)
        {
            return new ClientHttpWebRequest(uri);
        }
    }
}
