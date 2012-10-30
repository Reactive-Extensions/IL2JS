using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Xml
{
    [Import]
    public sealed class XmlNamedNodeMap
    {
        // Constructed by JavaScript runtime only
        public XmlNamedNodeMap(JSContext ctxt)
        {
        }

        extern public XmlNode this[int index]
        {
            [Import("item")]
            get;
        }

        extern public int Length { get; }
        extern public XmlNode GetNamedItem(string name);
        extern public XmlNode SetNamedItem(XmlNode newItem);
        extern public XmlNode RemoveNamedItem(string name);
        extern public XmlNode GetQualifiedItem(string baseName, string namespaceUri);
        extern public XmlNode RemoveQualifiedItem(string baseName, string namespaceUri);
        extern public XmlNode NextNode();
        extern public void Reset();
    }
}