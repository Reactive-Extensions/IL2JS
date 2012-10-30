using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class HtmlEvent
    {
        // Created by JavaScript runtime only
        public HtmlEvent(JSContext ctxt) { }

        extern public bool AltKey { get; }
        extern public bool CtrlKey { get; }
        extern public bool ShiftKey { get; }
        extern public bool ReturnValue { get; set; }
        extern public bool CancelBubble { get; set; }
        extern public HtmlElement FromElement { get; }
        extern public HtmlElement ToElement { get; }
        extern public int KeyCode { get; set; }
        extern public int Button { get; }
        extern public string Type { get; }
        extern public string Qualifier { get; }
        extern public int Reason { get; }
        extern public int X { get; }
        extern public int Y { get; }
        extern public int ClientX { get; }
        extern public int ClientY { get; }
        extern public int OffsetX { get; }
        extern public int OffsetY { get; }
        extern public int ScreenX { get; }
        extern public int ScreenY { get; }

        extern public HtmlElement RelatedTarget { get; }
        [Import("target")]
        extern public HtmlElement TargetElement { get; }
        [Import("currentTarget")]
        extern public HtmlElement CurrentTargetElement { get; }
        [Import("target")]
        extern public Document TargetDocument { get; }
        [Import("currentTarget")]
        extern public Document CurrentTargetDocument { get; }
        [Import("target")]
        extern public Window TargetWindow { get; }
        [Import("currentTarget")]
        extern public Window CurrentTargetWindow { get; }

        extern public void PreventDefault();
        extern public void StopPropagation();
    }
}