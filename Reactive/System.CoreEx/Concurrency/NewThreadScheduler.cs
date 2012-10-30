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
    /// Represents an object that schedules units of work on the current thread.
    /// </summary>
    public sealed class NewThreadScheduler : IScheduler
    {
        internal static readonly NewThreadScheduler Instance = new NewThreadScheduler();

        /// <summary>
        /// Gets the scheduler's notion of current time.
        /// </summary>
        public DateTimeOffset Now
        {
            get { return Scheduler.Now; }
        }

        /// <summary>
        /// Schedules action to be executed.
        /// </summary>
        public IDisposable Schedule(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var d = new BooleanDisposable();

            var thread = new Thread(() =>
                {
                    if (!d.IsDisposed)
                        action();
                });
            thread.Start();

            return d;
        }

        /// <summary>
        /// Schedules action to be executed after dueTime.
        /// </summary>
        public IDisposable Schedule(Action action, TimeSpan dueTime)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var dt = Scheduler.Normalize(dueTime);

            var g = new CompositeDisposable();

            g.Add(ThreadPoolScheduler.Instance.Schedule(() => g.Add(Schedule(action)), dt));

            return g;
        }
    }
#endif
}
