using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class StyleSheetPage
    {
        // Created by JavaScript runtime only
        public StyleSheetPage(JSContext ctxt) { }

        extern public string Selector { get; }
        extern public string PseudoClass { get; }
    }
}