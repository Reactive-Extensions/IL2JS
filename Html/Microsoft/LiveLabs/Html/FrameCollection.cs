using System.Collections;
using System.Collections.Generic;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    // No expandos in IE
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class FrameCollection : IEnumerable<Window>
    {
        // Created by JavaScript runtime only
        public FrameCollection(JSContext ctxt) { }

        extern public int Length { get; }

        extern public Window this[string key]
        {
            get;
        }

        extern public Window this[int index]
        {
            get;
        }

        public IEnumerator<Window> GetEnumerator()
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