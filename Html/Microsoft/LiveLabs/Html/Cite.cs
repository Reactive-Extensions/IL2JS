using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Cite : HtmlElement
    {
        public Cite(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""CITE""); }")]
        extern public Cite();
    }
}