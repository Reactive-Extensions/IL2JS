using System;
using Microsoft.LiveLabs.Html;
using Reactive.Disposables;

namespace Reactive.Concurrency
{
    public class JavaScriptTimeoutScheduler : IScheduler
    {
        internal readonly static JavaScriptTimeoutScheduler Instance = new JavaScriptTimeoutScheduler();

        private DateTimeOffset? last;

        public DateTimeOffset Now
        {
            get
            {
#if true
                return DateTimeOffset.Now;
#endif
#if false
                var now = DateTimeOffset.Now;
                if (last.HasValue)
                {
                    if (now < last.Value)
                        throw new InvalidOperationException("time is attempting to run backwards");
                }
                last = now;
                return now;
#endif
#if false
                if (last.HasValue)
                    last += TimeSpan.FromMilliseconds(1.0);
                else
                    last = DateTimeOffset.Now;
                return last.Value;
#endif
            }
        }

        public IDisposable Schedule(Action action)
        {
            return Schedule(action, TimeSpan.Zero);
        }

        public IDisposable Schedule(Action action, TimeSpan dueTime)
        {
            var id = Browser.Window.SetTimeout(action, dueTime.TotalMilliseconds);
            return Disposable.Create(() => Browser.Window.ClearTimeout(id));
        }
    }
}
