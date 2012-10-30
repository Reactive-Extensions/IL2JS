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
    class MockObservable<T> : IObservable<T>
    {
        Notification<T>[] notifications;

        public MockObservable(params Notification<T>[] notifications)
        {
            this.notifications = notifications;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            foreach (var n in notifications)
                n.Accept(observer);
            return Disposable.Empty;
        }
    }
}
