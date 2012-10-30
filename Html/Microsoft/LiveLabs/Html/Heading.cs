using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Heading : HtmlElement
    {
        public Heading(JSContext ctxt) : base(ctxt) { }
    }
}