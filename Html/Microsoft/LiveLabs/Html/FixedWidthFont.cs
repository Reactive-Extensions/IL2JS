using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class FixedWidthFont : HtmlElement
    {
        public FixedWidthFont(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""TT""); }")]
        extern public FixedWidthFont();
    }
}