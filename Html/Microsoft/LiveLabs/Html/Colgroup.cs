using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Colgroup : HtmlElement
    {
        public Colgroup(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""COLGROUP""); }")]
        extern public Colgroup();
    }
}