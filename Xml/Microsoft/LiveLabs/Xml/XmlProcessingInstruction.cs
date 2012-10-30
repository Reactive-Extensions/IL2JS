using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Xml
{
    [Import]
    public sealed class XmlProcessingInstruction : XmlNode
	{
        // Constructed by JavaScript runtime only
        public XmlProcessingInstruction(JSContext ctxt) : base(ctxt) { }

		extern public string Target { get; }
		extern public string Data { get; set; }
	}
}