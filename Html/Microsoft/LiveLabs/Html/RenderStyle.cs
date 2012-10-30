using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class RenderStyle
    {
        // Constructed by JavaScript runtime only
        public RenderStyle(JSContext ctxt) { }

        extern public string TextLineThroughStyle { get; set; }
        extern public string TextUnderlineStyle { get; set; }
        extern public string TextEffect { get; set; }
        extern public string TextColor { get; set; }
        extern public string TextBackgroundColor { get; set; }
        extern public string TextDecorationColor { get; set; }
        extern public int RenderingPriority { get; set; }
        extern public string DefaultTextSelection { get; set; }
        extern public string TextDecoration { get; set; }
    }
}