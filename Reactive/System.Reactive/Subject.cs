using System;
using System.Collections.Generic;
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
    /// Represents an object that is both an observable sequence as well as an observer.
    /// </summary>
    public class Subject<T> : ISubject<T>
    {
        List<IObserver<T>> observers;
        bool isStopped;

        /// <summary>
        /// Creates a subject.
        /// </summary>
        public Subject()
        {
            observers = new List<IObserver<T>>();
            isStopped = false;
        }

        /// <summary>
        /// Notifies all subscribed observers with the value.
        /// </summary>
        public void OnNext(T value)
        {
            var observers = default(IObserver<T>[]);

            lock (this.observers)
                if (!isStopped)
                    observers = this.observers.ToArray();

            if (observers != null)
                foreach (var observer in observers)
                    observer.OnNext(value);
        }

        /// <summary>
        /// Notifies all subscribed observers with the exception.
        /// </summary>
        public void OnError(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");

            var observers = default(IObserver<T>[]);

            lock (this.observers)
            {
                if (!isStopped)
                {
                    observers = this.observers.ToArray();
                    isStopped = true;
                }
            }

            if (observers != null)
                foreach (var observer in observers)
                    observer.OnError(exception);
        }

        /// <summary>
        /// Notifies all subscribed observers of the end of the sequence.
        /// </summary>
        public void OnCompleted()
        {
            var observers = default(IObserver<T>[]);

            lock (this.observers)
            {
                if (!isStopped)
                {
                    observers = this.observers.ToArray();
                    isStopped = true;
                }
            }

            if (observers != null)
                foreach (var observer in observers)
                    observer.OnCompleted();
        }

        /// <summary>
        /// Subscribes an observer to the subject.
        /// </summary>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (observer == null)
                throw new ArgumentNullException("observer");

            lock (this.observers)
            {
                if (!isStopped)
                {
                    observers.Add(observer);
                    return new RemovableDisposable(this, observer);
                }
                else
                    return Disposable.Empty;
            }
        }

        void Unsubscribe(IObserver<T> observer)
        {
            lock (this.observers)
            {
                observers.Remove(observer);
            }
        }

        sealed class RemovableDisposable : IDisposable
        {
            Subject<T> subject;
            IObserver<T> observer;

            public RemovableDisposable(Subject<T> subject, IObserver<T> observer)
            {
                this.subject = subject;
                this.observer = observer;
            }

            public void Dispose()
            {
                subject.Unsubscribe(observer);
                GC.SuppressFinalize(this);
            }
        }
    }
}
