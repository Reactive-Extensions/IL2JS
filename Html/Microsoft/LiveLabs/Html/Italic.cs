using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Italic : HtmlElement
    {
        public Italic(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""I""); }")]
        extern public Italic();
    }
}