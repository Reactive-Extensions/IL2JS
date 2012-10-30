using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class Rectangle
    {
        // Constructed by JavaScript runtime only
        public Rectangle(JSContext ctxt) { }

        extern public int Left { get; set; }
        extern public int Top { get; set; }
        extern public int Right { get; set; }
        extern public int Bottom { get; set; }
    }
}