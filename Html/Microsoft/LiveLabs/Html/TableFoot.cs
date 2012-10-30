using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Import]
    public class TableFoot : TableSection
    {
        public TableFoot(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""TFOOT""); }")]
        extern public TableFoot();
    }
}