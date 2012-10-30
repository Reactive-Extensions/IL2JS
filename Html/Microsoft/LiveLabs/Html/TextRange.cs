using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class TextRange
    {
        // Created by JavaScript runtime only
        public TextRange(JSContext ctxt) { }

        extern public string HtmlText { get; }
        extern public string Text { get; set; }
        extern public int OffsetTop { get; }
        extern public int OffsetLeft { get; }
        extern public int BoundingTop { get; }
        extern public int BoundingLeft { get; }
        extern public int BoundingWidth { get; }
        extern public int BoundingHeight { get; }
        extern public HtmlElement Parent();
        extern public TextRange Duplicate();
        extern public bool InRange(TextRange range);
        extern public bool IsEqual(TextRange range);
        extern public void ScrollIntoView(bool fStart);
        extern public void Collapse(bool start);
        extern public bool Expand(string unit);
        extern public int Move(string unit, int count);
        extern public int MoveStart(string unit, int count);
        extern public int MoveEnd(string unit, int count);

        [Import("select")]
        extern public void PerformSelect();

        extern public void PasteHtml(string html);
        extern public void MoveToText(HtmlElement element);
        extern public void SetEndPoint(string how, TextRange sourceRange);
        extern public int CompareEndPoints(string how, TextRange sourceRange);
        extern public bool FindText(string systemString, int count, int flags);
        extern public void MoveToPoint(int x, int y);
        extern public string GetBookmark();
        extern public bool MoveToBookmark(string bookmark);
    }
}