#if INTERNETEXPLORER
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html.InternetExplorer
{
    [Import]
    public class Example : HtmlElement
    {
        public Example(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""XMP""); }")]
        extern public Example();
    }
}
#endif