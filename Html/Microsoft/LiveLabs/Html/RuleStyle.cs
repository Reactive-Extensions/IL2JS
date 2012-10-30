using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class RuleStyle
    {
        // Constructed by JavaScript runtime only
        public RuleStyle(JSContext ctxt) { }

        extern public string FontFamily { get; set; }
        extern public string FontStyle { get; set; }
        extern public string FontVariant { get; set; }
        extern public string FontWeight { get; set; }
        extern public string FontSize { get; set; }
        extern public string Font { get; set; }
        extern public string Color { get; set; }
        extern public string Background { get; set; }
        extern public string BackgroundColor { get; set; }
        extern public string BackgroundImage { get; set; }
        extern public string BackgroundRepeat { get; set; }
        extern public string BackgroundAttachment { get; set; }
        extern public string BackgroundPosition { get; set; }
        extern public string BackgroundPositionX { get; set; }
        extern public string BackgroundPositionY { get; set; }
        extern public string WordSpacing { get; set; }
        extern public string LetterSpacing { get; set; }
        extern public string TextDecoration { get; set; }
        extern public bool TextDecorationNone { get; set; }
        extern public bool TextDecorationUnderline { get; set; }
        extern public bool TextDecorationOverline { get; set; }
        extern public bool TextDecorationLineThrough { get; set; }
        extern public bool TextDecorationBlink { get; set; }
        extern public string VerticalAlign { get; set; }
        extern public string TextTransform { get; set; }
        extern public string TextAlign { get; set; }
        extern public string TextIndent { get; set; }
        extern public string LineHeight { get; set; }
        extern public string MarginTop { get; set; }
        extern public string MarginRight { get; set; }
        extern public string MarginBottom { get; set; }
        extern public string MarginLeft { get; set; }
        extern public string Margin { get; set; }
        extern public string PaddingTop { get; set; }
        extern public string PaddingRight { get; set; }
        extern public string PaddingBottom { get; set; }
        extern public string PaddingLeft { get; set; }
        extern public string Padding { get; set; }
        extern public string Border { get; set; }
        extern public string BorderTop { get; set; }
        extern public string BorderRight { get; set; }
        extern public string BorderBottom { get; set; }
        extern public string BorderLeft { get; set; }
        extern public string BorderColor { get; set; }
        extern public string BorderTopColor { get; set; }
        extern public string BorderRightColor { get; set; }
        extern public string BorderBottomColor { get; set; }
        extern public string BorderLeftColor { get; set; }
        extern public string BorderWidth { get; set; }
        extern public string BorderTopWidth { get; set; }
        extern public string BorderRightWidth { get; set; }
        extern public string BorderBottomWidth { get; set; }
        extern public string BorderLeftWidth { get; set; }
        extern public string BorderStyle { get; set; }
        extern public string BorderTopStyle { get; set; }
        extern public string BorderRightStyle { get; set; }
        extern public string BorderBottomStyle { get; set; }
        extern public string BorderLeftStyle { get; set; }
        extern public string Width { get; set; }
        extern public string Height { get; set; }
        extern public string StyleFloat { get; set; }
        extern public string Clear { get; set; }
        extern public string Display { get; set; }
        extern public string Visibility { get; set; }
        extern public string ListStyleType { get; set; }
        extern public string ListStylePosition { get; set; }
        extern public string ListStyleImage { get; set; }
        extern public string ListStyle { get; set; }
        extern public string WhiteSpace { get; set; }
        extern public string Top { get; set; }
        extern public string Left { get; set; }
        extern public string Position { get; set; }
        extern public string ZIndex { get; set; }
        extern public string Overflow { get; set; }
        extern public string PageBreakBefore { get; set; }
        extern public string PageBreakAfter { get; set; }
        extern public string CssText { get; set; }
        extern public string Cursor { get; set; }
        extern public string Clip { get; set; }
        extern public string Filter { get; set; }
        extern public string TableLayout { get; set; }
        extern public string BorderCollapse { get; set; }
        extern public string Direction { get; set; }
        extern public string Behavior { get; set; }
        extern public string UnicodeBidi { get; set; }
        extern public string Bottom { get; set; }
        extern public string Right { get; set; }
        extern public int PixelBottom { get; set; }
        extern public int PixelRight { get; set; }
        extern public float PosBottom { get; set; }
        extern public float PosRight { get; set; }
        extern public string ImeMode { get; set; }
        extern public string RubyAlign { get; set; }
        extern public string RubyPosition { get; set; }
        extern public string RubyOverhang { get; set; }
        extern public string LayoutGridChar { get; set; }
        extern public string LayoutGridLine { get; set; }
        extern public string LayoutGridMode { get; set; }
        extern public string LayoutGridType { get; set; }
        extern public string LayoutGrid { get; set; }
        extern public string TextAutospace { get; set; }
        extern public string WordBreak { get; set; }
        extern public string LineBreak { get; set; }
        extern public string TextJustify { get; set; }
        extern public string TextJustifyTrim { get; set; }
        extern public string TextKashida { get; set; }
        extern public string OverflowX { get; set; }
        extern public string OverflowY { get; set; }
        extern public string Accelerator { get; set; }
        extern public string LayoutFlow { get; set; }
        extern public string Zoom { get; set; }
        extern public string WordWrap { get; set; }
        extern public string TextUnderlinePosition { get; set; }
        extern public string ScrollbarBaseColor { get; set; }
        extern public string ScrollbarFaceColor { get; set; }

        [Import("scrollbar3dLightColor")]
        extern public string Scrollbar3DLightColor { get; set; }

        extern public string ScrollbarShadowColor { get; set; }
        extern public string ScrollbarHighlightColor { get; set; }
        extern public string ScrollbarDarkShadowColor { get; set; }
        extern public string ScrollbarArrowColor { get; set; }
        extern public string ScrollbarTrackColor { get; set; }
        extern public string WritingMode { get; set; }
        extern public string TextAlignLast { get; set; }
        extern public string TextKashidaSpace { get; set; }
        extern public string TextOverflow { get; set; }
        extern public string MinHeight { get; set; }
        extern public void SetAttribute(string attributeName, string attributeValue, int flags);
        extern public string GetAttribute(string attributeName, int flags);
        extern public bool RemoveAttribute(string attributeName, int flags);
    }
}