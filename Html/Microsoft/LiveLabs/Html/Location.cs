using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class Location
    {
        // Constructed by JavaScript runtime only
        public Location(JSContext ctxt) { }

        [Import("href")]
        extern public string HRef { get; set; }

        extern public string Hash { get; set; }
        extern public string Host { get; set; }
        extern public string Hostname { get; set; }
        extern public string Pathname { get; set; }
        extern public int Port { get; set; }
        extern public string Protocol { get; set; }
        extern public string Search { get; set; }
    }
}