using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Bold : HtmlElement
    {
        public Bold(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""B""); }")]
        extern public Bold();
    }
}