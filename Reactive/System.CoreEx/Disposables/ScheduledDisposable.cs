using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading;
using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Concurrency;



namespace
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Disposables
{
    /// <summary>
    /// Represents an object that schedules units of work on a provided scheduler.
    /// </summary>
    public class ScheduledDisposable : IDisposable
    {
        int disposed = 0;

        /// <summary>
        /// Gets a value indicating the underlying disposable.
        /// </summary>
        public IDisposable Disposable { get; private set; }

        /// <summary>
        /// Gets a value indicating the scheduler.
        /// </summary>
        public IScheduler Scheduler { get; private set; }

        /// <summary>
        /// Constructs a ScheduledDisposable that uses a scheduler on which to dipose the disposable.
        /// </summary>
        public ScheduledDisposable(IScheduler scheduler, IDisposable disposable)
        {
            Scheduler = scheduler;
            Disposable = disposable;
        }

        /// <summary>
        /// Disposes the wrapped disposable on the provided scheduler.
        /// </summary>
        public void Dispose()
        {
            Scheduler.Schedule(DisposeInner);
        }

        void DisposeInner()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                Disposable.Dispose();
            }
        }
    }
}
