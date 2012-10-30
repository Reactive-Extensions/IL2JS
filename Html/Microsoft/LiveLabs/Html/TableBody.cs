using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class TableBody : TableSection
    {
        public TableBody(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""TBODY""); }")]
        extern public TableBody();
    }
}