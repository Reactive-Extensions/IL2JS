using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    public class MockObserver<T> : List<Recorded<Notification<T>>>, IObserver<T>
    {
        TestScheduler scheduler;

        public MockObserver(TestScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        public void OnNext(T value)
        {
            Add(new Recorded<Notification<T>>(scheduler.Ticks, new Notification<T>.OnNext(value)));
        }

        public void OnError(Exception exception)
        {
            Add(new Recorded<Notification<T>>(scheduler.Ticks, new Notification<T>.OnError(exception)));
        }

        public void OnCompleted()
        {
            Add(new Recorded<Notification<T>>(scheduler.Ticks, new Notification<T>.OnCompleted()));
        }
    }
}
