using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Center : HtmlElement
    {
        public Center(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""CENTER""); }")]
        extern public Center();
    }
}