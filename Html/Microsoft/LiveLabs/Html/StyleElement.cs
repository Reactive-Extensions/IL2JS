using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class StyleElement : HtmlElement
    {
        public StyleElement(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""STYLE""); }")]
        extern public StyleElement();

        extern public string Type { get; set; }
        extern public StyleSheet StyleSheet { get; }
        extern public string Media { get; set; }

        public event HtmlEventHandler Load { add { AttachEvent(this, "load", value); } remove { DetachEvent(this, "load", value); } }  
        public event HtmlEventHandler Error { add { AttachEvent(this, "error", value); } remove { DetachEvent(this, "error", value); } }  
    }
}