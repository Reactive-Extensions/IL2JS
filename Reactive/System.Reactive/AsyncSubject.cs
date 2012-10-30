using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Represents the result of an asynchronous operation.
    /// </summary>
    public class AsyncSubject<T> : ISubject<T>
    {
        List<IObserver<T>> observers;
        Notification<T> last;
        IScheduler scheduler;
        bool completed;

        /// <summary>
        /// Creates a subject that can only receive one value and that value is cached for all future observations.
        /// </summary>
        public AsyncSubject() : this(Scheduler.CurrentThread)
        {
        }

        /// <summary>
        /// Creates a subject that can only receive one value and that value is cached for all future observations.
        /// </summary>
        public AsyncSubject(IScheduler scheduler)
        {
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            this.observers = new List<IObserver<T>>();
            this.last = null;
            this.scheduler = scheduler;
            this.completed = false;
        }

        /// <summary>
        /// Notifies all subscribed observers with the value.
        /// </summary>
        public void OnNext(T value)
        {
            lock (this.observers)
                if (!completed)
                    last = new Notification<T>.OnNext(value);
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
                if (!completed)
                {
                    observers = this.observers.ToArray();
                    last = new Notification<T>.OnError(exception);
                    completed = true;
                    this.observers.Clear();
                }
            }
            if (observers != null)
                foreach (var observer in observers)
                    last.Accept(observer);
        }

        /// <summary>
        /// Notifies all subscribed observers of the end of the sequence.
        /// </summary>
        public void OnCompleted()
        {
            var observers = default(IObserver<T>[]);
            lock (this.observers)
            {
                if (!completed)
                {
                    completed = true;
                    observers = this.observers.ToArray();
                    if (last == null)
                        last = new Notification<T>.OnCompleted();
                    this.observers.Clear();
                }
            }
            if (observers != null)
                foreach (var observer in observers)
                {
                    last.Accept(observer);
                    if (last.HasValue)
                        observer.OnCompleted();
                }
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
                if (!completed)
                {
                    observers.Add(observer);
                    return new RemovableDisposable(this, observer);
                }
            }
            return scheduler.Schedule(() =>
            {
                var asyncObserver = new AsyncObserver(observer);
                last.Accept(asyncObserver);
            });
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
            AsyncSubject<T> subject;
            IObserver<T> observer;

            public RemovableDisposable(AsyncSubject<T> subject, IObserver<T> observer)
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

        class AsyncObserver : IObserver<T>
        {
            IObserver<T> observer;

            public AsyncObserver(IObserver<T> observer)
            {
                this.observer = observer;
            }

            public void OnNext(T value)
            {
                observer.OnNext(value);
                observer.OnCompleted();
            }

            public void OnError(Exception exception)
            {
                observer.OnError(exception);
            }

            public void OnCompleted()
            {
                observer.OnCompleted();
            }
        }
    }
}
