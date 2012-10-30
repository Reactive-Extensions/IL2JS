using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class BlockQuote : HtmlElement
    {
        public BlockQuote(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""BLOCKQUOTE""); }")]
        extern public BlockQuote();
    }
}