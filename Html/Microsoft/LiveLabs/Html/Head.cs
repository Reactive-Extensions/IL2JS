using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Head : HtmlElement
    {
        public Head(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""HEAD""); }")]
        extern public Head();

        extern public string Profile { get; set; }
    }
}