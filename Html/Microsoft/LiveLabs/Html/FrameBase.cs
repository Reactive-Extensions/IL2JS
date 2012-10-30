using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class FrameBase : HtmlElement
    {
        public FrameBase(JSContext ctxt) : base(ctxt) { }

        extern public string Src { get; set; }
        extern public string Name { get; set; }
        extern public string Border { get; set; }
        extern public string FrameBorder { get; set; }
        extern public string FrameSpacing { get; set; }
        extern public string MarginWidth { get; set; }
        extern public string MarginHeight { get; set; }
        extern public bool NoResize { get; set; }
        extern public string Scrolling { get; set; }
        extern public Window ContentWindow { get; }
        extern public bool AllowTransparency { get; set; }
    }
}