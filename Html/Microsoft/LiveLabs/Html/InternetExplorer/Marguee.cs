#if INTERNETEXPLORER
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html.InternetExplorer
{
    [Import]
    public class Marquee : HtmlElement
    {
        public Marquee(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""MARQUEE""); }")]
        extern public Marquee();
    }
}
#endif