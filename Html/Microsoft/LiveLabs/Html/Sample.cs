using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Sample : HtmlElement
    {
        public Sample(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""SAMP""); }")]
        extern public Sample();
    }
}