using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Heading4 : Heading
    {
        public Heading4(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""H4""); }")]
        extern public Heading4();
    }
}