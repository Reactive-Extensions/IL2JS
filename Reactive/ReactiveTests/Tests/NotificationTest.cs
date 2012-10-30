using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

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

namespace ReactiveTests.Tests
{
    [TestClass]
    public partial class NotificationTest : Test
    {
        [TestMethod]
        public void ToObservable_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Notification.ToObservable<int>(null));
            Throws<ArgumentNullException>(() => Notification.ToObservable<int>(null, new TestScheduler()));
            Throws<ArgumentNullException>(() => Notification.ToObservable<int>(new Notification<int>.OnNext(1), null));
        }

        [TestMethod]
        public void ToObservable_Empty()
        {
            var scheduler = new TestScheduler();

            var res = scheduler.Run(() => new Notification<int>.OnCompleted().ToObservable(scheduler)).ToArray();
            res.AssertEqual(
                OnCompleted<int>(201)
            );
        }

        [TestMethod]
        public void ToObservable_Return()
        {
            var scheduler = new TestScheduler();

            var res = scheduler.Run(() => new Notification<int>.OnNext(42).ToObservable(scheduler)).ToArray();
            res.AssertEqual(
                OnNext<int>(201, 42),
                OnCompleted<int>(201)
            );
        }

        [TestMethod]
        public void ToObservable_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var res = scheduler.Run(() => new Notification<int>.OnError(ex).ToObservable(scheduler)).ToArray();
            res.AssertEqual(
                OnError<int>(201, ex)
            );
        }

        [TestMethod]
        public void ToObservable_CurrentThread()
        {
            var evt = new ManualResetEvent(false);

            new Notification<int>.OnCompleted().ToObservable().Subscribe(_ => {}, () =>
            {
                evt.Set();
            });

            evt.WaitOne();
        }
    }
}
