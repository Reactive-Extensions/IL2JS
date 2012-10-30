#if INTERNETEXPLORER
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html.InternetExplorer
{
    [Import]
    public class Embed : HtmlElement
    {
        public Embed(JSContext ctxt) : base(ctxt) { }

        [Import(@"function() { return document.createElement(""EMBED""); }")]
        extern public Embed();

        extern public string Hidden { get; set; }
        extern public string Palette { get; }
        extern public string Pluginspage { get; }
        extern public string Src { get; set; }
        extern public string Units { get; set; }
        extern public string Name { get; set; }
        extern public string Width { get; set; }
        extern public string Height { get; set; }
    }
}
#endif