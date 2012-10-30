using System;
using System.Collections.Generic;
using System.Threading;
using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Disposables;


namespace
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Concurrency
{
    /// <summary>
    /// Represents an object that schedules units of work on the current thread.
    /// </summary>
    public sealed class CurrentThreadScheduler : IScheduler
    {
        internal static readonly CurrentThreadScheduler Instance = new CurrentThreadScheduler();

        CurrentThreadScheduler()
        {
        }

        [ThreadStatic]
        static Queue<Action> queue;

        /// <summary>
        /// Gets the scheduler's notion of current time.
        /// </summary>
        public DateTimeOffset Now { get { return Scheduler.Now; } }

        /// <summary>
        /// Ensures action is surrounded by a trampoline.
        /// </summary>
        public void EnsureTrampoline(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            if (queue == null)
            {
                try
                {
                    queue = new Queue<Action>();
                    action();
                    while (queue.Count > 0)
                        queue.Dequeue()();
                }
                finally
                {
                    queue = null;
                }
            }
            else
                action();
        }

        /// <summary>
        /// Schedules action to be executed.
        /// </summary>
        public IDisposable Schedule(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var cancelable = new BooleanDisposable();

            if (queue == null)
            {
                try
                {
                    queue = new Queue<Action>();
                    action();
                    while (queue.Count > 0)
                        queue.Dequeue()();
                }
                finally
                {
                    queue = null;
                }
            }
            else
            {
                queue.Enqueue(() =>
                {
                    if (!cancelable.IsDisposed)
                        action();
                });
            }
            return cancelable;
        }

        /// <summary>
        /// Schedules action to be executed after dueTime.
        /// </summary>
        public IDisposable Schedule(Action action, TimeSpan dueTime)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var dt = Scheduler.Normalize(dueTime);

            var cancelable = new BooleanDisposable();

            if (queue == null)
            {
                try
                {
                    queue = new Queue<Action>();
                    Thread.Sleep(dt);
                    action();
                    while (queue.Count > 0)
                        queue.Dequeue()();
                }
                finally
                {
                    queue = null;
                }
            }
            else
            {
                queue.Enqueue(() =>
                {
                    if (!cancelable.IsDisposed)
                    {
                        Thread.Sleep(dt);
                        if (!cancelable.IsDisposed)
                        {
                            action();
                        }
                    }
                });
            }
            return cancelable;
        }
    }
}
