using System;
using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Concurrency;

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
    /// Provides a set of static methods for creating Schedulers.
    /// </summary>
    public static class Scheduler
    {
        static internal DateTimeOffset Now
        {
            get
            {
                return DateTimeOffset.Now;
            }
        }

        static internal TimeSpan Normalize(TimeSpan timeSpan)
        {
#if IL2JS
            if (timeSpan.TotalMilliseconds < 0)
#else
            if (timeSpan.Ticks < 0)
#endif
                return TimeSpan.Zero;
            return timeSpan;
        }

        /// <summary>
        /// Gets the scheduler that schedules work immediately on the current thread.
        /// </summary>
        public static ImmediateScheduler Immediate { get { return ImmediateScheduler.Instance; } }

        /// <summary>
        /// Gets the scheduler that schedules work as soon as possible on the current thread.
        /// </summary>
        public static CurrentThreadScheduler CurrentThread { get { return CurrentThreadScheduler.Instance; } }

#if IL2JS
        /// <summary>
        /// Gets the scheduler that schedules work on the ThreadPool.
        /// </summary>
        public static IScheduler ThreadPool { get { return JavaScriptTimeoutScheduler.Instance; } }

        /// <summary>
        /// Gets the scheduler that schedules work on a new thread.
        /// </summary>
        public static IScheduler NewThread { get { return JavaScriptTimeoutScheduler.Instance; } }
#else
        /// <summary>
        /// Gets the scheduler that schedules work on the ThreadPool.
        /// </summary>
        public static ThreadPoolScheduler ThreadPool { get { return ThreadPoolScheduler.Instance; } }

        /// <summary>
        /// Gets the scheduler that schedules work on a new thread.
        /// </summary>
        public static NewThreadScheduler NewThread { get { return NewThreadScheduler.Instance; } }
#endif

#if !SILVERLIGHT && !NETCF37
        /// <summary>
        /// Gets the scheduler that schedules work on the default Task Factory.
        /// </summary>
        public static TaskPoolScheduler TaskPool { get { return TaskPoolScheduler.Instance; } }
#endif

#if !IL2JS
        /// <summary>
        /// Gets the scheduler that schedules work on the current Dispatcher.
        /// </summary>
        public static DispatcherScheduler Dispatcher { get { return s_dispatcher; } }

        private static readonly DispatcherScheduler s_dispatcher = new DispatcherScheduler(
#if SILVERLIGHT || NETCF37
            System.Windows.Deployment.Current.Dispatcher
#else
            System.Windows.Threading.Dispatcher.CurrentDispatcher
#endif
            );
#endif

        /// <summary>
        /// Schedules action to be executed at dueTime.
        /// </summary>
        public static IDisposable Schedule(this IScheduler scheduler, Action action, DateTimeOffset dueTime)
        {
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");
            if (action == null)
                throw new ArgumentNullException("action");

            return scheduler.Schedule(action, dueTime - scheduler.Now);
        }

        /// <summary>
        /// Schedules action to be executed recursively.
        /// </summary>
        public static IDisposable Schedule(this IScheduler scheduler, Action<Action> action)
        {
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");
            if (action == null)
                throw new ArgumentNullException("action");

            var group = new CompositeDisposable();
            var recursiveAction = default(Action);
            var gate = new object();
            var asyncLock = new AsyncLock();
            recursiveAction = () => asyncLock.Wait(() => action(() =>
                {
                    var isAdded = false;
                    var isDone = false;
                    var d = default(IDisposable);
                    d = scheduler.Schedule(() =>
                        {
                            recursiveAction();
                            lock (gate)
                            {
                                if (isAdded)
                                    group.Remove(d);
                                else
                                    isDone = true;
                            }

                        });
                    lock (gate)
                    {
                        if (!isDone)
                        {
                            group.Add(d);
                            isAdded = true;
                        }
                    }
                }));
            group.Add(scheduler.Schedule(recursiveAction));

            return group;
        }

        /// <summary>
        /// Schedules action to be executed recursively after each dueTime.
        /// </summary>
        public static IDisposable Schedule(this IScheduler scheduler, Action<Action<TimeSpan>> action, TimeSpan dueTime)
        {
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");
            if (action == null)
                throw new ArgumentNullException("action");

            var group = new CompositeDisposable();
            var recursiveAction = default(Action);
            var gate = new object();
            var asyncLock = new AsyncLock();
            recursiveAction = () => asyncLock.Wait(() => action(dt =>
                {
                    var isAdded = false;
                    var isDone = false;
                    var d = default(IDisposable);
                    d = scheduler.Schedule(() =>
                    {
                        recursiveAction();
                        lock (gate)
                        {
                            if (isAdded)
                                group.Remove(d);
                            else
                                isDone = true;
                        }
                    }, dt);

                    lock (gate)
                    {
                        if (!isDone)
                        {
                            group.Add(d);
                            isAdded = true;
                        }
                    }
                }));
            group.Add(scheduler.Schedule(recursiveAction, dueTime));

            return group;
        }

        /// <summary>
        /// Schedules action to be executed recursively at each dueTime.
        /// </summary>
        public static IDisposable Schedule(this IScheduler scheduler, Action<Action<DateTimeOffset>> action, DateTimeOffset dueTime)
        {
            if (scheduler == null)
                throw new ArgumentNullException("scheduler");
            if (action == null)
                throw new ArgumentNullException("action");

            var group = new CompositeDisposable();
            var recursiveAction = default(Action);
            var gate = new object();
            var asyncLock = new AsyncLock();
            recursiveAction = () => asyncLock.Wait(() => action(dt =>
            {
                var isAdded = false;
                var isDone = false;
                var d = default(IDisposable);
                d = scheduler.Schedule(() =>
                {
                    recursiveAction();
                    lock (gate)
                    {
                        if (isAdded)
                            group.Remove(d);
                        else
                            isDone = true;
                    }
                }, dt);

                lock (gate)
                {
                    if (!isDone)
                    {
                        group.Add(d);
                        isAdded = true;
                    }
                }
            }));
            group.Add(scheduler.Schedule(recursiveAction, dueTime));

            return group;
        }
    }
}
