using System;
using 
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Disposables;

#if !NETCF37 && !SILVERLIGHT




namespace
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Concurrency
{
    /// <summary>
    /// Represents an object that schedules units of work on the message loop associated with a control.
    /// </summary>
    public class ControlScheduler : IScheduler
    {
        System.Windows.Forms.Control control;

        /// <summary>
        /// Constructs a ControlScheduler that schedules units of work on the message loop associated with control.
        /// </summary>
        public ControlScheduler(System.Windows.Forms.Control control)
        {
            if (control == null)
                throw new ArgumentNullException("control");

            this.control = control;
        }

        /// <summary>
        /// Gets the scheduler's notion of current time.
        /// </summary>
        public DateTimeOffset Now { get { return Scheduler.Now; } }

        /// <summary>
        /// Gets the control associated with the ControlScheduler.
        /// </summary>
        public System.Windows.Forms.Control Control { get { return control; } }

        /// <summary>
        /// Schedules action to be executed on the message loop associated with the control.
        /// </summary>
        public IDisposable Schedule(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var cancelable = new BooleanDisposable();
            control.BeginInvoke(new Action(() =>
            {
                if (!cancelable.IsDisposed)
                    action();
            }));
            return cancelable;
        }

        /// <summary>
        /// Schedules action to be executed after dueTime on the message loop associated with the control.
        /// </summary>
        public IDisposable Schedule(Action action, TimeSpan dueTime)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            var dt = Scheduler.Normalize(dueTime);
            var time = (int)dt.TotalMilliseconds;
            if (time == 0)
                return Schedule(action);

            System.Windows.Forms.Timer timer = null;
            Control.Invoke(new Action(() =>
            {
                timer = new System.Windows.Forms.Timer();
                timer.Tick += (s, e) =>
                {
                    var t = timer;
                    if (t != null)
                        t.Enabled = false;
                    timer = null;
                    action();
                };
                timer.Interval = time;
                timer.Enabled = true;
            }));

            return new AnonymousDisposable(() =>
            {
                var t = timer;
                if (t != null)
                    t.Enabled = false;
                timer = null;
            });
        }
    }

}
#endif
