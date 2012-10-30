using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Variable : HtmlElement
    {
        public Variable(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""VAR""); }")]
        extern public Variable();
    }
}