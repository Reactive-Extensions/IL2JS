using System.Collections;
using System.Collections.Generic;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.Xml
{
    [Interop(State = InstanceState.JavaScriptOnly)]
    [Import]
    public sealed class XmlNodeList : IEnumerable<XmlNode>
    {
        // Constructed by JavaScript runtime only
        public XmlNodeList(JSContext ctxt)
        {
        }

        extern public int Length
        {
            [Import(@"function(inst) {
                         if (inst.snapshotLength != null)
                             return inst.snapshotLength;
                         else
                             return inst.length;
                     }", PassInstanceAsArgument = true)]
            get;
        }

        extern public XmlNode this[int index]
        {
            [Import(@"function(inst, index) {
                         if (inst.snapshotItem != null)
                             return inst.snapshotItem(index);
                         else
                             return inst.item(index);
                     }", PassInstanceAsArgument = true)]
            get;
        }

        extern public XmlNode NextNode();
        extern public void Reset();

        public IEnumerator<XmlNode> GetEnumerator()
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