using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Inserted : HtmlElement
    {
        public Inserted(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""INS""); }")]
        extern public Inserted();

        extern public string DateTime { get; set; }
    }
}