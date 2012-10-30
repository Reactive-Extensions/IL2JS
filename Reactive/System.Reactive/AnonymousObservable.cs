using System;
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
Reactive.Collections.Generic
{
    class AnonymousObservable<T> : IObservable<T>
    {
        Func<IObserver<T>, IDisposable> subscribe;

        public AnonymousObservable(Func<IObserver<T>, IDisposable> subscribe)
        {
            this.subscribe = subscribe;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null)
                throw new ArgumentNullException("observer");

            var autoDetachObserver = observer as AutoDetachObserver;
            if (autoDetachObserver == null)
                autoDetachObserver = new AutoDetachObserver(observer);
            var subscription = new Disposable(autoDetachObserver);
            autoDetachObserver.Add(subscription);
            Scheduler.CurrentThread.EnsureTrampoline(() => subscription.Set(subscribe(autoDetachObserver)));
            return subscription;
        }

        class AutoDetachObserver : AbstractObserver<T>
        {
            IObserver<T> observer;
            CompositeDisposable group = new CompositeDisposable();

            public AutoDetachObserver(IObserver<T> observer)
            {
                this.observer = observer;
            }

            public void Add(IDisposable disposable)
            {
                group.Add(disposable);
            }

            protected override void Next(T value)
            {
                observer.OnNext(value);
            }

            protected override void Error(Exception exception)
            {
                observer.OnError(exception);
                group.Dispose();
            }

            protected override void Completed()
            {
                observer.OnCompleted();
                group.Dispose();
            }
        }

        sealed class Disposable : IDisposable
        {
            AutoDetachObserver observer;
            IDisposable disposable;
            bool disposed;
            object gate = new object(); // protects disposable state

            public Disposable(AutoDetachObserver observer)
            {
                this.observer = observer;
            }

            public void Set(IDisposable disposable)
            {
                if (disposable == null)
                    throw new ArgumentNullException("disposable");

                var shouldDispose = false;
                lock (gate)
                {
                    if (!disposed)
                        this.disposable = disposable;
                    else
                        shouldDispose = true;
                }
                if (shouldDispose)
                    disposable.Dispose();
            }

            public void Dispose()
            {
                observer.Stop();
                var disposable = default(IDisposable);
                lock (gate)
                {
                    if (!disposed)
                    {
                        disposed = true;
                        disposable = this.disposable;
                    }
                }
                if (disposable != null)
                    disposable.Dispose();
            }

        }
    }
}
