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
Reactive.Diagnostics;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Linq;
using System.Text;

namespace
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive
{
    /// <summary>
    /// Provides a set of static methods for subscribing delegates to observables.
    /// </summary>
    public static class ObservableExtensions
    {
        /// <summary>
        /// Evaluates the observable sequence.
        /// </summary>
        public static IDisposable Subscribe<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Subscribe(_ => { }, exception => { throw exception.PrepareForRethrow(); }, () => { });
        }

        /// <summary>
        /// Subscribes a value handler to an observable sequence.
        /// </summary>
        public static IDisposable Subscribe<TSource>(this IObservable<TSource> source, Action<TSource> onNext)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (onNext == null)
                throw new ArgumentNullException("onNext");

            return source.Subscribe(onNext, exception => { throw exception.PrepareForRethrow(); }, () => { });
        }

        /// <summary>
        /// Subscribes a value handler and an exception handler to an observable sequence.
        /// </summary>
        public static IDisposable Subscribe<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action<Exception> onError)
        {
            if (onNext == null)
                throw new ArgumentNullException("onNext");
            if (onError == null)
                throw new ArgumentNullException("onError");

            return source.Subscribe(onNext, onError, () => { });
        }

        /// <summary>
        /// Subscribes a value handler and a completion handler to an observable sequence.
        /// </summary>
        public static IDisposable Subscribe<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action onCompleted)
        {
            if (onNext == null)
                throw new ArgumentNullException("onNext");
            if (onCompleted == null)
                throw new ArgumentNullException("onCompleted");


            return source.Subscribe(onNext, exception => { throw exception.PrepareForRethrow(); }, onCompleted);
        }

        /// <summary>
        /// Subscribes a value handler, an exception handler, and a completion handler to an observable sequence.
        /// </summary>
        public static IDisposable Subscribe<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action<Exception> onError, Action onCompleted)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (onNext == null)
                throw new ArgumentNullException("onNext");
            if (onError == null)
                throw new ArgumentNullException("onError");
            if (onCompleted == null)
                throw new ArgumentNullException("onCompleted");

            return source.Subscribe(new AnonymousObserver<TSource>(onNext, onError, onCompleted));
        }
    }
}
