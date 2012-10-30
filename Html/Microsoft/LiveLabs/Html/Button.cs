using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Button : HtmlElement
    {
        public Button(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""BUTTON""); }")]
        extern public Button();

        extern public string Type { get; }
        extern public string Value { get; set; }
        extern public string Name { get; set; }
        extern public string Status { get; set; }
        extern public Form Form { get; }
        extern public TextRange CreateTextRange();
    }
}