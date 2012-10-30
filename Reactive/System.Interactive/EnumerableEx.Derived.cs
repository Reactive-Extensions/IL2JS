using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Concurrency;
using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Diagnostics;
using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Collections.Generic;

namespace
#if WM7
Microsoft.Windows.Phone.
#endif
 Interactive.Linq
{
    public static partial class EnumerableEx
    {
        /// <summary>
        /// Prepends a value to a sequence.
        /// </summary>
        public static IEnumerable<TSource> StartWith<TSource>(this IEnumerable<TSource> source, TSource first)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return Return(first).Concat(source);
        }

        /// <summary>
        /// Prepends values to a sequence.
        /// </summary>
        public static IEnumerable<TSource> StartWith<TSource>(this IEnumerable<TSource> source, params TSource[] first)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (first == null)
                throw new ArgumentNullException("first");
            
            return first.Concat(source);
        }

        /// <summary>
        /// Projects each value of a sequence to a sequence and flattens the resulting sequences into one sequence.
        /// </summary>
        public static IEnumerable<TOther> SelectMany<TSource, TOther>(this IEnumerable<TSource> source, IEnumerable<TOther> other)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (other == null)
                throw new ArgumentNullException("other");

            return source.SelectMany(_ => other);
        }

        /// <summary>
        /// Invokes the action for its side-effects on each value in the sequence.
        /// </summary>
        public static IEnumerable<TSource> Do<TSource>(this IEnumerable<TSource> source, Action<TSource> onNext)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (onNext == null)
                throw new ArgumentNullException("onNext");

            return DoHelper(source, onNext, _ => { }, () => { });
        }

        /// <summary>
        /// Invokes the action for its side-effects on each value in the sequence.
        /// </summary>
        public static IEnumerable<TSource> Do<TSource>(this IEnumerable<TSource> source, Action<TSource> onNext, Action onCompleted)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (onNext == null)
                throw new ArgumentNullException("onNext");
            if (onCompleted == null)
                throw new ArgumentNullException("onCompleted");

            return DoHelper(source, onNext, _ => { }, onCompleted);
        }

        /// <summary>
        /// Invokes the action for its side-effects on each value in the sequence.
        /// </summary>
        public static IEnumerable<TSource> Do<TSource>(this IEnumerable<TSource> source, Action<TSource> onNext, Action<Exception> onError)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (onNext == null)
                throw new ArgumentNullException("onNext");
            if (onError == null)
                throw new ArgumentNullException("onError");

            return DoHelper(source, onNext, onError, () => { });
        }

        /// <summary>
        /// Invokes the action for its side-effects on each value in the sequence.
        /// </summary>
        public static IEnumerable<TSource> Do<TSource>(this IEnumerable<TSource> source, Action<TSource> onNext, Action<Exception> onError, Action onCompleted)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (onNext == null)
                throw new ArgumentNullException("onNext");
            if (onError == null)
                throw new ArgumentNullException("onError");
            if (onCompleted == null)
                throw new ArgumentNullException("onCompleted");

            return DoHelper(source, onNext, onError, onCompleted);
        }

        private static IEnumerable<TSource> DoHelper<TSource>(this IEnumerable<TSource> source, Action<TSource> onNext, Action<Exception> onError, Action onCompleted)
        {
            using (var e = source.GetEnumerator())
            {
                while (true)
                {
                    var current = default(TSource);
                    try
                    {
                        if (!e.MoveNext())
                            break;

                        current = e.Current;
                    }
                    catch (Exception ex)
                    {
                        onError(ex);
                        throw ex.PrepareForRethrow();
                    }

                    onNext(current);
                    yield return current;
                }

                onCompleted();
            }
        }

        /// <summary>
        /// Evaluates the sequence for its side-effects.
        /// </summary>
        public static void Run<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
//we want the side-effects
#pragma warning disable 168
            foreach(var item in source)
