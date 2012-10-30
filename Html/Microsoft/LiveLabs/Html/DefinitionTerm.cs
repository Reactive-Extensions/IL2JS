using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class DefinitionTerm : HtmlElement
    {
        public DefinitionTerm(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""DT""); }")]
        extern public DefinitionTerm();

        extern public bool NoWrap { get; set; }
    }
}