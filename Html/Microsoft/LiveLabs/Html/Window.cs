using System;
using Microsoft.LiveLabs.JavaScript;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class Window
    {
        // Constructed by JavaScript runtime only
        public Window(JSContext ctxt) { }

        extern public int Length { get; }
        extern public int InnerWidth { get; }
        extern public int InnerHeight { get; }
        extern public FrameCollection Frames { get; }
        extern public Frame FrameElement { get; }
        extern public DataTransfer ClipboardData { get; }
        extern public string DefaultStatus { get; set; }
        extern public string Status { get; set; }

        [Import(MemberNameCasing = Casing.Exact)]
        extern public ImageFactory Image { get; }

        extern public Location Location { get; }
        extern public History History { get; }
        extern public Window Opener { get; set; }
        extern public Navigator Navigator { get; }
        extern public string Name { get; set; }
        extern public Window Parent { get; }
        extern public Window Self { get; }
        extern public Window Top { get; }

        [Import("window")]
        extern public Window CurrentWindow { get; }

        extern public Document Document { get; }

        [Import(@"function(inst) {
                      var evnt = {};
                      for (var p in inst.event)
                          evnt[p] = inst.event[p];
                      return evnt;
                  }")]
        extern private static HtmlEvent GetEvent(Window inst);

        public HtmlEvent Event { get { return GetEvent(this); } }

        extern public Screen Screen { get; }
        extern public bool Closed { get; }
        extern public Navigator ClientInformation { get; }
        extern public string OffscreenBuffering { get; set; }

        // SEE ALSO: Document, HtmlElement

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
        extern public static void AttachEvent(Window inst, string eventName, HtmlEventHandler handler);

        [Import(@"function(inst, eventName, handler) {
                      if (inst.removeEventListener != null)
                          inst.removeEventListener(eventName, handler, false);
                      else {
                          if (handler.Action != null)
                              inst.detachEvent(""on"" + eventName, handler.Action);
                      }
                  }")]
        extern public static void DetachEvent(Window inst, string eventName, HtmlEventHandler handler);

        public event HtmlEventHandler Focus { add { AttachEvent(this, "focus", value); } remove { DetachEvent(this, "focus", value); } }
        public event HtmlEventHandler Blur { add { AttachEvent(this, "blur", value); } remove { DetachEvent(this, "blur", value); } }
        public event HtmlEventHandler Load { add { AttachEvent(this, "load", value); } remove { DetachEvent(this, "load", value); } }
        public event HtmlEventHandler BeforeUnload { add { AttachEvent(this, "beforeunload", value); } remove { DetachEvent(this, "beforeunload", value); } }
        public event HtmlEventHandler Unload { add { AttachEvent(this, "unload", value); } remove { DetachEvent(this, "unload", value); } }
        public event HtmlEventHandler Help { add { AttachEvent(this, "help", value); } remove { DetachEvent(this, "help", value); } }
        public event HtmlEventHandler Resize { add { AttachEvent(this, "resize", value); } remove { DetachEvent(this, "resize", value); } }
        public event HtmlEventHandler Scroll { add { AttachEvent(this, "scroll", value); } remove { DetachEvent(this, "scroll", value); } }

        [Import("onerror")]
        extern public ErrorEventHandler OnError { get; set; }

        extern public int SetTimeout(Action callback, double msec);
        extern public void ClearTimeout(int timerID);
        extern public void Alert(string message);
        extern public bool Confirm(string message);
        extern public string Prompt(string message, string defstr);
        extern public void Close();
        extern public Window Open(string url, string name, string features, bool replace);
        extern public void Navigate(string url);
        extern public bool ShowModalDialog(string dialog, string options);
        extern public void ShowHelp(string helpUrl, string context);

        [Import("focus")]
        extern public void PerformFocus();

        [Import("blur")]
        extern public void PerformBlur();

        [Import("scroll")]
        extern public void PerformScroll(int x, int y);

        extern public int SetInterval(Action callback, int msec);
        extern public void ClearInterval(int timerID);
        extern public void ScrollBy(int x, int y);
        extern public void ScrollTo(int x, int y);
        extern public void MoveTo(int x, int y);
        extern public void MoveBy(int x, int y);
        extern public void ResizeTo(int x, int y);
        extern public void ResizeBy(int x, int y);
        extern public void Print();
        extern public Window CreatePopup();
        extern public JSObject Eval(string script);
    }
}