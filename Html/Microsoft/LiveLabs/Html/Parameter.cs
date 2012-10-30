using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Parameter : HtmlElement
    {
        public Parameter(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""PARAM""); }")]
        extern public Parameter();

        extern public string Name { get; set; }
        extern public string Type { get; set; }
        extern public string Value { get; set; }
        extern public string ValueType { get; set; }
    }
}