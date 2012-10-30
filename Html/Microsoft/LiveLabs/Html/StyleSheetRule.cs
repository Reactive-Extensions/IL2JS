using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class StyleSheetRule
    {
        // Created by JavaScript runtime only
        public StyleSheetRule(JSContext ctxt) { }

        extern public string SelectorText { get; set; }
        extern public RuleStyle Style { get; }
        extern public bool ReadOnly { get; }
    }
}