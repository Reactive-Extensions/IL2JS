using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class DomAttribute
    {
        // Created by JavaScript runtime only
        public DomAttribute(JSContext ctxt) { }

        extern public string NodeName { get; }
        extern public string NodeValue { get; set; }
        extern public bool Specified { get; }
        extern public string Value { get; set; }
        extern public string Name { get; set; }
    }
}