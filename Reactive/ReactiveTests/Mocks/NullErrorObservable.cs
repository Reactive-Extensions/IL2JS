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
    class NullErrorObservable<T> : IObservable<T>
    {
        public static NullErrorObservable<T> Instance = new NullErrorObservable<T>();

        private NullErrorObservable()
        {
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            observer.OnError(null);
            return Disposable.Empty;
        }
    }
}
