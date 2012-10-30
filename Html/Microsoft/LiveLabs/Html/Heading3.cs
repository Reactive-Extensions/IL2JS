using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Heading3 : Heading
    {
        public Heading3(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""H3""); }")]
        extern public Heading3();
    }
}