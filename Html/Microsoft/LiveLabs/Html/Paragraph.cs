using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Paragraph : HtmlElement
    {
        public Paragraph(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""P""); }")]
        extern public Paragraph();
    }
}