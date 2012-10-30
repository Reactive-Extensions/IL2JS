using System;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Linq;
using System.Text;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Disposables;

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Collections.Generic
{
    /// <summary>
    /// Represents an observable that can be connected and disconnected from its source.
    /// </summary>
    public class ConnectableObservable<T> : IConnectableObservable<T>
    {
        ISubject<T> subject;
        IObservable<T> source;
        bool hasSubscription;
        IDisposable subscription;
        object gate;

        /// <summary>
        /// Creates an observable that can be connected and disconnected from its source.
        /// </summary>
        public ConnectableObservable(IObservable<T> source, ISubject<T> subject)
        {
            this.subject = subject;
            this.source = source.AsObservable();
            this.gate = new object();
        }

        /// <summary>
        /// Creates an observable that can be connected and disconnected from its source.
        /// </summary>
        public ConnectableObservable(IObservable<T> source)
            : this(source, new Subject<T>())
        {
        }

        /// <summary>
        /// Connects the observable to its source.
        /// </summary>
        public IDisposable Connect()
        {
            var group = new CompositeDisposable(Disposable.Create(() =>
            {
                lock (gate)
                    hasSubscription = false;
            }));
            var shouldRun = false;
            var result = default(IDisposable);
            lock (gate)
            {
                if (!hasSubscription)
                {
                    hasSubscription = true;
                    subscription = group;
                    shouldRun = true;
                }
                result = subscription;
            }
            if (shouldRun)
                group.Add(source.Subscribe(subject));
            return result;
        }

        /// <summary>
        /// Subscribes an observer to the observable sequence.
        /// </summary>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            return subject.Subscribe(observer);
        }
    }
}
