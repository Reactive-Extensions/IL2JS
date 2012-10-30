namespace Microsoft.LiveLabs.Xml
{
	public enum XmlNodeType
	{
		Invalid = 0,
		Element = 1,
		Attribute = 2,
		Text = 3,
		CData = 4,
		Reference = 5,
		Entity = 6,
		ProcessingInstruction = 7,
		Comment = 8,
		Document = 9,
		DocumentType = 10,
		DocumentFragment = 11,
		Notation = 12
	}
}