using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    [Interop(State = InstanceState.JavaScriptOnly)]
	[Import]
	public sealed class TextRectangle
	{
        // Created by JavaScript runtime only 
        public TextRectangle(JSContext ctxt) { }

	    extern public int Bottom { get; set; }
		extern public int Top { get; set; }
		extern public int Left { get; set; }
		extern public int Right { get; set; }
	}
}