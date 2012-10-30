using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Div : HtmlElement
    {
        public Div(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""DIV""); }")]
        extern public Div();

        extern public string Align { get; set; }
        extern public bool NoWrap { get; set; }
    }
}