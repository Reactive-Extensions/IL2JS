using System;
using System.Linq;
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
using System.Threading;

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Linq
{
	public static partial class Observable
	{
        static TimeSpan Normalize(TimeSpan timeSpan)
        {
            if (timeSpan.CompareTo(TimeSpan.Zero) < 0)
                return TimeSpan.Zero;
            return timeSpan;
        }

        /// <summary>
        /// Returns an observable sequence that produces a value after each period.
        /// </summary>
        public static IObservable<long> Interval(TimeSpan period)
        {
            return Interval(period, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Returns an observable sequence that produces a value after each period.
        /// </summary>
        public static IObservable<long> Interval(TimeSpan period, IScheduler scheduler)
        {
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return Timer(period, period, scheduler);
        }

        /// <summary>
        /// Returns an observable sequence that produces a value after the dueTime has elapsed.
        /// </summary>
        public static IObservable<long> Timer(TimeSpan dueTime)
        {
            return Timer(dueTime, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Returns an observable sequence that produces a value at dueTime.
        /// </summary>
        public static IObservable<long> Timer(DateTimeOffset dueTime)
        {
            return Timer(dueTime, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Returns an observable sequence that produces a value after dueTime has elapsed and then after each period.
        /// </summary>
        public static IObservable<long> Timer(TimeSpan dueTime, TimeSpan period)
        {
            return Timer(dueTime, period, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Returns an observable sequence that produces a value at dueTime and then after each period.
        /// </summary>
        public static IObservable<long> Timer(DateTimeOffset dueTime, TimeSpan period)
        {
            return Timer(dueTime, period, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Returns an observable sequence that produces a value after the dueTime has elapsed.
        /// </summary>
        public static IObservable<long> Timer(TimeSpan dueTime, IScheduler scheduler)
        {
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            var d = Normalize(dueTime);

            return new AnonymousObservable<long>(observer =>
                scheduler.Schedule(() =>
                {
                    observer.OnNext(0);
                    observer.OnCompleted();
                }, d));
        }

        /// <summary>
        /// Returns an observable sequence that produces a value at dueTime.
        /// </summary>
        public static IObservable<long> Timer(DateTimeOffset dueTime, IScheduler scheduler)
        {
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<long>(observer =>
                scheduler.Schedule(() =>
                    {
                        observer.OnNext(0);
                        observer.OnCompleted();
                    }, dueTime));

        }

        /// <summary>
        /// Returns an observable sequence that produces a value after dueTime has elapsed and then after each period.
        /// </summary>
        public static IObservable<long> Timer(TimeSpan dueTime, TimeSpan period, IScheduler scheduler)
        {
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return Observable.Defer(() => Timer(scheduler.Now + dueTime, period, scheduler));
        }

        /// <summary>
        /// Returns an observable sequence that produces a value at dueTime and then after each period.
        /// </summary>
        public static IObservable<long> Timer(DateTimeOffset dueTime, TimeSpan period, IScheduler scheduler)
        {
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            var p = Normalize(period);

            return new AnonymousObservable<long>(observer =>
            {
                var d = dueTime;
                long count = 0;
                return scheduler.Schedule(self =>
                {
                    d = d + p;
                    observer.OnNext(count);
                    count = unchecked(count + 1);
                    self(d);
                }, d);
            });
        }

        /// <summary>
        /// Time shifts the observable sequence by dueTime.
        /// The relative time intervals between the values are preserved.
        /// </summary>
        public static IObservable<TSource> Delay<TSource>(this IObservable<TSource> source, TimeSpan dueTime)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Delay(dueTime, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Time shifts the observable sequence by dueTime.
        /// The relative time intervals between the values are preserved.
        /// </summary>
        public static IObservable<TSource> Delay<TSource>(this IObservable<TSource> source, DateTimeOffset dueTime)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Delay(dueTime, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Time shifts the observable sequence by dueTime.
        /// The relative time intervals between the values are preserved.
        /// </summary>
        public static IObservable<TSource> Delay<TSource>(this IObservable<TSource> source, TimeSpan dueTime, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<TSource>
                (observer =>
                     {
                         var scheduleGroup = new CompositeDisposable();
                         var outerGroup = new CompositeDisposable(scheduleGroup);
                         outerGroup.Add
                             (source.Subscribe
                                  (v => scheduleGroup.Add(scheduler.Schedule(() => observer.OnNext(v), dueTime)),
                                   e => { 
                                       scheduleGroup.Dispose();
                                       observer.OnError(e);
                                   },
                                   () => scheduleGroup.Add(scheduler.Schedule(observer.OnCompleted, dueTime))));
                         return outerGroup;
                     });
        }

	    /// <summary>
        /// Time shifts the observable sequence by dueTime.
        /// The relative time intervals between the values are preserved.
        /// </summary>
        public static IObservable<TSource> Delay<TSource>(this IObservable<TSource> source, DateTimeOffset dueTime, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return Observable.Defer(() =>
                {
                    var timeSpan = dueTime.Subtract(scheduler.Now);
                    return source.Delay(timeSpan, scheduler);
                });
        }
        /// <summary>
        /// Ignores values from an observable sequence which are followed by another value before dueTime.
        /// </summary>
        public static IObservable<TSource> Throttle<TSource>(this IObservable<TSource> source, TimeSpan dueTime)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Throttle(dueTime, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Ignores values from an observable sequence which are followed by another value before dueTime.
        /// </summary>
        public static IObservable<TSource> Throttle<TSource>(this IObservable<TSource> source, TimeSpan dueTime, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<TSource>(observer =>
            {
                var gate = new object();
                var value = default(TSource);
                var hasValue = false;
                var cancelable = new MutableDisposable();
                var id = 0UL;

                var subscription = source.Subscribe(x =>
                    {
                        ulong currentid;
                        lock (gate)
                        {
                            hasValue = true;
                            value = x;
                            id = unchecked(id + 1);
                            currentid = id;
                        }
                        var d = new MutableDisposable();
                        cancelable.Disposable = d;
                        d.Disposable = scheduler.Schedule(() =>
                            {
                                lock (gate)
                                {
                                    if (hasValue && id == currentid)
                                        observer.OnNext(value);
                                    hasValue = false;
                                }
                            }, dueTime);
                    },
                    exception =>
                    {
                        cancelable.Dispose();

                        lock (gate)
                        {
                            observer.OnError(exception);
                            hasValue = false;
                            id = unchecked(id + 1);
                        }                        
                    },
                    () =>
                    {
                        cancelable.Dispose();

                        lock (gate)
                        {
                            if (hasValue)
                                observer.OnNext(value);
                            observer.OnCompleted();
                            hasValue = false;
                            id = unchecked(id + 1);
                        }
                    });

                return new CompositeDisposable(subscription, cancelable);
            });
        }

        /// <summary>
        /// Projects each value of an observable sequence into a buffer.
        /// </summary>
        public static IObservable<IList<TSource>> BufferWithTime<TSource>(this IObservable<TSource> source, TimeSpan timeSpan, TimeSpan timeShift, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return Timer(timeSpan, timeShift, scheduler).Combine<long, TSource, IList<TSource>>(source, (observer, leftSubscription, rightSubscription) =>
                {
                    var list = new List<Timestamped<TSource>>();
                    var currentWindowStart = scheduler.Now;

                    Func<IList<TSource>> getCurrentWindow = () =>
                        (from p in list
                         where p.Timestamp.CompareTo(currentWindowStart) >= 0
                         select p.Value)
                        .ToList();

                    return new BinaryObserver<long, TSource>(
                        left =>
                        {
                            var result = getCurrentWindow();

                            var nextWindowStart = scheduler.Now.Add(timeShift).Subtract(timeSpan);
                            if (list.Count > 0)
                            {
                                var partition = 0;
                                for (; partition < list.Count && list[partition].Timestamp.CompareTo(nextWindowStart) <= 0; ++partition) ;
                                list.RemoveRange(0, partition);
                            }

                            observer.OnNext(result);

                            currentWindowStart = nextWindowStart;
                        },
                        right =>
                        {
                            if (right.Kind == NotificationKind.OnNext)
                            {
                                list.Add(new Timestamped<TSource>(right.Value, scheduler.Now));
                            }
                            else
                            {
                                var result = getCurrentWindow();
                                observer.OnNext(result);

                                if (right.Kind == NotificationKind.OnError)
                                    observer.OnError(right.Exception);
                                else
                                    observer.OnCompleted();

                                leftSubscription.Dispose();
                            }
                        });
                });
        }

        /// <summary>
        /// Projects each value of an observable sequence into a buffer.
        /// </summary>
        public static IObservable<IList<TSource>> BufferWithTime<TSource>(this IObservable<TSource> source, TimeSpan timeSpan, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return source.BufferWithTime(timeSpan, timeSpan, scheduler);
        }

        /// <summary>
        /// Projects each value of an observable sequence into a buffer.
        /// </summary>
        public static IObservable<IList<TSource>> BufferWithTime<TSource>(this IObservable<TSource> source, TimeSpan timeSpan, TimeSpan timeShift)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.BufferWithTime(timeSpan, timeShift, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Projects each value of an observable sequence into a buffer.
        /// </summary>
        public static IObservable<IList<TSource>> BufferWithTime<TSource>(this IObservable<TSource> source, TimeSpan timeSpan)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.BufferWithTime(timeSpan, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Projects each value of an observable sequence into a buffer that's sent out when either it's full or a given amount of time has elapsed.
        /// </summary>
        public static IObservable<IList<TSource>> BufferWithTimeOrCount<TSource>(this IObservable<TSource> source, TimeSpan timeSpan, int count, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (count <= 0)
                throw new ArgumentOutOfRangeException("count");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<IList<TSource>>(observer =>
            {
                var bufferId = 0UL;
                var gate = new object();
                var data = new List<TSource>();

                var flushBuffer = new Action(() =>
                {
                    observer.OnNext(data);
                    data = new List<TSource>();
                    bufferId = unchecked(bufferId + 1);
                });

                var timer = new MutableDisposable();

                var startTimer = default(Action<ulong>);
                startTimer = myId =>
                {
                    var workItem = scheduler.Schedule(() =>
                    {
                        var shouldRecurse = false;
                        var nextId = 0UL;

                        lock (gate)
                        {
                            if (myId == bufferId)
                            {
                                flushBuffer();
                                nextId = bufferId;
                                shouldRecurse = true;
                            }
                        }

                        if (shouldRecurse)
                            startTimer(nextId);
                    }, timeSpan);

                    timer.Disposable = workItem;
                };

                startTimer(bufferId);

                var subscription = source.Subscribe(
                    x =>
                    {
                        var shouldStartNewTimer = false;
                        var nextId = 0UL;

                        lock (gate)
                        {
                            data.Add(x);
                            if (data.Count == count)
                            {
                                flushBuffer();
                                nextId = bufferId;
                                shouldStartNewTimer = true;
                            }
                        }

                        if (shouldStartNewTimer)
                            startTimer(nextId);
                    },
                    exception =>
                    {
                        lock (gate)
                        {
                            observer.OnNext(data);
                            bufferId = unchecked(bufferId + 1);
                            observer.OnError(exception);
                        }
                    },
                    () =>
                    {
                        lock (gate)
                        {
                            observer.OnNext(data);
                            bufferId = unchecked(bufferId + 1);
                            observer.OnCompleted();
                        }
                    }
                );

                return new CompositeDisposable(subscription, timer);
            });
        }

        /// <summary>
        /// Projects each value of an observable sequence into a buffer that's sent out when either it's full or a given amount of time has elapsed.
        /// </summary>
        public static IObservable<IList<TSource>> BufferWithTimeOrCount<TSource>(this IObservable<TSource> source, TimeSpan timeSpan, int count)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (count <= 0)
                throw new ArgumentOutOfRangeException("count");

            return source.BufferWithTimeOrCount(timeSpan, count, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Records the time interval for each value of an observable sequence.
        /// </summary>
        public static IObservable<TimeInterval<TSource>> TimeInterval<TSource>(this IObservable<TSource> source, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return Defer(() =>
            {
                var last = scheduler.Now;
                return source.Select(x =>
                {
                    var now = scheduler.Now;
                    var span = now.Subtract(last);
                    last = now;
                    return new TimeInterval<TSource>(x, span);
                });
            });
        }

        /// <summary>
        /// Records the time interval for each value of an observable sequence.
        /// </summary>
        public static IObservable<TimeInterval<TSource>> TimeInterval<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.TimeInterval(Scheduler.ThreadPool);
        }

        /// <summary>
        /// Removes the timestamp from each value of an observable sequence.
        /// </summary>
        public static IObservable<TSource> RemoveTimeInterval<TSource>(this IObservable<TimeInterval<TSource>> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Select(x => x.Value);
        }

        /// <summary>
        /// Records the timestamp for each value of an observable sequence.
        /// </summary>
        public static IObservable<Timestamped<TSource>> Timestamp<TSource>(this IObservable<TSource> source, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return source.Select(x => new Timestamped<TSource>(x, scheduler.Now));
        }

        /// <summary>
        /// Records the timestamp for each value of an observable sequence.
        /// </summary>
        public static IObservable<Timestamped<TSource>> Timestamp<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Timestamp(Scheduler.ThreadPool);
        }

        /// <summary>
        /// Removes the timestamp from each value of an observable sequence.
        /// </summary>
        public static IObservable<TSource> RemoveTimestamp<TSource>(this IObservable<Timestamped<TSource>> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Select(x => x.Value);
        }

        /// <summary>
        /// Samples the observable sequence at each interval.
        /// </summary>
        public static IObservable<TSource> Sample<TSource>(this IObservable<TSource> source, TimeSpan interval, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return source.Combine(Interval(interval, scheduler), (IObserver<TSource> observer, IDisposable leftSubscription, IDisposable rightSubscription) =>
            {
                var value = default(Notification<TSource>);
                return new BinaryObserver<TSource, long>(
                    newValue => value = newValue,
                    _ =>
                    {
                        var myValue = value;
                        value = null;
                        if (myValue != null)
                            myValue.Accept(observer);
                    });
            });
        }

        /// <summary>
        /// Samples the observable sequence at each interval.
        /// </summary>
        public static IObservable<TSource> Sample<TSource>(this IObservable<TSource> source, TimeSpan interval)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Sample(interval, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Returns either the observable sequence or an TimeoutException if dueTime elapses.
        /// </summary>
        public static IObservable<TSource> Timeout<TSource>(this IObservable<TSource> source, TimeSpan dueTime)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Timeout(dueTime, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Returns the source observable sequence or the other observable sequence if dueTime elapses.
        /// </summary>
        public static IObservable<TSource> Timeout<TSource>(this IObservable<TSource> source, TimeSpan dueTime, IObservable<TSource> other)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (other == null)
                throw new ArgumentNullException("other");

            return source.Timeout(dueTime, other, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Returns either the observable sequence or an TimeoutException if dueTime elapses.
        /// </summary>
        public static IObservable<TSource> Timeout<TSource>(this IObservable<TSource> source, DateTimeOffset dueTime)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Timeout(dueTime, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Returns the source observable sequence or the other observable sequence if dueTime elapses.
        /// </summary>
        public static IObservable<TSource> Timeout<TSource>(this IObservable<TSource> source, DateTimeOffset dueTime, IObservable<TSource> other)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (other == null)
                throw new ArgumentNullException("other");

            return source.Timeout(dueTime, other, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Returns either the observable sequence or an TimeoutException if dueTime elapses.
        /// </summary>
        public static IObservable<TSource> Timeout<TSource>(this IObservable<TSource> source, TimeSpan dueTime, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return source.Timeout(dueTime, Throw<TSource>(new TimeoutException()), scheduler);
        }

        /// <summary>
        /// Returns the source observable sequence or the other observable sequence if dueTime elapses.
        /// </summary>
        public static IObservable<TSource> Timeout<TSource>(this IObservable<TSource> source, TimeSpan dueTime, IObservable<TSource> other, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");
            if (other == null)
                throw new ArgumentNullException("other");

            return new AnonymousObservable<TSource>(observer =>
                {
                    var subscription = new MutableDisposable();
                    var timer = new MutableDisposable();
                    var original = new MutableDisposable();

                    subscription.Disposable = original;

                    var gate = new object();
                    var id = 0UL;
                    var initial = id;
                    var switched = false;

                    timer.Disposable = scheduler.Schedule(() =>
                        {
                            var timerWins = false;

                            lock (gate)
                            {
                                switched = id == initial;
                                timerWins = switched;
                            }

                            if (timerWins)
                                subscription.Disposable = other.Subscribe(observer);
                        }, dueTime);

                    original.Disposable = source.Subscribe(
                        x =>
                        {
                            var onNextWins = false;
                            var value = 0UL;

                            lock (gate)
                            {
                                onNextWins = !switched;
                                if (onNextWins)
                                {
                                    id = unchecked(id + 1);
                                    value = id;
                                }
                            }

                            if (onNextWins)
                            {
                                observer.OnNext(x);
                                timer.Disposable = scheduler.Schedule(() =>
                                    {
                                        var timerWins = false;

                                        lock (gate)
                                        {
                                            switched = id == value;
                                            timerWins = switched;
                                        }

                                        if (timerWins)
                                            subscription.Disposable = other.Subscribe(observer);
                                    }, dueTime);
                            }
                        },
                        exception =>
                        {
                            var onErrorWins = false;

                            lock (gate)
                            {
                                onErrorWins = !switched;
                                if (onErrorWins)
                                {
                                    id = unchecked(id + 1);
                                }
                            }

                            if (onErrorWins)
                                observer.OnError(exception);
                        },
                        () =>
                        {
                            var onCompletedWins = false;

                            lock (gate)
                            {
                                onCompletedWins = !switched;
                                if (onCompletedWins)
                                {
                                    id = unchecked(id + 1);
                                }
                            }

                            if (onCompletedWins)
                                observer.OnCompleted();
                        });

                    return new CompositeDisposable(subscription, timer);
                });
        }

        /// <summary>
        /// Returns either the observable sequence or an TimeoutException if dueTime elapses.
        /// </summary>
        public static IObservable<TSource> Timeout<TSource>(this IObservable<TSource> source, DateTimeOffset dueTime, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return source.Timeout(dueTime, Throw<TSource>(new TimeoutException()), scheduler);
        }

        /// <summary>
        /// Returns the source observable sequence or the other observable sequence if dueTime elapses.
        /// </summary>
        public static IObservable<TSource> Timeout<TSource>(this IObservable<TSource> source, DateTimeOffset dueTime, IObservable<TSource> other, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");
            if (other == null)
                throw new ArgumentNullException("other");


            return new AnonymousObservable<TSource>(observer =>
            {
                var subscription = new MutableDisposable();
                var original = new MutableDisposable();

                subscription.Disposable = original;

                var gate = new object();
                var switched = false;

                var timer = scheduler.Schedule(() =>
                {
                    var timerWins = false;

                    lock (gate)
                    {
                        timerWins = !switched;
                        switched = true;
                    }

                    if (timerWins)
                        subscription.Disposable = other.Subscribe(observer);
                }, dueTime);

                original.Disposable = source.Subscribe(
                    x =>
                    {
                        lock (gate)
                        {
                            if (!switched)
                                observer.OnNext(x);
                        }
                    },
                    exception =>
                    {
                        var onErrorWins = false;

                        lock (gate)
                        {
                            onErrorWins = !switched;
                            switched = true;
                        }

                        if (onErrorWins)
                            observer.OnError(exception);
                    },
                    () =>
                    {
                        var onCompletedWins = false;

                        lock (gate)
                        {
                            onCompletedWins = !switched;
                            switched = true;
                        }

                        if (onCompletedWins)
                            observer.OnCompleted();
                    });

                return new CompositeDisposable(subscription, timer);
            });
        }

        /// <summary>
        /// Generates an observable sequence by iterating a state from an initial state until
        /// the condition fails.
        /// </summary>
        public static IObservable<TResult> GenerateWithTime<TState, TResult>(TState initialState, Func<TState, bool> condition, Func<TState, TResult> resultSelector, Func<TState, TimeSpan> timeSelector, Func<TState, TState> iterate, IScheduler scheduler)
        {
            if (condition == null)
                throw new ArgumentNullException("condition");
            if (resultSelector == null)
                throw new ArgumentNullException("resultSelector");
            if (timeSelector == null)
                throw new ArgumentNullException("timeSelector");
            if (iterate == null)
                throw new ArgumentNullException("iterate");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<TResult>(observer =>
            {
                var state = initialState;
                var first = true;
                var hasResult = false;
                var result = default(TResult);
                var time = default(TimeSpan);
                return scheduler.Schedule(self =>
                {
                    if (hasResult)
                        observer.OnNext(result);
                    try
                    {
                        if (first)
                            first = false;
                        else
                            state = iterate(state);
                        hasResult = condition(state);
                        if (hasResult)
                        {
                            result = resultSelector(state);
                            time = timeSelector(state);
                        }
                    }
                    catch (Exception exception)
                    {
                        observer.OnError(exception);
                        return;
                    }

                    if (hasResult)
                        self(time);
                    else
                        observer.OnCompleted();
                }, TimeSpan.Zero);
            });
        }

        /// <summary>
        /// Generates an observable sequence by iterating a state from an initial state until
        /// the condition fails.
        /// </summary>
        public static IObservable<TResult> GenerateWithTime<TState, TResult>(TState initialState, Func<TState, bool> condition, Func<TState, TResult> resultSelector, Func<TState, TimeSpan> timeSelector, Func<TState, TState> iterate)
        {
            if (condition == null)
                throw new ArgumentNullException("condition");
            if (resultSelector == null)
                throw new ArgumentNullException("resultSelector");
            if (timeSelector == null)
                throw new ArgumentNullException("timeSelector");
            if (iterate == null)
                throw new ArgumentNullException("iterate");

            return GenerateWithTime(initialState, condition, resultSelector, timeSelector, iterate, Scheduler.ThreadPool);
        }

        /// <summary>
        /// Generates an observable sequence by iterating a state from an initial state until
        /// the condition fails.
        /// </summary>
        public static IObservable<TResult> GenerateWithTime<TState, TResult>(TState initialState, Func<TState, bool> condition, Func<TState, TResult> resultSelector, Func<TState, DateTimeOffset> timeSelector, Func<TState, TState> iterate, IScheduler scheduler)
        {
            if (condition == null)
                throw new ArgumentNullException("condition");
            if (resultSelector == null)
                throw new ArgumentNullException("resultSelector");
            if (timeSelector == null)
                throw new ArgumentNullException("timeSelector");
            if (iterate == null)
                throw new ArgumentNullException("iterate");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<TResult>(observer =>
            {
                var state = initialState;
                var first = true;
                var hasResult = false;
                var result = default(TResult);
                var time = default(DateTimeOffset);
                return scheduler.Schedule(self =>
                {
                    if (hasResult)
                        observer.OnNext(result);
                    try
                    {
                        if (first)
                            first = false;
                        else
                            state = iterate(state);
                        hasResult = condition(state);
                        if (hasResult)
                        {
                            result = resultSelector(state);
                            time = timeSelector(state);
                        }
                    }
                    catch (Exception exception)
                    {
                        observer.OnError(exception);
                        return;
                    }

                    if (hasResult)
                        self(time);
                    else
                        observer.OnCompleted();
                }, scheduler.Now);
            });
        }

        /// <summary>
        /// Generates an observable sequence by iterating a state from an initial state until
        /// the condition fails.
        /// </summary>
        public static IObservable<TResult> GenerateWithTime<TState, TResult>(TState initialState, Func<TState, bool> condition, Func<TState, TResult> resultSelector, Func<TState, DateTimeOffset> timeSelector, Func<TState, TState> iterate)
        {
            if (condition == null)
                throw new ArgumentNullException("condition");
            if (resultSelector == null)
                throw new ArgumentNullException("resultSelector");
            if (timeSelector == null)
                throw new ArgumentNullException("timeSelector");
            if (iterate == null)
                throw new ArgumentNullException("iterate");

            return GenerateWithTime(initialState, condition, resultSelector, timeSelector, iterate, Scheduler.ThreadPool);
        }
    }
}
