using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Base : HtmlElement
    {
        public Base(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""BASE""); }")]
        extern public Base();

        extern public string Href { get; set; }
        extern public string Target { get; set; }
    }
}