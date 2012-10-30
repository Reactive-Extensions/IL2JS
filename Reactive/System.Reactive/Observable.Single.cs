using System;
using System.Collections.Generic;
using System.Linq;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Collections.Generic;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Concurrency;
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
        /// Materializes the implicit notifications of an observable sequence as explicit notification values.
        /// </summary>
        public static IObservable<Notification<TSource>> Materialize<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return new AnonymousObservable<Notification<TSource>>(observer =>
                source.Subscribe(
                    value => observer.OnNext(new Notification<TSource>.OnNext(value)),
                    exception =>
                    {
                        observer.OnNext(new Notification<TSource>.OnError(exception));
                        observer.OnCompleted();
                    },
                    () =>
                    {
                        observer.OnNext(new Notification<TSource>.OnCompleted());
                        observer.OnCompleted();
                    }));
        }

        /// <summary>
        /// Dematerializes the explicit notification values of an observable sequence as implicit notifications.
        /// </summary>
        public static IObservable<TSource> Dematerialize<TSource>(this IObservable<Notification<TSource>> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return new AnonymousObservable<TSource>(observer =>
                source.Subscribe(x => x.Accept(observer), observer.OnError, observer.OnCompleted));
        }

        /// <summary>
        /// Hides the identity of an observable sequence.
        /// </summary>
        public static IObservable<TSource> AsObservable<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return new AnonymousObservable<TSource>(observer => source.Subscribe(observer));
        }

        /// <summary>
        /// Projects each value of an observable sequence into a buffer.
        /// </summary>
        public static IObservable<IList<TSource>> BufferWithCount<TSource>(this IObservable<TSource> source, int count, int skip)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (count <= 0)
                throw new ArgumentOutOfRangeException("count");
            if (skip <= 0)
                throw new ArgumentOutOfRangeException("skip");

            return new AnonymousObservable<IList<TSource>>(observer =>
            {
                var list = new List<TSource>();
                var n = 0;
                return source.Subscribe(
                    x =>
                    {
                        if (n == 0)
                            list.Add(x);
                        else
                            n--;

                        if (list.Count == count)
                        {
                            var result = list.ToList();
                            list.RemoveRange(0, Math.Min(skip, list.Count));
                            n = Math.Max(0, skip - count);
                            observer.OnNext(result);
                        }
                    },
                    ex =>
                    {
                        var result = list.ToList();
                        if (result.Count > 0)
                            observer.OnNext(result);

                        observer.OnError(ex);
                    },
                    () =>
                    {
                        var result = list.ToList();
                        if (result.Count > 0)
                            observer.OnNext(result);

                        observer.OnCompleted();
                    }
                );
            });
        }

        /// <summary>
        /// Projects each value of an observable sequence into a buffer.
        /// </summary>
        public static IObservable<IList<TSource>> BufferWithCount<TSource>(this IObservable<TSource> source, int count)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (count <= 0)
                throw new ArgumentOutOfRangeException("count");

            return source.BufferWithCount(count, count);
        }

        /// <summary>
        /// Prepends a sequence values to an observable sequence.
        /// </summary>
        public static IObservable<TSource> StartWith<TSource>(this IObservable<TSource> source, params TSource[] values)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.StartWith(Scheduler.CurrentThread, values);
        }

        /// <summary>
        /// Prepends a sequence values to an observable sequence.
        /// </summary>
        public static IObservable<TSource> StartWith<TSource>(this IObservable<TSource> source, IScheduler scheduler, params TSource[] values)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return values.ToObservable(scheduler).Concat(source, Scheduler.Immediate);
        }

        /// <summary>
        /// Applies an accumulator function over an observable sequence and returns each intermediate result.  
        /// The specified seed value is used as the initial accumulator value.
        /// </summary>
        public static IObservable<TAccumulate> Scan<TSource, TAccumulate>(this IObservable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> accumulator)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (accumulator == null)
                throw new ArgumentNullException("accumulator");

            return Defer(() =>
            {
                var accumulation = default(TAccumulate);
                var hasAccumulation = false;
                return source.Select(x =>
                {
                    if (hasAccumulation)
                        accumulation = accumulator(accumulation, x);
                    else
                    {
                        accumulation = accumulator(seed, x);
                        hasAccumulation = true;
                    }
                    return accumulation;
                });
            });
        }

        /// <summary>
        /// Applies an accumulator function over an observable sequence and returns each intermediate result.  
        /// </summary>
        public static IObservable<TSource> Scan<TSource>(this IObservable<TSource> source, Func<TSource, TSource, TSource> accumulator)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (accumulator == null)
                throw new ArgumentNullException("accumulator");

            return Defer(() =>
            {
                var accumulation = default(TSource);
                var hasAccumulation = false;
                return source.Select(x =>
                {
                    if (hasAccumulation)
                        accumulation = accumulator(accumulation, x);
                    else
                    {
                        accumulation = x;
                        hasAccumulation = true;
                    }
                    return accumulation;
                });
            });
        }

        /// <summary>
        /// Returns an observable sequence that contains only distinct contiguous values according to the keySelector and comparer.
        /// </summary>
        public static IObservable<TSource> DistinctUntilChanged<TSource, TKey>(this IObservable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (keySelector == null)
                throw new ArgumentNullException("keySelector");
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            return new AnonymousObservable<TSource>(observer =>
            {
                var currentKey = default(TKey);
                var hasCurrentKey = false;
                return source.Subscribe(
                    value =>
                    {
                        var key = default(TKey);
                        try
                        {
                            key = keySelector(value);
                        }
                        catch (Exception exception)
                        {
                            observer.OnError(exception);
                            return;
                        }
                        var comparerEquals = false;
                        if (hasCurrentKey)
                        {
                            try
                            {
                                comparerEquals = comparer.Equals(currentKey, key);
                            }
                            catch (Exception exception)
                            {
                                observer.OnError(exception);
                                return;
                            }
                        }
                        if (!hasCurrentKey || !comparerEquals)
                        {
                            hasCurrentKey = true;
                            currentKey = key;
                            observer.OnNext(value);
                        }
                    },
                    observer.OnError,
                    observer.OnCompleted);

            });
        }

        /// <summary>
        /// Returns an observable sequence that contains only distinct contiguous values according to the comparer.
        /// </summary>
        public static IObservable<TSource> DistinctUntilChanged<TSource>(this IObservable<TSource> source, IEqualityComparer<TSource> comparer)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            return source.DistinctUntilChanged(x => x, comparer);
        }

        /// <summary>
        /// Returns an observable sequence that contains only distinct contiguous values according to the keySelector.
        /// </summary>
        public static IObservable<TSource> DistinctUntilChanged<TSource, TKey>(this IObservable<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (keySelector == null)
                throw new ArgumentNullException("keySelector");

            return source.DistinctUntilChanged(keySelector, EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Returns an observable sequence that contains only distinct contiguous values.
        /// </summary>
        public static IObservable<TSource> DistinctUntilChanged<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.DistinctUntilChanged(x => x, EqualityComparer<TSource>.Default);
        }

        /// <summary>
        /// Invokes finallyAction after source observable sequence terminates normally or by an exception.
        /// </summary>
        public static IObservable<TSource> Finally<TSource>(this IObservable<TSource> source, Action finallyAction)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (finallyAction == null)
                throw new ArgumentNullException("finallyAction");

            return new AnonymousObservable<TSource>(observer =>
                {
                    var subscription = source.Subscribe(observer);

                    return Disposable.Create(() =>
                        {
                            try
                            {
                                subscription.Dispose();
                            }
                            finally
                            {
                                finallyAction();
                            }
                        });
                });
        }

        /// <summary>
        /// Invokes the action for its side-effects on each value in the observable sequence.
        /// </summary>
        public static IObservable<TSource> Do<TSource>(this IObservable<TSource> source, Action<TSource> onNext)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (onNext == null)
                throw new ArgumentNullException("onNext");

            return source.Select(x =>
            {
                onNext(x);
                return x;
            });
        }

        /// <summary>
        /// Invokes the action for its side-effects on each value in the observable sequence.
        /// </summary>
        public static IObservable<TSource> Do<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action onCompleted)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (onNext == null)
                throw new ArgumentNullException("onNext");
            if (onCompleted == null)
                throw new ArgumentNullException("onCompleted");

            return new AnonymousObservable<TSource>(obs =>
            {
                return source.Subscribe(
                    x =>
                    {
                        onNext(x);
                        obs.OnNext(x);
                    },
                    () =>
                    {
                        onCompleted();
                        obs.OnCompleted();
                    });
            });
        }

        /// <summary>
        /// Invokes the action for its side-effects on each value in the observable sequence.
        /// </summary>
        public static IObservable<TSource> Do<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action<Exception> onError)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (onNext == null)
                throw new ArgumentNullException("onNext");
            if (onError == null)
                throw new ArgumentNullException("onError");

            return new AnonymousObservable<TSource>(obs =>
            {
                return source.Subscribe(
                    x =>
                    {
                        onNext(x);
                        obs.OnNext(x);
                    },
                    ex =>
                    {
                        onError(ex);
                        obs.OnError(ex);
                    });
            });
        }

        /// <summary>
        /// Invokes the action for its side-effects on each value in the observable sequence.
        /// </summary>
        public static IObservable<TSource> Do<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action<Exception> onError, Action onCompleted)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (onNext == null)
                throw new ArgumentNullException("onNext");
            if (onError == null)
                throw new ArgumentNullException("onError");
            if (onCompleted == null)
                throw new ArgumentNullException("onCompleted");

            return new AnonymousObservable<TSource>(obs =>
            {
                return source.Subscribe(
                    x =>
                    {
                        onNext(x);
                        obs.OnNext(x);
                    },
                    ex =>
                    {
                        onError(ex);
                        obs.OnError(ex);
                    },
                    () =>
                    {
                        onCompleted();
                        obs.OnCompleted();
                    });
            });
        }

        static IEnumerable<IObservable<T>> WhileCore<T>(Func<bool> condition, IObservable<T> source)
        {
            while (condition())
                yield return source;
        }

        public static IObservable<T> While<T>(Func<bool> condition, IObservable<T> source, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (condition == null)
                throw new ArgumentNullException("condition");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return WhileCore(condition, source).Concat(scheduler);
        }

        public static IObservable<T> While<T>(Func<bool> condition, IObservable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (condition == null)
                throw new ArgumentNullException("condition");

            return While(condition, source, Scheduler.Immediate);
        }

        public static IObservable<T> If<T>(Func<bool> condition, IObservable<T> thenSource, IObservable<T> elseSource)
        {
            if (condition == null)
                throw new ArgumentNullException("condition");
            if (thenSource == null)
                throw new ArgumentNullException("thenSource");
            if (elseSource == null)
                throw new ArgumentNullException("elseSource");

            return Observable.Defer(() => condition() ? thenSource : elseSource);
        }

        public static IObservable<T> DoWhile<T>(Func<bool> condition, IObservable<T> source, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (condition == null)
                throw new ArgumentNullException("condition");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return source.Concat(While(condition, source, scheduler), scheduler);
        }

        public static IObservable<T> DoWhile<T>(Func<bool> condition, IObservable<T> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (condition == null)
                throw new ArgumentNullException("condition");

            return DoWhile(condition, source, Scheduler.Immediate);
        }

        public static IObservable<U> Case<T, U>(Func<T> selector, IDictionary<T, IObservable<U>> sources, IObservable<U> defaultSource)
        {
            if (selector == null)
                throw new ArgumentNullException("selector");
            if (sources == null)
                throw new ArgumentNullException("sources");
            if (defaultSource == null)
                throw new ArgumentNullException("defaultSource");

            return Observable.Defer(() =>
                {
                    IObservable<U> result;
                    if (!sources.TryGetValue(selector(), out result))
                        result = defaultSource;
                    return result;
                });
        }

        public static IObservable<U> Case<T, U>(Func<T> selector, IDictionary<T, IObservable<U>> sources, IScheduler scheduler)
        {
            if (selector == null)
                throw new ArgumentNullException("selector");
            if (sources == null)
                throw new ArgumentNullException("sources");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return Case(selector, sources, Observable.Empty<U>(scheduler));
        }

        public static IObservable<U> Case<T, U>(Func<T> selector, IDictionary<T, IObservable<U>> sources)
        {
            if (selector == null)
                throw new ArgumentNullException("selector");
            if (sources == null)
                throw new ArgumentNullException("sources");

            return Case(selector, sources, Scheduler.CurrentThread);
        }

        static IEnumerable<IObservable<U>> ForCore<T, U>(IEnumerable<T> source, Func<T, IObservable<U>> resultSelector)
        {
            foreach (var item in source)
                yield return resultSelector(item);
        }

        public static IObservable<U> For<T, U>(IEnumerable<T> source, Func<T, IObservable<U>> resultSelector, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (resultSelector == null)
                throw new ArgumentNullException("resultSelector");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return ForCore(source, resultSelector).Concat(scheduler);
        }

        public static IObservable<U> For<T, U>(IEnumerable<T> source, Func<T, IObservable<U>> resultSelector)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (resultSelector == null)
                throw new ArgumentNullException("resultSelector");

            return For(source, resultSelector, Scheduler.Immediate);
        }

        public static IObservable<U> Let<T, U>(T value, Func<T, IObservable<U>> selector)
        {
            if (selector == null)
                throw new ArgumentNullException("selector");

            return Defer(() => selector(value));
        }
    }
}
