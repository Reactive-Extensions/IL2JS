using System;
using System.Windows.Threading;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Linq;

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Windows.Threading
{
#if !IL2JS
    /// <summary>
    /// Provides a set of static methods for subscribing to IObservables using Dispatchers.
    /// </summary>
    public static class DispatcherObservableExtensions
    {
        /// <summary>
        /// Asynchronously notify observers using the dispatcher.
        /// </summary>
        public static IObservable<TSource> ObserveOn<TSource>(this IObservable<TSource> source, Dispatcher dispatcher)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (dispatcher == null)
                throw new ArgumentNullException("dispatcher");

            return source.ObserveOn(new DispatcherSynchronizationContext(dispatcher));
        }

        /// <summary>
        /// Asynchronously subscribes and unsubscribes observers using the dispatcher.
        /// </summary>
        public static IObservable<TSource> SubscribeOn<TSource>(this IObservable<TSource> source, Dispatcher dispatcher)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (dispatcher == null)
                throw new ArgumentNullException("dispatcher");

            return source.SubscribeOn(new DispatcherSynchronizationContext(dispatcher));
        }
    }
#endif
}
