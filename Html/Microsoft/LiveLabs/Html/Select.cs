using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Select : HtmlElement
    {
        public Select(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""SELECT""); }")]
        extern public Select();

        extern public int Size { get; set; }
        extern public bool Multiple { get; set; }
        extern public string Name { get; set; }
        extern public int Length { get; }
        extern public int SelectedIndex { get; set; }
        extern public string Type { get; }
        extern public string Value { get; set; }
        extern public Form Form { get; }

        public event HtmlEventHandler Change { add { AttachEvent(this, "change", value); } remove { DetachEvent(this, "change", value); } }  

        extern public void Add(HtmlElement element, HtmlElement before);
        extern public void Add(HtmlElement element);
        extern public void Remove(int index);
        extern public Option this[int index] { get; }
        extern new public Option this[string name] { get; }

        // TODO
        // extern public Option this[string name, int index] { get; }

        extern public HtmlElementCollection Tags(string tagName);
    }
}