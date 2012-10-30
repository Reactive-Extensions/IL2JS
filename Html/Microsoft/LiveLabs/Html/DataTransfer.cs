using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class DataTransfer
    {
        // Created by JavaScript runtime only
        public DataTransfer(JSContext ctxt) { }

        extern public string DropEffect { get; set; }
        extern public string EffectAllowed { get; set; }
        extern public bool SetData(string format, string data);
        extern public string GetData(string format);
        extern public bool ClearData(string format);
    }
}