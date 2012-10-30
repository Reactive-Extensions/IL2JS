using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Bidirectional : HtmlElement
    {
        public Bidirectional(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""BDO""); }")]
        extern public Bidirectional();
    }
}