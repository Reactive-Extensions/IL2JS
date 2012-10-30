using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Label : HtmlElement
    {
        public Label(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""LABEL""); }")]
        extern public Label();

        extern public string HtmlFor { get; set; }
        extern public Form Form { get; }
    }
}