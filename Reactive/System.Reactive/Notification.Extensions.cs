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
Reactive.Linq;
using System.Text;
using System.Diagnostics;
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
    /// <summary>
    /// Provides a set of static methods for subscribing delegates to observables.
    /// </summary>
    public static class Notification
    {
        /// <summary>
        /// Returns an observable sequence with a single notification.
        /// </summary>
        public static IObservable<T> ToObservable<T>(this Notification<T> notification)
        {
            if (notification == null)
                throw new ArgumentNullException("notification");

            return notification.ToObservable(Scheduler.CurrentThread);
        }

        /// <summary>
        /// Returns an observable sequence with a single notification.
        /// </summary>
        public static IObservable<T> ToObservable<T>(this Notification<T> notification, IScheduler scheduler)
        {
            if (notification == null)
                throw new ArgumentNullException("notification");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            return new AnonymousObservable<T>(observer => scheduler.Schedule(() =>
                                                 {
                                                     notification.Accept(observer);
                                                     if (notification.Kind == NotificationKind.OnNext)
                                                         observer.OnCompleted();
                                                 }));
        }
    }
}
