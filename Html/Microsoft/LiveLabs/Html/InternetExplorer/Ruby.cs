#if INTERNETEXPLORER
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html.InternetExplorer
{
    [Import]
    public class Ruby : HtmlElement
    {
        public Ruby(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""RUBY""); }")]
        extern public Ruby();
    }
}
#endif