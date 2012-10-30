using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class ListItem : HtmlElement
    {
        public ListItem(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""LI""); }")]
        extern public ListItem();

        extern public string Compact { get; set; }
    }
}