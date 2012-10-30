using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Subscript : HtmlElement
    {
        public Subscript(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""SUB""); }")]
        extern public Subscript();
    }
}