#if !NETCF37 && !SILVERLIGHT
using System;
using System.Diagnostics;
using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Disposables;
using System.Threading;
using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Diagnostics;



namespace
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Concurrency
{
    /// <summary>
    /// Represents an object that schedules units of work using a provided TaskFactory.
    /// </summary>
    public sealed class TaskPoolScheduler : IScheduler
    {
        internal static readonly TaskPoolScheduler Instance = new TaskPoolScheduler(System.Threading.Tasks.Task.Factory);
        System.Threading.Tasks.TaskFactory taskFactory;

        /// <summary>
        /// Creates an object that schedules units of work using the provided TaskFactory.
        /// </summary>
        public TaskPoolScheduler(System.Threading.Tasks.TaskFactory taskFactory)
        {
            this.taskFactory = taskFactory;
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

            var cancelable = new CancellationDisposable();
            taskFactory.StartNew(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        var raise = new Thread(() => { throw ex.PrepareForRethrow(); });
                        raise.Start();
                        raise.Join();
                    }
                }, cancelable.Token);
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
   
            var g = new CompositeDisposable();

            g.Add(ThreadPoolScheduler.Instance.Schedule(() => g.Add(Schedule(action)), dt));

            return g;
        }
    }
}
#endif
