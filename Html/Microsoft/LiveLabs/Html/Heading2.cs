using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Heading2 : Heading
    {
        public Heading2(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""H2""); }")]
        extern public Heading2();
    }
}