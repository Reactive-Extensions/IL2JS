//
// A proxy for an arbitrary JavaScript array
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript
{
    [Import]
    public class JSArray : JSObject, IList<JSObject>
    {
        [Import(@"function(size) { return new Array(size); }")]
        extern public JSArray(int size);

        public static JSArray FromArray(params JSObject[] items)
        {
            if (items == null)
                return null;
            var arr = new JSArray(items.Length);
            for (var i = 0; i < items.Length; i++)
                arr[i] = items[i];
            return arr;
        }

        public static JSArray FromArray(params object[] items)
        {
            if (items == null)
                return null;
            var arr = new JSArray(items.Length);
            for (var i = 0; i < items.Length; i++)
                arr[i] = FromObject(items[i]);
            return arr;
        }

        public static JSObject[] ToArray(JSArray arr)
        {
            if (arr == null)
                return null;
            var items = new JSObject[arr.Length];
            for (var i = 0; i < items.Length; i++)
                items[i] = arr[i];
            return items;
        }

        public new IEnumerator<JSObject> GetEnumerator()
        {
            for (var i = 0; i < Length; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [Import(@"function(inst, item) {
                      for (var i = 0; i < inst.length; i++) {
                          if (inst[i] == item)
                              return i;
                      }
                      return -1;
                  }", PassInstanceAsArgument = true)]
        extern public int IndexOf(JSObject item);

        [Import(@"function(inst, index, item) { inst.splice(index, 0, item); }", PassInstanceAsArgument = true)]
        extern public void Insert(int index, JSObject item);

        [Import(@"function(inst, index) { inst.splice(index, 1); }", PassInstanceAsArgument = true)]
        extern public void RemoveAt(int index);

        [Import(@"function(inst, item) { inst.push(item); }", PassInstanceAsArgument = true)]
        extern public void Add(JSObject item);

        [Import(@"function(inst) { inst.splice(0, inst.length); }", PassInstanceAsArgument = true)]
        extern public void Clear();

        public bool Contains(JSObject item) { return IndexOf(item) >= 0; }

        public void CopyTo(JSObject[] array, int index)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (index < 0)
                throw new ArgumentOutOfRangeException("arrayIndex");
            if (index + Length > array.Length)
                throw new ArgumentOutOfRangeException();
            for (var i = 0; i < Length; i++)
                array[index + i] = this[i];
        }

        public int Count { get { return Length; } }

        public bool IsReadOnly { get { return false; } }

        public bool Remove(JSObject item)
        {
            var i = IndexOf(item);
            if (i >= 0)
            {
                RemoveAt(i);
                return true;
            }
            else
                return false;
        }

        extern public int Length { get; }
        extern public JSObject this[int index] { get; set; }
        extern public override string ToString();
        extern public string Join();
        extern public string Join(string separator);
        extern public void Reverse();
        extern public void Sort();
        extern public void Sort(Comparison<JSObject> comparison);
        extern public JSArray Concat(JSArray other);
        extern public JSArray Slice(int beginIndex);
        extern public JSArray Slice(int beginIndex, int endIndex);
        extern public JSArray Splice(int beginIndex);
        extern public JSArray Splice(int beginIndex, int endIndex);
        extern public JSArray Splice(int beginIndex, int endIndex, JSArray other);
        extern public JSArray Splice(int beginIndex, int endIndex, JSObject item);
        extern public int Push(JSObject item);
        extern public JSObject Pop();
        extern public JSObject Shift();
        extern public int Unshift(JSObject item);
    }
}