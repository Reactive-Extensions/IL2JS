using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Deleted : HtmlElement
    {
        public Deleted(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""DEL""); }")]
        extern public Deleted();

        extern public string DateTime { get; set; }
    }
}