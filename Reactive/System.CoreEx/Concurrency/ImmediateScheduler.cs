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
    /// <summary>
    /// Represents an object that schedules units of work to run immediately on the current thread.
    /// </summary>
    public sealed class ImmediateScheduler : IScheduler
    {
        internal static readonly ImmediateScheduler Instance = new ImmediateScheduler();

        ImmediateScheduler()
        {
        }

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

            action();
            return Disposable.Empty;
        }

        /// <summary>
        /// Schedules action to be executed after dueTime.
        /// </summary>
        public IDisposable Schedule(Action action, TimeSpan dueTime)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var dt = Scheduler.Normalize(dueTime);

            Thread.Sleep(dt);
            action();
            return Disposable.Empty;
        }
    }
}
