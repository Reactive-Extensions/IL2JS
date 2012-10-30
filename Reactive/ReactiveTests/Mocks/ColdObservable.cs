using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Concurrency;

using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Linq;

using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Disposables;

using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Collections.Generic;

namespace ReactiveTests.Mocks
{
    public class ColdObservable<T> : IObservable<T>
    {
        public readonly TestScheduler Scheduler;
        public readonly List<Subscription> Subscriptions = new List<Subscription>();
        public readonly Recorded<Notification<T>>[] Messages;

        public ColdObservable(TestScheduler scheduler, params Recorded<Notification<T>>[] messages)
        {
            Scheduler = scheduler;
            Messages = messages;
        }

        public virtual IDisposable Subscribe(IObserver<T> observer)
        {
            Subscriptions.Add(new Subscription(Scheduler.Ticks));
            var index = Subscriptions.Count - 1;

            var d = new CompositeDisposable();

            for (var i = 0; i < Messages.Length; ++i)
            {
                var notification = Messages[i].Value;
                d.Add(Scheduler.Schedule(() => notification.Accept(observer), Messages[i].Time));
            }

            return Disposable.Create(() =>
            {
                Subscriptions[index] = new Subscription(Subscriptions[index].Subscribe, Scheduler.Ticks);
                d.Dispose();
            });
        }
    }
}
