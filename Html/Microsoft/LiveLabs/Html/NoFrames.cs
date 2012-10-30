using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class NoFrames : HtmlElement
    {
        public NoFrames(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""NOFRAMES""); }")]
        extern public NoFrames();
    }
}