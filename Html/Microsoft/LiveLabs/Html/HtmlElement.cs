using System.Collections;
using System.Collections.Generic;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class HtmlElement : DomNode
    {
        public HtmlElement(JSContext ctxt) : base(ctxt) { }

        public string Text
        {
            get
            {
                return InnerText ?? TextContent;
            }
            set
            {
                InnerText = value;
                TextContent = value;
            }
        }

        [Import("function(tag) { return document.createElement(tag); }")]
        extern public HtmlElement(string tag);
    
        extern public string ClassName { get; set; }
        extern public string Id { get; set; }
        extern public string TagName { get; }

        [Import("parentElement")]
        extern public HtmlElement Parent { get; }

        extern public Style Style { get; }
        extern public Document Document { get; }
        extern public string Title { get; set; }
        extern public string Language { get; set; }
        extern public int SourceIndex { get; }
        extern public int RecordNumber { get; }
        extern public string Lang { get; set; }
        extern public int OffsetLeft { get; }
        extern public int OffsetTop { get; }
        extern public int OffsetWidth { get; }
        extern public int OffsetHeight { get; }
        extern public HtmlElement OffsetParent { get; }

        [Import("innerHTML")]
        extern public string InnerHtml { get; set; }

        extern public string InnerText { get; set; }

        extern public string TextContent { get; set; }

        [Import("outerHTML")]
        extern public string OuterHtml { get; set; }

        extern public string OuterText { get; set; }

        extern public HtmlElement ParentTextEdit { get; }
        extern public bool IsTextEdit { get; }
        extern public HtmlElementCollection All { get; }
        extern public string ScopeName { get; }
        extern public CurrentStyle CurrentStyle { get; }
        extern public short TabIndex { get; set; }
        extern public string AccessKey { get; set; }
        extern public int ClientHeight { get; }
        extern public int ClientWidth { get; }
        extern public int ClientTop { get; }
        extern public int ClientLeft { get; }
        extern public string ReadyState { get; }
        extern public string Dir { get; set; }
        extern public int ScrollHeight { get; }
        extern public int ScrollWidth { get; }
        extern public int ScrollTop { get; set; }
        extern public int ScrollLeft { get; set; }
        extern public bool CanHaveChildren { get; }
        extern public Style RuntimeStyle { get; }
        extern public string TagUrn { get; set; }
        extern public int ReadyStateValue { get; }
        extern public bool IsMultiLine { get; }

        [Import("canHaveHTML")]
        extern public bool CanHaveHtml { get; }

        extern public bool InflateBlock { get; set; }
        extern public string ContentEditable { get; set; }
        extern public bool IsContentEditable { get; }
        extern public bool HideFocus { get; set; }
        extern public bool Disabled { get; set; }
        extern public bool IsDisabled { get; }
        extern public int GlyphMode { get; }

        /// <summary>
        /// Returns a an ordered collection of element objects that are children of the 
        /// current element.
        /// </summary>
        /// <returns>Returns a collection of child elements of the given element.</returns>
        /// <remarks>For a list of browsers that support this property <seealso href="http://www.quirksmode.org/dom/w3c_core.html"/>.</remarks>
        [Import("children")]
        extern public HtmlElementCollection Children
        {
            get;
        }

        /// <summary>
        /// Returns the element's first child element or null if there are no child elements.
        /// </summary>
        /// <returns>An HtmlElementCollection.</returns>
        /// <remarks>See the "W3C - Element Traversal Specification" for details <seealso href="http://www.w3.org/TR/ElementTraversal/"/>.
        /// For a list of browsers that support this property <seealso href="http://www.quirksmode.org/dom/w3c_traversal.html"/>.
        /// </remarks>
        [Import("firstElementChild")]
        extern public HtmlElement FirstElementChild
        {
            get;
        }

        /// <summary>
        /// Returns the element's last child element or null if there are no child elements.
        /// </summary>
        /// <returns>An HtmlElement.</returns>
        /// <remarks>See the "W3C - Element Traversal Specification" for details <seealso href="http://www.w3.org/TR/ElementTraversal/"/>.
        /// For a list of browsers that support this property <seealso href="http://www.quirksmode.org/dom/w3c_traversal.html"/>.
        /// </remarks>        
        [Import("lastElementChild")]
        extern public HtmlElement LastElementChild
        {
            get;
        }

        /// <summary>
        /// Returns the element immediately prior to the specified one in its parent's children list, 
        /// or null if the specified element is the first one in the list.
        /// </summary>
        /// <returns>An HtmlElement.</returns>
        /// <remarks>See the "W3C - Element Traversal Specification" for details <seealso href="http://www.w3.org/TR/ElementTraversal/"/>.
        /// For a list of browsers that support this property <seealso href="http://www.quirksmode.org/dom/w3c_traversal.html"/>.
        /// </remarks>
        [Import("previousElementSibling")]
        extern public HtmlElement PreviousElementSibling
        {
            get;
        }

        /// <summary>
        /// Returns the element immediately following the specified one in its parent's children list, 
        /// or null if the specified element is the last one in the list.
        /// </summary>
        /// <returns>An HtmlElement.</returns>
        /// <remarks>See the "W3C - Element Traversal Specification" for details <seealso href="http://www.w3.org/TR/ElementTraversal/"/>.
        /// For a list of browsers that support this property <seealso href="http://www.quirksmode.org/dom/w3c_traversal.html"/>.
        /// </remarks>
        [Import("nextElementSibling")]
        extern public HtmlElement NextElementSibling
        {
            get;
        }

        // SEE ALSO: Window, Document

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
        extern public static void AttachEvent(HtmlElement inst, string eventName, HtmlEventHandler handler);

        [Import(@"function(inst, eventName, handler) {
                      if (inst.removeEventListener != null)
                          inst.removeEventListener(eventName, handler, false);
                      else {
                          if (handler.Action != null)
                              inst.detachEvent(""on"" + eventName, handler.Action);
                      }
                  }")]
        extern public static void DetachEvent(HtmlElement inst, string eventName, HtmlEventHandler handler);

        public event HtmlEventHandler Help { add { AttachEvent(this, "help", value); } remove { DetachEvent(this, "help", value); } }
        public event HtmlEventHandler Click { add { AttachEvent(this, "click", value); } remove { DetachEvent(this, "click", value); } }
        public event HtmlEventHandler DblClick { add { AttachEvent(this, "dblclick", value); } remove { DetachEvent(this, "dblclick", value); } }
        public event HtmlEventHandler KeyDown { add { AttachEvent(this, "keydown", value); } remove { DetachEvent(this, "keydown", value); } }
        public event HtmlEventHandler KeyUp { add { AttachEvent(this, "keyup", value); } remove { DetachEvent(this, "keyup", value); } }
        public event HtmlEventHandler KeyPress { add { AttachEvent(this, "keypress", value); } remove { DetachEvent(this, "keypress", value); } }
        public event HtmlEventHandler MouseOut { add { AttachEvent(this, "mouseout", value); } remove { DetachEvent(this, "mouseout", value); } }
        public event HtmlEventHandler MouseOver { add { AttachEvent(this, "mouseover", value); } remove { DetachEvent(this, "mouseover", value); } }
        public event HtmlEventHandler MouseMove { add { AttachEvent(this, "mousemove", value); } remove { DetachEvent(this, "mousemove", value); } }
        public event HtmlEventHandler MouseDown { add { AttachEvent(this, "mousedown", value); } remove { DetachEvent(this, "mousedown", value); } }
        public event HtmlEventHandler MouseUp { add { AttachEvent(this, "mouseup", value); } remove { DetachEvent(this, "mouseup", value); } }
        public event HtmlEventHandler SelectStart { add { AttachEvent(this, "selectstart", value); } remove { DetachEvent(this, "selectstart", value); } }
        public event HtmlEventHandler DragStart { add { AttachEvent(this, "dragstart", value); } remove { DetachEvent(this, "dragstart", value); } }
        public event HtmlEventHandler BeforeUpdate { add { AttachEvent(this, "beforeupdate", value); } remove { DetachEvent(this, "beforeupdate", value); } }
        public event HtmlEventHandler AfterUpdate { add { AttachEvent(this, "afterupdate", value); } remove { DetachEvent(this, "afterupdate", value); } }
        public event HtmlEventHandler ErrorUpdate { add { AttachEvent(this, "errorupdate", value); } remove { DetachEvent(this, "errorupdate", value); } }
        public event HtmlEventHandler RowExit { add { AttachEvent(this, "rowexit", value); } remove { DetachEvent(this, "rowexit", value); } }
        public event HtmlEventHandler RowEnter { add { AttachEvent(this, "rowenter", value); } remove { DetachEvent(this, "rowenter", value); } }
        public event HtmlEventHandler DataSetChanged { add { AttachEvent(this, "datasetchanged", value); } remove { DetachEvent(this, "datasetchanged", value); } }
        public event HtmlEventHandler DataAvailable { add { AttachEvent(this, "dataavailable", value); } remove { DetachEvent(this, "dataavailable", value); } }
        public event HtmlEventHandler DataSetComplete { add { AttachEvent(this, "datasetcomplete", value); } remove { DetachEvent(this, "datasetcomplete", value); } }
        public event HtmlEventHandler Load { add { AttachEvent(this, "load", value); } remove { DetachEvent(this, "load", value); } }
        public event HtmlEventHandler LoseCapture { add { AttachEvent(this, "losecapture", value); } remove { DetachEvent(this, "losecapture", value); } }
        public event HtmlEventHandler Scroll { add { AttachEvent(this, "scroll", value); } remove { DetachEvent(this, "scroll", value); } }
        public event HtmlEventHandler Drag { add { AttachEvent(this, "drag", value); } remove { DetachEvent(this, "drag", value); } }
        public event HtmlEventHandler DragEnd { add { AttachEvent(this, "dragend", value); } remove { DetachEvent(this, "dragend", value); } }
        public event HtmlEventHandler DragEnter { add { AttachEvent(this, "dragenter", value); } remove { DetachEvent(this, "dragenter", value); } }
        public event HtmlEventHandler DragOver { add { AttachEvent(this, "dragover", value); } remove { DetachEvent(this, "dragover", value); } }
        public event HtmlEventHandler DragLeave { add { AttachEvent(this, "dragleave", value); } remove { DetachEvent(this, "dragleave", value); } }
        public event HtmlEventHandler Drop { add { AttachEvent(this, "drop", value); } remove { DetachEvent(this, "drop", value); } }
        public event HtmlEventHandler BeforeCut { add { AttachEvent(this, "beforecut", value); } remove { DetachEvent(this, "beforecut", value); } }
        public event HtmlEventHandler Cut { add { AttachEvent(this, "cut", value); } remove { DetachEvent(this, "cut", value); } }
        public event HtmlEventHandler BeforeCopy { add { AttachEvent(this, "beforecopy", value); } remove { DetachEvent(this, "beforecopy", value); } }
        public event HtmlEventHandler Copy { add { AttachEvent(this, "copy", value); } remove { DetachEvent(this, "copy", value); } }
        public event HtmlEventHandler BeforePaste { add { AttachEvent(this, "beforepaste", value); } remove { DetachEvent(this, "beforepaste", value); } }
        public event HtmlEventHandler Paste { add { AttachEvent(this, "paste", value); } remove { DetachEvent(this, "paste", value); } }
        public event HtmlEventHandler PropertyChange { add { AttachEvent(this, "propertychange", value); } remove { DetachEvent(this, "propertychange", value); } }
        public event HtmlEventHandler Blur { add { AttachEvent(this, "blur", value); } remove { DetachEvent(this, "blur", value); } }
        public event HtmlEventHandler Focus { add { AttachEvent(this, "focus", value); } remove { DetachEvent(this, "focus", value); } }
        public event HtmlEventHandler Resize { add { AttachEvent(this, "resize", value); } remove { DetachEvent(this, "resize", value); } }
        public event HtmlEventHandler ReadyStateChange { add { AttachEvent(this, "readystatechange", value); } remove { DetachEvent(this, "readystatechange", value); } }
        public event HtmlEventHandler RowsDelete { add { AttachEvent(this, "rowsdelete", value); } remove { DetachEvent(this, "rowsdelete", value); } }
        public event HtmlEventHandler RowsInserted { add { AttachEvent(this, "rowsinserted", value); } remove { DetachEvent(this, "rowsinserted", value); } }
        public event HtmlEventHandler CellChange { add { AttachEvent(this, "cellchange", value); } remove { DetachEvent(this, "cellchange", value); } }
        public event HtmlEventHandler ContextMenu { add { AttachEvent(this, "contextmenu", value); } remove { DetachEvent(this, "contextmenu", value); } }
        public event HtmlEventHandler LayoutComplete { add { AttachEvent(this, "layoutcomplete", value); } remove { DetachEvent(this, "layoutcomplete", value); } }
        public event HtmlEventHandler Page { add { AttachEvent(this, "page", value); } remove { DetachEvent(this, "page", value); } }
        public event HtmlEventHandler Move { add { AttachEvent(this, "move", value); } remove { DetachEvent(this, "move", value); } }
        public event HtmlEventHandler ControlSelect { add { AttachEvent(this, "controlselect", value); } remove { DetachEvent(this, "controlselect", value); } }
        public event HtmlEventHandler ResizeStart { add { AttachEvent(this, "resizestart", value); } remove { DetachEvent(this, "resizestart", value); } }
        public event HtmlEventHandler ResizeEnd { add { AttachEvent(this, "resizeend", value); } remove { DetachEvent(this, "resizeend", value); } }
        public event HtmlEventHandler MoveStart { add { AttachEvent(this, "movestart", value); } remove { DetachEvent(this, "movestart", value); } }
        public event HtmlEventHandler MoveEnd { add { AttachEvent(this, "moveend", value); } remove { DetachEvent(this, "moveend", value); } }
        public event HtmlEventHandler MouseEnter { add { AttachEvent(this, "mouseenter", value); } remove { DetachEvent(this, "mouseenter", value); } }
        public event HtmlEventHandler MouseLeave { add { AttachEvent(this, "mouseleave", value); } remove { DetachEvent(this, "mouseleave", value); } }

        [Import("function(inst, attributeName) { return inst[attributeName]; }", PassInstanceAsArgument = true)]
        extern public string GetAttributeFromArray(string attributeName);

        extern public void SetAttribute(string attributeName, string attributeValue);
        extern public string GetAttribute(string attributeName);
        extern public bool HasAttribute(string attributeName);
        extern public bool RemoveAttribute(string attributeName);

        extern public void ScrollIntoView(bool start);
        extern public bool Contains(HtmlElement pChild);
        extern public void InsertAdjacentHtml(string where, string html);
        extern public void InsertAdjacentText(string where, string text);

        [Import("click")]
        extern public void PerformClick();

        [Import("onclick")]
        extern public void PerformOnClick();

        extern public void SetCapture(bool containerCapture);
        extern public void ReleaseCapture();
        extern public string ComponentFromPoint(int x, int y);
        extern public void DoScroll(string action);
        extern public RectangleCollection GetClientRects();
        extern public Rectangle GetBoundingClientRect();
        extern public void SetExpression(string propname, string expression, string language);
        extern public string GetExpression(string propname);
        extern public bool RemoveExpression(string propname);

        [Import("focus")]
        extern public void PerformFocus();

        [Import("blur")]
        extern public void PerformBlur();

        extern public void ClearAttributes();
        extern public HtmlElement InsertAdjacent(string where, HtmlElement inserted);
        extern public HtmlElement Apply(HtmlElement apply, string where);
        extern public string GetAdjacentText(string where);
        extern public string ReplaceAdjacentText(string where, string newText);
        extern public HtmlElementCollection GetElementsByTagName(string v);
        extern public HtmlElementCollection QuerySelectorAll(string selector);
        extern public void SetActive();
        extern public bool FireEvent(string eventName, object eventObject);
        extern public bool DragDrop();
        extern public void Normalize();
    }
}