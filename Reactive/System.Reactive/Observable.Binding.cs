using System;
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
        /// <summary>
        /// Bind the source to the parameter without sharing subscription side-effects.
        /// </summary>
        public static IObservable<TResult> Let<TSource, TResult>(this IObservable<TSource> source, Func<IObservable<TSource>, IObservable<TResult>> function)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (function == null)
                throw new ArgumentNullException("function");

            return function(source);
        }

        /// <summary>
        /// Returns an observable sequence that stays connected to the source as long as there is at least one subscription to the observable sequence.
        /// </summary>
        public static IObservable<TSource> RefCount<TSource>(this IConnectableObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            var gate = new object();
            var count = 0;
            var connectable = source;
            var connectableSubscription = default(IDisposable);

            return new AnonymousObservable<TSource>(observer =>
            {
                var shouldConnect = false;

                lock (gate)
                {
                    count++;
                    shouldConnect = count == 1;
                }

                var subscription = connectable.Subscribe(observer);

                if (shouldConnect)
                    connectableSubscription = connectable.Connect();

                return Disposable.Create(() =>
                {
                    subscription.Dispose();

                    lock (gate)
                    {
                        count--;
                        if (count == 0)
                            connectableSubscription.Dispose();
                    }
                });
            });
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying source.
        /// </summary>
        public static IConnectableObservable<TSource> Publish<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return new ConnectableObservable<TSource>(source, new Subject<TSource>());
        }

        /// <summary>
        /// Returns an observable sequence that is the result of invoking the selector on a connectable observable sequence that shares a single subscription to the underlying source.
        /// </summary>
        public static IObservable<TResult> Publish<TSource, TResult>(this IObservable<TSource> source, Func<IObservable<TSource>, IObservable<TResult>> selector)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");

            return new AnonymousObservable<TResult>(observer =>
                {
                    var connectable = source.Publish();
                    return new CompositeDisposable(selector(connectable).Subscribe(observer), connectable.Connect());
                });
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying source containing only the last notification.
        /// </summary>
        public static IConnectableObservable<TSource> Prune<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Prune(Scheduler.CurrentThread);
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying source containing only the last notification.
        /// </summary>
        public static IConnectableObservable<TSource> Prune<TSource>(this IObservable<TSource> source, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new ConnectableObservable<TSource>(source, new AsyncSubject<TSource>(scheduler));
        }

        /// <summary>
        /// Returns an observable sequence that is the result of invoking the selector on a connectable observable sequence that shares a single subscription to the underlying source containing only the last notification.
        /// </summary>
        public static IObservable<TResult> Prune<TSource, TResult>(this IObservable<TSource> source, Func<IObservable<TSource>, IObservable<TResult>> selector)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");

            return source.Prune(selector, Scheduler.CurrentThread);
        }

        /// <summary>
        /// Returns an observable sequence that is the result of invoking the selector on a connectable observable sequence that shares a single subscription to the underlying source containing only the last notification.
        /// </summary>
        public static IObservable<TResult> Prune<TSource, TResult>(this IObservable<TSource> source, Func<IObservable<TSource>, IObservable<TResult>> selector, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<TResult>(observer =>
            {
                var connectable = source.Prune(scheduler);
                return new CompositeDisposable(selector(connectable).Subscribe(observer), connectable.Connect());
            });
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying source replaying all notifications.
        /// </summary>
        public static IConnectableObservable<TSource> Replay<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return new ConnectableObservable<TSource>(source, new ReplaySubject<TSource>(Scheduler.CurrentThread));
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying source replaying all notifications.
        /// </summary>
        public static IConnectableObservable<TSource> Replay<TSource>(this IObservable<TSource> source, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");            

            return new ConnectableObservable<TSource>(source, new ReplaySubject<TSource>(scheduler));
        }

        /// <summary>
        /// Returns an observable sequence that is the result of invoking the selector on a connectable observable sequence that shares a single subscription to the underlying source replaying all notifications.
        /// </summary>
        public static IObservable<TResult> Replay<TSource, TResult>(this IObservable<TSource> source, Func<IObservable<TSource>, IObservable<TResult>> selector)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");

            return source.Replay(selector, Scheduler.CurrentThread);
        }

        /// <summary>
        /// Returns an observable sequence that is the result of invoking the selector on a connectable observable sequence that shares a single subscription to the underlying source replaying all notifications.
        /// </summary>
        public static IObservable<TResult> Replay<TSource, TResult>(this IObservable<TSource> source, Func<IObservable<TSource>, IObservable<TResult>> selector, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<TResult>(observer =>
            {
                var connectable = source.Replay(scheduler);
                return new CompositeDisposable(selector(connectable).Subscribe(observer), connectable.Connect());
            });
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying source replaying all notifications within window.
        /// </summary>
        public static IConnectableObservable<TSource> Replay<TSource>(this IObservable<TSource> source, TimeSpan window)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (window.TotalMilliseconds < 0)
                throw new ArgumentOutOfRangeException("window");

            return new ConnectableObservable<TSource>(source, new ReplaySubject<TSource>(window, Scheduler.CurrentThread));
        }

        /// <summary>
        /// Returns an observable sequence that is the result of invoking the selector on a connectable observable sequence that shares a single subscription to the underlying source replaying all notifications within window.
        /// </summary>
        public static IObservable<TResult> Replay<TSource, TResult>(this IObservable<TSource> source, Func<IObservable<TSource>, IObservable<TResult>> selector, TimeSpan window)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");
            if (window.TotalMilliseconds < 0)
                throw new ArgumentOutOfRangeException("window");

            return new AnonymousObservable<TResult>(observer =>
            {
                var connectable = source.Replay(window);
                return new CompositeDisposable(selector(connectable).Subscribe(observer), connectable.Connect());
            });
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying source replaying all notifications within window.
        /// </summary>
        public static IConnectableObservable<TSource> Replay<TSource>(this IObservable<TSource> source, TimeSpan window, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (window.TotalMilliseconds < 0)
                throw new ArgumentOutOfRangeException("window");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new ConnectableObservable<TSource>(source, new ReplaySubject<TSource>(window, scheduler));
        }


        /// <summary>
        /// Returns an observable sequence that is the result of invoking the selector on a connectable observable sequence that shares a single subscription to the underlying source replaying all notifications within window.
        /// </summary>
        public static IObservable<TResult> Replay<TSource, TResult>(this IObservable<TSource> source, Func<IObservable<TSource>, IObservable<TResult>> selector, TimeSpan window, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");
            if (window.TotalMilliseconds < 0)
                throw new ArgumentOutOfRangeException("window");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<TResult>(observer =>
            {
                var connectable = source.Replay(window, scheduler);
                return new CompositeDisposable(selector(connectable).Subscribe(observer), connectable.Connect());
            });
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying source replaying bufferSize notifications.
        /// </summary>
        public static IConnectableObservable<TSource> Replay<TSource>(this IObservable<TSource> source, int bufferSize, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException("bufferSize");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new ConnectableObservable<TSource>(source, new ReplaySubject<TSource>(bufferSize, scheduler));
        }

        /// <summary>
        /// Returns an observable sequence that is the result of invoking the selector on a connectable observable sequence that shares a single subscription to the underlying source replaying bufferSize notifications.
        /// </summary>
        public static IObservable<TResult> Replay<TSource, TResult>(this IObservable<TSource> source, Func<IObservable<TSource>, IObservable<TResult>> selector, int bufferSize, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException("bufferSize");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<TResult>(observer =>
            {
                var connectable = source.Replay(bufferSize, scheduler);
                return new CompositeDisposable(selector(connectable).Subscribe(observer), connectable.Connect());
            });
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying source replaying bufferSize notifications.
        /// </summary>
        public static IConnectableObservable<TSource> Replay<TSource>(this IObservable<TSource> source, int bufferSize)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException("bufferSize");

            return new ConnectableObservable<TSource>(source, new ReplaySubject<TSource>(bufferSize, Scheduler.CurrentThread));
        }

        /// <summary>
        /// Returns an observable sequence that is the result of invoking the selector on a connectable observable sequence that shares a single subscription to the underlying source replaying bufferSize notifications.
        /// </summary>
        public static IObservable<TResult> Replay<TSource, TResult>(this IObservable<TSource> source, Func<IObservable<TSource>, IObservable<TResult>> selector, int bufferSize)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException("bufferSize");

            return new AnonymousObservable<TResult>(observer =>
            {
                var connectable = source.Replay(bufferSize);
                return new CompositeDisposable(selector(connectable).Subscribe(observer), connectable.Connect());
            });
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying source replaying bufferSize notifications within window.
        /// </summary>
        public static IConnectableObservable<TSource> Replay<TSource>(this IObservable<TSource> source, int bufferSize, TimeSpan window)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException("bufferSize");
            if (window.TotalMilliseconds < 0)
                throw new ArgumentOutOfRangeException("window");

            return new ConnectableObservable<TSource>(source, new ReplaySubject<TSource>(bufferSize, window));
        }

        /// <summary>
        /// Returns an observable sequence that is the result of invoking the selector on a connectable observable sequence that shares a single subscription to the underlying source replaying bufferSize notifications within window.
        /// </summary>
        public static IObservable<TResult> Replay<TSource, TResult>(this IObservable<TSource> source, Func<IObservable<TSource>, IObservable<TResult>> selector, int bufferSize, TimeSpan window)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException("bufferSize");
            if (window.TotalMilliseconds < 0)
                throw new ArgumentOutOfRangeException("window");

            return new AnonymousObservable<TResult>(observer =>
            {
                var connectable = source.Replay(bufferSize, window);
                return new CompositeDisposable(selector(connectable).Subscribe(observer), connectable.Connect());
            });
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying source replaying bufferSize notifications within window.
        /// </summary>
        public static IConnectableObservable<TSource> Replay<TSource>(this IObservable<TSource> source, int bufferSize, TimeSpan window, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException("bufferSize");
            if (window.TotalMilliseconds < 0)
                throw new ArgumentOutOfRangeException("window");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new ConnectableObservable<TSource>(source, new ReplaySubject<TSource>(bufferSize, window, scheduler));
        }

        /// <summary>
        /// Returns an observable sequence that is the result of invoking the selector on a connectable observable sequence that shares a single subscription to the underlying source replaying bufferSize notifications within window.
        /// </summary>
        public static IObservable<TResult> Replay<TSource, TResult>(this IObservable<TSource> source, Func<IObservable<TSource>, IObservable<TResult>> selector, int bufferSize, TimeSpan window, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException("bufferSize");
            if (window.TotalMilliseconds < 0)
                throw new ArgumentOutOfRangeException("window");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<TResult>(observer =>
            {
                var connectable = source.Replay(bufferSize, window, scheduler);
                return new CompositeDisposable(selector(connectable).Subscribe(observer), connectable.Connect());
            });
        }

        /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying source and starts with initialValue.
        /// </summary>
        public static IConnectableObservable<TSource> Publish<TSource>(this IObservable<TSource> source, TSource initialValue)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Publish(initialValue, Scheduler.CurrentThread);
        }

	    /// <summary>
        /// Returns a connectable observable sequence that shares a single subscription to the underlying source and starts with initialValue.
        /// </summary>
        public static IConnectableObservable<TSource> Publish<TSource>(this IObservable<TSource> source, TSource initialValue, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new ConnectableObservable<TSource>(source, new BehaviorSubject<TSource>(initialValue, scheduler));
        }

        /// <summary>
        /// Returns an observable sequence that is the result of invoking the selector on a connectable observable sequence that shares a single subscription to the underlying source and starts with initialValue.
        /// </summary>
        public static IObservable<TResult> Publish<TSource, TResult>(this IObservable<TSource> source, Func<IObservable<TSource>, IObservable<TResult>> selector, TSource initialValue)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");

            return source.Publish(selector, initialValue, Scheduler.CurrentThread);
        }

	    /// <summary>
        /// Returns an observable sequence that is the result of invoking the selector on a connectable observable sequence that shares a single subscription to the underlying source and starts with initialValue.
        /// </summary>
        public static IObservable<TResult> Publish<TSource, TResult>(this IObservable<TSource> source, Func<IObservable<TSource>, IObservable<TResult>> selector, TSource initialValue, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<TResult>(observer =>
            {
                var connectable = source.Publish(initialValue, scheduler);
                return new CompositeDisposable(selector(connectable).Subscribe(observer), connectable.Connect());
            });
        }
    }
}
