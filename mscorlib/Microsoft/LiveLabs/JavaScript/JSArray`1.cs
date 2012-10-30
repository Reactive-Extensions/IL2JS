//
// A proxy for a JavaScript array of known type
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using System;
using System.Collections;
using System.Collections.Generic;

using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript
{
    [Import]
    public sealed class JSArray<T> : JSObject, IList<T>
    {
        [Import(@"function(size) { return new Array(size); }")]
        extern public JSArray(int size);

        public static JSArray<U> FromArray<U>(params U[] items)
        {
            if (items == null)
                return null;
            var arr = new JSArray<U>(items.Length);
            for (var i = 0; i < items.Length; i++)
                arr[i] = items[i];
            return arr;
        }

        public static T[] ToArray(JSArray<T> arr)
        {
            if (arr == null)
                return null;
            var items = new T[arr.Length];
            for (var i = 0; i < items.Length; i++)
                items[i] = arr[i];
            return items;
        }

        public new IEnumerator<T> GetEnumerator()
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
        extern public int IndexOf(T item);

        [Import(@"function(inst, index, item) { inst.splice(index, 0, item); }", PassInstanceAsArgument = true)]
        extern public void Insert(int index, T item);

        [Import(@"function(inst, index) { inst.splice(index, 1); }", PassInstanceAsArgument = true)]
        extern public void RemoveAt(int index);

        [Import(@"function(inst, item) { inst.push(item); }", PassInstanceAsArgument = true)]
        extern public void Add(T item);

        [Import(@"function(inst) { inst.splice(0, inst.length); }", PassInstanceAsArgument = true)]
        extern public void Clear();

        public bool Contains(T item) { return IndexOf(item) >= 0; }

        public void CopyTo(T[] array, int index)
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

        public bool Remove(T item)
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
        extern public T this[int index] { get; set; }
        extern public override string ToString();
        extern public string Join();
        extern public string Join(string separator);
        extern public void Reverse();
        extern public void Sort();
        extern public void Sort(Comparison<T> comparison);
        extern public JSArray<T> Concat(JSArray<T> other);
        extern public JSArray<T> Slice(int beginIndex);
        extern public JSArray<T> Slice(int beginIndex, int endIndex);
        extern public JSArray<T> Splice(int beginIndex);
        extern public JSArray<T> Splice(int beginIndex, int endIndex);
        extern public JSArray<T> Splice(int beginIndex, int endIndex, JSArray<T> other);
        extern public JSArray<T> Splice(int beginIndex, int endIndex, T item);
        extern public int Push(T item);
        extern public T Pop();
        extern public T Shift();
        extern public int Unshift(T item);
    }
}