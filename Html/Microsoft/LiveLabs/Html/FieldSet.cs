using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class FieldSet : HtmlElement
    {
        public FieldSet(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""FIELDSET""); }")]
        extern public FieldSet();

        extern public string Align { get; set; }
    }
}