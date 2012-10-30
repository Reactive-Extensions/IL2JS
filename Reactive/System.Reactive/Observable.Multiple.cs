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
        internal static IObservable<TResult> Combine<TLeft, TRight, TResult>(this IObservable<TLeft> leftSource, IObservable<TRight> rightSource, Func<IObserver<TResult>, IDisposable, IDisposable, IObserver<Either<Notification<TLeft>, Notification<TRight>>>> combinerSelector)
        {
            return new AnonymousObservable<TResult>(observer =>
            {
                var leftSubscription = new MutableDisposable();
                var rightSubscription = new MutableDisposable();

                var combiner = combinerSelector(observer, leftSubscription, rightSubscription);
                var gate = new object();

                leftSubscription.Disposable = leftSource.Materialize().Select(x => Either<Notification<TLeft>, Notification<TRight>>.CreateLeft(x)).Synchronize(gate).Subscribe(combiner);
                rightSubscription.Disposable = rightSource.Materialize().Select(x => Either<Notification<TLeft>, Notification<TRight>>.CreateRight(x)).Synchronize(gate).Subscribe(combiner);

                return new CompositeDisposable(leftSubscription, rightSubscription);
            });
        }

        /// <summary>
        /// Merges an observable sequence of observable sequences into an observable sequence.
        /// </summary>
        public static IObservable<TSource> Merge<TSource>(this IObservable<IObservable<TSource>> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return new AnonymousObservable<TSource>(observer =>
            {
                var gate = new object();
                var isStopped = false;
                var group = new CompositeDisposable();
                var outerSubscription = new MutableDisposable();

                group.Add(outerSubscription);

                outerSubscription.Disposable = source.Subscribe(
                    innerSource =>
                    {
                        var innerSubscription = new MutableDisposable();
                        group.Add(innerSubscription);
                        innerSubscription.Disposable = innerSource.Subscribe(
                            x =>
                            {
                                lock (gate)
                                    observer.OnNext(x);
                            },
                            exception =>
                            {
                                lock (gate)
                                    observer.OnError(exception);
                            },
                            () =>
                            {
                                group.Remove(innerSubscription);   // modification MUST occur before subsequent check
                                if (isStopped && group.Count == 1) // isStopped must be checked before group Count to ensure outer is not creating more groups
                                    lock (gate)
                                        observer.OnCompleted();
                            });
                    },
                    exception =>
                    {
                        lock (gate)
                            observer.OnError(exception);
                    },
                    () =>
                    {
                        isStopped = true;     // modification MUST occur before subsequent check
                        if (group.Count == 1)
                            lock (gate)
                                observer.OnCompleted();
                    });

                return group;
            });
        }

        /// <summary>
        /// Transforms an observable sequence of observable sequences into an observable sequence producing values only from the most recent observable sequence.
        /// </summary>
        public static IObservable<TSource> Switch<TSource>(this IObservable<IObservable<TSource>> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return new AnonymousObservable<TSource>(observer =>
            {
                var gate = new object();
                var innerSubscription = new MutableDisposable();
                var subscription = new MutableDisposable();
                var isStopped = false;

                subscription.Disposable = source.Subscribe(
                    innerSource =>
                    {
                        var d = new MutableDisposable();
                        innerSubscription.Disposable = d;
                        d.Disposable = innerSource.Subscribe(
                            x =>
                            {
                                lock (gate)
                                    observer.OnNext(x);
                            },
                            exception =>
                            {
                                subscription.Dispose();
                                innerSubscription.Dispose();
                                lock (gate)
                                    observer.OnError(exception);
                            },
                            () =>
                            {
                                innerSubscription.Disposable = null;
                                if (isStopped)
                                    lock (gate)
                                        observer.OnCompleted();
                            });
                    },
                    exception =>
                    {
                        innerSubscription.Dispose();
                        lock (gate)
                            observer.OnError(exception);
                    },
                    () =>
                    {
                        isStopped = true;
                        if (innerSubscription.Disposable == null)
                            lock (gate)
                                observer.OnCompleted();
                    });

                return new CompositeDisposable(subscription, innerSubscription);
            });
        }

        /// <summary>
        /// Concatenates two observable sequences.
        /// </summary>
        public static IObservable<TSource> Concat<TSource>(this IObservable<TSource> first, IObservable<TSource> second)
        {
            if (first == null)
                throw new ArgumentNullException("first");
            if (second == null)
                throw new ArgumentNullException("second");

            return first.Concat(second, Scheduler.Immediate);
        }

        /// <summary>
        /// Concatenates two observable sequences.
        /// </summary>
        public static IObservable<TSource> Concat<TSource>(this IObservable<TSource> first, IObservable<TSource> second, IScheduler scheduler)
        {
            if (first == null)
                throw new ArgumentNullException("first");
            if (second == null)
                throw new ArgumentNullException("second");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return Concat(scheduler, first, second);
        }

        /// <summary>
        /// Concatenates all the observable sequences.
        /// </summary>
        public static IObservable<TSource> Concat<TSource>(IScheduler scheduler, params IObservable<TSource>[] sources)
        {
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");
            if (sources == null)
                throw new ArgumentNullException("sources");

            return ((IEnumerable<IObservable<TSource>>)sources).Concat(scheduler);
        }

        /// <summary>
        /// Concatenates all the observable sequences.
        /// </summary>
        public static IObservable<TSource> Concat<TSource>(params IObservable<TSource>[] sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            return ((IEnumerable<IObservable<TSource>>)sources).Concat(Scheduler.Immediate);
        }

        /// <summary>
        /// Concatenates all the observable sequences.
        /// </summary>
        public static IObservable<TSource> Concat<TSource>(this IEnumerable<IObservable<TSource>> sources, IScheduler scheduler)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<TSource>(observer =>
            {
                var e = sources.GetEnumerator();
                var subscription = new MutableDisposable();

                var cancelable = scheduler.Schedule(self =>
                {
                    var current = default(IObservable<TSource>);
                    var hasNext = false;
                    try
                    {
                        hasNext = e.MoveNext();
                        if (hasNext)
                            current = e.Current;
                        else
                            e.Dispose();
                    }
                    catch (Exception exception)
                    {
                        observer.OnError(exception);
                        e.Dispose();
                        return;
                    }

                    if (!hasNext)
                    {
                        observer.OnCompleted();
                        return;
                    }

                    var d = new MutableDisposable();
                    subscription.Disposable = d;
                    d.Disposable = current.Subscribe(observer.OnNext, observer.OnError, self);
                });

                return new CompositeDisposable(subscription, cancelable);
            });
        }

        /// <summary>
        /// Concatenates all the observable sequences.
        /// </summary>
        public static IObservable<TSource> Concat<TSource>(this IEnumerable<IObservable<TSource>> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            return sources.Concat(Scheduler.Immediate);
        }

        /// <summary>
        /// Continues an observable sequence that is terminated by an exception of the specified type with the observable sequence
        /// produced by the handler.
        /// </summary>
        public static IObservable<TSource> Catch<TSource, TException>(this IObservable<TSource> source, Func<TException, IObservable<TSource>> handler) where TException : Exception
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (handler == null)
                throw new ArgumentNullException("handler");

            return new AnonymousObservable<TSource>(observer =>
            {
                var subscription = new MutableDisposable();

                subscription.Disposable = source.Subscribe(observer.OnNext,
                    exception =>
                    {
                        var e = exception as TException;
                        if (e != null)
                        {
                            IObservable<TSource> result;
                            try
                            {
                                result = handler(e);
                            }
                            catch (Exception ex)
                            {
                                observer.OnError(ex);
                                return;
                            }

                            var d = new MutableDisposable();
                            subscription.Disposable = d;
                            d.Disposable = result.Subscribe(observer);
                        }
                        else
                            observer.OnError(exception);
                    }, observer.OnCompleted);

                return subscription;
            });
        }


        /// <summary>
        /// Continues an observable sequence that is terminated by an exception with the next observable sequence.
        /// </summary>
        public static IObservable<TSource> Catch<TSource>(this IObservable<TSource> first, IObservable<TSource> second)
        {
            if (first == null)
                throw new ArgumentNullException("first");
            if (second == null)
                throw new ArgumentNullException("second");

            return first.Catch(second, Scheduler.Immediate);
        }

        /// <summary>
        /// Continues an observable sequence that is terminated by an exception with the next observable sequence.
        /// </summary>
        public static IObservable<TSource> Catch<TSource>(params IObservable<TSource>[] sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            return Catch(sources, Scheduler.Immediate);
        }

        /// <summary>
        /// Continues an observable sequence that is terminated by an exception with the next observable sequence.
        /// </summary>
        public static IObservable<TSource> Catch<TSource>(this IEnumerable<IObservable<TSource>> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            return sources.Catch(Scheduler.Immediate);
        }

        /// <summary>
        /// Continues an observable sequence that is terminated by an exception with the next observable sequence.
        /// </summary>
        public static IObservable<TSource> Catch<TSource>(this IObservable<TSource> first, IObservable<TSource> second, IScheduler scheduler)
        {
            if (first == null)
                throw new ArgumentNullException("first");
            if (second == null)
                throw new ArgumentNullException("second");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return Catch(scheduler, first, second);
        }

        /// <summary>
        /// Continues an observable sequence that is terminated by an exception with the next observable sequence.
        /// </summary>
        public static IObservable<TSource> Catch<TSource>(IScheduler scheduler, params IObservable<TSource>[] sources)
        {
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");
            if (sources == null)
                throw new ArgumentNullException("sources");

            return ((IEnumerable<IObservable<TSource>>)sources).Catch(scheduler);
        }

        /// <summary>
        /// Continues an observable sequence that is terminated by an exception with the next observable sequence.
        /// </summary>
        public static IObservable<TSource> Catch<TSource>(this IEnumerable<IObservable<TSource>> sources, IScheduler scheduler)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<TSource>(observer =>
            {
                var e = sources.GetEnumerator();
                var subscription = new MutableDisposable();
                var lastException = default(Exception);

                var cancelable = scheduler.Schedule(self =>
                {
                    if (subscription != null)
                    {
                        var current = default(IObservable<TSource>);
                        var hasNext = false;
                        try
                        {
                            hasNext = e.MoveNext();
                            if (hasNext)
                                current = e.Current;
                            else
                                e.Dispose();
                        }
                        catch (Exception exception)
                        {
                            observer.OnError(exception);
                            e.Dispose();
                            return;
                        }

                        if (!hasNext)
                        {
                            if (lastException != null)
                                observer.OnError(lastException);
                            else
                                observer.OnCompleted();
                            return;
                        }

                        var d = new MutableDisposable();
                        subscription.Disposable = d;
                        d.Disposable = current.Subscribe(observer.OnNext, exception =>
                        {
                            lastException = exception;
                            self();
                        }, observer.OnCompleted);
                    }
                });

                return new CompositeDisposable(subscription, cancelable);
            });
        }

        /// <summary>
        /// Continues an observable sequence that is terminated normally or by an exception with the next observable sequence.
        /// </summary>
        public static IObservable<TSource> OnErrorResumeNext<TSource>(this IObservable<TSource> first, IObservable<TSource> second, IScheduler scheduler)
        {
            if (first == null)
                throw new ArgumentNullException("first");
            if (second == null)
                throw new ArgumentNullException("second");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return OnErrorResumeNext(scheduler, first, second);
        }

        /// <summary>
        /// Continues an observable sequence that is terminated normally or by an exception with the next observable sequence.
        /// </summary>
        public static IObservable<TSource> OnErrorResumeNext<TSource>(IScheduler scheduler, params IObservable<TSource>[] sources)
        {
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");
            if (sources == null)
                throw new ArgumentNullException("sources");

            return ((IEnumerable<IObservable<TSource>>)sources).OnErrorResumeNext(scheduler);
        }

        /// <summary>
        /// Continues an observable sequence that is terminated normally or by an exception with the next observable sequence.
        /// </summary>
        public static IObservable<TSource> OnErrorResumeNext<TSource>(this IEnumerable<IObservable<TSource>> sources, IScheduler scheduler)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<TSource>(observer =>
            {
                var e = sources.GetEnumerator();
                var subscription = new MutableDisposable();

                var cancelable = scheduler.Schedule(self =>
                {
                    var hasNext = false;
                    var current = default(IObservable<TSource>);
                    try
                    {
                        hasNext = e.MoveNext();
                        if (hasNext)
                            current = e.Current;
                        else
                            e.Dispose();
                    }
                    catch (Exception exception)
                    {
                        observer.OnError(exception);
                        e.Dispose();
                        return;
                    }

                    if (!hasNext)
                    {
                        observer.OnCompleted();
                        return;
                    }

                    var d = new MutableDisposable();
                    subscription.Disposable = d;
                    d.Disposable = current.Subscribe(observer.OnNext, exception => self(), self);
                });

                return new CompositeDisposable(subscription, cancelable);
            });
        }

        /// <summary>
        /// Continues an observable sequence that is terminated normally or by an exception with the next observable sequence.
        /// </summary>
        public static IObservable<TSource> OnErrorResumeNext<TSource>(this IObservable<TSource> first, IObservable<TSource> second)
        {
            if (first == null)
                throw new ArgumentNullException("first");
            if (second == null)
                throw new ArgumentNullException("second");

            return first.OnErrorResumeNext(second, Scheduler.Immediate);
        }

        /// <summary>
        /// Continues an observable sequence that is terminated normally or by an exception with the next observable sequence.
        /// </summary>
        public static IObservable<TSource> OnErrorResumeNext<TSource>(params IObservable<TSource>[] sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            return OnErrorResumeNext(sources, Scheduler.Immediate);
        }

        /// <summary>
        /// Continues an observable sequence that is terminated normally or by an exception with the next observable sequence.
        /// </summary>
        public static IObservable<TSource> OnErrorResumeNext<TSource>(this IEnumerable<IObservable<TSource>> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            return sources.OnErrorResumeNext(Scheduler.Immediate);
        }

        /// <summary>
        /// Merges two observable sequences into one observable sequence by using the selector function.
        /// </summary>
        public static IObservable<TResult> Zip<TLeft, TRight, TResult>(this IObservable<TLeft> leftSource, IObservable<TRight> rightSource, Func<TLeft, TRight, TResult> selector)
        {
            if (leftSource == null)
                throw new ArgumentNullException("leftSource");
            if (rightSource == null)
                throw new ArgumentNullException("rightSource");
            if (selector == null)
                throw new ArgumentNullException("selector");

            return leftSource.Combine(rightSource, (IObserver<TResult> observer, IDisposable leftSubscription, IDisposable rightSubscription) =>
            {
                var combiner = new ZipHelper<TLeft, TRight, TResult>(selector, observer);
                return new BinaryObserver<TLeft, TRight>(combiner.Left.OnNext, combiner.Right.OnNext);
            });
        }

        class ZipHelper<TLeft, TRight, TResult>
        {
            private Func<TLeft, TRight, TResult> selector;
            private IObserver<TResult> observer;

            private Queue<Notification<TLeft>> leftQ;
            private Queue<Notification<TRight>> rightQ;

            public ZipHelper(Func<TLeft, TRight, TResult> selector, IObserver<TResult> observer)
            {
                this.selector = selector;
                this.observer = observer;

                leftQ = new Queue<Notification<TLeft>>();
                rightQ = new Queue<Notification<TRight>>();

                Left = Observer.Create<Notification<TLeft>>(left =>
                {
                    if (left.Kind == NotificationKind.OnError)
                    {
                        observer.OnError(left.Exception);
                        return;
                    }

                    if (rightQ.Count == 0)
                        leftQ.Enqueue(left);
                    else
                        OnNext(left, rightQ.Dequeue());
                });

                Right = Observer.Create<Notification<TRight>>(right =>
                {
                    if (right.Kind == NotificationKind.OnError)
                    {
                        observer.OnError(right.Exception);
                        return;
                    }

                    if (leftQ.Count == 0)
                        rightQ.Enqueue(right);
                    else
                        OnNext(leftQ.Dequeue(), right);
                });
            }

            public IObserver<Notification<TLeft>> Left { get; private set; }
            public IObserver<Notification<TRight>> Right { get; private set; }

            private void OnNext(Notification<TLeft> left, Notification<TRight> right)
            {
                if (left.Kind == NotificationKind.OnCompleted || right.Kind == NotificationKind.OnCompleted)
                {
                    observer.OnCompleted();
                    return;
                }

                TResult result;
                try
                {
                    result = selector(left.Value, right.Value);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                    return;
                }

                observer.OnNext(result);
            }
        }

        /// <summary>
        /// Merges an observable sequence and an enumerable sequence into one observable sequence by using the selector function.
        /// </summary>
        public static IObservable<TResult> Zip<TLeft, TRight, TResult>(this IObservable<TLeft> leftSource, IEnumerable<TRight> rightSource, Func<TLeft, TRight, TResult> selector)
        {
            if (leftSource == null)
                throw new ArgumentNullException("leftSource");
            if (rightSource == null)
                throw new ArgumentNullException("rightSource");
            if (selector == null)
                throw new ArgumentNullException("selector");

            return new AnonymousObservable<TResult>(observer =>
            {
                var rightEnumerator = rightSource.GetEnumerator();
                var leftSubscription = leftSource.Subscribe(left =>
                    {
                        var hasNext = false;
                        try
                        {
                            hasNext = rightEnumerator.MoveNext();
                        }
                        catch (Exception ex)
                        {
                            observer.OnError(ex);
                            return;
                        }

                        if (hasNext)
                        {
                            var right = default(TRight);
                            try
                            {
                                right = rightEnumerator.Current;
                            }
                            catch (Exception ex)
                            {
                                observer.OnError(ex);
                                return;
                            }

                            TResult result;
                            try
                            {
                                result = selector(left, right);
                            }
                            catch (Exception ex)
                            {
                                observer.OnError(ex);
                                return;
                            }
                            observer.OnNext(result);
                        }
                        else
                        {
                            observer.OnCompleted();
                        }
                    },
                    observer.OnError,
                    observer.OnCompleted
                );

                return new CompositeDisposable(leftSubscription, rightEnumerator);
            });
        }

        /// <summary>
        /// Merges two observable sequences into one observable sequence by using the selector function
        /// whenever one of the observable sequences has a new value.
        /// </summary>
        public static IObservable<TResult> CombineLatest<TLeft, TRight, TResult>(this IObservable<TLeft> leftSource, IObservable<TRight> rightSource, Func<TLeft, TRight, TResult> selector)
        {
            if (leftSource == null)
                throw new ArgumentNullException("leftSource");
            if (rightSource == null)
                throw new ArgumentNullException("rightSource");
            if (selector == null)
                throw new ArgumentNullException("selector");

            return leftSource.Combine(rightSource, (IObserver<TResult> observer, IDisposable leftSubscription, IDisposable rightSubscription) =>
            {
                var combiner = new CombineLatestHelper<TLeft, TRight, TResult>(selector, observer);
                return new BinaryObserver<TLeft, TRight>(combiner.Left.OnNext, combiner.Right.OnNext);
            });
        }

        class CombineLatestHelper<TLeft, TRight, TResult>
        {
            private Func<TLeft, TRight, TResult> selector;
            private IObserver<TResult> observer;

            private bool leftStopped;
            private bool rightStopped;

            private Notification<TLeft>.OnNext leftValue;
            private Notification<TRight>.OnNext rightValue;

            public CombineLatestHelper(Func<TLeft, TRight, TResult> selector, IObserver<TResult> observer)
            {
                this.selector = selector;
                this.observer = observer;

                Left = Observer.Create<Notification<TLeft>>(left =>
                {
                    if (left.Kind == NotificationKind.OnNext)
                    {
                        leftValue = (Notification<TLeft>.OnNext)left;
                        if (rightValue != null)
                            OnNext();
                        else if (rightStopped) // other side empty
                            observer.OnCompleted();
                    }
                    else if (left.Kind == NotificationKind.OnError)
                    {
                        observer.OnError(left.Exception);
                    }
                    else
                    {
                        leftStopped = true;
                        if (rightStopped)
                            observer.OnCompleted();
                    }
                });

                Right = Observer.Create<Notification<TRight>>(right =>
                {
                    if (right.Kind == NotificationKind.OnNext)
                    {
                        rightValue = (Notification<TRight>.OnNext)right;
                        if (leftValue != null)
                            OnNext();
                        else if (leftStopped) // other side empty
                            observer.OnCompleted();
                    }
                    else if (right.Kind == NotificationKind.OnError)
                    {
                        observer.OnError(right.Exception);
                    }
                    else
                    {
                        rightStopped = true;
                        if (leftStopped)
                            observer.OnCompleted();
                    }
                });
            }

            public IObserver<Notification<TLeft>> Left { get; private set; }
            public IObserver<Notification<TRight>> Right { get; private set; }

            private void OnNext()
            {
                TResult result;
                try
                {
                    result = selector(leftValue.Value, rightValue.Value);
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                    return;
                }

                observer.OnNext(result);
            }
        }

        /// <summary>
        /// Returns the observable sequence that reacts first.
        /// </summary>
        public static IObservable<TSource> Amb<TSource>(this IObservable<TSource> leftSource, IObservable<TSource> rightSource)
        {
            if (leftSource == null)
                throw new ArgumentNullException("leftSource");
            if (rightSource == null)
                throw new ArgumentNullException("rightSource");

            return AmbHelper(leftSource, rightSource);
        }

        /// <summary>
        /// Returns the observable sequence that reacts first.
        /// </summary>
        public static IObservable<TSource> Amb<TSource>(params IObservable<TSource>[] sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            return AmbHelper(sources);
        }

        /// <summary>
        /// Returns the observable sequence that reacts first.
        /// </summary>
        public static IObservable<TSource> Amb<TSource>(this IEnumerable<IObservable<TSource>> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            return AmbHelper(sources);
        }

        private static IObservable<TSource> AmbHelper<TSource>(IEnumerable<IObservable<TSource>> sources)
        {
            return sources.Aggregate(Observable.Never<TSource>(), (previous, current) => previous.Amb(current));
        }

        private static IObservable<TSource> AmbHelper<TSource>(IObservable<TSource> leftSource, IObservable<TSource> rightSource)
        {
            return leftSource.Combine(rightSource, (IObserver<TSource> observer, IDisposable leftSubscription, IDisposable rightSubscription) =>
            {
                var choice = AmbState.Neither;
                return new BinaryObserver<TSource, TSource>(
                    left =>
                    {
                        if (choice == AmbState.Neither)
                        {
                            choice = AmbState.Left;
                            rightSubscription.Dispose();
                        }
                        if (choice == AmbState.Left)
                            left.Accept(observer);
                    },
                    right =>
                    {
                        if (choice == AmbState.Neither)
                        {
                            choice = AmbState.Right;
                            leftSubscription.Dispose();
                        }
                        if (choice == AmbState.Right)
                            right.Accept(observer);
                    });
            });
        }

        enum AmbState
        {
            Left,
            Right,
            Neither
        }

        /// <summary>
        /// Runs two observable sequences in parallel and combines their last values.
        /// </summary>
        public static IObservable<TResult> ForkJoin<TLeft, TRight, TResult>(this IObservable<TLeft> leftSource, IObservable<TRight> rightSource, Func<TLeft, TRight, TResult> selector)
        {
            if (leftSource == null)
                throw new ArgumentNullException("leftSource");
            if (rightSource == null)
                throw new ArgumentNullException("rightSource");
            if (selector == null)
                throw new ArgumentNullException("selector");

            return leftSource.Combine<TLeft, TRight, TResult>(rightSource, (observer, leftSubscription, rightSubscription) =>
                {
                    var leftStopped = false;
                    var rightStopped = false;
                    var hasLeft = false;
                    var hasRight = false;
                    var lastLeft = default(TLeft);
                    var lastRight = default(TRight);

                    return new BinaryObserver<TLeft, TRight>(
                        left =>
                        {
                            switch (left.Kind)
                            {
                                case NotificationKind.OnNext:
                                    hasLeft = true;
                                    lastLeft = left.Value;
                                    break;
                                case NotificationKind.OnError:
                                    rightSubscription.Dispose();
                                    observer.OnError(left.Exception);
                                    break;
                                case NotificationKind.OnCompleted:
                                    leftStopped = true;
                                    if (rightStopped)
                                    {
                                        if (!hasLeft)
                                            observer.OnCompleted();
                                        else if (!hasRight)
                                            observer.OnCompleted();
                                        else
                                        {
                                            TResult result;
                                            try
                                            {
                                                result = selector(lastLeft, lastRight);
                                            }
                                            catch (Exception exception)
                                            {
                                                observer.OnError(exception);
                                                return;
                                            }
                                            observer.OnNext(result);
                                            observer.OnCompleted();
                                        }
                                    }
                                    break;
                            }
                        },
                        right =>
                        {
                            switch (right.Kind)
                            {
                                case NotificationKind.OnNext:
                                    hasRight = true;
                                    lastRight = right.Value;
                                    break;
                                case NotificationKind.OnError:
                                    rightSubscription.Dispose();
                                    observer.OnError(right.Exception);
                                    break;
                                case NotificationKind.OnCompleted:
                                    rightStopped = true;
                                    if (leftStopped)
                                    {
                                        if (!hasLeft)
                                            observer.OnCompleted();
                                        else if (!hasRight)
                                            observer.OnCompleted();
                                        else
                                        {
                                            TResult result;
                                            try
                                            {
                                                result = selector(lastLeft, lastRight);
                                            }
                                            catch (Exception exception)
                                            {
                                                observer.OnError(exception);
                                                return;
                                            }
                                            observer.OnNext(result);
                                            observer.OnCompleted();
                                        }
                                    }
                                    break;
                            }
                        });
                });
        }

        /// <summary>
        /// Runs all observable sequences in parallel and combines their last values.
        /// </summary>
        public static IObservable<TSource[]> ForkJoin<TSource>(params IObservable<TSource>[] sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            return sources.ForkJoin();
        }

        /// <summary>
        /// Runs all observable sequences in parallel and combines their last values.
        /// </summary>
        public static IObservable<TSource[]> ForkJoin<TSource>(this IEnumerable<IObservable<TSource>> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            return sources.Aggregate(Observable.Return<List<TSource>>(new List<TSource>()), (xs, ys) => xs.ForkJoin(ys, (x, y) => { x.Add(y); return x; })).Select(x => x.ToArray());
        }

        /// <summary>
        /// Returns the values from the source observable sequence until the other observable sequence produces a value.
        /// </summary>
        public static IObservable<TSource> TakeUntil<TSource, TOther>(this IObservable<TSource> source, IObservable<TOther> other)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (other == null)
                throw new ArgumentNullException("other");

            return other.Combine(source, (IObserver<TSource> observer, IDisposable otherSubscription, IDisposable sourceSubscription) =>
            {
                var isSourceStopped = false;
                var isOtherStopped = false;

                return new BinaryObserver<TOther, TSource>(
                    otherValue =>
                    {
                        if (!isSourceStopped && !isOtherStopped)
                        {
                            if (otherValue.Kind == NotificationKind.OnCompleted)
                            {
                                isOtherStopped = true;
                            }
                            else if (otherValue.Kind == NotificationKind.OnError)
                            {
                                isOtherStopped = true;
                                isSourceStopped = true;
                                observer.OnError(otherValue.Exception);
                            }
                            else
                            {
                                isSourceStopped = true;
                                observer.OnCompleted();
                            }

                        }
                    },
                    sourceValue =>
                    {
                        if (!isSourceStopped)
                        {
                            sourceValue.Accept(observer);
                            isSourceStopped = sourceValue.Kind != NotificationKind.OnNext;
                            if (isSourceStopped)
                            {
                                otherSubscription.Dispose();
                            }
                        }
                    });
            });
        }

        /// <summary>
        /// Returns the values from the source observable sequence only after the other observable sequence produces a value.
        /// </summary>
        public static IObservable<TSource> SkipUntil<TSource, TOther>(this IObservable<TSource> source, IObservable<TOther> other)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (other == null)
                throw new ArgumentNullException("other");

            return source.Combine(other, (IObserver<TSource> observer, IDisposable leftSubscription, IDisposable rightSubscription) =>
            {
                var open = false;
                var rightStopped = false;
                return new BinaryObserver<TSource, TOther>(
                    left =>
                    {
                        if (open)
                            left.Accept(observer);
                    },
                    right =>
                    {
                        if (!rightStopped)
                        {
                            if (right.Kind == NotificationKind.OnNext)
                                open = true;
                            else if (right.Kind == NotificationKind.OnError)
                                observer.OnError(right.Exception);

                            rightStopped = true;
                            rightSubscription.Dispose();
                        }
                    });
            });
        }

        /// <summary>
        /// Merges two observable sequences into a single observable sequence.
        /// </summary>
        public static IObservable<TSource> Merge<TSource>(this IObservable<TSource> leftSource, IObservable<TSource> rightSource, IScheduler scheduler)
        {
            if (leftSource == null)
                throw new ArgumentNullException("leftSource");
            if (rightSource == null)
                throw new ArgumentNullException("rightSource");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return Merge(new[] { leftSource, rightSource }, scheduler);
        }

        /// <summary>
        /// Merges two observable sequences into a single observable sequence.
        /// </summary>
        public static IObservable<TSource> Merge<TSource>(this IObservable<TSource> leftSource, IObservable<TSource> rightSource)
        {
            if (leftSource == null)
                throw new ArgumentNullException("leftSource");
            if (rightSource == null)
                throw new ArgumentNullException("rightSource");

            return Merge(new[] { leftSource, rightSource });
        }

        /// <summary>
        /// Merges all the observable sequences into a single observable sequence.
        /// </summary>
        public static IObservable<TSource> Merge<TSource>(params IObservable<TSource>[] sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            return sources.Merge();
        }

        /// <summary>
        /// Merges all the observable sequences into a single observable sequence.
        /// </summary>
        public static IObservable<TSource> Merge<TSource>(IScheduler scheduler, params IObservable<TSource>[] sources)
        {
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");
            if (sources == null)
                throw new ArgumentNullException("sources");

            return sources.Merge(scheduler);
        }

        /// <summary>
        /// Merges all the observable sequences into a single observable sequence.
        /// </summary>
        public static IObservable<TSource> Merge<TSource>(this IEnumerable<IObservable<TSource>> sources)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");

            return sources.Merge(Scheduler.Immediate);
        }

        /// <summary>
        /// Merges an observable sequence of observable sequences into an observable sequence.
        /// </summary>
        public static IObservable<TSource> Merge<TSource>(this IEnumerable<IObservable<TSource>> sources, IScheduler scheduler)
        {
            if (sources == null)
                throw new ArgumentNullException("sources");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return sources.ToObservable(scheduler).Merge();
        }
    }

}
