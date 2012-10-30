using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Option : HtmlElement
    {
        public Option(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""OPTION""); }")]
        extern public Option();

        extern public bool Selected { get; set; }
        extern public string Value { get; set; }
        extern public bool DefaultSelected { get; set; }
        extern public int Index { get; set; }
        extern public string Text { get; set; }
        extern public Form Form { get; }
        extern public string Label { get; set; }
    }
}