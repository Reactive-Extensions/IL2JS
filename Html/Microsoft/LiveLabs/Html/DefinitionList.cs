using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class DefinitionList : HtmlElement
    {
        public DefinitionList(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""DL""); }")]
        extern public DefinitionList();

        extern public bool Compact { get; set; }
    }
}