using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class TableSection : HtmlElement
    {
        public TableSection(JSContext ctxt) : base(ctxt) { }

        extern public string Align { get; set; }
        extern public string VAlign { get; set; }
        extern public string BgColor { get; set; }
        extern public HtmlElementCollection Rows { get; }
        extern public string Ch { get; set; }
        extern public string ChOff { get; set; }
        extern public TableRow InsertRow(int index);
        extern public void DeleteRow(int index);
        extern public TableRow MoveRow(int indexFrom, int indexTo);
    }
}