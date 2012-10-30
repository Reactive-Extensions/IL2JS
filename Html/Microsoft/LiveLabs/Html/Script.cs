using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Script : HtmlElement
    {
        public Script(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""SCRIPT""); }")]
        extern public Script();

        extern public bool Defer { get; set; }
        extern public string Event { get; set; }
        extern public string HtmlFor { get; set; }
        extern public string Src { get; set; }
        extern public string Text { get; set; }
        extern public string Type { get; set; }
        extern public string Charset { get; set; }

        public event HtmlEventHandler Error { add { AttachEvent(this, "error", value); } remove { DetachEvent(this, "error", value); } }  
    }
}