using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Break : HtmlElement
    {
        public Break(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""BR""); }")]
        extern public Break();

        extern public string Clear { get; set; }
    }
}