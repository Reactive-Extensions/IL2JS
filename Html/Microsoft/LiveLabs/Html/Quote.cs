using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Quote : HtmlElement
    {
        public Quote(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""Q""); }")]
        extern public Quote();

        extern public string Cite { get; set; }
    }
}