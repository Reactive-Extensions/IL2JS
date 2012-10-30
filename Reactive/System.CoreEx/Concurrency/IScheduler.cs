
using System;

namespace
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Concurrency
{
    /// <summary>
    /// Represents an object that schedules units of work.
    /// </summary>
    public interface IScheduler
    {
        /// <summary>
        /// Gets the scheduler's notion of current time.
        /// </summary>
        DateTimeOffset Now { get; }

        /// <summary>
        /// Schedules action to be executed.
        /// </summary>
        IDisposable Schedule(Action action);

        /// <summary>
        /// Schedules action to be executed after dueTime.
        /// </summary>
        IDisposable Schedule(Action action, TimeSpan dueTime);
    }
}
