using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Heading6 : Heading
    {
        public Heading6(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""H6""); }")]
        extern public Heading6();
    }
}