using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Frame : FrameBase
    {
        public Frame(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""FRAME""); }")]
        extern public Frame();

        extern public string BorderColor { get; set; }
        extern public string Height { get; set; }
        extern public string Width { get; set; }
    }
}