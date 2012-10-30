using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class StyleSheet
    {
        // Created by JavaScript runtime only
        public StyleSheet(JSContext ctxt) { }

        extern public string Title { get; set; }
        extern public StyleSheet ParentStyleSheet { get; }

        [Import("owningElement")]
        extern public HtmlElement Owning { get; }

        extern public bool Disabled { get; set; }
        extern public bool ReadOnly { get; }
        extern public StyleSheetCollection Imports { get; }
        extern public string Href { get; set; }
        extern public string Type { get; }
        extern public string Id { get; }
        extern public string Media { get; set; }
        extern public string CssText { get; set; }
        extern public StyleSheetRuleCollection Rules { get; }
        extern public StyleSheetPageCollection Pages { get; }
        extern public int AddImport(string url, int index);
        extern public int AddRule(string selector, string style, int index);
        extern public void RemoveImport(int index);
        extern public void RemoveRule(int index);
        extern public int AddPageRule(string selector, string style, int index);
    }
}