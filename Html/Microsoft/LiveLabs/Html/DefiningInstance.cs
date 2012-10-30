using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class DefiningInstance : HtmlElement
    {
        public DefiningInstance(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""DFN""); }")]
        extern public DefiningInstance();
    }
}