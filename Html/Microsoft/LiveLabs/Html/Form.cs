using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Form : HtmlElement
    {
        public Form(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""FORM""); }")]
        extern public Form();

        extern public string Action { get; set; }
        extern public string Encoding { get; set; }
        extern public string Method { get; set; }
        extern public string Target { get; set; }
        extern public string Name { get; set; }
        extern public int Length { get; set; }

        public event HtmlEventHandler Submit { add { AttachEvent(this, "submit", value); } remove { DetachEvent(this, "submit", value); } } 
        public event HtmlEventHandler Reset { add { AttachEvent(this, "reset", value); } remove { DetachEvent(this, "reset", value); } }

        [Import("submit")]
        extern public void PerformSubmit();

        [Import("reset")]
        extern public void PerformReset();

        extern public HtmlElement Item(int index);
        extern public HtmlElement Item(int index, int subIndex);
    }
}