using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Acronym : HtmlElement
    {
        public Acronym(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""ACRONYM""); }")]
        extern public Acronym();
    }
}
