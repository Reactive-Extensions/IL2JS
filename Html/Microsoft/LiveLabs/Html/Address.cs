using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Address : HtmlElement
    {
        public Address(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""ADDRESS""); }")]
        extern public Address();
    }
}