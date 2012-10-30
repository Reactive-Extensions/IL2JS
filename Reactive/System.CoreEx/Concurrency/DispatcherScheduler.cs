using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
#if !IL2JS
    /// <summary>
    /// Represents an object that schedules units of work on a Dispatcher.
    /// </summary>
    public class DispatcherScheduler : IScheduler
    {
        System.Windows.Threading.Dispatcher dispatcher;

        /// <summary>
        /// Constructs an DispatcherScheduler that schedules units of work on dispatcher.
        /// </summary>
        public DispatcherScheduler(System.Windows.Threading.Dispatcher dispatcher)
        {
            if (dispatcher == null)
                throw new ArgumentNullException("dispatcher");

            this.dispatcher = dispatcher;
        }

        /// <summary>
        /// Gets the scheduler's notion of current time.
        /// </summary>
        public DateTimeOffset Now { get { return Scheduler.Now; } }

        /// <summary>
        /// Gets the dispatcher associated with the DispatcherScheduler.
        /// </summary>
        public System.Windows.Threading.Dispatcher Dispatcher { get { return dispatcher; } }

        /// <summary>
        /// Schedules action to be executed on the dispatcher.
        /// </summary>
        public IDisposable Schedule(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var cancelable = new BooleanDisposable();
            dispatcher.BeginInvoke(new Action(() =>
            {
                if (!cancelable.IsDisposed)
                    action();
            }));
            return cancelable;
        }

        /// <summary>
        /// Schedules action to be executed after dueTime on the dispatcher.
        /// </summary>
        public IDisposable Schedule(Action action, TimeSpan dueTime)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var dt = Scheduler.Normalize(dueTime);

#if SILVERLIGHT || NETCF37
            var timer = new System.Windows.Threading.DispatcherTimer();
#else
            var timer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Background, dispatcher);
#endif
            timer.Tick += (s, e) =>
            {
                var t = timer;
                if (t != null)
                    t.Stop();
                timer = null;
                action();
            };

            timer.Interval = dt;
            timer.Start();

            return new AnonymousDisposable(() =>
            {
                var t = timer;
                if (t != null)
                    t.Stop();
                timer = null;
            });
        }
    }
#endif
}
