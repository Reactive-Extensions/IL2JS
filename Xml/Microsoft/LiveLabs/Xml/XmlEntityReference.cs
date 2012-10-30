using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Xml
{
    [Import]
    public sealed class XmlEntityReference : XmlNode
	{
        // Constructed by JavaScript runtime only
        public XmlEntityReference(JSContext ctxt) : base(ctxt) { }
	}
}