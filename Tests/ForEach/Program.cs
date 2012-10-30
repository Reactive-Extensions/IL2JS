using System.Collections.Generic;

namespace Microsoft.LiveLabs.JavaScript.Tests
{
    static class TestForEach
    {
        static void Main()
        {
            foreach (var i in Take(10, Filter(FromTo(10, 1000000), x => x % 2 == 0)))
                TestLogger.Log(i);
        }

        static IEnumerable<T> Take<T>(int n, IEnumerable<T> stream)
        {
            var e = stream.GetEnumerator();
            for (var i = 0; i < n; i++)
            {
                e.MoveNext();
                yield return e.Current;
            }
        }

        static IEnumerable<int> FromTo(int from, int to)
        {
            for (var i = from; i <= to; i++)
            {
                yield return i;
            }
        }

        delegate bool Predicate<T>(T val);

        static IEnumerable<T> Filter<T>(IEnumerable<T> stream, Predicate<T> pred)
        {
            foreach (var t in stream)
            {
                if (pred(t))
                    yield return t;
            }
        }
    }
}