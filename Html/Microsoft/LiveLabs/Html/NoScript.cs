using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class NoScript : HtmlElement
    {
        public NoScript(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""NOSCRIPT""); }")]
        extern public NoScript();
    }
}