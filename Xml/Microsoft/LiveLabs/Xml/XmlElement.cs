using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Xml
{
    [Import]
    public sealed class XmlElement : XmlNode
    {
        // Constructed by JavaScript runtime only
        public XmlElement(JSContext ctxt) : base(ctxt) { }

		extern public string GetAttribute(string name);
		extern public void SetAttribute(string name, string value);
		extern public void RemoveAttribute(string name);
		extern public XmlAttribute GetAttributeNode(string name);
		extern public XmlAttribute SetAttributeNode(XmlAttribute domAttribute);
		extern public XmlAttribute RemoveAttributeNode(XmlAttribute domAttribute);
		extern public XmlNodeList GetElementsByTagName(string tagName);
		extern public void Normalize();
	}
}