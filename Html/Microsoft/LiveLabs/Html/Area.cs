using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Area : HtmlElement
    {
        public Area(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""AREA""); }")]
        extern public Area();

        extern public string Shape { get; set; }
        extern public string Coords { get; set; }
        extern public string Href { get; set; }
        extern public string Target { get; set; }
        extern public string Alt { get; set; }
        extern public bool NoHref { get; set; }
        extern public string Host { get; set; }
        extern public string Hostname { get; set; }
        extern public string Pathname { get; set; }
        extern public string Port { get; set; }
        extern public string Protocol { get; set; }
        extern public string Search { get; set; }
        extern public string Hash { get; set; }
    }
}