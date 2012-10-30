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
    /// Represents a value that changes over time.
    /// </summary>
    public class BehaviorSubject<T> : ReplaySubject<T>
    {
        /// <summary>
        /// Creates a subject that caches its last value and starts with the specified value.
        /// </summary>
        public BehaviorSubject(T value, IScheduler scheduler)
            : base(1, scheduler)
        {
            OnNext(value);
        }

        /// <summary>
        /// Creates a subject that caches its last value and starts with the specified value.
        /// </summary>
        public BehaviorSubject(T value)
            : this(value, Scheduler.CurrentThread)
        {
        }
    }
}
