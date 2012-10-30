using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    public partial class ObservableExtensionsTest : Test
    {
        [TestMethod]
        public void Subscribe_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => ObservableExtensions.Subscribe<int>(default(IObservable<int>)));

            Throws<ArgumentNullException>(() => ObservableExtensions.Subscribe<int>(default(IObservable<int>), _ => { }));
            Throws<ArgumentNullException>(() => ObservableExtensions.Subscribe<int>(someObservable, default(Action<int>)));

            Throws<ArgumentNullException>(() => ObservableExtensions.Subscribe<int>(default(IObservable<int>), _ => { }, () => { }));
            Throws<ArgumentNullException>(() => ObservableExtensions.Subscribe<int>(someObservable, default(Action<int>), () => { }));
            Throws<ArgumentNullException>(() => ObservableExtensions.Subscribe<int>(someObservable, _ => { }, default(Action)));

            Throws<ArgumentNullException>(() => ObservableExtensions.Subscribe<int>(default(IObservable<int>), _ => { }, (Exception _) => { }));
            Throws<ArgumentNullException>(() => ObservableExtensions.Subscribe<int>(someObservable, default(Action<int>), (Exception _) => { }));
            Throws<ArgumentNullException>(() => ObservableExtensions.Subscribe<int>(someObservable, _ => { }, default(Action<Exception>)));

            Throws<ArgumentNullException>(() => ObservableExtensions.Subscribe<int>(default(IObservable<int>), _ => { }, (Exception _) => { }, () => { }));
            Throws<ArgumentNullException>(() => ObservableExtensions.Subscribe<int>(someObservable, default(Action<int>), (Exception _) => { }, () => { }));
            Throws<ArgumentNullException>(() => ObservableExtensions.Subscribe<int>(someObservable, _ => { }, default(Action<Exception>), () => { }));
            Throws<ArgumentNullException>(() => ObservableExtensions.Subscribe<int>(someObservable, _ => { }, (Exception _) => { }, default(Action)));
        }

        [TestMethod]
        public void Subscribe_None_Return()
        {
            Observable.Return(1, Scheduler.Immediate).Subscribe();
        }

        [TestMethod]
        public void Subscribe_None_Throw()
        {
            var ex = new Exception();
            var e = default(Exception);
            try
            {
                Observable.Throw<int>(ex, Scheduler.Immediate).Subscribe();
            }
            catch (Exception e_)
            {
                e = e_;
            }

            Assert.AreSame(ex, e);
        }

        [TestMethod]
        public void Subscribe_None_Empty()
        {
            Observable.Empty<int>(Scheduler.Immediate).Subscribe((int _) => { Assert.Fail(); });
        }

        [TestMethod]
        public void Subscribe_OnNext_Return()
        {
            int _x = -1;
            Observable.Return(42, Scheduler.Immediate).Subscribe((int x) => { _x = x; });
            Assert.AreEqual(42, _x);
        }

        [TestMethod]
        public void Subscribe_OnNext_Throw()
        {
            var ex = new Exception();
            var e = default(Exception);
            try
            {
                Observable.Throw<int>(ex, Scheduler.Immediate).Subscribe((int _) => { Assert.Fail(); });
            }
            catch (Exception e_)
            {
                e = e_;
            }

            Assert.AreSame(ex, e);
        }

        [TestMethod]
        public void Subscribe_OnNext_Empty()
        {
            Observable.Empty<int>(Scheduler.Immediate).Subscribe((int _) => { Assert.Fail(); });
        }

        [TestMethod]
        public void Subscribe_OnNextOnCompleted_Return()
        {
            bool finished = false;
            int _x = -1;
            Observable.Return(42, Scheduler.Immediate).Subscribe((int x) => { _x = x; }, () => { finished = true; });
            Assert.AreEqual(42, _x);
            Assert.IsTrue(finished);
        }

        [TestMethod]
        public void Subscribe_OnNextOnCompleted_Throw()
        {
            var ex = new Exception();
            var e = default(Exception);
            try
            {
                Observable.Throw<int>(ex, Scheduler.Immediate).Subscribe((int _) => { Assert.Fail(); }, () => { Assert.Fail(); });
            }
            catch (Exception e_)
            {
                e = e_;
            }

            Assert.AreSame(ex, e);
        }

        [TestMethod]
        public void Subscribe_OnNextOnCompleted_Empty()
        {
            bool finished = false;
            Observable.Empty<int>(Scheduler.Immediate).Subscribe((int _) => { Assert.Fail(); }, () => { finished = true; });
            Assert.IsTrue(finished);
        }
    }
}
