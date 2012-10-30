using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class Screen
    {
        // Constructed by JavaScript runtime only
        public Screen(JSContext ctxt) { }

        extern public int ColorDepth { get; }
        extern public int BufferDepth { get; set; }
        extern public int Width { get; }
        extern public int Height { get; }
        extern public int UpdateInterval { get; set; }
        extern public int AvailHeight { get; }
        extern public int AvailWidth { get; }
        extern public bool FontSmoothingEnabled { get; }

        [Import("logicalXDPI")]
        extern public int LogicalXdpi { get; }

        [Import("logicalYDPI")]
        extern public int LogicalYdpi { get; }

        [Import("deviceXDPI")]
        extern public int DeviceXdpi { get; }

        [Import("deviceYDPI")]
        extern public int DeviceYdpi { get; }
    }
}