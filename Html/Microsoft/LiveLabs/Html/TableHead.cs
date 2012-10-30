using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class TableHead : TableSection
    {
        public TableHead(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""THEAD""); }")]
        extern public TableHead();
    }
}