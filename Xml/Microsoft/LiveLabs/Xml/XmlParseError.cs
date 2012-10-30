using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Xml
{
    [Import]
    public sealed class XmlParseError
    {
        // Constructed by JavaScript runtime only
        public XmlParseError(JSContext ctxt)
        {
        }

        extern public int ErrorCode { get; }
        extern public string Url { get; }
        extern public string Reason { get; }
        extern public string SrcText { get; }
        extern public int Line { get; }
        extern public int Linepos { get; }
        extern public int Filepos { get; }
    }
}