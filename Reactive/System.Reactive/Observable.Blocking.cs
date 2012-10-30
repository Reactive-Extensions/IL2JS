using System;
using System.Collections.Generic;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Collections.Generic;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Diagnostics;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Disposables;

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Linq
{
    public static partial class Observable
    {
        /// <summary>
        /// Converts an observable sequence to an enumerable sequence.
        /// </summary>
        public static IEnumerable<TSource> ToEnumerable<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return new AnonymousEnumerable<TSource>(()=>source.GetEnumerator());
        }

        internal static IEnumerator<TSource> PushToPull<TSource>(this IObservable<TSource> source, Action<Notification<TSource>> push, Func<Notification<TSource>> pull)
        {
            var subscription = default(IDisposable);
            var adapter = new PushPullAdapter<TSource>(push, pull, () => subscription.Dispose());
            subscription = source.Subscribe(adapter);
            return adapter;
        }

        /// <summary>
        /// Returns an enumerator that enumerates all values of the observable sequence.
        /// </summary>
        public static IEnumerator<TSource> GetEnumerator<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            var q = new Queue<Notification<TSource>>();
            var s = new Semaphore(0, int.MaxValue);
            return source.PushToPull(
                x =>
                {
                    lock (q)
                        q.Enqueue(x);
                    s.Release();
                },
                () =>
                {
                    s.WaitOne();
                    lock (q)
                        return q.Dequeue();
                });
        }

        internal static IEnumerator<TSource> GetMostRecentEnumerator<TSource>(this IObservable<TSource> source, TSource initialValue)
        {
            var notification = (Notification<TSource>)new Notification<TSource>.OnNext(initialValue);

            return source.PushToPull(
                x => notification = x,
                () => notification);
        }

        internal static IEnumerator<TSource> GetNextEnumerator<TSource>(this IObservable<TSource> source)
        {
            var s = new Semaphore(0, 1);
            var waiting = false;
            var gate = new object();
            Notification<TSource> notification = null;

            return source.PushToPull(
                x =>
                {
                    lock (gate)
                    {
                        if (waiting)
                        {
                            notification = x;
                            s.Release();
                        }
                        waiting = false;
                    }
                },
                () =>
                {
                    lock (gate)
                        waiting = true;
                    s.WaitOne();
                    return notification;
                });
        }

        internal static IEnumerator<TSource> GetLatestEnumerator<TSource>(this IObservable<TSource> source)
        {
            var gate = new object();
            Notification<TSource> notification = null;
            Notification<TSource> current = null;
            var s = new Semaphore(0, 1);

            return source.PushToPull(
                x =>
                {
                    var lackedValue = false;
                    lock (gate)
                    {
                        lackedValue = notification == null;
                        notification = x;
                    }
                    if (lackedValue)
                        s.Release();
                },
                () =>
                {
                    s.WaitOne();
                    lock (gate)
                    {
                        current = notification;
                        notification = null;
                    }
                    return current;
                });
        }

        /// <summary>
        /// Samples the most recent value (buffer of size one without consumption) in an observable sequence.
        /// </summary>
        public static IEnumerable<TSource> MostRecent<TSource>(this IObservable<TSource> source, TSource initialValue)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return new AnonymousEnumerable<TSource>(() => source.GetMostRecentEnumerator(initialValue));
        }

        /// <summary>
        /// Samples the next value (blocking without buffering) from in an observable sequence.
        /// </summary>
        public static IEnumerable<TSource> Next<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return new AnonymousEnumerable<TSource>(()=>source.GetNextEnumerator());
        }

        /// <summary>
        /// Samples the most recent value (buffer of size one with consumption) in an observable sequence.
        /// </summary>
        public static IEnumerable<TSource> Latest<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return new AnonymousEnumerable<TSource>(()=>source.GetLatestEnumerator());
        }

        /// <summary>
        /// Returns the first value of an observable sequence.
        /// </summary>
        public static TSource First<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return FirstOrDefaultInternal(source, true);

        }
        /// <summary>
        /// Returns the first value of an observable sequence, or a default value if no value is found.
        /// </summary>
        public static TSource FirstOrDefault<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return FirstOrDefaultInternal(source, false);
        }

        private static TSource FirstOrDefaultInternal<TSource>(this IObservable<TSource> source, bool throwOnEmpty)
        {
            var value = default(TSource);
            var seenValue = false;
            var ex = default(Exception);
            var gate = new Semaphore(0, int.MaxValue);

            using (source.Subscribe(
                v =>
                {
                    if (!seenValue)
                    {
                        value = v;
                    }
                    seenValue = true;
                    gate.Release();
                },
                e =>
                {
                    ex = e;
                    gate.Release();
                },
                () =>
                {
                    gate.Release();
                }))
            {
                gate.WaitOne();
            }

            if (ex != null)
                throw ex.PrepareForRethrow();

            if (throwOnEmpty && !seenValue)
                throw new InvalidOperationException("Sequence contains no elements.");

            return value;
        }


        /// <summary>
        /// Returns the last value of an observable sequence.
        /// </summary>
        public static TSource Last<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return LastOrDefaultInternal(source, true);
        }

        /// <summary>
        /// Returns the last value of an observable sequence, or a default value if no value is found.
        /// </summary>
        public static TSource LastOrDefault<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return LastOrDefaultInternal(source, false);
        }


        private static TSource LastOrDefaultInternal<TSource>(this IObservable<TSource> source, bool throwOnEmpty)
        {
            var value = default(TSource);
            var seenValue = false;
            var ex = default(Exception);
            var gate = new Semaphore(0, int.MaxValue);

            using (source.Subscribe(
                v =>
                {
                    seenValue = true;
                    value = v;
                },
                e =>
                {
                    ex = e;
                    gate.Release();
                },
                () =>
                {
                    gate.Release();
                }))
            {
                gate.WaitOne();
            }

            if (ex != null)
                throw ex.PrepareForRethrow();

            if (throwOnEmpty && !seenValue)
                throw new InvalidOperationException("Sequence contains no elements.");

            return value;
        }

        /// <summary>
        /// Returns the only value of an observable sequence, and throws an exception if there is not exactly one value in the observable sequence.
        /// </summary>
        public static TSource Single<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return SingleOrDefaultInternal(source, true);
        }

        /// <summary>
        /// Returns the only value of an observable sequence, or a default value if the observable sequence is empty; this method throws an exception if there is more than one value in the observable sequence.
        /// </summary>
        public static TSource SingleOrDefault<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return SingleOrDefaultInternal(source, false);
        }

        private static TSource SingleOrDefaultInternal<TSource>(this IObservable<TSource> source, bool throwOnEmpty)
        {
            var value = default(TSource);
            var seenValue = false;
            var ex = default(Exception);
            var gate = new Semaphore(0, int.MaxValue);

            using (source.Subscribe(
                v =>
                {
                    if (seenValue)
                    {
                        ex = new InvalidOperationException("Sequence contains more than one element.");
                        gate.Release();
                    }
                    value = v;
                    seenValue = true;

                },
                e =>
                {
                    ex = e;
                    gate.Release();
                },
                () =>
                {
                    gate.Release();
                }))
            {
                gate.WaitOne();
            }

            if (ex != null)
                throw ex.PrepareForRethrow();

            if (throwOnEmpty && !seenValue)
                throw new InvalidOperationException("Sequence contains no elements.");

            return value;
        }

        /// <summary>
        /// Invokes the observable sequence for its side-effects and blocks till the sequence is terminated.
        /// </summary>
        public static void Run<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            Run(source, _ => { }, ex => { throw ex.PrepareForRethrow(); }, () => { });
        }

        /// <summary>
        /// Invokes the observer methods for their side-effects and blocks till the sequence is terminated.
        /// </summary>
        public static void Run<TSource>(this IObservable<TSource> source, IObserver<TSource> observer)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (observer == null)
                throw new ArgumentNullException("observer");

            Run(source, observer.OnNext, observer.OnError, observer.OnCompleted);
        }

        /// <summary>
        /// Invokes the action for its side-effects on each value in the observable sequence and blocks till the sequence is terminated.
        /// </summary>
        public static void Run<TSource>(this IObservable<TSource> source, Action<TSource> onNext)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (onNext == null)
                throw new ArgumentNullException("onNext");

            Run(source, onNext, ex => { throw ex.PrepareForRethrow(); }, () => { });
        }

        /// <summary>
        /// Invokes the action for its side-effects on each value in the observable sequence and blocks till the sequence is terminated.
        /// </summary>
        public static void Run<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action onCompleted)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (onNext == null)
                throw new ArgumentNullException("onNext");
            if (onCompleted == null)
                throw new ArgumentNullException("onCompleted");

            Run(source, onNext, ex => { throw ex.PrepareForRethrow(); }, onCompleted);
        }

        /// <summary>
        /// Invokes the action for its side-effects on each value in the observable sequence and blocks till the sequence is terminated.
        /// </summary>
        public static void Run<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action<Exception> onError)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (onNext == null)
                throw new ArgumentNullException("onNext");
            if (onError == null)
                throw new ArgumentNullException("onError");

            Run(source, onNext, onError, () => { });
        }

        /// <summary>
        /// Invokes the action for its side-effects on each value in the observable sequence and blocks till the sequence is terminated.
        /// </summary>
        public static void Run<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action<Exception> onError, Action onCompleted)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (onNext == null)
                throw new ArgumentNullException("onNext");
            if (onError == null)
                throw new ArgumentNullException("onError");
            if (onCompleted == null)
                throw new ArgumentNullException("onCompleted");

            var evt = new ManualResetEvent(false);
            using (source.Subscribe(
                onNext,
                ex =>
                {
                    try
                    {
                        onError(ex);
                    }
                    finally
                    {
                        evt.Set();
                    }
                },
                () =>
                {
                    try
                    {
                        onCompleted();
                    }
                    finally
                    {
                        evt.Set();
                    }
                }
            ))
            {
                evt.WaitOne();
            }
        }
    }
}
