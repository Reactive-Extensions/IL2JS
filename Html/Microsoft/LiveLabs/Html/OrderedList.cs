using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class OrderedList : HtmlElement
    {
        public OrderedList(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""OL""); }")]
        extern public OrderedList();

        extern public bool Compact { get; set; }
        extern public string Start { get; set; }
        extern public string Type { get; set; }
    }
}