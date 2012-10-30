using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Xml
{
    [Import]
	public sealed class XmlImplementation
	{
        // Constructed by JavaScript runtime only
        public XmlImplementation(JSContext ctxt)
        {
        }

        extern public bool HasFeature(string feature, string version);
	}
}