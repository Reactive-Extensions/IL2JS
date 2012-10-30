using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class CurrentStyle
    {
        // Created by JavaScript runtime only
        public CurrentStyle(JSContext ctxt) { }

        extern public string Position { get; }
        extern public string StyleFloat { get; }
        extern public string Color { get; }
        extern public string BackgroundColor { get; }
        extern public string FontFamily { get; }
        extern public string FontStyle { get; }
        extern public string FontVariant { get; }
        extern public string FontWeight { get; }
        extern public string FontSize { get; }
        extern public string BackgroundImage { get; }
        extern public string BackgroundPositionX { get; }
        extern public string BackgroundPositionY { get; }
        extern public string BackgroundRepeat { get; }
        extern public string BorderLeftColor { get; }
        extern public string BorderTopColor { get; }
        extern public string BorderRightColor { get; }
        extern public string BorderBottomColor { get; }
        extern public string BorderTopStyle { get; }
        extern public string BorderRightStyle { get; }
        extern public string BorderBottomStyle { get; }
        extern public string BorderLeftStyle { get; }
        extern public string BorderTopWidth { get; }
        extern public string BorderRightWidth { get; }
        extern public string BorderBottomWidth { get; }
        extern public string BorderLeftWidth { get; }
        extern public string Left { get; }
        extern public string Top { get; }
        extern public string Width { get; }
        extern public string Height { get; }
        extern public string PaddingLeft { get; }
        extern public string PaddingTop { get; }
        extern public string PaddingRight { get; }
        extern public string PaddingBottom { get; }
        extern public string TextAlign { get; }
        extern public string TextDecoration { get; }
        extern public string Display { get; }
        extern public string Visibility { get; }
        extern public string ZIndex { get; }
        extern public string LetterSpacing { get; }
        extern public string LineHeight { get; }
        extern public string TextIndent { get; }
        extern public string VerticalAlign { get; }
        extern public string BackgroundAttachment { get; }
        extern public string MarginTop { get; }
        extern public string MarginRight { get; }
        extern public string MarginBottom { get; }
        extern public string MarginLeft { get; }
        extern public string Clear { get; }
        extern public string ListStyleType { get; }
        extern public string ListStylePosition { get; }
        extern public string ListStyleImage { get; }
        extern public string ClipTop { get; }
        extern public string ClipRight { get; }
        extern public string ClipBottom { get; }
        extern public string ClipLeft { get; }
        extern public string Overflow { get; }
        extern public string PageBreakBefore { get; }
        extern public string PageBreakAfter { get; }
        extern public string Cursor { get; }
        extern public string TableLayout { get; }
        extern public string BorderCollapse { get; }
        extern public string Direction { get; }
        extern public string Behavior { get; }
        extern public string UnicodeBidi { get; }
        extern public string Right { get; }
        extern public string Bottom { get; }
        extern public string ImeMode { get; }
        extern public string RubyAlign { get; }
        extern public string RubyPosition { get; }
        extern public string RubyOverhang { get; }
        extern public string TextAutospace { get; }
        extern public string LineBreak { get; }
        extern public string WordBreak { get; }
        extern public string TextJustify { get; }
        extern public string TextJustifyTrim { get; }
        extern public string TextKashida { get; }
        extern public string BlockDirection { get; }
        extern public string LayoutGridChar { get; }
        extern public string LayoutGridLine { get; }
        extern public string LayoutGridMode { get; }
        extern public string LayoutGridType { get; }
        extern public string BorderStyle { get; }
        extern public string BorderColor { get; }
        extern public string BorderWidth { get; }
        extern public string Padding { get; }
        extern public string Margin { get; }
        extern public string Accelerator { get; }
        extern public string OverflowX { get; }
        extern public string OverflowY { get; }
        extern public string TextTransform { get; }
        extern public string GetAttribute(string attributeName, int flags);
    }
}