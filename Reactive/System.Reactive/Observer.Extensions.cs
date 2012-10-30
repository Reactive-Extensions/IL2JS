using System;
using System.Diagnostics;
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
Microsoft.Windows.Phone
#endif
Reactive.Linq
{
    /// <summary>
    /// Provides a set of static methods for creating observers.
    /// </summary>
    public static class Observer
    {
        /// <summary>
        /// Creates an observer from a notification callback.
        /// </summary>
        public static IObserver<T> ToObserver<T>(this Action<Notification<T>> handler)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            return new AnonymousObserver<T>(
                x => handler(new Notification<T>.OnNext(x)),
                exception => handler(new Notification<T>.OnError(exception)),
                () => handler(new Notification<T>.OnCompleted()));
        }

        /// <summary>
        /// Creates a notification callback from an observer.
        /// </summary>
        public static Action<Notification<T>> ToNotifier<T>(this IObserver<T> observer)
        {
            if (observer == null)
                throw new ArgumentNullException("observer");

            return n => n.Accept(observer);
        }      

        /// <summary>
        /// Creates an observer from the specified OnNext action.
        /// </summary>
        public static IObserver<T> Create<T>(Action<T> onNext)
        {
            if (onNext == null)
                throw new ArgumentNullException("onNext");

            return new AnonymousObserver<T>(onNext, e => { throw e.PrepareForRethrow(); }, () => { });
        }

        /// <summary>
        /// Creates an observer from the specified OnNext and OnError actions.
        /// </summary>
        public static IObserver<T> Create<T>(Action<T> onNext, Action<Exception> onError)
        {
            if (onNext == null)
                throw new ArgumentNullException("onNext");
            if (onError == null)
                throw new ArgumentNullException("onError");

            return new AnonymousObserver<T>(onNext, onError, () => { });
        }

        /// <summary>
        /// Creates an observer from the specified OnNext and OnCompleted actions.
        /// </summary>
        public static IObserver<T> Create<T>(Action<T> onNext, Action onCompleted)
        {
            if (onNext == null)
                throw new ArgumentNullException("onNext");
            if (onCompleted == null)
                throw new ArgumentNullException("onCompleted");

            return new AnonymousObserver<T>(onNext, e => { throw e.PrepareForRethrow(); }, onCompleted);
        }

        /// <summary>
        /// Creates an observer from the specified OnNext, OnError, and OnCompleted actions.
        /// </summary>
        public static IObserver<T> Create<T>(Action<T> onNext, Action<Exception> onError, Action onCompleted)
        {
            if (onNext == null)
                throw new ArgumentNullException("onNext");
            if (onError == null)
                throw new ArgumentNullException("onError");
            if (onCompleted == null)
                throw new ArgumentNullException("onCompleted");

            return new AnonymousObserver<T>(onNext, onError, onCompleted);
        }

        /// <summary>
        /// Hides the identity of an observer.
        /// </summary>
        public static IObserver<TSource> AsObserver<TSource>(this IObserver<TSource> observer)
        {
            if (observer == null)
                throw new ArgumentNullException("observer");

            return new AnonymousObserver<TSource>(observer.OnNext, observer.OnError, observer.OnCompleted);
        }
    }
}
