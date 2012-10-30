using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Xml
{
    [Import]
    public sealed class XmlEntity : XmlNode
    {
        // Constructed by JavaScript runtime only
        public XmlEntity(JSContext ctxt) : base(ctxt) { }

        public extern string NotationName { get; }
        public extern string PublicId { get; }
        public extern string SystemId { get; }
    }
}