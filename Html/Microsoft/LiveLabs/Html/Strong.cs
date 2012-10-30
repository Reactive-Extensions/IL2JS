using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Strong : HtmlElement
    {
        public Strong(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""STRONG""); }")]
        extern public Strong();
    }
}