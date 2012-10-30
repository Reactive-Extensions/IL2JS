using System.Collections;
using System.Collections.Generic;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    // No expandos in IE
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class DomNodeCollection : IEnumerable<DomNode>
    {
        // Created by JavaScript runtime only
        public DomNodeCollection(JSContext ctxt) { }

        extern public int Length { get; }

        extern public DomNode this[int index]
        {
            [Import("item")]
            get;
        }

        public IEnumerator<DomNode> GetEnumerator()
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