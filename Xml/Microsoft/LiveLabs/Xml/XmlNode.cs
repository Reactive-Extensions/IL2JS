using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Xml
{
    [Interop(@"function(root, inst) {
                   var c = root.XML;
                   if (c == null) {
                       c = root.XML = {};
                       var p = ""Microsoft.LiveLabs.Xml."";
                       c.Table = [
                           null,                             // 0
                           [p+""XmlElement""],               // 1
                           [p+""XmlAttribute""],             // 2
                           [p+""XmlText""],                  // 3
                           [p+""XmlCDataSection""],          // 4
                           [p+""XmlEntityReference""],       // 5
                           [p+""XmlEntity""],                // 6
                           [p+""XmlProcessingInstruction""], // 7
                           [p+""XmlComment""],               // 8
                           [p+""XmlDocument""],              // 9
                           [p+""XmlDocumentType""],          // 10
                           [p+""XmlDocumentFragment""],      // 11
                           [p+""XmlNotation""]               // 12
                       ];
                   }
                   return c.Table[inst.nodeType];
               }", State = InstanceState.JavaScriptOnly)]
    [Import]
    public class XmlNode
    {
        public XmlNode(JSContext ctxt)
        {
        }

        extern public string NodeName { get; }
        extern public string NodeValue { get; set; }
        extern public XmlNodeType NodeType { get; }
        extern public XmlNode ParentNode { get; }
        extern public XmlNodeList ChildNodes { get; }
        extern public XmlNode FirstChild { get; }
        extern public XmlNode LastChild { get; }
        extern public XmlNode PreviousSibling { get; }
        extern public XmlNode NextSibling { get; }
        extern public XmlNamedNodeMap Attributes { get; }
        extern public XmlDocument OwnerDocument { get; }
        extern public string NodeTypeString { get; }

        [Import(@"function(inst) {
                     if (inst.compareDocumentPosition != null)
                         return inst.textContent;
                     else
                         return inst.text;
                 }")]
        extern private static string GetText(XmlNode inst);

        [Import(@"function(inst, value) {
                     if (inst.compareDocumentPosition != null)
                         inst.textContent = value;
                     else
                         inst.text = value;
                 }")]
        extern private static void SetText(XmlNode inst, string value);

        public string Text
        {
            get { return GetText(this); }
            set { SetText(this, value); }
        }

        extern public bool Specified { get; }
        extern public XmlNode Definition { get; }
        extern public string NodeTypedValue { get; set; }
        extern public string DataType { get; set; }

        [Import(@"function(inst) {
                     if (inst.compareDocumentPosition != null)
                         return inst.nodeValue;
                     else
                         return inst.xml;
                 }")]
        extern private static string GetXml(XmlNode inst);

        public string Xml
        {
            get { return GetXml(this); }
        }

        extern public bool Parsed { get; }
        extern public string NamespaceURI { get; }
        extern public string Prefix { get; }
        extern public string BaseName { get; }
        extern public XmlNode InsertBefore(XmlNode newChild, XmlNode refChild);
        extern public XmlNode ReplaceChild(XmlNode newChild, XmlNode oldChild);
        extern public XmlNode RemoveChild(XmlNode childNode);
        extern public XmlNode AppendChild(XmlNode newChild);
        extern public bool HasChildNodes();
        extern public XmlNode CloneNode(bool deep);
        extern public string TransformNode(XmlNode stylesheet);

        [Import(@"function(inst, queryString) {
                      if (inst.evaluate != null) {
                          var document = inst.ownerDocument;
                          if (document == null) 
                              document = inst;
                          var result = document.evaluate(queryString, inst, null,
                                                         XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null);
                          return result;
                      }
                      else
                          return inst.selectNodes(queryString);
                  }")]
        extern private static XmlNodeList SelectNodes(XmlNode inst, string queryString);

        public XmlNodeList SelectNodes(string queryString)
        {
            return SelectNodes(this, queryString);
        }

        [Import(@"function(inst, queryString) {
                      if (inst.evaluate != null) {
                          var document = inst.ownerDocument;
                          if (document == null) 
                              document = inst;
                          var result = document.evaluate(queryString, inst, null,
                                                         XPathResult.FIRST_ORDERED_NODE_TYPE, null);
                          return result.singleNodeValue;
                      }
                      else
                          return inst.selectSingleNode(queryString);
                  }")]
        extern private static XmlNode SelectSingleNode(XmlNode inst, string queryString);

        public XmlNode SelectSingleNode(string queryString)
        {
            return SelectSingleNode(this, queryString);
        }
    }
}