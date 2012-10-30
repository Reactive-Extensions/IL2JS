using System.Collections.Generic;
using System.Collections;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    // No expandos in IE
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class StyleSheetCollection : IEnumerable<StyleSheet>
    {
        // Created by JavaScript runtime only
        public StyleSheetCollection(JSContext ctxt) { } 

        extern public int Length { get; }
        extern public StyleSheet this[string key] { get; }
        extern public StyleSheet this[int index] { get; }

        public IEnumerator<StyleSheet> GetEnumerator()
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