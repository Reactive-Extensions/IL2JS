using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class KeyboardFont : HtmlElement
    {
        public KeyboardFont(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""KBD""); }")]
        extern public KeyboardFont();
    }
}