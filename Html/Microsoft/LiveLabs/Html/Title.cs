using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Title : HtmlElement
    {
        public Title(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""TITLE""); }")]
        extern public Title();

        extern public string Text { get; set; }
    }
}