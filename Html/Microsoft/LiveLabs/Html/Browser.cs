using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public static class Browser
    {
        public static extern Document Document { get; }
        public static extern Window Window { get; }

        extern public static bool IsAvailable
        {
            [Import(@"function() { return typeof window != ""undefined""; }")]
            get;
        }
    }
}