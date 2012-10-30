using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Small : HtmlElement
    {
        public Small(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""SMALL""); }")]
        extern public Small();
    }
}