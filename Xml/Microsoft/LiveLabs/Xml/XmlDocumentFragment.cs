using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Xml
{
    [Import]
    public sealed class XmlDocumentFragment : XmlNode
	{
        // Constructed by JavaScript runtime only
        public XmlDocumentFragment(JSContext ctxt) : base(ctxt) { }
	}
}