using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class SelectionObject
    {
        // Constructed by JavaScript runtime only
        public SelectionObject(JSContext ctxt) { }

        extern public string Type { get; }
        extern public string TypeDetail { get; }
        extern public TextRange CreateRange();
        extern public void Empty();
        extern public void Clear();
        extern public TextRange CreateRangeCollection();
    }
}