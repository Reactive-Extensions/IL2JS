using Microsoft.LiveLabs.JavaScript.Interop;
using System.Concurrency;
using System.Disposables;

namespace System.Concurrency
{
    public class DispatcherScheduler : IScheduler
    {
        internal readonly static DispatcherScheduler Instance = new DispatcherScheduler();

        private DateTimeOffset? last;

        public DateTimeOffset Now
        {
            get
            {
                return DateTimeOffset.Now;
            }
        }

        public IDisposable Schedule(Action action)
        {
            return Schedule(action, TimeSpan.Zero);
        }

        [Import("function(action, ms) { return window.setTimeout(action, ms); }")]
        extern private static int SetTimeout(Action action, double ms);

        [Import("function(id) { return window.clearTimeout(id); }")]
        extern private static void ClearTimeout(int id);

        public IDisposable Schedule(Action action, TimeSpan dueTime)
        {
            var id = SetTimeout(action, dueTime.TotalMilliseconds);
            return Disposable.Create(() => ClearTimeout(id));
        }
    }
}