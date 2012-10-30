using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Xml
{
    [Import]
    public sealed class XmlDocumentType : XmlNode
	{
        // Constructed by JavaScript runtime only
        public XmlDocumentType(JSContext ctxt) : base(ctxt) { }

		extern public string Name { get; }
		extern public XmlNamedNodeMap Entities { get; }
		extern public XmlNamedNodeMap Notations { get; }
	}
}