using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReactiveTests.Mocks;

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

using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive;

namespace ReactiveTests
{
    public static class TestSchedulerExtensions
    {
        public static IEnumerable<Recorded<Notification<T>>> Run<T>(this TestScheduler scheduler, Func<IObservable<T>> create, ushort created, ushort subscribed, ushort disposed)
        {
            var source = default(IObservable<T>);
            var subscription = default(IDisposable);
            var observer = new MockObserver<T>(scheduler);

            scheduler.Schedule(() => source = create(), created);
            scheduler.Schedule(() => subscription = source.Subscribe(observer), subscribed);
            scheduler.Schedule(() => subscription.Dispose(), disposed);

            scheduler.Run();

            return observer;
        }

        public static IEnumerable<Recorded<Notification<T>>> Run<T>(this TestScheduler scheduler, Func<IObservable<T>> create, ushort disposed)
        {
            return scheduler.Run(create, ObservableTest.Created, ObservableTest.Subscribed, disposed);
        }

        public static IEnumerable<Recorded<Notification<T>>> Run<T>(this TestScheduler scheduler, Func<IObservable<T>> create)
        {
            return scheduler.Run(create, ObservableTest.Created, ObservableTest.Subscribed, ObservableTest.Disposed);
        }

        public static HotObservable<T> CreateHotObservable<T>(this TestScheduler scheduler, params Recorded<Notification<T>>[] messages)
        {
            return new HotObservable<T>(scheduler, messages);
        }

        public static ColdObservable<T> CreateColdObservable<T>(this TestScheduler scheduler, params Recorded<Notification<T>>[] messages)
        {
            return new ColdObservable<T>(scheduler, messages);
        }
    }
}
