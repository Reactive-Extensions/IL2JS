using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Heading5 : Heading
    {
        public Heading5(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""H5""); }")]
        extern public Heading5();
    }
}