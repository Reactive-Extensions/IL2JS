using System;
using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Disposables;

using System.Threading;


namespace
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Concurrency
{
#if !IL2JS
    /// <summary>
    /// Represents an object that schedules units of work on the threadpool.
    /// </summary>
    public sealed class ThreadPoolScheduler : IScheduler
    {
        internal static readonly ThreadPoolScheduler Instance = new ThreadPoolScheduler();

        ThreadPoolScheduler()
        {
        }

        /// <summary>
        /// Gets the scheduler's notion of current time.
        /// </summary>
        public DateTimeOffset Now { get { return Scheduler.Now; } }
        
        /// <summary>
        /// Schedules action to be executed.
        /// </summary>
        public IDisposable Schedule(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var cancelable = new BooleanDisposable();
            ThreadPool.QueueUserWorkItem(_ =>
                {
                    if (!cancelable.IsDisposed)
                        action();
                }, null);
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

            var timer = default(Timer);
            timer = new Timer(_ =>
            {
                timer = null;
                action();
            }, null, dt, TimeSpan.FromMilliseconds(System.Threading.Timeout.Infinite));

            return new AnonymousDisposable(() =>
            {
                var t = timer;
                if (t != null)
                    t.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                timer = null;
            });
        }
    }
#endif
}
