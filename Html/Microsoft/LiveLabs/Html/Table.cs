using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Table : HtmlElement
    {
        public Table(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""TABLE""); }")]
        extern public Table();

        extern public int Cols { get; set; }
        extern public string Border { get; set; }
        extern public string Frame { get; set; }
        extern public string Rules { get; set; }
        extern public string CellSpacing { get; set; }
        extern public string CellPadding { get; set; }
        extern public string Background { get; set; }
        extern public string BgColor { get; set; }
        extern public string BorderColor { get; set; }
        extern public string BorderColorLight { get; set; }
        extern public string BorderColorDark { get; set; }
        extern public string Align { get; set; }
        extern public HtmlElementCollection Rows { get; }
        extern public string Width { get; set; }
        extern public string Height { get; set; }
        extern public int DataPageSize { get; set; }
        extern public TableHead THead { get; }
        extern public TableFoot TFoot { get; }
        extern public HtmlElementCollection TBodies { get; }
        extern public TableCaption Caption { get; }
        extern public HtmlElementCollection Cells { get; }
        extern public string Summary { get; set; }
        extern public void Refresh();
        extern public void NextPage();
        extern public void PreviousPage();
        extern public TableHead CreateTHead();
        extern public void DeleteTHead();
        extern public TableFoot CreateTFoot();
        extern public void DeleteTFoot();
        extern public TableCaption CreateCaption();
        extern public void DeleteCaption();
        extern public TableRow InsertRow(int index);
        extern public void DeleteRow(int index);
        extern public void FirstPage();
        extern public void LastPage();
        extern public TableRow MoveRow(int indexFrom, int indexTo);
    }
}
