using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Body : HtmlElement
    {
        public Body(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""BODY""); }")]
        extern public Body();

        extern public string Background { get; set; }
        extern public string BgProperties { get; set; }
        extern public string LeftMargin { get; set; }
        extern public string TopMargin { get; set; }
        extern public string RightMargin { get; set; }
        extern public string BottomMargin { get; set; }
        extern public bool NoWrap { get; set; }
        extern public string BgColor { get; set; }
        extern public string Text { get; set; }
        extern public string Link { get; set; }
        extern public string VLink { get; set; }
        extern public string ALink { get; set; }
        extern public TextRange CreateTextRange();
    }
}