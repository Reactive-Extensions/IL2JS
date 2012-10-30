using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Map : HtmlElement
    {
        public Map(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""MAP""); }")]
        extern public Map();

        extern public AreaCollection Areas { get; }
        extern public string Name { get; set; }
    }
}