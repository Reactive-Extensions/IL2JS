using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Underline : HtmlElement
    {
        public Underline(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""U""); }")]
        extern public Underline();
    }
}
