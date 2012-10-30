using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Anchor : HtmlElement
    {
        public Anchor(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""A""); }")]
        extern public Anchor();

        extern public string Href { get; set; }
        extern public string Target { get; set; }
        extern public string Rel { get; set; }
        extern public string Rev { get; set; }
        extern public string Urn { get; set; }

        [Import(MemberNameCasing = Casing.Exact)]
        extern public string Methods { get; set; }

        extern public string Name { get; set; }
        extern public string Host { get; set; }
        extern public string Hostname { get; set; }
        extern public string Pathname { get; set; }
        extern public string Port { get; set; }
        extern public string Protocol { get; set; }
        extern public string Search { get; set; }
        extern public string Hash { get; set; }
        extern public string ProtocolLong { get; }
        extern public string MimeType { get; }
        extern public string NameProp { get; }
        extern public string Charset { get; set; }
        extern public string Coords { get; set; }
        extern public string Hreflang { get; set; }
        extern public string Shape { get; set; }
        extern public string Type { get; set; }
    }
}