using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Col : HtmlElement
    {
        public Col(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""COL""); }")]
        extern public Col();
    }
}