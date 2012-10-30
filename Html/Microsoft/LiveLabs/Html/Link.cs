using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Link : HtmlElement
    {
        public Link(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""LINK""); }")]
        extern public Link();

        extern public string Href { get; set; }
        extern public string Media { get; set; }
        extern public string Rel { get; set; }
        extern public string Rev { get; set; }
        extern public StyleSheet StyleSheet { get; }
        extern public string Type { get; set; }
        extern public string Target { get; set; }
        extern public string Charset { get; set; }
        extern public string Hreflang { get; set; }

        public event HtmlEventHandler Load { add { AttachEvent(this, "load", value); } remove { DetachEvent(this, "load", value); } }  
        public event HtmlEventHandler Error { add { AttachEvent(this, "error", value); } remove { DetachEvent(this, "error", value); } }  
    }
}