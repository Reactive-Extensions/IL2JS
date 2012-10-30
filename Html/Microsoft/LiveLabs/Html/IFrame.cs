using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class IFrame : FrameBase
    {
        public IFrame(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""IFRAME""); }")]
        extern public IFrame();

        extern public int Vspace { get; set; }
        extern public int Hspace { get; set; }
        extern public string Align { get; set; }
        extern public string Height { get; set; }
        extern public string Width { get; set; }
    }
}