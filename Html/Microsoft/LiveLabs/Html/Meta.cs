using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Meta : HtmlElement
    {
        public Meta(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""META""); }")]
        extern public Meta();

        extern public string Charset { get; set; }
        extern public string Content { get; set; }
        extern public string HttpEquiv { get; set; }
        extern public string Name { get; set; }
        extern public string Url { get; set; }
        extern public string Scheme { get; set; }
    }
}