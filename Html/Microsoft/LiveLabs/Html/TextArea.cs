using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class TextArea : HtmlElement
    {
        public TextArea(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""TEXTAREA""); }")]
        extern public TextArea();

        extern public string Type { get; }
        extern public string Value { get; set; }
        extern public string Name { get; set; }
        extern public string Status { get; set; }
        extern public Form Form { get; }
        extern public string DefaultValue { get; set; }
        extern public bool ReadOnly { get; set; }
        extern public int Rows { get; set; }
        extern public int Cols { get; set; }
        extern public string Wrap { get; set; }

        [Import("select")]
        extern public void PerformSelect();

        extern public TextRange CreateTextRange();
    }
}