using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Legend : HtmlElement
    {
        public Legend(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""LEGEND""); }")]
        extern public Legend();
    }
}