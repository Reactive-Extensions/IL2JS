using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class FrameSet : HtmlElement
    {
        public FrameSet(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""FRAMESET""); }")]
        extern public FrameSet();

        extern public string Rows { get; set; }
        extern public string Cols { get; set; }
        extern public string Border { get; set; }
        extern public string BorderColor { get; set; }
        extern public string FrameBorder { get; set; }
        extern public string FrameSpacing { get; set; }
        extern public string Name { get; set; }
    }
}