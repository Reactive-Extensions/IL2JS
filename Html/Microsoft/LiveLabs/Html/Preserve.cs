using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Preserve : HtmlElement
    {
        public Preserve(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""PRE""); }")]
        extern public Preserve();
    }
}