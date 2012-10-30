using System;
using Microsoft.LiveLabs.JavaScript.Interop;

//
// NOTE: On IE this object won't support expandos, thus we can't have any fields in this type, thus we can't
//       emulate multicast events for 'ReadyStateChange'.
//

namespace Microsoft.LiveLabs.Xml
{
    [Import]
    public sealed class XmlHttpRequest
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
        extern public XmlDocument ResponseXML { get; }
        extern public int Status { get; }
        extern public string StatusText { get; }
        extern public void Abort();
        extern public string GetAllResponseHeaders();
        extern public string GetResponseHeader(string headerName);
        extern public void Open(string method, string url, bool async, string user, string password);
        extern public void Send(string body);
        extern public void SetRequestHeader(string name, string value);
    }
}