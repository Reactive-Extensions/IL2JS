using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Span : HtmlElement
    {
        public Span(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""SPAN""); }")]
        extern public Span();
    }
}