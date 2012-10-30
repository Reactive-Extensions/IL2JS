using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class UnorderedList : HtmlElement
    {
        public UnorderedList(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""UL""); }")]
        extern public UnorderedList();

        extern public bool Compact { get; set; }
        extern public string Type { get; set; }
    }
}