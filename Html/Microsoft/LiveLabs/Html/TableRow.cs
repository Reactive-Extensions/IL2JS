using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class TableRow : HtmlElement
    {
        public TableRow(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""TR""); }")]
        extern public TableRow();

        extern public string Align { get; set; }
        extern public string VAlign { get; set; }
        extern public string BgColor { get; set; }
        extern public string BorderColor { get; set; }
        extern public string BorderColorLight { get; set; }
        extern public string BorderColorDark { get; set; }
        extern public int RowIndex { get; }
        extern public int SectionRowIndex { get; }
        extern public HtmlElementCollection Cells { get; }
        extern public string Height { get; set; }
        extern public string Ch { get; set; }
        extern public string ChOff { get; set; }
        extern public TableCell InsertCell(int index);
        extern public void DeleteCell(int index);
    }
}