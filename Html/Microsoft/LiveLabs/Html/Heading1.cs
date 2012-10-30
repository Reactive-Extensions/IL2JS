using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Heading1 : Heading
    {
        public Heading1(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""H1""); }")]
        extern public Heading1();
    }
}