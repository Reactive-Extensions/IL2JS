using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.LiveLabs.Extras
{
    public interface IImSeq<T> : IEnumerable<T>
    {
        int Count { get; }
        bool IsReadOnly { get; }
        bool Contains(T item);
        void CopyTo(T[] array, int arrayIndex);
        T this[int index] { get; }
        int IndexOf(T item);
    }

    public interface ISeq<T> : IImSeq<T> {
        // Re-implement from above
        new bool IsReadOnly { get; }
        new T this[int index] { get; set; }

        void Add(T item);
        void Clear();
        bool Remove(T item);
        void Insert(int index, T item);
        void RemoveAt(int index);
    }

    [DebuggerDisplay("Count = {Count}"), DebuggerTypeProxy(typeof(SeqDebugView<>))]
    public class Seq<T> : ISeq<T>
    {
        private T[] elems;
        private int c;

        public Seq()
        {
            elems = null;
            c = 0;
        }

        public Seq(int n)
        {
            elems = n <= 0 ? null : new T[n];
            c = 0;
        }

        public Seq(IImSeq<T> ts)
        {
            if (ts == null || ts.Count == 0)
            {
                elems = null;
                c = 0;
            }
            else
            {
                elems = new T[ts.Count];
                ts.CopyTo(elems, 0);
                c = ts.Count;
            }
        }

        public Seq(IList<T> ts)
        {
            if (ts == null || ts.Count == 0)
            {
                elems = null;
                c = 0;
            }
            else
            {
                elems = new T[ts.Count];
                ts.CopyTo(elems, 0);
                c = ts.Count;
            }
        }

        public Seq(params T[] ts)
        {
            if (ts == null || ts.Length == 0)
            {
                elems = null;
                c = 0;
            }
            else
            {
                elems = new T[ts.Length];
                ts.CopyTo(elems, 0);
                c = ts.Length;
            }
        }

        public IEnumerator<T> SubEnumerator(int start, int length)
        {
            return new SubSeqEnumerator<T>(this, start, length);
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (elems != null)
            {
                for (var i = 0; i < c; i++)
                    yield return elems[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get { return c; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Contains(T item)
        {
            for (var i = 0; i < c; i++)
            {
                if (elems[i].Equals(item))
                    return true;
            }
            return false;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            for (var i = 0; i < c; i++)
                array[arrayIndex + i] = elems[i];
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= c)
                    throw new IndexOutOfRangeException();
                return elems[index];
            }
            set
            {
                if (index < 0 || index >= c)
                    throw new IndexOutOfRangeException();
                elems[index] = value;
            }
        }

        public int IndexOf(T item)
        {
            for (var i = 0; i < c; i++)
            {
                if (elems[i].Equals(item))
                    return i;
            }
            return -1;
        }

        public void Insert(int index, T item)
        {
                if (index < 0 || index > c)
                    throw new IndexOutOfRangeException();
            if (elems == null)
            {
                elems = new T[1];
                elems[c++] = item;
            }
            else if (c == elems.Length)
            {
                var arr2 = new T[c*2];
                for (var i = 0; i < index; i++)
                    arr2[i] = elems[i];
                arr2[index] = item;
                for (var i = index; i < c; i++)
                    arr2[i + 1] = elems[i];
                elems = arr2;
                c++;
            }
            else
            {
                for (var i = c - 1; i >= index; i--)
                    elems[i + 1] = elems[i];
                elems[index] = item;
                c++;
            }
        }

        public void RemoveAt(int index)
        {
            RemoveRange(index, 1);
        }

        public void RemoveRange(int index, int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException();
            if (index < 0 || index + length > c)
                throw new IndexOutOfRangeException();

            c -= length;
            if (c == 0)
                elems = null;
            else if (c <= elems.Length / 2)
            {
                var arr2 = new T[c];
                for (var i = 0; i < index; i++)
                    arr2[i] = elems[i];
                for (var i = index; i < c; i++)
                    arr2[i] = elems[i + length];
                elems = arr2;
            }
            else
            {
                for (var i = index; i < c; i++)
                    elems[i] = elems[i + length];
                for (var i = c; i < c + length; i++)
                    elems[i] = default(T);
            }
        }

        public void Add(T item)
        {
            Insert(c, item);
        }

        public void Clear()
        {
            elems = null;
            c = 0;
        }

        public bool Remove(T item)
        {
            var i = IndexOf(item);
            if (i < 0)
                return false;
            RemoveAt(i);
            return true;
        }

        public void Sort(Comparison<T> cmp)
        {
            if (c > 0)
            {
                var tmp = new List<T>(c);
                for (var i = 0; i < c; i++)
                    tmp.Add(elems[i]);
                tmp.Sort(cmp);
                for (var i = 0; i < c; i++)
                    elems[i] = tmp[i];
            }
        }

        bool IImSeq<T>.IsReadOnly
        {
            get { return true; }
        }

        T IImSeq<T>.this[int index]
        {
            get
            {
                if (index < 0 || index >= c)
                    throw new IndexOutOfRangeException();
                return elems[index];
            }
        }

        public void Push(T item)
        {
            Add(item);
        }

        public T Pop()
        {
            if (c == 0)
                throw new InvalidOperationException("empty list");
            var item = elems[c-1];
            RemoveAt(c - 1);
            return item;
        }

        public T Peek()
        {
            if (c == 0)
                throw new InvalidOperationException("empty list");
            return elems[c - 1];
        }
    }

    internal sealed class SeqDebugView<T>
    {
        private IImSeq<T> seq;

        public SeqDebugView(IImSeq<T> seq)
        {
            this.seq = seq;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                var array = new T[seq.Count];
                seq.CopyTo(array, 0);
                return array;
            }
        }
    }

    public static class SeqExtensions
    {
        public static Seq<T> ToSeq<T>(this IEnumerable<T> ts)
        {
            var res = new Seq<T>();
            foreach (var t in ts)
                res.Add(t);
            return res;
        }

        public static Seq<U> ToSeq<T, U>(this IEnumerable<T> ts, Func<T, U> f)
        {
            var res = new Seq<U>();
            foreach (var t in ts)
                res.Add(f(t));
            return res;
        }
    }

    public class SubSeqEnumerator<T> : IEnumerator<T> {
        private IImSeq<T> seq;
        private readonly int start;
        private readonly int length;
        private int i;

        public SubSeqEnumerator(IImSeq<T> seq, int start, int length)
        {
            this.seq = seq;
            this.start = start;
            this.length = length;
            i = -1;
        }

        public void Dispose()
        {
            seq = null;
        }

        public bool MoveNext()
        {
            if (i < 0)
                i = 0;
            else
                i++;
            return i < length;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public T Current
        {
            get
            {
                if (i < 0 || i >= length)
                    throw new InvalidOperationException("no current element");
                return seq[start + i];
            }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}