using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Font : HtmlElement
    {
        public Font(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""FONT""); }")]
        extern public Font();
    }
}