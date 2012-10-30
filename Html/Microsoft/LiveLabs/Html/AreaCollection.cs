using System.Collections;
using System.Collections.Generic;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    // No expandos in IE
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class AreaCollection : IEnumerable<Area>
    {
        // Constructed by JavaScript runtime only
        public AreaCollection(JSContext ctxt) { }

        extern public int Length { get; set; }

        extern public Area this[string key]
        {
            [Import("item")]
            get;
        }

        extern public Area this[int index]
        {
            [Import("item")]
            get;
        }

        extern public void Add(Area area);
        extern public void Add(Area area, int index);
        extern public HtmlElementCollection Tags(string tagName);

        public IEnumerator<Area> GetEnumerator()
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