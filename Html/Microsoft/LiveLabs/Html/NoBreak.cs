using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class NoBreak : HtmlElement
    {
        public NoBreak(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""NOBR""); }")]
        extern public NoBreak();
    }
}