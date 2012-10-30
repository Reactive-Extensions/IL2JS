using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Xml
{
    [Import]
    public sealed class XmlDocument : XmlNode
    {
        [Import(@"function(xmlStr) {
                      if (window.DOMParser)
                      {
                          var parser = new DOMParser();
                          return parser.parseFromString(xmlStr, ""text/xml"");
                      }
                      else
                      {
                          var xmlDoc = new ActiveXObject('MSXML2.DomDocument');
                          xmlDoc.loadXML(xmlStr);
                          return xmlDoc;
                      }
                  }")]
        extern public XmlDocument(string xmlStr);

        extern public XmlDocumentType Doctype { get; }
        extern public XmlImplementation Implementation { get; }
        extern public XmlElement DocumentElement { get; set; }
        extern public int ReadyState { get; }
        extern public XmlParseError ParseError { get; }
        extern public string Url { get; }
        extern public bool Async { get; set; }
        extern public bool ValidateOnParse { get; set; }
        extern public bool ResolveExternals { get; set; }
        extern public bool PreserveWhiteSpace { get; set; }
        extern public XmlElement CreateElement(string tagName);
        extern public XmlDocumentFragment CreateDocumentFragment();
        extern public XmlText CreateTextNode(string data);
        extern public XmlComment CreateComment(string data);

        [Import("createCDATASection")]
        extern public XmlCDataSection CreateCdataSection(string data);

        extern public XmlProcessingInstruction CreateProcessingInstruction(string target, string data);
        extern public XmlAttribute CreateAttribute(string name);
        extern public XmlEntityReference CreateEntityReference(string name);
        extern public XmlNodeList GetElementsByTagName(string tagName);
        extern public XmlNode CreateNode(XmlNodeType type, string name, string namespaceUri);
        extern public XmlNode NodeFromID(string idString);
        extern public bool Load(string xmlSource);
        extern public void Abort();

        extern public void Save(string destination);
        extern public void SetProperty(string name, string value);
        extern public string GetProperty(string name);
    }
}