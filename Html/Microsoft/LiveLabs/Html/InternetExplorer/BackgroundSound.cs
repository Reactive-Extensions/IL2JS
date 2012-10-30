#if INTERNETEXPLORER
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html.InternetExplorer
{
    [Import]
    public class BackgroundSound : HtmlElement
    {
        public BackgroundSound(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""BGSOUND""); }")]
        extern public BackgroundSound();

        extern public string Src { get; set; }
    }
}
#endif