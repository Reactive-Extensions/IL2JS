using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class SoftLineBreak : HtmlElement
    {
        public SoftLineBreak(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""WRB""); }")]
        extern public SoftLineBreak();
    }
}