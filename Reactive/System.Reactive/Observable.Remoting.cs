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

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Linq
{
	public static partial class Observable
	{
#if DESKTOPCLR20 || DESKTOPCLR40
        /// <summary>
        /// Makes an observable sequence remotable.
        /// </summary>
        public static IObservable<TSource> Remotable<TSource>(this IObservable<TSource> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return new SerializableObservable<TSource>(new RemotableObservable<TSource>(source));
        }

        [Serializable]
        class SerializableObservable<T> : IObservable<T>
        {
            readonly RemotableObservable<T> remotableObservable;

            public SerializableObservable(RemotableObservable<T> remotableObservable)
            {
                this.remotableObservable = remotableObservable;
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                return remotableObservable.Subscribe(new RemotableObserver<T>(observer));
            }
        }

        class RemotableObserver<T> : MarshalByRefObject, IObserver<T>
        {
            readonly IObserver<T> underlyingObserver;

            public RemotableObserver(IObserver<T> underlyingObserver)
            {
                this.underlyingObserver = underlyingObserver;
            }

            public void OnNext(T value)
            {
                underlyingObserver.OnNext(value);
            }

            public void OnError(Exception exception)
            {
                underlyingObserver.OnError(exception);
            }

            public void OnCompleted()
            {
                underlyingObserver.OnCompleted();
            }
        }

        sealed class RemotableObservable<T> : MarshalByRefObject, IObservable<T>
        {
            readonly IObservable<T> underlyingObservable;

            public RemotableObservable(IObservable<T> underlyingObservable)
            {
                this.underlyingObservable = underlyingObservable;
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                return new RemotableSubscription(underlyingObservable.Subscribe(observer));
            }

            sealed class RemotableSubscription : MarshalByRefObject, IDisposable
            {
                readonly IDisposable underlyingSubscription;

                public RemotableSubscription(IDisposable underlyingSubscription)
                {
                    this.underlyingSubscription = underlyingSubscription;
                }

                public void Dispose()
                {
                    underlyingSubscription.Dispose();
                }
            }
        }
#endif
    }
}
