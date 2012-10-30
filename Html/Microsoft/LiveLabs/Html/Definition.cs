using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Definition : HtmlElement
    {
        public Definition(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""DD""); }")]
        extern public Definition();

        extern public bool NoWrap { get; set; }
    }
}