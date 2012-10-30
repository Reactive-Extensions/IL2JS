#if INTERNETEXPLORER
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html.InternetExplorer
{
    [Import]
    public class RubyText : HtmlElement
    {
        public RubyText(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""RT""); }")]
        extern public RubyText();
    }
}
#endif