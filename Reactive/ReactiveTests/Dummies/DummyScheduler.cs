using System;
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

namespace ReactiveTests.Dummies
{
    class DummyScheduler : IScheduler
    {
        public static readonly DummyScheduler Instance = new DummyScheduler();

        DummyScheduler()
        {
        }

        public IDisposable Schedule(Action action)
        {
            throw new NotImplementedException();
        }

        public IDisposable Schedule(Action action, TimeSpan dueTime)
        {
            throw new NotImplementedException();
        }

        public DateTimeOffset Now { get { throw new NotImplementedException(); } }
    }
}
