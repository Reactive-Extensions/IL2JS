using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class TableHeader : HtmlElement
    {
        public TableHeader(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""TH""); }")]
        extern public TableHeader();

        extern public int RowSpan { get; set; }
        extern public int ColSpan { get; set; }
        extern public string Align { get; set; }
        extern public string VAlign { get; set; }
        extern public string BgColor { get; set; }
        extern public bool NoWrap { get; set; }
        extern public string Background { get; set; }
        extern public string BorderColor { get; set; }
        extern public string BorderColorLight { get; set; }
        extern public string BorderColorDark { get; set; }
        extern public string Width { get; set; }
        extern public string Height { get; set; }
        extern public int CellIndex { get; }
        extern public string Abbr { get; set; }
        extern public string Axis { get; set; }
        extern public string Ch { get; set; }
        extern public string ChOff { get; set; }
        extern public string Headers { get; set; }
        extern public string Scope { get; set; }
    }
}