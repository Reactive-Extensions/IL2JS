using System.Collections;
using System.Collections.Generic;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    // No expandos in IE
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class HtmlElementCollection : IEnumerable<HtmlElement>
    {
        // Created by JavaScript runtime only
        public HtmlElementCollection(JSContext ctxt) { }

        extern public int Length { get; set; }

        extern public HtmlElement this[string key]
        {
            [Import("namedItem")]
            get;
        }

        extern public HtmlElement this[int index]
        {
            [Import("item")]
            get;
        }

        extern public HtmlElementCollection Tags(string tagName);

        public IEnumerator<HtmlElement> GetEnumerator()
        {
            for (var i = 0; i < Length; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}