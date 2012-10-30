using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class Navigator
    {
        // Constructed by JavaScript runtime only
        public Navigator(JSContext ctxt) { }

        extern public string AppCodeName { get; }
        extern public string AppName { get; }
        extern public string AppVersion { get; }
        extern public bool CookieEnabled { get; }
        extern public string BrowserLanguage { get; }
        extern public bool OnLine { get; }
        extern public string Platform { get; }
        extern public string UserAgent { get; }
        extern public bool JavaEnabled();
    }
}