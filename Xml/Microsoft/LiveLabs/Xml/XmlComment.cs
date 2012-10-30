using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Xml
{
    [Import]
    public sealed class XmlComment : XmlNode
	{
        // Constructed by JavaScript runtime only
        public XmlComment(JSContext ctxt) : base(ctxt) { }

		extern public string Data { get; set; }
		extern public int Length { get; }
		extern public string SubstringData(int offset, int count);
		extern public void AppendData(string data);
		extern public void InsertData(int offset, string data);
		extern public void DeleteData(int offset, int count);
		extern public void ReplaceData(int offset, int count, string data);
	}
}