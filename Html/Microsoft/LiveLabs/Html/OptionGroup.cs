using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class OptionGroup : HtmlElement
    {
        public OptionGroup(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""OPTGROUP""); }")]
        extern public OptionGroup();

        extern public string Label { get; set; }
    }
}