using System;
using System.Collections.Generic;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Linq;
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
    /// <summary>
    /// Represents an object that is both an observable sequence as well as an observer.
    /// </summary>
    public class ReplaySubject<T> : ISubject<T>
    {
        private readonly TimeSpan _window;
        private const int InfiniteBufferSize = int.MaxValue;

        IScheduler scheduler;
        Queue<Timestamped<Notification<T>>> q;
        List<Queue<Timestamped<Notification<T>>>> qs;
        int bufferSize;
        List<IObserver<T>> observers;
        bool isStopped;
        TimeSpan window;

        /// <summary>
        /// Creates a replayable subject.
        /// </summary>
        public ReplaySubject(int bufferSize, TimeSpan window, IScheduler scheduler)
        {
            if (bufferSize < 0)
                throw new ArgumentOutOfRangeException("bufferSize");
            if (window.TotalMilliseconds < 0)
                throw new ArgumentOutOfRangeException("window");
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");

            this.scheduler = scheduler;
            this.bufferSize = bufferSize;
            this.window = window;
            q = new Queue<Timestamped<Notification<T>>>();
            qs = new List<Queue<Timestamped<Notification<T>>>>();
            observers = new List<IObserver<T>>();
            isStopped = false;
        }

        /// <summary>
        /// Creates a replayable subject.
        /// </summary>
        public ReplaySubject(int bufferSize, TimeSpan window)
            : this(bufferSize, window, Scheduler.CurrentThread)
        {
        }

        /// <summary>
        /// Creates a replayable subject.
        /// </summary>
        public ReplaySubject()
            : this(InfiniteBufferSize, TimeSpan.MaxValue, Scheduler.CurrentThread)
        {
        }

        /// <summary>
        /// Creates a replayable subject.
        /// </summary>
        public ReplaySubject(IScheduler scheduler)
            : this(InfiniteBufferSize, TimeSpan.MaxValue, scheduler)
        {
        }

        /// <summary>
        /// Creates a replayable subject.
        /// </summary>
        public ReplaySubject(int bufferSize, IScheduler scheduler)
            : this(bufferSize, TimeSpan.MaxValue, scheduler)
        {
        }

        /// <summary>
        /// Creates a replayable subject.
        /// </summary>
        public ReplaySubject(int bufferSize)
            : this(bufferSize, TimeSpan.MaxValue, Scheduler.CurrentThread)
        {
        }

        /// <summary>
        /// Creates a replayable subject.
        /// </summary>
        public ReplaySubject(TimeSpan window, IScheduler scheduler)
            : this(InfiniteBufferSize, window, scheduler)
        {
            _window = window;
        }

        /// <summary>
        /// Creates a replayable subject.
        /// </summary>
        public ReplaySubject(TimeSpan window)
            : this(InfiniteBufferSize, window, Scheduler.CurrentThread)
        {
        }

        void Trim(DateTimeOffset now)
        {
            while (q.Count > bufferSize)
                q.Dequeue();
            while (q.Count > 0 && now.Subtract(q.Peek().Timestamp).CompareTo(window) > 0)
                q.Dequeue();
        }

        void Enqueue(Notification<T> n)
        {
            var now = scheduler.Now;
            var t = new Timestamped<Notification<T>>(n, now);
            q.Enqueue(t);
            foreach (var qq in qs)
                qq.Enqueue(t);
            Trim(now);
        }

        /// <summary>
        /// Notifies all subscribed observers with the value.
        /// </summary>
        public void OnNext(T value)
        {
            var observers = default(IObserver<T>[]);
            lock (this.observers)
            {
                if (!isStopped)
                {
                    observers = this.observers.ToArray();
                    Enqueue(new Notification<T>.OnNext(value));
                }
            }
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
                    Enqueue(new Notification<T>.OnError(exception));
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
                    Enqueue(new Notification<T>.OnCompleted());
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

            var subscription = new RemovableDisposable(this, observer);
            var group = new CompositeDisposable(subscription);
            var myq = default(Queue<Timestamped<Notification<T>>>);
            lock (this.observers)
            {
                Trim(scheduler.Now);
                myq = new Queue<Timestamped<Notification<T>>>(q);
                qs.Add(myq);
            }
            group.Add(scheduler.Schedule(self =>
             {
                 var ts = default(Timestamped<Notification<T>>);
                 lock (this.observers)
                 {
                     if (!subscription.IsStopped && myq.Count > 0)
                         ts = myq.Dequeue();
                     else
                     {
                         qs.Remove(myq);
                         observers.Add(observer);
                         subscription.IsStarted = true;
                     }
                 }

                 if (ts.Value != null)
                 {
                     ts.Value.Accept(observer);
                     self();
                 }
             }));
            return group;
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
            ReplaySubject<T> subject;
            IObserver<T> observer;

            public RemovableDisposable(ReplaySubject<T> subject, IObserver<T> observer)
            {
                this.subject = subject;
                this.observer = observer;
            }

            public void Dispose()
            {
                lock (subject.observers)
                {
                    if (IsStarted)
                        subject.Unsubscribe(observer);
                    IsStopped = true;
                }
                GC.SuppressFinalize(this);
            }

            public bool IsStopped { get; private set; }
            public bool IsStarted { get; set; }
        }
    }
}
