using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class HorizontalRule : HtmlElement
    {
        public HorizontalRule(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""HR""); }")]
        extern public HorizontalRule();

        extern public string Align { get; set; }
        extern public string Color { get; set; }
        extern public bool NoShade { get; set; }
        extern public string Width { get; set; }
        extern public string Size { get; set; }
    }
}