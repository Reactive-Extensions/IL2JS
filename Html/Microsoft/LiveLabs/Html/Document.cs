using System;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class Document : DomNode
    {
        public Document(JSContext ctxt) : base(ctxt) { }

        extern public HtmlElementCollection All { get; }
        extern public Body Body { get; }
        extern public HtmlElement ActiveElement { get; }
        extern public HtmlElementCollection Images { get; }
        extern public HtmlElementCollection Applets { get; }
        extern public HtmlElementCollection Links { get; }
        extern public HtmlElementCollection Forms { get; }
        extern public HtmlElementCollection Anchors { get; }
        extern public string Title { get; set; }
        extern public HtmlElementCollection Scripts { get; }
        extern public string DesignMode { get; set; }
        extern public SelectionObject Selection { get; }
        extern public string ReadyState { get; }
        extern public FrameCollection Frames { get; }
        extern public HtmlElementCollection Embeds { get; }
        extern public HtmlElementCollection Plugins { get; }
        extern public string AlinkColor { get; set; }
        extern public string BgColor { get; set; }
        extern public string FgColor { get; set; }
        extern public string LinkColor { get; set; }
        extern public string VlinkColor { get; set; }
        extern public string Referrer { get; }
        extern public Location Location { get; }
        extern public string LastModified { get; }
        extern public string Url { get; set; }
        extern public string Domain { get; set; }
        extern public string Cookie { get; set; }
        extern public bool Expando { get; set; }
        extern public string Charset { get; set; }
        extern public string DefaultCharset { get; set; }
        extern public string MimeType { get; }
        extern public string FileSize { get; }
        extern public string FileCreatedDate { get; }
        extern public string FileModifiedDate { get; }
        extern public string FileUpdatedDate { get; }
        extern public string Security { get; }
        extern public string Protocol { get; }
        extern public string NameProp { get; }
        extern public Window ParentWindow { get; }

        /// <summary>
        /// Generally a reference to the window object for the document.
        /// </summary>
        /// <return>A reference to the default AbstractView for the document, 
        /// or null if none available </return>
        extern public Window DefaultView { get; }

        extern public StyleSheetCollection StyleSheets { get; }
        extern public HtmlElement DocumentElement { get; }
        extern public string UniqueID { get; }
        extern public string Dir { get; set; }
        extern public Document ParentDocument { get; }
        extern public bool EnableDownload { get; set; }
        extern public string BaseUrl { get; set; }
        extern public bool InheritStyleSheets { get; set; }
        extern public string Media { get; set; }

        [Import("URLUnencoded")]
        extern public string UrlUnencoded { get; }

        extern public DomNode Doctype { get; }
        extern public DomImplementation Implementation { get; }
        extern public int DocumentMode { get; }
        extern public string CompatMode { get; }

        public HtmlElement this[string id]
        {
            get
            {
                return GetById(id);
            }
        }

        // SEE ALSO: Window, HtmlElement

        [Import(@"function(inst, eventName, handler) {
                      if (inst.addEventListener != null)
                          inst.addEventListener(eventName, handler, false);
                      else {
                          var action = function() { 
                              var evnt = {};
                              for (var p in window.event)
                                  evnt[p] = window.event[p];
                              evnt.target = evnt.srcElement;
                              evnt.currentTarget = inst;
                              handler(evnt);
                          };
                          handler.Action = action;
                          inst.attachEvent(""on"" + eventName, action);
                      }
                  }")]
        extern public static void AttachEvent(Document inst, string eventName, HtmlEventHandler handler);

        [Import(@"function(inst, eventName, handler) {
                      if (inst.removeEventListener != null)
                          inst.removeEventListener(eventName, handler, false);
                      else {
                          if (handler.Action != null)
                              inst.detachEvent(""on"" + eventName, handler.Action);
                      }
                  }")]
        extern public static void DetachEvent(Document inst, string eventName, HtmlEventHandler handler);

        public event HtmlEventHandler Help { add { AttachEvent(this, "help", value); } remove { DetachEvent(this, "help", value); } }
        public event HtmlEventHandler Click { add { AttachEvent(this, "click", value); } remove { DetachEvent(this, "click", value); } }
        public event HtmlEventHandler DblClick { add { AttachEvent(this, "dblclick", value); } remove { DetachEvent(this, "dblclick", value); } }
        public event HtmlEventHandler KeyUp { add { AttachEvent(this, "keyup", value); } remove { DetachEvent(this, "keyup", value); } }
        public event HtmlEventHandler KeyDown { add { AttachEvent(this, "keydown", value); } remove { DetachEvent(this, "keydown", value); } }
        public event HtmlEventHandler KeyPress { add { AttachEvent(this, "keypress", value); } remove { DetachEvent(this, "keypress", value); } }
        public event HtmlEventHandler MouseUp { add { AttachEvent(this, "mouseup", value); } remove { DetachEvent(this, "mouseup", value); } }
        public event HtmlEventHandler MouseDown { add { AttachEvent(this, "mousedown", value); } remove { DetachEvent(this, "mousedown", value); } }
        public event HtmlEventHandler MouseMove { add { AttachEvent(this, "mousemove", value); } remove { DetachEvent(this, "mousemove", value); } }
        public event HtmlEventHandler MouseOut { add { AttachEvent(this, "mouseout", value); } remove { DetachEvent(this, "mouseout", value); } }
        public event HtmlEventHandler MouseOver { add { AttachEvent(this, "mouseover", value); } remove { DetachEvent(this, "mouseover", value); } }
        public event HtmlEventHandler ReadyStateChange { add { AttachEvent(this, "readystatechange", value); } remove { DetachEvent(this, "readystatechange", value); } }
        public event HtmlEventHandler AfterUpdate { add { AttachEvent(this, "afterupdate", value); } remove { DetachEvent(this, "afterupdate", value); } }
        public event HtmlEventHandler RowExit { add { AttachEvent(this, "rowexit", value); } remove { DetachEvent(this, "rowexit", value); } }
        public event HtmlEventHandler RowEnter { add { AttachEvent(this, "rowenter", value); } remove { DetachEvent(this, "rowenter", value); } }
        public event HtmlEventHandler DragStart { add { AttachEvent(this, "dragstart", value); } remove { DetachEvent(this, "dragstart", value); } }
        public event HtmlEventHandler SelectStart { add { AttachEvent(this, "selectstart", value); } remove { DetachEvent(this, "selectstart", value); } }
        public event HtmlEventHandler BeforeUpdate { add { AttachEvent(this, "beforeupdate", value); } remove { DetachEvent(this, "beforeupdate", value); } }
        public event HtmlEventHandler ErrorUpdate { add { AttachEvent(this, "errorupdate", value); } remove { DetachEvent(this, "errorupdate", value); } }
        public event HtmlEventHandler RowsDelete { add { AttachEvent(this, "rowsdelete", value); } remove { DetachEvent(this, "rowsdelete", value); } }
        public event HtmlEventHandler RowsInserted { add { AttachEvent(this, "rowsinserted", value); } remove { DetachEvent(this, "rowsinserted", value); } }
        public event HtmlEventHandler CellChange { add { AttachEvent(this, "cellchange", value); } remove { DetachEvent(this, "cellchange", value); } }
        public event HtmlEventHandler DataSetChanged { add { AttachEvent(this, "datasetchanged", value); } remove { DetachEvent(this, "datasetchanged", value); } }
        public event HtmlEventHandler DataAvailable { add { AttachEvent(this, "dataavailable", value); } remove { DetachEvent(this, "dataavailable", value); } }
        public event HtmlEventHandler DataSetComplete { add { AttachEvent(this, "datasetcomplete", value); } remove { DetachEvent(this, "datasetcomplete", value); } }
        public event HtmlEventHandler PropertyChange { add { AttachEvent(this, "propertychange", value); } remove { DetachEvent(this, "propertychange", value); } }
        public event HtmlEventHandler ContextMenu { add { AttachEvent(this, "contextmenu", value); } remove { DetachEvent(this, "contextmenu", value); } }
        public event HtmlEventHandler Stop { add { AttachEvent(this, "stop", value); } remove { DetachEvent(this, "stop", value); } }
        public event HtmlEventHandler BeforeEditFocus { add { AttachEvent(this, "beforeeditfocus", value); } remove { DetachEvent(this, "beforeeditfocus", value); } }

        extern public Document Open(string url, string name, string features, bool replace);
        extern public void Close();
        extern public void Clear();
        extern public HtmlElement CreateElement(string eTag);
        extern public HtmlElement ElementFromPoint(int x, int y);
        extern public StyleSheet CreateStyleSheet(string href);
        extern public void ReleaseCapture();
        extern public void Recalc(bool fForce);
        extern public DomNode CreateTextNode(string text);
        extern public Document CreateDocumentFragment();
        extern public HtmlElementCollection GetElementsByName(string v);

        [Import("getElementById")]
        extern public HtmlElement GetById(string v);

        public T GetById<T>(string v) where T : HtmlElement
        {
            return (T)GetById(v);
        }

        extern public HtmlElementCollection GetElementsByTagName(string v);

        [Import("focus")]
        extern public void PerformFocus();

        extern public bool HasFocus();
        extern public Document CreateDocumentFromUrl(string url, string options);
        extern public HtmlEvent CreateEventObject(object eventObject);
        extern public bool FireEvent(string eventName, object eventObject);
        extern public RenderStyle CreateRenderStyle(string v);
        extern public DomAttribute CreateAttribute(string bstrattrName);
        extern public DomNode CreateComment(string bstrdata);
    }

    public static class DocumentExtensions
    {

        public static Head Head(this Document doc)
        {
            var elems = doc.GetElementsByTagName("head");
            if (elems.Length != 1)
                throw new InvalidOperationException("cannot find document 'head' element");
            var res = elems[0] as Head;
            if (res == null)
                throw new InvalidOperationException("cannot find document 'head' element");
            return res;
        }

        public static void IncludeStyleSheets(this Document doc, params string[] hrefs)
        {
            var head = Head(doc);
            foreach (var href in hrefs)
            {
                var link = new Link { Href = href, Rel = "stylesheet", Type = "text/css" };
                head.AppendChild(link);
            }
        }

        private static void IncludeScriptsFrom(this Document doc, Action loaded, int i, string[] srcs)
        {
            if (i >= srcs.Length)
                loaded();
            else
            {
                var head = Head(doc);
                var script = new Script { Src = srcs[i], Type = "text/javascript" };
                script.SetAttribute("firedAlready", "false");
                script.ReadyStateChange += _ =>
                                               {
                                                   if ((script.ReadyState == "loaded" ||
                                                        script.ReadyState == "complete") &&
                                                       script.GetAttribute("firedAlready") == "false")
                                                   {
                                                       script.SetAttribute("firedAlready", "true");
                                                       IncludeScriptsFrom(doc, loaded, i + 1, srcs);
                                                   }
                                               };
                script.Load += _ =>
                                   {
                                       if (script.GetAttribute("firedAlready") == "false")
                                       {
                                           script.SetAttribute("firedAlready", "true");
                                           IncludeScriptsFrom(doc, loaded, i + 1, srcs);
                                       }
                                   };
                head.AppendChild(script);
            }
        }

        public static void IncludeScripts(this Document doc, Action loaded, params string[] srcs)
        {
            IncludeScriptsFrom(doc, loaded, 0, srcs);
        }

        public static void IncludeScripts(this Document doc, params string[] srcs)
        {
            var head = Head(doc);
            foreach (var src in srcs)
            {
                var script = new Script { Src = src, Type = "text/javascript" };
                head.AppendChild(script);
            }
        }
    }
}