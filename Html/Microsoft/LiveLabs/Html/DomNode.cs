using System.Collections;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Interop(@"function(root, inst) {
                   var c = root.HTML;
                   if (c == null) {
                       c = root.HTML = {};
                       var p = ""Microsoft.LiveLabs.Html."";
                       c.Table = {};

                       var f = function(t, n) { c.Table[t] = [p+n]; };
                       f(""DOCUMENT"", ""Document"");
                       f(""#DOCUMENT"", ""Document"");
                       f(""HTML"", ""HtmlElement"");

                       c.HtmlElement = p+""HtmlElement"";;
                       f = function(t, n) { c.Table[t] = [p+n, c.HtmlElement]; };
                       f(""A"", ""Anchor"");
                       f(""AREA"", ""Area"");
                       f(""BASE"", ""Base"");
                       f(""BODY"", ""Body"");
                       f(""BR"", ""Break"");
                       f(""BUTTON"", ""Button"");
                       f(""CENTER"", ""Center"");
                       f(""DD"", ""Definition"");
                       f(""DIV"", ""Div"");
                       f(""DL"", ""DefinitionList"");
                       f(""DT"", ""DefinitionTerm"");
                       f(""FIELDSET"", ""FieldSet"");
                       f(""FONT"", ""Font"");
                       f(""FORM"", ""Form"");
                       f(""FRAMEBASE"", ""FrameBase"");
                       f(""FRAMESET"", ""FrameSet"");
                       f(""HEAD"", ""Head"");
                       f(""HR"", ""HorizontalRule"");
                       f(""OBJECT"", ""HtmlObject"");
                       f(""IMG"", ""Image"");
                       f(""LABEL"", ""Label"");
                       f(""OPTION"", ""Option"");
                       f(""SELECT"", ""Select"");
                       f(""SPAN"", ""Span"");
                       f(""STYLEELEMENT"", ""StyleElement"");
                       f(""TABLE"", ""Table"");
                       f(""CAPTION"", ""TableCaption"");
                       f(""TD"", ""TableCell"");
                       f(""TR"", ""TableRow"");
                       f(""TH"", ""TableHeader"");
                       f(""TEXTAREA"", ""TextArea"");
                       f(""INPUT"", ""Input"");
                       f(""ABBR"", ""Abbreviation"");
                       f(""ACRONYM"", ""Acronym"");
                       f(""ADDRESS"", ""Address"");
                       f(""B"", ""Bold"");
                       f(""BDO"", ""Bidirectional"");
                       f(""BIG"", ""Big"");
                       f(""BLOCKQUOTE"", ""BlockQuote"");
                       f(""CITE"", ""Cite"");
                       f(""CODE"", ""Code"");
                       f(""COL"", ""Col"");
                       f(""COLGROUP"", ""Colgroup"");
                       f(""DFN"", ""DefiningInstance"");
                       f(""EM"", ""Emphasized"");
                       f(""I"", ""Italic"");
                       f(""KBD"", ""KeyboardFont"");
                       f(""NOBR"", ""NoBreak"");
                       f(""NOFRAMES"", ""NoFrames"");
                       f(""NOSCRIPT"", ""NoScript"");
                       f(""SAMP"", ""Sample"");
                       f(""SMALL"", ""Small"");
                       f(""STRONG"", ""Strong"");
                       f(""SUB"", ""Subscript"");
                       f(""SUP"", ""Superscript"");
                       f(""TT"", ""FixedWidthFont"");
                       f(""VAR"", ""Variable"");
                       f(""WRB"", ""SoftLineBreak"");
                       f(""LEGEND"", ""Legend"");
                       f(""LI"", ""ListItem"");
                       f(""LINK"", ""Link"");
                       f(""MAP"", ""Map"");
                       f(""META"", ""Meta"");
                       f(""OL"", ""OrderedList"");
                       f(""OPTGROUP"", ""OptionGroup"");
                       f(""P"", ""Paragraph"");
                       f(""PARAM"", ""Parameter"");
                       f(""PRE"", ""Preserve"");
                       f(""Q"", ""Quote"");
                       f(""SCRIPT"", ""Script"");
                       f(""TITLE"", ""Title"");
                       f(""U"", ""Underline"");
                       f(""UL"", ""UnorderedList"");
                       f(""INS"", ""Inserted"");
                       f(""DEL"", ""Deleted"");
                       f(""STYLE"", ""StyleElement"");" +
#if INTERNETEXPLORER
@"                     f(""BASEFONT"", ""InternetExplorer.BaseFont"");
                       f(""BGSOUND"", ""InternetExplorer.BackgroundSound"");
                       f(""EMBED"", ""InternetExplorer.Embed"");
                       f(""MARQUEE"", ""InternetExplorer.Marquee"");
                       f(""RT"", ""InternetExplorer.RubyText"");
                       f(""RUBY"", ""InternetExplorer.Ruby"");
                       f(""XMP"", ""InternetExplorer.Example"");" +
#endif
@"
                       c.FrameBase = p+""FrameBase"";
                       f = function(t, n) { c.Table[t] = [p+n, c.FrameBase, c.HtmlElement]; };
                       f(""IFRAME"", ""IFrame"");
                       f(""FRAME"", ""Frame"");

                       c.Heading = p+""Heading"";
                       f = function(t, n) { c.Table[t] = [p+n, c.Heading, c.HtmlElement]; };
                       f(""H1"", ""Heading1"");
                       f(""H2"", ""Heading2"");
                       f(""H3"", ""Heading3"");
                       f(""H4"", ""Heading4"");
                       f(""H5"", ""Heading5"");
                       f(""H6"", ""Heading6"");

                       c.TableSection = p+""TableSection"";
                       f = function(t, n) { c.Table[t] = [p+n, c.TableSection, c.HtmlElement]; };
                       f(""TBODY"", ""TableBody"");
                       f(""TFOOT"", ""TableFoot"");
                       f(""THEAD"", ""TableHead"");
                   }
                   var n = inst.nodeName;
                   if (n == null || n == """" || n.charAt(0) == ""/"")
                       return null;
                   var res = c.Table[n.toUpperCase()];
                   if (res != null)
                       return res;
                   if (inst.hasAttribute == null)
                       return [c.HtmlElement];
                   return null;
               }", State = InstanceState.JavaScriptOnly)]
    [Import]
    public class DomNode
    {
        // Constructed by JavaScript runtime only
        public DomNode(JSContext ctxt)
        {
        }
                       
        [Import("function(inst, value) { inst.appendChild(document.createTextNode(value)); }", PassInstanceAsArgument=  true)]
        extern public void Add(string value);

        [Import("appendChild")]
        extern public void Add(DomNode child);

        extern public int NodeType { get; }
        extern public DomNode ParentNode { get; }
        extern public DomNodeCollection ChildNodes { get; }
        extern public DomAttributeCollection Attributes { get; }
        extern public string NodeName { get; }
        extern public string NodeValue { get; set; }
        extern public DomNode FirstChild { get; }
        extern public DomNode LastChild { get; }
        extern public DomNode PreviousSibling { get; }
        extern public DomNode NextSibling { get; }
        extern public Document OwnerDocument { get; }
        extern public bool HasChildNodes();
        extern public DomNode InsertBefore(DomNode newChild, DomNode child);
        extern public DomNode RemoveChild(DomNode oldChild);
        extern public DomNode ReplaceChild(DomNode newChild, DomNode oldChild);
        extern public DomNode RemoveNode(bool deep);
        extern public DomNode SwapNode(DomNode otherNode);
        extern public DomNode ReplaceNode(DomNode replacement);
        extern public void AppendChild(DomNode newChild);
        extern public DomNode CloneNode(bool deep);
    }
}