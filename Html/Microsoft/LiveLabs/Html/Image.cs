using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Image : HtmlElement
    {
        public Image(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""IMG""); }")]
        extern public Image();

        extern public bool IsMap { get; set; }
        extern public string UseMap { get; set; }
        extern public string MimeType { get; }
        extern public string FileSize { get; }
        extern public string FileCreatedDate { get; }
        extern public string FileModifiedDate { get; }
        extern public string FileUpdatedDate { get; }
        extern public string Protocol { get; }
        extern public string Href { get; }
        extern public string NameProp { get; }
        extern public string Border { get; set; }
        extern public int Vspace { get; set; }
        extern public int Hspace { get; set; }
        extern public string Alt { get; set; }
        extern public string Src { get; set; }
        extern public string Lowsrc { get; set; }
        extern public string Vrml { get; set; }
        extern public string Dynsrc { get; set; }
        extern public bool Complete { get; }
        extern public string Loop { get; set; }
        extern public string Align { get; set; }
        extern public string Name { get; set; }
        extern public int Width { get; set; }
        extern public int Height { get; set; }

        [Import("Start")]
        extern public string Start { get; set; }

        extern public string LongDesc { get; set; }
    }
}