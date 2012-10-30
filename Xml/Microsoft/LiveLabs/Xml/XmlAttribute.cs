using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Xml
{
    [Import]
	public sealed class XmlAttribute : XmlNode
	{
        // Constructed by JavaScript runtime only
        public XmlAttribute(JSContext ctxt) : base(ctxt) { }

        extern public string Name { get; }
        extern public string Value { get; set; }
	}
}