#pragma warning restore 168
            {
            }
        }

        /// <summary>
        /// Evaluates the sequence and invokes the action for its side-effects on each value in the sequence.
        /// </summary>
        public static void Run<TSource>(this IEnumerable<TSource> source, Action<TSource> onNext)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (onNext == null)
                throw new ArgumentNullException("onNext");

            source.Do(onNext).Run();
        }

        /// <summary>
        /// Evaluates the sequence and invokes the action for its side-effects on each value in the sequence.
        /// </summary>
        public static void Run<TSource>(this IEnumerable<TSource> source, Action<TSource> onNext, Action<Exception> onError)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (onNext == null)
                throw new ArgumentNullException("onNext");
            if (onError == null)
                throw new ArgumentNullException("onError");

            source.Do(onNext, onError).Run();
        }

        /// <summary>
        /// Evaluates the sequence and invokes the action for its side-effects on each value in the sequence.
        /// </summary>
        public static void Run<TSource>(this IEnumerable<TSource> source, Action<TSource> onNext, Action onCompleted)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (onNext == null)
                throw new ArgumentNullException("onNext");
            if (onCompleted == null)
                throw new ArgumentNullException("onCompleted");

            source.Do(onNext, onCompleted).Run();
        }

        /// <summary>
        /// Evaluates the sequence and invokes the action for its side-effects on each value in the sequence.
        /// </summary>
        public static void Run<TSource>(this IEnumerable<TSource> source, Action<TSource> onNext, Action<Exception> onError, Action onCompleted)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (onNext == null)
                throw new ArgumentNullException("onNext");
            if (onError == null)
                throw new ArgumentNullException("onError");
            if (onCompleted == null)
                throw new ArgumentNullException("onCompleted");

            source.Do(onNext, onError, onCompleted).Run();
        }

        /// <summary>
        /// Merges two sequences into a single sequence.
        /// </summary>
        public static IEnumerable<TSource> Merge<TSource>(this IEnumerable<TSource> leftSource, IEnumerable<TSource> rightSource)
        {
            if (leftSource == null)
                throw new ArgumentNullException("leftSource");
            if (rightSource == null)
                throw new ArgumentNullException("rightSource");

            return MergeHelper(new[] { leftSource, rightSource }, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Merges two sequences into a single sequence.
        /// </summary>
        public static IEnumerable<TSource> Merge<TSource>(this IEnumerable<TSource> leftSource, IScheduler scheduler, IEnumerable<TSource> rightSource)
        {
            if (leftSource == null)
                throw new ArgumentNullException("leftSource");
            if (rightSource == null)
                throw new ArgumentNullException("rightSource");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return MergeHelper(new[] { leftSource, rightSource }, scheduler);
        }

        /// <summary>
        /// Merges all the sequences into a single sequence.
        /// </summary>
        public static IEnumerable<TSource> Merge<TSource>(params IEnumerable<TSource>[] sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            return MergeHelper(sources, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Merges all the sequences into a single sequence.
        /// </summary>
        public static IEnumerable<TSource> Merge<TSource>(IScheduler scheduler, params IEnumerable<TSource>[] sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return MergeHelper(sources, scheduler);
        }

        /// <summary>
        /// Merges all the sequences into a single sequence.
        /// </summary>
        public static IEnumerable<TSource> Merge<TSource>(this IEnumerable<IEnumerable<TSource>> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            return MergeHelper(sources.ToArray(), Scheduler.ThreadPool);
        }

        /// <summary>
        /// Merges all the sequences into a single sequence.
        /// </summary>
        public static IEnumerable<TSource> Merge<TSource>(this IEnumerable<IEnumerable<TSource>> sources, IScheduler scheduler)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return MergeHelper(sources.ToArray(), Scheduler.ThreadPool);
        }

        private static IEnumerable<TSource> MergeHelper<TSource>(IEnumerable<TSource>[] sources, IScheduler scheduler)
        {
            if (sources.Length == 0)
                yield break;

            // Intended strictness in acquiring enumerators.
            var enumerators = sources.Select(source => source.Materialize().GetEnumerator()).ToList();

            var queue = new Queue<Notification<TSource>>();
            var consumerProducerSemaphore = new Semaphore(0, int.MaxValue);

            var startEnumerators = new ManualResetEvent(false);

            bool startedAllEnumerators = false;
            bool stopped = false;
            try
            {
                foreach (var enumerator in enumerators)
                {
                    var enumeratorClosure = enumerator;
                    scheduler.Schedule(() =>
                    {
                        startEnumerators.WaitOne();

                        using (enumeratorClosure)
                        {
                            while (!stopped && enumeratorClosure.MoveNext())
                            {
                                lock (queue)
                                {
                                    queue.Enqueue(enumeratorClosure.Current);
                                }
                                consumerProducerSemaphore.Release();
                            }
                        }
                    });
                }

                startedAllEnumerators = true;
            }
            finally /* really fault */
            {
                // Let running actions clean up their enumerator.
                if (!startedAllEnumerators)
                {
                    stopped = true;
                    startEnumerators.Set();

                    foreach (var enumerator in enumerators)
                    {
                        enumerator.Dispose();
                    }
                }
            }

            startEnumerators.Set();

            int producerCompleteCount = 0;
            while (!stopped)
            {
                Notification<TSource> result;

                consumerProducerSemaphore.WaitOne();
                lock (queue)
                {
                    result = queue.Dequeue();
                }

                if (result.Kind == NotificationKind.OnError)
                {
                    stopped = true;
                    throw ((Notification<TSource>.OnError)result).Exception;
                }
                else if (result.Kind == NotificationKind.OnNext)
                {
                    yield return result.Value;
                }
                else
                {
                    producerCompleteCount++;
                    if (producerCompleteCount == enumerators.Count)
                    {
                        stopped = true;
                    }
                }
            }
        }

        /// <summary>
        /// Applies an accumulator function over a sequence and returns each intermediate result.  
        /// The specified seed value is used as the initial accumulator value.
        /// </summary>
        public static IEnumerable<TAccumulate> Scan<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> accumulator)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (accumulator == null)
                throw new ArgumentNullException("accumulator");

            return Return(seed).Concat(Defer(() =>
            {
                var accumulation = seed;
                return source.Select(x => accumulation = accumulator(accumulation, x));
            }));
        }

        /// <summary>
        /// Applies an accumulator function over a sequence and returns each intermediate result.  
        /// </summary>
        public static IEnumerable<TSource> Scan<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, TSource> accumulator)
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
        /// Returns the sequence that responds first.
        /// </summary>
        public static IEnumerable<TSource> Amb<TSource>(this IEnumerable<TSource> leftSource, IEnumerable<TSource> rightSource)
        {
            if (leftSource == null)
                throw new ArgumentNullException("leftSource");
            if (rightSource == null)
                throw new ArgumentNullException("rightSource");

            return AmbHelper(new[] { leftSource, rightSource }, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Returns the sequence that responds first.
        /// </summary>
        public static IEnumerable<TSource> Amb<TSource>(this IEnumerable<TSource> leftSource, IScheduler scheduler, IEnumerable<TSource> rightSource)
        {
            if (leftSource == null)
                throw new ArgumentNullException("leftSource");
            if (rightSource == null)
                throw new ArgumentNullException("rightSource");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return AmbHelper(new[] { leftSource, rightSource }, scheduler);
        }

        /// <summary>
        /// Returns the sequence that responds first.
        /// </summary>
        public static IEnumerable<TSource> Amb<TSource>(params IEnumerable<TSource>[] sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            return AmbHelper(sources, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Returns the sequence that responds first.
        /// </summary>
        public static IEnumerable<TSource> Amb<TSource>(IScheduler scheduler, params IEnumerable<TSource>[] sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return AmbHelper(sources, scheduler);
        }

        /// <summary>
        /// Returns the sequence that responds first.
        /// </summary>
        public static IEnumerable<TSource> Amb<TSource>(this IEnumerable<IEnumerable<TSource>> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            return AmbHelper(sources.ToArray(), Scheduler.ThreadPool);
        }

        /// <summary>
        /// Returns the sequence that responds first.
        /// </summary>
        public static IEnumerable<TSource> Amb<TSource>(this IEnumerable<IEnumerable<TSource>> sources, IScheduler scheduler)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return AmbHelper(sources.ToArray(), scheduler);
        }

        private static IEnumerable<TSource> AmbHelper<TSource>(IEnumerable<TSource>[] sources, IScheduler scheduler)
        {
            if (sources.Length == 0)
                yield break;

            // Intended strictness in acquiring enumerators.
            var enumerators = sources.Select(source => source.GetEnumerator()).ToList();

            var electWinnerGate = new object();
            var hasElected = false;
            var winner = default(IEnumerator<TSource>);
            var hasFirst = false;
            var exception = default(Exception);
            var electWinner = new ManualResetEvent(false);

            var startEnumerators = new ManualResetEvent(false);

            bool startedAllEnumerators = false;
            try
            {
                foreach (var enumerator in enumerators)
                {
                    var enumeratorClosure = enumerator;
                    scheduler.Schedule(() =>
                    {
                        startEnumerators.WaitOne();

                        if (hasElected)
                        {
                            enumeratorClosure.Dispose();
                            return;
                        }

                        bool hasNext = false;
                        Exception ex = null;
                        try
                        {
                            hasNext = enumeratorClosure.MoveNext();
                        }
                        catch (Exception e)
                        {
                            ex = e.PrepareForRethrow();
                        }

                        lock (electWinnerGate)
                        {
                            if (!hasElected)
                            {
                                winner = enumeratorClosure;
                                hasFirst = hasNext;
                                exception = ex;
                                hasElected = true;
                                electWinner.Set();
                            }
                            else
                            {
                                enumeratorClosure.Dispose();
                            }
                        }
                    });
                }

                startedAllEnumerators = true;
            }
            finally /* really fault */
            {
                // Let running actions clean up their enumerator.
                if (!startedAllEnumerators)
                {
                    hasElected = true;
                    startEnumerators.Set();

                    foreach (var enumerator in enumerators)
                    {
                        enumerator.Dispose();
                    }
                }
            }

            startEnumerators.Set();
            electWinner.WaitOne();

            using (winner)
            {
                if (exception != null)
                    throw exception;
                
                if (hasFirst)
                    yield return winner.Current;

                while (winner.MoveNext())
                    yield return winner.Current;
            }
        }
       
        /// <summary>
        /// Continues the source sequence with the next sequence whether 
        /// the source sequence terminates normally or by an exception.
        /// </summary>
        public static IEnumerable<TSource> OnErrorResumeNext<TSource>(this IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            if (first == null)
                throw new ArgumentNullException("first");
            if (second == null)
                throw new ArgumentNullException("second");

            return OnErrorResumeNext(new[] {first, second});
        }

        /// <summary>
        /// Continues a sequence that is terminated normally or by an exception with the next sequence.
        /// </summary>
        public static IEnumerable<TSource> OnErrorResumeNext<TSource>(params IEnumerable<TSource>[] sources)
        {
            return sources.OnErrorResumeNext();
        }

        /// <summary>
        /// Continues an observable sequence that is terminated normally or by an exception with the next observable sequence.
        /// </summary>
        public static IEnumerable<TSource> OnErrorResumeNext<TSource>(this IEnumerable<IEnumerable<TSource>> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            return OnErrorResumeNextHelper(sources);
        }

        private static IEnumerable<TSource> OnErrorResumeNextHelper<TSource>(IEnumerable<IEnumerable<TSource>> sources)
        {
            var outerEnumerator = sources.GetEnumerator();
            try
            {
                while (true)
                {
                    if (!outerEnumerator.MoveNext())
                        yield break;

                    using (var innerEnumerator = outerEnumerator.Current.GetEnumerator())
                    {

                        while (true)
                        {
                            var value = default(TSource);
                            try
                            {
                                if (!innerEnumerator.MoveNext())
                                    break;
                                value = innerEnumerator.Current;
                            }
                            catch (Exception)
                            {
                                break;
                            }
                            yield return value;
                        }
                    }
                }
            }
            finally
            {
                outerEnumerator.Dispose();
            }
        }

        /// <summary>
        /// Invokes finallyAction after source sequence terminates normally or by an exception.
        /// </summary>
        public static IEnumerable<TSource> Finally<TSource>(this IEnumerable<TSource> source, Action finallyAction)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (finallyAction == null)
                throw new ArgumentNullException("finallyAction");

            return FinallyHelper(source, finallyAction);
        }

        private static IEnumerable<TSource> FinallyHelper<TSource>(this IEnumerable<TSource> source, Action finallyAction)
        {            
            try
            {
                foreach (var item in source)
                {
                    yield return item;                
                }
            }
            finally
            {
                finallyAction();
            }
        }

        /// <summary>
        /// Retrieves resource from resourceSelector for use in resourceUsage and disposes 
        /// the resource once the resulting sequence terminates.
        /// </summary>
        public static IEnumerable<TSource> Using<TSource, TResource>(Func<TResource> resourceSelector, Func<TResource, IEnumerable<TSource>> resourceUsage) where TResource : IDisposable
        {
            if (resourceSelector == null)
                throw new ArgumentNullException("resourceSelector");
            if (resourceUsage == null)
                throw new ArgumentNullException("resourceUsage");

            return Defer(() =>
            {
                var resource = resourceSelector();
                return resourceUsage(resource).Finally(resource.Dispose);
            });
        }

        /// <summary>
        /// Projects each value of an enumerable sequence into a buffer.
        /// </summary>
        public static IEnumerable<IList<TSource>> BufferWithCount<TSource>(this IEnumerable<TSource> source, int count, int skip)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (count <= 0)
                throw new ArgumentOutOfRangeException("count");
            if (skip <= 0)
                throw new ArgumentOutOfRangeException("skip");

            return BufferHelper(source, count, skip);
        }

        private static IEnumerable<IList<TSource>> BufferHelper<TSource>(IEnumerable<TSource> source, int count, int skip)
        {
            var list = new List<TSource>();
            var n = 0;
            foreach (var x in source.Materialize())
            {
                if (x.Kind == NotificationKind.OnNext)
                {
                    if (n == 0)
                        list.Add(x.Value);
                    else
                        n--;

                    if (list.Count == count)
                    {
                        var result = list.ToList();
                        list.RemoveRange(0, Math.Min(skip, list.Count));
                        n = Math.Max(0, skip - count);
                        yield return result;
                    }
                }
                else
                {
                    var result = list.ToList();
                    if (result.Count > 0)
                        yield return result;

                    if (x.Kind == NotificationKind.OnError)
                    {
                        throw ((Notification<TSource>.OnError)x).Exception;
                    }
                    else
                    {
                        yield break;
                    }
                }
            }
        }

        /// <summary>
        /// Projects each value of an enumerable sequence into a buffer.
        /// </summary>
        public static IEnumerable<IList<TSource>> BufferWithCount<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (count <= 0)
                throw new ArgumentOutOfRangeException("count");

            return source.BufferWithCount(count, count);
        }

        /// <summary>
        /// Time shifts the enumerable sequence by dueTime.
        /// The relative time intervals between the values are preserved.
        /// </summary>
        public static IEnumerable<TSource> Delay<TSource>(this IEnumerable<TSource> source, TimeSpan dueTime)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Delay(Scheduler.ThreadPool, dueTime);
        }

        /// <summary>
        /// Time shifts the enumerable sequence by dueTime.
        /// The relative time intervals between the values are preserved.
        /// </summary>
        public static IEnumerable<TSource> Delay<TSource>(this IEnumerable<TSource> source, DateTimeOffset dueTime)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Delay(Scheduler.ThreadPool, dueTime);
        }

        /// <summary>
        /// Time shifts the enumerable sequence by dueTime.
        /// The relative time intervals between the values are preserved.
        /// </summary>
        public static IEnumerable<TSource> Delay<TSource>(this IEnumerable<TSource> source, IScheduler scheduler, TimeSpan dueTime)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return DelayHelper(source, scheduler, dueTime);
        }

        /// <summary>
        /// Time shifts the enumerable sequence by dueTime.
        /// The relative time intervals between the values are preserved.
        /// </summary>
        public static IEnumerable<TSource> Delay<TSource>(this IEnumerable<TSource> source, IScheduler scheduler, DateTimeOffset dueTime)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return Defer(() =>
            {
                var timeSpan = dueTime.Subtract(scheduler.Now);
                return source.Delay(scheduler, timeSpan);
            });
        }

        private static IEnumerable<TSource> DelayHelper<TSource>(this IEnumerable<TSource> source, IScheduler scheduler, TimeSpan dueTime)
        {
            var evt = new ManualResetEvent(false);
            scheduler.Schedule(() => evt.Set(), dueTime);
            evt.WaitOne();

            foreach (var item in source)
                yield return item;
        }

        /// <summary>
        /// Returns the element with the minimum value by using a specified comparer.
        /// </summary>
        public static TSource Min<TSource>(this IEnumerable<TSource> source, IComparer<TSource> comparer)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            return MinBy(source, x => x, comparer);
        }

        /// <summary>
        /// Returns the element with the minimum key value by using the default comparer for keys.
        /// </summary>
        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (keySelector == null)
                throw new ArgumentNullException("keySelector");

            return MinBy(source, keySelector, Comparer<TKey>.Default);
        }

        /// <summary>
        /// Returns the element with the minimum key value by using a specified key comparer.
        /// </summary>
        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (keySelector == null)
                throw new ArgumentNullException("keySelector");
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            return ExtremaBy(source, keySelector, (key, minValue) => comparer.Compare(key, minValue) < 0);
        }

        /// <summary>
        /// Returns the element with the minimum value by using a specified comparer.
        /// </summary>
        public static TSource Max<TSource>(this IEnumerable<TSource> source, IComparer<TSource> comparer)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            return MaxBy(source, x => x, comparer);
        }

        /// <summary>
        /// Returns the element with the maximum key value by using the default comparer for keys.
        /// </summary>
        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (keySelector == null)
                throw new ArgumentNullException("keySelector");

            return MaxBy(source, keySelector, Comparer<TKey>.Default);
        }

        /// <summary>
        /// Returns the element with the maximum key value by using a specified key comparer.
        /// </summary>
        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (keySelector == null)
                throw new ArgumentNullException("keySelector");
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            return ExtremaBy(source, keySelector, (key, minValue) => comparer.Compare(key, minValue) > 0);
        }

        private static TSource ExtremaBy<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, TKey, bool> compare)
        {
            using (var e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                    throw new InvalidOperationException("Source sequence doesn't contain any elements.");

                var result = e.Current;
                var resKey = keySelector(result);

                while (e.MoveNext())
                {
                    var cur = e.Current;
                    var key = keySelector(cur);
                    if (compare(key, resKey))
                    {
                        result = cur;
                        resKey = key;
                    }
                }

                return result;
            }
        }
    }
}