#if INTERNETEXPLORER
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html.InternetExplorer
{
    [Import]
    public class BaseFont : HtmlElement
    {
        public BaseFont(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""BASEFONT""); }")]
        extern public BaseFont();

        extern public string Color { get; set; }
        extern public string Face { get; set; }
        extern public int Size { get; set; }
    }
}
#endif