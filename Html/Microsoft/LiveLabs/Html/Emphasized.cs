using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Emphasized : HtmlElement
    {
        public Emphasized(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""EM""); }")]
        extern public Emphasized();
    }
}