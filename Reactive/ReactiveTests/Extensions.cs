using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ReactiveTests.Mocks;
using ReactiveTests.Dummies;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Concurrency;

using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Linq;

using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Disposables;

using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Collections.Generic;

namespace ReactiveTests
{
    static class Extensions
    {
        static void Append<T>(StringBuilder sb, IEnumerable<T> xs)
        {
            sb.Append("[");
            var first = true;
            foreach (var x in xs)
            {
                if (!first)
                    sb.Append(", ");
                first = false;
                sb.Append(x.ToString());
            }
            sb.Append("]");
        }

        static string Message<T>(IEnumerable<T> actual, params T[] expected)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.Append("Expected: ");
            Append(sb, expected);
            sb.AppendLine();
            sb.Append("Actual..: ");
            Append(sb, actual);
            sb.AppendLine();
            return sb.ToString();
        }

        public static void AssertEqual<T>(this IEnumerable<T> actual, params T[] expected)
        {
            var a = actual.ToArray();
            var e = expected;

            if (a.Length != e.Length)
                Assert.Fail(Message(actual, expected));
            for (var i = 0; i < a.Length; ++i)
                if (!EqualityComparer<T>.Default.Equals(a[i], e[i]))
                    Assert.Fail(Message(actual, expected));
        }

        public static void AssertEqual<T>(this IEnumerable<T> actual, IEnumerable<T> expected)
        {
            actual.AssertEqual(expected.ToArray());
        }

        public static void AssertEqual<T>(this IObservable<T> actual, IObservable<T> expected)
        {
            actual.Materialize().ToEnumerable()
                .AssertEqual(expected.Materialize().ToEnumerable());
        }

        public static IObservable<T> OnDispose<T>(this IObservable<T> xs, Action action)
        {
            return Observable.Create<T>(observer =>
            {
                var d = xs.Subscribe(observer);
                return () =>
                {
                    d.Dispose();
                    action();
                };
            });
        }
    }
}
