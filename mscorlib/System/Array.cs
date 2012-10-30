//
// A re-implementation of Array for the JavaScript runtime.
// Underlying representation is a JavaScript array with type field.
//

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

using System.Collections;
using System.Collections.Generic;

namespace System
{
    [Runtime(true)]
    public abstract class Array : ICloneable, IList
    {
        public static int BinarySearch<T>(T[] array, T value)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            return BinarySearch<T>(array, 0, array.Length, value, null);
        }

        public static int BinarySearch(Array array, object value)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            return BinarySearch(array, 0, array.Length, value, null);
        }

        public static int BinarySearch<T>(T[] array, T value, IComparer<T> comparer)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            return BinarySearch<T>(array, 0, array.Length, value, comparer);
        }

        public static int BinarySearch(Array array, object value, IComparer comparer)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            return BinarySearch(array, 0, array.Length, value, comparer);
        }

        public static int BinarySearch(Array array, int index, int length, object value)
        {
            return BinarySearch(array, index, length, value, null);
        }

        public static int BinarySearch<T>(T[] array, int index, int length, T value)
        {
            return BinarySearch<T>(array, index, length, value, null);
        }

        public static int BinarySearch(Array array, int index, int length, object value, IComparer comparer)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (length < 0)
                throw new ArgumentOutOfRangeException("length");
            if (index + length > array.Length)
                throw new ArgumentException();
            if (comparer == null)
                comparer = Comparer.Default;
            int low = index;
            int hi = index + length - 1;
            while (low <= hi)
            {
                int k;
                int i = low + (hi - low) / 2;
                try
                {
                    k = comparer.Compare(array.GetValue(i), value);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException("comparison failed", exception);
                }
                if (k == 0)
                    return i;
                if (k < 0)
                    low = i + 1;
                else
                    hi = i - 1;
            }
            return ~low;
        }

        public static int BinarySearch<T>(T[] array, int index, int length, T value, IComparer<T> comparer)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (length < 0)
                throw new ArgumentOutOfRangeException("length");
            if (index + length > array.Length)
                throw new ArgumentException();
            if (comparer == null)
                comparer = Comparer<T>.Default;
            int low = index;
            int hi = index + length - 1;
            while (low <= hi)
            {
                int k;
                int i = low + (hi - low) / 2;
                try
                {
                    k = comparer.Compare(array[i], value);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException("comparison failed", exception);
                }
                if (k == 0)
                    return i;
                if (k < 0)
                    low = i + 1;
                else
                    hi = i - 1;
            }
            return ~low;
        }

        [Import(@"function(root, array, index, length) { 
                      if (array == null)
                          throw root.NullReferenceException();
                      if (index < 0 || length < 0 || index + length > array.length)
                          throw root.InvalidOperationException();
                      var elemType = array.T.L[0];
                      if (elemType.V) {
                          for (var i = 0; i < length; i++)
                              array[index + i] = elemType.D();
                      }
                      else {
                          var d = elemType.D();
                          for (var i = 0; i < length; i++)
                              array[index + i] = d;
                      }
                  }", PassRootAsArgument = true)]
        extern public static void Clear(Array array, int index, int length);

        public virtual object Clone()
        {
            return MemberwiseClone(); // Run-time knows how to clone arrays
        }

        public static void Copy(Array sourceArray, Array destinationArray, int length)
        {
            Copy(sourceArray, 0, destinationArray, 0, length);
        }

        [Import(@"function(root, sourceArray, sourceIndex, destinationArray, destinationIndex, length) {
                      if (sourceArray == null || destinationArray == null)
                          throw root.ArgumentNullException();
                      if (sourceIndex < 0 || destinationIndex < 0 || length < 0)
                          throw root.InvalidOperationException();
                      if (sourceIndex + length > sourceArray.Length)
                          throw root.InvalidOperationException();
                      if (destinationIndex + length > destinationArray.Length)
                          throw root.InvalidOperationException();
                      var elemType = sourceArray.T.L[0];
                      if (elemType !== destinationArray.T.L[0])
                          throw root.ArrayTypeMismatchException();
                      if (elemType.V) {
                          if (sourceIndex < destinationIndex) {
                              for (var i = length - 1; i >= 0; i--)
                                  destinationArray[destinationIndex + i] = elemType.C(sourceArray[sourceIndex + i]);
                          }
                          else {
                              for (var i = 0; i < length; i++)
                                  destinationArray[destinationIndex + i] = elemType.C(sourceArray[sourceIndex + i]);
                          }
                      }
                      else {
                          if (sourceIndex < destinationIndex) {
                              for (var i = length - 1; i >= 0; i--)
                                  destinationArray[destinationIndex + i] = sourceArray[sourceIndex + i];
                          }
                          else {
                              for (var i = 0; i < length; i++)
                                  destinationArray[destinationIndex + i] = sourceArray[sourceIndex + i];
                          }
                      }
                  }", PassRootAsArgument = true)]
        extern public static void Copy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length);

        public virtual void CopyTo(Array array, int index)
        {
            Copy(this, 0, array, index, Length);
        }

        public static Array CreateInstance(Type elementType, int length)
        {
            if (elementType == null)
                throw new ArgumentNullException("elementType");
            if (length < 0)
                throw new ArgumentOutOfRangeException("length");
            return Array.PrimCreateInstance(elementType, length);
        }

        [Import("function (root, elementType, length) { return root.Y(elementType, length); }",
                PassRootAsArgument = true)]
        extern private static Array PrimCreateInstance(Type elementType, int length);

        public static bool Exists<T>(T[] array, Predicate<T> match)
        {
            return FindIndex<T>(array, match) != -1;
        }

        public static T Find<T>(T[] array, Predicate<T> match)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (match == null)
                throw new ArgumentNullException("match");
            for (var i = 0; i < array.Length; i++)
            {
                if (match(array[i]))
                    return array[i];
            }
            return default(T);
        }

        public static T[] FindAll<T>(T[] array, Predicate<T> match)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (match == null)
                throw new ArgumentNullException("match");
            var list = new List<T>();
            for (var i = 0; i < array.Length; i++)
            {
                if (match(array[i]))
                    list.Add(array[i]);
            }
            return list.ToArray();
        }

        public static int FindIndex<T>(T[] array, Predicate<T> match)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            return FindIndex<T>(array, 0, array.Length, match);
        }

        public static int FindIndex<T>(T[] array, int startIndex, Predicate<T> match)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            return FindIndex<T>(array, startIndex, array.Length - startIndex, match);
        }

        public static int FindIndex<T>(T[] array, int startIndex, int count, Predicate<T> match)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (startIndex < 0 || startIndex > array.Length)
                throw new ArgumentOutOfRangeException("startIndex");
            if (count < 0 || startIndex + count > array.Length)
                throw new ArgumentOutOfRangeException("count");
            if (match == null)
                throw new ArgumentNullException("match");
            for (var i = 0; i < count; i++)
            {
                if (match(array[startIndex + i]))
                    return i;
            }
            return -1;
        }

        public static T FindLast<T>(T[] array, Predicate<T> match)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (match == null)
                throw new ArgumentNullException("match");
            for (var i = array.Length - 1; i >= 0; i--)
            {
                if (match(array[i]))
                    return array[i];
            }
            return default(T);
        }

        public static int FindLastIndex<T>(T[] array, Predicate<T> match)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            return FindLastIndex<T>(array, array.Length - 1, array.Length, match);
        }

        public static int FindLastIndex<T>(T[] array, int startIndex, Predicate<T> match)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            return FindLastIndex<T>(array, startIndex, startIndex + 1, match);
        }

        public static int FindLastIndex<T>(T[] array, int startIndex, int count, Predicate<T> match)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (match == null)
                throw new ArgumentNullException("match");
            if (array.Length == 0)
            {
                if (startIndex != -1)
                    throw new ArgumentOutOfRangeException("startIndex");
            }
            else if (startIndex < 0 || startIndex >= array.Length)
                throw new ArgumentOutOfRangeException("startIndex");
            if (count < 0 || startIndex - count + 1 < 0)
                throw new ArgumentOutOfRangeException("count");
            int endIndex = startIndex - count + 1;
            for (int i = startIndex; i >= endIndex; i--)
            {
                if (match(array[i]))
                    return i;
            }
            return -1;
        }

        public static void ForEach<T>(T[] array, Action<T> action)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (action == null)
                throw new ArgumentNullException("action");
            for (var i = 0; i < array.Length; i++)
                action(array[i]);
        }

        // Intercepted by compiler, implemented by runtime
        public abstract IEnumerator GetEnumerator();

        [Import("function (root, inst, index) { return root.GetArrayValue(inst, index); }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern public object GetValue(int index);

        public static int IndexOf<T>(T[] array, T value)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            return IndexOf<T>(array, value, 0, array.Length);
        }

        public static int IndexOf(Array array, object value)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            return IndexOf(array, value, 0, array.Length);
        }

        public static int IndexOf<T>(T[] array, T value, int startIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            return IndexOf<T>(array, value, startIndex, array.Length - startIndex);
        }

        public static int IndexOf(Array array, object value, int startIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            return IndexOf(array, value, startIndex, array.Length - startIndex);
        }

        public static int IndexOf(Array array, object value, int startIndex, int count)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (startIndex < 0 || startIndex > array.Length)
                throw new ArgumentOutOfRangeException("startIndex");
            if (count < 0 || count > array.Length - startIndex)
                throw new ArgumentOutOfRangeException("count");
            for (var k = 0; k < count; k++)
            {
                object obj = array.GetValue(startIndex + k);
                if (obj == null)
                {
                    if (value == null)
                        return startIndex + k;
                }
                else if (obj.Equals(value))
                    return startIndex + k;
            }
            return -1;
        }

        public static int IndexOf<T>(T[] array, T value, int startIndex, int count)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (startIndex < 0 || startIndex > array.Length)
                throw new ArgumentOutOfRangeException("startIndex");
            if (count < 0 || startIndex + count > array.Length)
                throw new ArgumentOutOfRangeException("count");
            var def = EqualityComparer<T>.Default;
            for (var i = 0; i < count; i++)
            {
                if (def.Equals(array[startIndex + i], value))
                    return startIndex + i;
            }
            return -1;
        }

        public static int LastIndexOf(Array array, object value)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            return LastIndexOf(array, value, array.Length - 1, array.Length);
        }

        public static int LastIndexOf<T>(T[] array, T value)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            return LastIndexOf<T>(array, value, array.Length - 1, array.Length);
        }

        public static int LastIndexOf<T>(T[] array, T value, int startIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            return LastIndexOf<T>(array, value, startIndex, array.Length == 0 ? 0 : startIndex + 1);
        }

        public static int LastIndexOf(Array array, object value, int startIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            return LastIndexOf(array, value, startIndex, startIndex + 1);
        }

        public static int LastIndexOf<T>(T[] array, T value, int startIndex, int count)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Length == 0)
            {
                if (startIndex != -1 && startIndex != 0)
                    throw new ArgumentOutOfRangeException("startIndex");
                if (count != 0)
                    throw new ArgumentOutOfRangeException("count");
                return -1;
            }
            if (startIndex < 0 || startIndex >= array.Length)
                throw new ArgumentOutOfRangeException("startIndex");
            if (count < 0 || startIndex - count + 1 < 0)
                throw new ArgumentOutOfRangeException("count");
            var def = EqualityComparer<T>.Default;
            var endIndex = startIndex - count + 1;
            for (var i = startIndex; i >= endIndex; i--)
            {
                if (def.Equals(array[i], value))
                    return i;
            }
            return -1;
        }

        public static int LastIndexOf(Array array, object value, int startIndex, int count)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (array.Length != 0)
            {
                if (startIndex < 0 || startIndex >= array.Length)
                    throw new ArgumentOutOfRangeException("startIndex");
                if (count < 0)
                    throw new ArgumentOutOfRangeException("count");
                if (count > startIndex + 1)
                    throw new ArgumentOutOfRangeException();
                int endIndex = startIndex - count + 1;
                for (var k = startIndex; k >= endIndex; k--)
                {
                    object obj = array.GetValue(k);
                    if (obj == null)
                    {
                        if (value == null)
                            return k;
                    }
                    else if (obj.Equals(value))
                        return k;
                }
            }
            return -1;
        }

        public static void Resize<T>(ref T[] array, int newSize)
        {
            if (newSize < 0)
                throw new ArgumentOutOfRangeException("newSize");
            var sourceArray = array;
            if (sourceArray == null)
                array = new T[newSize];
            else if (sourceArray.Length != newSize)
            {
                var destinationArray = new T[newSize];
                Copy(sourceArray, 0, destinationArray, 0, Math.Min(newSize, sourceArray.Length));
                array = destinationArray;
            }
        }

        public static void Reverse(Array array)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            Reverse(array, 0, array.Length);
        }

        public static void Reverse(Array array, int index, int length)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (length < 0)
                throw new ArgumentOutOfRangeException("length");
            if (length + index > array.Length)
                throw new ArgumentException();
            int lo = index;
            int hi = index + length - 1;
            while (lo < hi)
            {
                object obj = array.GetValue(lo);
                array.SetValue(array.GetValue(hi), lo);
                array.SetValue(obj, hi);
                lo++;
                hi--;
            }
        }

        [Import("function(root, inst, value, index) { root.SetArrayValue(inst, index, value); }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern public void SetValue(object value, int index);

        public static void Sort(Array array)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            Sort(array, null, 0, array.Length, null);
        }

        public static void Sort<T>(T[] array)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            Sort<T>(array, 0, array.Length, null);
        }

        public static void Sort<T>(T[] array, IComparer<T> comparer)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            Sort<T>(array, 0, array.Length, comparer);
        }

        public static void Sort<T>(T[] array, Comparison<T> comparison)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (comparison == null)
                throw new ArgumentNullException("comparison");
            IComparer<T> comparer = new FunctorComparer<T>(comparison);
            Sort<T>(array, comparer);
        }

        public static void Sort(Array keys, Array items)
        {
            if (keys == null)
                throw new ArgumentNullException("keys");
            Sort(keys, items, 0, keys.Length, null);
        }

        public static void Sort(Array array, IComparer comparer)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            Sort(array, null, 0, array.Length, comparer);
        }

        public static void Sort<TKey, TValue>(TKey[] keys, TValue[] items)
        {
            if (keys == null)
                throw new ArgumentNullException("keys");
            Sort<TKey, TValue>(keys, items, 0, keys.Length, null);
        }

        public static void Sort<T>(T[] array, int index, int length)
        {
            Sort<T>(array, index, length, null);
        }

        public static void Sort<TKey, TValue>(TKey[] keys, TValue[] items, IComparer<TKey> comparer)
        {
            if (keys == null)
                throw new ArgumentNullException("keys");
            Sort<TKey, TValue>(keys, items, 0, keys.Length, comparer);
        }

        public static void Sort(Array keys, Array items, IComparer comparer)
        {
            if (keys == null)
                throw new ArgumentNullException("keys");
            Sort(keys, items, 0, keys.Length, comparer);
        }

        public static void Sort(Array array, int index, int length)
        {
            Sort(array, null, index, length, null);
        }

        public static void Sort(Array array, int index, int length, IComparer comparer)
        {
            Sort(array, null, index, length, comparer);
        }

        public static void Sort<T>(T[] array, int index, int length, IComparer<T> comparer)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (length < 0)
                throw new ArgumentOutOfRangeException("length");
            if (index + length > array.Length)
                throw new ArgumentException();
            if (length > 1)
                QuickSort<T, object>(array, null, index, index + length - 1, comparer);
        }

        public static void Sort(Array keys, Array items, int index, int length)
        {
            Sort(keys, items, index, length, null);
        }

        public static void Sort<TKey, TValue>(TKey[] keys, TValue[] items, int index, int length)
        {
            Sort<TKey, TValue>(keys, items, index, length, null);
        }

        public static void Sort<TKey, TValue>(TKey[] keys, TValue[] items, int index, int length, IComparer<TKey> comparer)
        {
            if (keys == null)
                throw new ArgumentNullException("keys");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (length < 0)
                throw new ArgumentOutOfRangeException("length");
            if (index + length > keys.Length || items != null && index + length > items.Length)
                throw new ArgumentException();
            if (length > 1)
                QuickSort<TKey, TValue>(keys, items, index, index + length - 1, comparer);
        }

        public static void Sort(Array keys, Array items, int index, int length, IComparer comparer)
        {
            if (keys == null)
                throw new ArgumentNullException("keys");
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (length < 0)
                throw new ArgumentOutOfRangeException("length");
            if (index + length > keys.Length || items != null && length + index > items.Length)
                throw new ArgumentException();
            if (length > 1)
                QuickSort(keys, items, index, index + length - 1, comparer);
        }

        private static void SwapIfGreaterWithItems<TKey, TValue>(TKey[] keys, TValue[] values, IComparer<TKey> comparer, int a, int b)
        {
            if (a != b && comparer.Compare(keys[a], keys[b]) > 0)
            {
                var tmp = keys[a];
                keys[a] = keys[b];
                keys[b] = tmp;
                if (values != null)
                {
                    var tmp2 = values[a];
                    values[a] = values[b];
                    values[b] = tmp2;
                }
            }
        }

        private static void QuickSort<TKey, TValue>(TKey[] keys, TValue[] values, int left, int right, IComparer<TKey> comparer)
        {
            do
            {
                int a = left;
                int b = right;
                int m = a + (b - a) / 2;
                SwapIfGreaterWithItems<TKey, TValue>(keys, values, comparer, a, m);
                SwapIfGreaterWithItems<TKey, TValue>(keys, values, comparer, a, b);
                SwapIfGreaterWithItems<TKey, TValue>(keys, values, comparer, m, b);
                var y = keys[m];
                do
                {
                    while (comparer.Compare(keys[a], y) < 0)
                        a++;
                    while (comparer.Compare(y, keys[b]) < 0)
                        b--;
                    if (a > b)
                        break;
                    if (a < b)
                    {
                        var tmp = keys[a];
                        keys[a] = keys[b];
                        keys[b] = tmp;
                        if (values != null)
                        {
                            var tmp2 = values[a];
                            values[a] = values[b];
                            values[b] = tmp2;
                        }
                    }
                    a++;
                    b--;
                }
                while (a <= b);
                if (b - left <= right - a)
                {
                    if (left < b)
                        QuickSort<TKey, TValue>(keys, values, left, b, comparer);
                    left = a;
                }
                else
                {
                    if (a < right)
                        QuickSort<TKey, TValue>(keys, values, a, right, comparer);
                    right = b;
                }
            }
            while (left < right);
        }

        private static void SwapIfGreaterWithItems(Array keys, Array values, IComparer comparer, int a, int b)
        {
            if (a != b)
            {
                try
                {
                    if (comparer.Compare(keys.GetValue(a), keys.GetValue(b)) > 0)
                    {
                        var tmp = keys.GetValue(a);
                        keys.SetValue(keys.GetValue(b), a);
                        keys.SetValue(tmp, b);
                        if (values != null)
                        {
                            var tmp2 = values.GetValue(a);
                            values.SetValue(values.GetValue(b), a);
                            values.SetValue(tmp2, b);
                        }
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    throw new ArgumentException();
                }
                catch
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private static void QuickSort(Array keys, Array values, int left, int right, IComparer comparer)
        {
            do
            {
                int a = left;
                int b = right;
                int m = a + (b - a) / 2;
                SwapIfGreaterWithItems(keys, values, comparer, a, m);
                SwapIfGreaterWithItems(keys, values, comparer, a, b);
                SwapIfGreaterWithItems(keys, values, comparer, m, b);
                var y = keys.GetValue(m);
                do
                {
                    try
                    {
                        while (comparer.Compare(keys.GetValue(a), y) < 0)
                            a++;
                        while (comparer.Compare(y, keys.GetValue(b)) < 0)
                            b--;
                    }
                    catch (IndexOutOfRangeException)
                    {
                        throw new ArgumentException();
                    }
                    catch
                    {
                        throw new InvalidOperationException();
                    }
                    if (a > b)
                        break;
                    if (a < b)
                    {
                        var tmp = keys.GetValue(a);
                        keys.SetValue(keys.GetValue(b), a);
                        keys.SetValue(tmp, b);
                        if (values != null)
                        {
                            var tmp2 = values.GetValue(a);
                            values.SetValue(values.GetValue(b), a);
                            values.SetValue(tmp2, b);
                        }
                    }
                    a++;
                    b--;
                }
                while (a <= b);
                if (b - left <= right - a)
                {
                    if (left < b)
                        QuickSort(keys, values, left, b, comparer);
                    left = a;
                }
                else
                {
                    if (a < right)
                        QuickSort(keys, values, a, right, comparer);
                    right = b;
                }
            }
            while (left < right);
        }

        int IList.Add(object value)
        {
            throw new NotSupportedException();
        }

        void IList.Clear()
        {
            Clear(this, 0, Length);
        }

        bool IList.Contains(object value)
        {
            return IndexOf(this, value) >= 0;
        }

        int IList.IndexOf(object value)
        {
            return IndexOf(this, value);
        }

        void IList.Insert(int index, object value)
        {
            throw new NotSupportedException();
        }

        void IList.Remove(object value)
        {
            throw new NotSupportedException();
        }

        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public static bool TrueForAll<T>(T[] array, Predicate<T> match)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (match == null)
                throw new ArgumentNullException("match");
            for (var i = 0; i < array.Length; i++)
            {
                if (!match(array[i]))
                    return false;
            }
            return true;
        }

        public virtual bool IsFixedSize
        {
            get { return true; }
        }

        public virtual bool IsReadOnly
        {
            get { return false; }
        }

        [Import]
        extern public int Length { get; }

        int ICollection.Count
        {
            get { return Length; }
        }

        object IList.this[int index]
        {
            get { return GetValue(index); }
            set { SetValue(value, index); }
        }

        public extern int Rank
        {
            [Import("function(root, inst) { return root.GetRank(inst); }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
            get;
        }

        [Import("function(root, inst, dimension) { return root.GetLowerBound(inst, dimension); }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        public extern int GetLowerBound(int dimension);

        [Import("function(root, inst, dimension) { return root.GetUpperBound(inst, dimension); }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        public extern int GetUpperBound(int dimension);

        public int GetLength(int dimension)
        {
            return GetUpperBound(dimension) + 1;
        }

        // Instances constructed by runtime
        [UsedType(true)]
        private class GenericEnumerator<T> : IEnumerator<T>
        {
            private T[] array;
            private int index;

            [Used(true)]
            public GenericEnumerator(T[] arr)
            {
                array = arr;
                index = -1;
            }

            T IEnumerator<T>.Current
            {
                get
                {
                    if (index < 0 || index >= array.Length)
                        throw new InvalidOperationException();
                    return array[index];
                }
            }

            void IDisposable.Dispose()
            {
                array = null;
                index = -1;
            }

            object IEnumerator.Current
            {
                get
                {
                    if (index < 0 || index >= array.Length)
                        throw new InvalidOperationException();
                    return array[index];
                }
            }

            bool IEnumerator.MoveNext()
            {
                if (index < array.Length)
                    index++;
                return index < array.Length;
            }

            void IEnumerator.Reset()
            {
                index = 0;
            }
        }

        internal sealed class FunctorComparer<T> : IComparer<T>
        {
            private Comparison<T> comparison;

            public FunctorComparer(Comparison<T> comparison)
            {
                this.comparison = comparison;
            }

            public int Compare(T x, T y)
            {
                return comparison(x, y);
            }
        }


        public object SyncRoot
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsSynchronized
        {
            get { throw new NotImplementedException(); }
        }
    }
}