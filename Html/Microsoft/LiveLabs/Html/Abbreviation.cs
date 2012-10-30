using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Abbreviation : HtmlElement
    {
        public Abbreviation(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""ABBR""); }")]
        extern public Abbreviation();
    }
}