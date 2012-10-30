using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Superscript : HtmlElement
    {
        public Superscript(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""SUP""); }")]
        extern public Superscript();
    }
}