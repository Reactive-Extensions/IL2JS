using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Big : HtmlElement
    {
        public Big(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""BIG""); }")]
        extern public Big();
    }
}