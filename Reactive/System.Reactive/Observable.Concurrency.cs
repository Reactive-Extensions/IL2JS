using System;
using System.Collections.Generic;
#if !NETCF37 && !SILVERLIGHT
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Windows.Forms;
#endif
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Collections.Generic;
using System.Threading;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Disposables;
using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Windows.Threading;
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
        /// Asynchronously notify observers using the scheduler.
        /// </summary>
        public static IObservable<TSource> ObserveOn<TSource>(this IObservable<TSource> source, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<TSource>(observer =>
                {
                    var q = new Queue<Notification<TSource>>();
                    var active = false;
                    var gate = new object();
                    var cancelable = new MutableDisposable();
                    var subscription = source.Materialize().Subscribe(n =>
                        {
                            var shouldStart = false;
                            lock (gate)
                            {
                                shouldStart = !active;
                                active = true;
                                q.Enqueue(n);
                            }

                            if (shouldStart)
                                cancelable.Disposable = scheduler.Schedule(self =>
                                    {
                                        var notification = default(Notification<TSource>);
                                        lock (gate)
                                        {
                                            notification = q.Dequeue();
                                        }

                                        notification.Accept(observer);

                                        var shouldRecurse = false;
                                        lock (gate)
                                        {
                                            shouldRecurse = active = q.Count > 0;
                                        }

                                        if (shouldRecurse)
                                            self();
                                    });
                        });

                    return new CompositeDisposable(subscription, cancelable);
                });
        }

        /// <summary>
        /// Asynchronously subscribes and unsubscribes observers using scheduler.
        /// </summary>
        public static IObservable<TSource> SubscribeOn<TSource>(this IObservable<TSource> source, IScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<TSource>(observer =>
                {
                    var d = new MutableDisposable();

                    scheduler.Schedule(() => d.Disposable = new ScheduledDisposable(scheduler, source.Subscribe(observer)));

                    return d;
                });
        }

#if !IL2JS
        /// <summary>
        /// Asynchronously notify observers using the scheduler.
        /// </summary>
        public static IObservable<TSource> ObserveOn<TSource>(this IObservable<TSource> source, DispatcherScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return source.ObserveOn(scheduler.Dispatcher);
        } 
#endif     

#if !IL2JS
        /// <summary>
        /// Asynchronously notify observers using the current dispatcher.
        /// </summary>
        public static IObservable<TSource> ObserveOnDispatcher<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

#if SILVERLIGHT || NETCF37
            return source.ObserveOn(System.Windows.Deployment.Current.Dispatcher);
#else
            return source.ObserveOn(System.Windows.Threading.Dispatcher.CurrentDispatcher);
#endif
        }
#endif

#if !IL2JS
        /// <summary>
        /// Asynchronously subscribes and unsubscribes observers using the current dispatcher.
        /// </summary>
        public static IObservable<TSource> SubscribeOnDispatcher<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

#if SILVERLIGHT || NETCF37
            return source.SubscribeOn(System.Windows.Deployment.Current.Dispatcher);
#else
            return source.SubscribeOn(System.Windows.Threading.Dispatcher.CurrentDispatcher);
#endif
        }
#endif

#if !IL2JS
        /// <summary>
        /// Asynchronously subscribes and unsubscribes observers using the scheduler.
        /// </summary>
        public static IObservable<TSource> SubscribeOn<TSource>(this IObservable<TSource> source, DispatcherScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return source.SubscribeOn(scheduler.Dispatcher);
        } 
#endif       

#if !IL2JS
        /// <summary>
        /// Asynchronously subscribes and unsubscribes observers on the synchronization context.
        /// </summary>
        public static IObservable<TSource> SubscribeOn<TSource>(this IObservable<TSource> source, SynchronizationContext context)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (context == null)
                throw new ArgumentNullException("context");

            return new AnonymousObservable<TSource>(observer =>
            {
                var subscription = new MutableDisposable();
                context.Post(_ => subscription.Disposable = new ContextDisposable(context, source.Subscribe(observer)), null);
                return subscription;
            });
        }
#endif

#if !IL2JS
        /// <summary>
        /// Asynchronously notify observers on the synchronization context.
        /// </summary>
        public static IObservable<TSource> ObserveOn<TSource>(this IObservable<TSource> source, SynchronizationContext synchronizationContext)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (synchronizationContext == null)
                throw new ArgumentNullException("synchronizationContext");

            return new AnonymousObservable<TSource>(observer => source.Subscribe(
                        x => synchronizationContext.Post(_ => observer.OnNext(x), null),
                        exception => synchronizationContext.Post(_ => observer.OnError(exception), null),
                        () => synchronizationContext.Post(_ => observer.OnCompleted(), null)));
        }
#endif

#if DESKTOPCLR20 || DESKTOPCLR40

        /// <summary>
        /// Asynchronously subscribes and unsubscribes observers using the scheduler.
        /// </summary>
        public static IObservable<TSource> SubscribeOn<TSource>(this IObservable<TSource> source, ControlScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return source.SubscribeOn(scheduler.Control);
        }

        /// <summary>
        /// Asynchronously notify observers using the scheduler.
        /// </summary>
        public static IObservable<TSource> ObserveOn<TSource>(this IObservable<TSource> source, ControlScheduler scheduler)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return source.ObserveOn(scheduler.Control);
        }
#endif

        /// <summary>
        /// Synchronizes the observable sequence.
        /// </summary>
        public static IObservable<TSource> Synchronize<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return Defer(() =>
            {
                var gate = new object();
                return source.Synchronize(gate);
            });
        }

        /// <summary>
        /// Synchronizes the observable sequence.
        /// </summary>
        public static IObservable<TSource> Synchronize<TSource>(this IObservable<TSource> source, object gate)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (gate == null)
                throw new ArgumentNullException("gate");

            return new AnonymousObservable<TSource>(observer => source.Subscribe(new SynchronizedObserver<TSource>(observer, gate)));
        }
    }
}
