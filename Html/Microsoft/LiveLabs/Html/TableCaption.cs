using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class TableCaption : HtmlElement
    {
        public TableCaption(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""CAPTION""); }")]
        extern public TableCaption();

        extern public string Align { get; set; }
        extern public string VAlign { get; set; }
    }
}