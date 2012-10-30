using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class HtmlObject : HtmlElement
    {
        public HtmlObject(JSContext ctxt) : base(ctxt) { }

        [Import("function(id) { return document.getElementById(id); }")]
        public extern HtmlObject(string id);

        [Import(@"function(parent, classId, id, width, height) {
                      var span = document.createElement(""SPAN"");
                      span.innerHTML = ""<object classId='"" + classId + ""' id='"" + id + ""' width='"" + width + ""' height='"" + height + ""'></object>"";
                      return span.children.item(0);
                  }")]
        extern public HtmlObject(HtmlElement parent, string classId, string id, string width, string height);
 
        extern public string Classid { get; set; }
        extern public string Data { get; set; }
        extern public string Align { get; set; }
        extern public string Name { get; set; }
        extern public string CodeBase { get; set; }
        extern public string CodeType { get; set; }
        extern public string Code { get; set; }

        [Import(MemberNameCasing = Casing.Exact)]
        extern public string BaseHref { get; }

        extern public string Type { get; set; }
        extern public Form Form { get; }
        extern public string Width { get; set; }
        extern public string Height { get; set; }
        extern public string AltHtml { get; set; }
        extern public int Vspace { get; set; }
        extern public int Hspace { get; set; }
        extern public string Archive { get; set; }
        extern public string Alt { get; set; }
        extern public bool Declare { get; set; }
        extern public string Standby { get; set; }
        extern public string Border { get; set; }
        extern public string UseMap { get; set; }

        public event HtmlEventHandler Error { add { AttachEvent(this, "error", value); } remove { DetachEvent(this, "error", value); } }

        [Import("function(inst) { return inst.object; }", PassInstanceAsArgument = true)]
        extern public T GetObject<T>();
    }
}