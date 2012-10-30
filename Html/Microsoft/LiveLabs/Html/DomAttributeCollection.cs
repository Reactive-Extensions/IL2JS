using System.Collections;
using System.Collections.Generic;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Html
{
    // No expandos in IE
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class DomAttributeCollection : IEnumerable<DomAttribute>
    {
        // Created by JavaScript runtime only
        public DomAttributeCollection(JSContext ctxt)
        {
        }

        extern public DomAttribute this[int index]
        {
            [Import("item")]
            get;
        }

        extern public int Length { get; }
        extern public DomAttribute GetNamedItem(string name);
        extern public DomAttribute SetNamedItem(DomAttribute node);
        extern public DomAttribute RemoveNamedItem(string name);

        public void Add(DomAttribute attribute)
        {
            SetNamedItem(attribute);
        }

        public void Add(string name, string key)
        {
            var attr = Browser.Document.CreateAttribute(name);
            attr.Value = key;
            SetNamedItem(attr);
        }

        public IEnumerator<DomAttribute> GetEnumerator()
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