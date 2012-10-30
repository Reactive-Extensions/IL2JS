using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class DomImplementation
    {
        // Created by JavaScript runtime only
        public DomImplementation(JSContext ctxt) { }

        extern public bool HasFeature(string bstrfeature, string version);
    }
}