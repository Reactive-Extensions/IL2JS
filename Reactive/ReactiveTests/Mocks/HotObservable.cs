using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    public class HotObservable<T> : IObservable<T>
    {
        public readonly TestScheduler Scheduler;
        public readonly List<IObserver<T>> Observers = new List<IObserver<T>>();
        public readonly List<Subscription> Subscriptions = new List<Subscription>();
        public readonly Recorded<Notification<T>>[] Messages;

        public HotObservable(TestScheduler scheduler, params Recorded<Notification<T>>[] messages)
        {
            Scheduler = scheduler;
            Messages = messages;

            for (var i = 0; i < messages.Length; ++i)
            {
                var notification = messages[i].Value;
                scheduler.Schedule(() =>
                {
                    var _observers = Observers.ToArray();
                    for (var j = 0; j < _observers.Length; ++j)
                    {
                        notification.Accept(_observers[j]);
                    }

                }, messages[i].Time);
            }
        }

        public virtual IDisposable Subscribe(IObserver<T> observer)
        {
            Observers.Add(observer);
            Subscriptions.Add(new Subscription(Scheduler.Ticks));
            var index = Subscriptions.Count - 1;

            return Disposable.Create(() =>
            {
                Observers.Remove(observer);
                Subscriptions[index] = new Subscription(Subscriptions[index].Subscribe, Scheduler.Ticks);
            });
        }
    }
}
