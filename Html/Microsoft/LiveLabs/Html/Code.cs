using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Code : HtmlElement
    {
        public Code(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""CODE""); }")]
        extern public Code();
    }
}