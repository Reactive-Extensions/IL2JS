using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Xml
{
    [Import]
    public sealed class XmlNotation : XmlNode
	{
        // Constructed by JavaScript runtime only
        public XmlNotation(JSContext ctxt) : base(ctxt) { }

        public extern string PublicId { get; }
        public extern string SystemId { get; }
    }
}