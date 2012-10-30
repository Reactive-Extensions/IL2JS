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
Reactive.Disposables;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Concurrency;

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Linq
{
    public static partial class Observable
    {
        /// <summary>
        /// Projects each value of an observable sequence into a new form.
        /// </summary>
        public static IObservable<TResult> Select<TSource, TResult>(this IObservable<TSource> source, Func<TSource, TResult> selector)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");

            return new AnonymousObservable<TResult>(observer => source.Subscribe(
                        x =>
                        {
                            TResult result;
                            try
                            {
                                result = selector(x);
                            }
                            catch (Exception exception)
                            {
                                observer.OnError(exception);
                                return;
                            }
                            observer.OnNext(result);
                        },
                        observer.OnError,
                        observer.OnCompleted));
        }

        /// <summary>
        /// Projects each value of an observable sequence into a new form by incorporating the element's index.
        /// </summary>
        public static IObservable<TResult> Select<TSource, TResult>(this IObservable<TSource> source, Func<TSource, int, TResult> selector)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");

            return Defer(() =>
            {
                var index = 0;
                return source.Select(x => selector(x, index++));
            });
        }

        /// <summary>
        /// Filters the values of an observable sequence based on a predicate.
        /// </summary>
        public static IObservable<TSource> Where<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (predicate == null)
                throw new ArgumentNullException("predicate");

            return new AnonymousObservable<TSource>(observer => source.Subscribe(
                        x =>
                        {
                            bool shouldRun;
                            try
                            {
                                shouldRun = predicate(x);
                            }
                            catch (Exception exception)
                            {
                                observer.OnError(exception);
                                return;
                            }
                            if (shouldRun)
                                observer.OnNext(x);
                        },
                        observer.OnError,
                        observer.OnCompleted));
        }

        /// <summary>
        /// Filters the values of an observable sequence based on a predicate by incorporating the element's index.
        /// </summary>
        public static IObservable<TSource> Where<TSource>(this IObservable<TSource> source, Func<TSource, int, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (predicate == null)
                throw new ArgumentNullException("predicate");


            return Defer(() =>
            {
                var index = 0;
                return source.Where(x => predicate(x, index++));
            });
        }

        /// <summary>
        /// Groups the elements of an observable sequence and selects the resulting elements by using a specified function.
        /// </summary>
        public static IObservable<IGroupedObservable<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IObservable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (keySelector == null)
                throw new ArgumentNullException("keySelector");
            if (elementSelector == null)
                throw new ArgumentNullException("elementSelector");

            return source.GroupBy(keySelector, elementSelector, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Groups the elements of an observable sequence according to a specified key selector function and comparer.
        /// </summary>
        public static IObservable<IGroupedObservable<TKey, TSource>> GroupBy<TSource, TKey>(this IObservable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (keySelector == null)
                throw new ArgumentNullException("keySelector");
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            return source.GroupBy(keySelector, x => x, comparer);
        }

        /// <summary>
        /// Groups the elements of an observable sequence according to a specified key selector function.
        /// </summary>
        public static IObservable<IGroupedObservable<TKey, TSource>> GroupBy<TSource, TKey>(this IObservable<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (keySelector == null)
                throw new ArgumentNullException("keySelector");

            return source.GroupBy(keySelector, x => x, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Groups the elements of an observable sequence according to a specified key selector function and comparer and selects the resulting elements by using a specified function.
        /// </summary>
        public static IObservable<IGroupedObservable<TKey, TElement>> GroupBy<TSource, TKey, TElement>(this IObservable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (keySelector == null)
                throw new ArgumentNullException("keySelector");
            if (elementSelector == null)
                throw new ArgumentNullException("elementSelector");
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            return new AnonymousObservable<IGroupedObservable<TKey, TElement>>(observer =>
            {
                var map = new Dictionary<TKey, Subject<TElement>>(comparer);

                var subscription = new MutableDisposable();
                var refCountDisposable = new RefCountDisposable(subscription);

                subscription.Disposable = source.Subscribe(x =>
                {
                    var key = default(TKey);
                    try
                    {
                        key = keySelector(x);
                    }
                    catch (Exception exception)
                    {
                        foreach (var w in map.Values)
                            w.OnError(exception);
                        observer.OnError(exception);
                        return;
                    }

                    var fireNewMapEntry = false;
                    var writer = default(Subject<TElement>);
                    try
                    {
                        if (!map.TryGetValue(key, out writer))
                        {
                            writer = new Subject<TElement>();
                            map.Add(key, writer);
                            fireNewMapEntry = true;
                        }
                    }
                    catch (Exception exception)
                    {
                        foreach (var w in map.Values)
                            w.OnError(exception);
                        observer.OnError(exception);
                        return;
                    }

                    if (fireNewMapEntry)
                        observer.OnNext(new GroupedObservable<TKey, TElement>(key, writer, refCountDisposable));

                    var element = default(TElement);
                    try
                    {
                        element = elementSelector(x);
                    }
                    catch (Exception exception)
                    {
                        foreach (var w in map.Values)
                            w.OnError(exception);
                        observer.OnError(exception);
                        return;
                    }

                    writer.OnNext(element);
                },
                e =>
                {
                    foreach (var w in map.Values)
                        w.OnError(e);
                    observer.OnError(e);
                },
                () =>
                {
                    foreach (var w in map.Values)
                        w.OnCompleted();
                    observer.OnCompleted();
                });

                return refCountDisposable;
            });
        }
        
        /// <summary>
        /// Returns a specified number of contiguous values from the start of an observable sequence.
        /// </summary>
        public static IObservable<TSource> Take<TSource>(this IObservable<TSource> source, int count)
        {
            return source.Take(count, Scheduler.CurrentThread);
        }

        /// <summary>
        /// Returns a specified number of contiguous values from the start of an observable sequence.
        /// </summary>
        public static IObservable<TSource> Take<TSource>(this IObservable<TSource> source, int count, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");

            return new AnonymousObservable<TSource>(observer =>
            {
                if (count == 0)
                {
                    source.Subscribe(_ => { }, _ => { }, () => { }).Dispose();
                    return Empty<TSource>(scheduler).Subscribe(observer);
                }

                var remaining = count;
                return source.Subscribe(
                    x =>
                    {
                        if (remaining > 0)
                        {
                            --remaining;
                            observer.OnNext(x);
                            if (remaining == 0)
                                observer.OnCompleted();
                        }
                    },
                    observer.OnError,
                    observer.OnCompleted);
            });
        }

        /// <summary>
        /// Bypasses a specified number of values in an observable sequence and then returns the remaining values.
        /// </summary>
        public static IObservable<TSource> Skip<TSource>(this IObservable<TSource> source, int count)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");

            return new AnonymousObservable<TSource>(observer =>
            {
                var remaining = count;
                return source.Subscribe(
                    x =>
                    {
                        if (remaining <= 0)
                            observer.OnNext(x);
                        else
                            remaining--;
                    },
                    observer.OnError,
                    observer.OnCompleted);
            });
        }

        /// <summary>
        /// Returns values from an observable sequence as long as a specified condition is true, and then skips the remaining values.
        /// </summary>
        public static IObservable<TSource> TakeWhile<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (predicate == null)
                throw new ArgumentNullException("predicate");

            return new AnonymousObservable<TSource>(observer =>
            {
                var running = true;
                return source.Subscribe(
                    x =>
                    {
                        if (running)
                        {
                            try
                            {
                                running = predicate(x);
                            }
                            catch (Exception exception)
                            {
                                observer.OnError(exception);
                                return;
                            }
                            if (running)
                                observer.OnNext(x);
                            else
                                observer.OnCompleted();
                        }
                    },
                    observer.OnError,
                    observer.OnCompleted);
            });
        }

        /// <summary>
        /// Bypasses values in an observable sequence as long as a specified condition is true and then returns the remaining values.
        /// </summary>
        public static IObservable<TSource> SkipWhile<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (predicate == null)
                throw new ArgumentNullException("predicate");

            return new AnonymousObservable<TSource>(observer =>
            {
                var running = false;
                return source.Subscribe(
                    x =>
                    {
                        if (!running)
                            try
                            {
                                running = !predicate(x);
                            }
                            catch (Exception exception)
                            {
                                observer.OnError(exception);
                                return;
                            }
                        if (running)
                            observer.OnNext(x);
                    },
                    observer.OnError,
                    observer.OnCompleted);
            });
        }

        /// <summary>
        /// Projects each value of an observable sequence to an observable sequence and flattens the resulting observable sequences into one observable sequence.
        /// </summary>
        public static IObservable<TOther> SelectMany<TSource, TOther>(this IObservable<TSource> source, IObservable<TOther> other)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (other == null)
                throw new ArgumentNullException("other");


            return source.SelectMany(_ => other);
        }

        /// <summary>
        /// Projects each value of an observable sequence to an observable sequence and flattens the resulting observable sequences into one observable sequence.
        /// </summary>
        public static IObservable<TResult> SelectMany<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IObservable<TResult>> selector)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");

            return source.Select(selector).Merge();
        }

        /// <summary>
        /// Projects each value of an observable sequence to an observable sequence and flattens the resulting observable sequences into one observable sequence.
        /// </summary>
        public static IObservable<TResult> SelectMany<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IObservable<TResult>> onNext, Func<Exception, IObservable<TResult>> onError, Func<IObservable<TResult>> onCompleted)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (onNext == null)
                throw new ArgumentNullException("onNext");
            if (onError == null)
                throw new ArgumentNullException("onError");
            if (onCompleted == null)
                throw new ArgumentNullException("onCompleted");

            return source.Materialize().SelectMany(notification =>
            {
                if (notification.Kind == NotificationKind.OnNext)
                    return onNext(notification.Value);
                else if (notification.Kind == NotificationKind.OnError)
                    return onError(notification.Exception);
                else
                    return onCompleted();
            });

        }

        /// <summary>
        /// Projects each value of an observable sequence to an observable sequence and flattens the resulting observable sequences into one observable sequence.
        /// </summary>
        public static IObservable<TResult> SelectMany<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");

            return new AnonymousObservable<TResult>(observer => source.Subscribe(
                        x =>
                        {
                            var xs = default(IEnumerable<TResult>);
                            try
                            {
                                xs = selector(x);
                            }
                            catch (Exception exception)
                            {
                                observer.OnError(exception);
                                return;
                            }

                            var e = xs.GetEnumerator();

                            try
                            {
                                var hasNext = true;
                                while (hasNext)
                                {
                                    hasNext = false;
                                    var current = default(TResult);

                                    try
                                    {
                                        hasNext = e.MoveNext();
                                        if (hasNext)
                                            current = e.Current;
                                    }
                                    catch (Exception exception)
                                    {
                                        observer.OnError(exception);
                                        return;
                                    }

                                    if (hasNext)
                                        observer.OnNext(current);
                                }
                            }
                            finally
                            {
                                if (e != null)
                                    e.Dispose();
                            }
                        },
                        observer.OnError,
                        observer.OnCompleted));
        }

        /// <summary>
        /// Projects each value of an observable sequence to an observable sequence, flattens the resulting observable sequences into one observable sequence, and invokes a result selector function on each value therein.
        /// </summary>
        public static IObservable<TResult> SelectMany<TSource, TCollection, TResult>(this IObservable<TSource> source, Func<TSource, IObservable<TCollection>> collectionSelector, Func<TSource, TCollection, TResult> resultSelector)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (collectionSelector == null)
                throw new ArgumentNullException("collectionSelector");
            if (resultSelector == null)
                throw new ArgumentNullException("resultSelector");

            return source.SelectMany(x => collectionSelector(x).Select(y => resultSelector(x, y)));
        }

        /// <summary>
        /// Filters values of the given type.
        /// </summary>
        public static IObservable<TResult> OfType<TResult>(this IObservable<object> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Where(x => x is TResult).Cast<TResult>();
        }

        /// <summary>
        /// Casts values to the given type.
        /// </summary>
        public static IObservable<TResult> Cast<TResult>(this IObservable<object> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Select(x => (TResult)x);
        }
    }
}
