using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Collections.Generic;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Linq;

namespace ReactiveTests.Tests
{
    [TestClass]
    public partial class ObserverTest : Test
    {
        [TestMethod]
        public void ToObserver_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observer.ToObserver<int>(default(Action<Notification<int>>)));
        }

        [TestMethod]
        public void ToObserver_NotificationOnNext()
        {
            int i = 0;
            Action<Notification<int>> next = n =>
            {
                Assert.AreEqual(i++, 0);
                Assert.AreEqual(n.Kind, NotificationKind.OnNext);
                Assert.AreEqual(n.Value, 42);
                Assert.AreEqual(n.Exception, null);
                Assert.IsTrue(n.HasValue);
            };
            next.ToObserver().OnNext(42);
        }

        [TestMethod]
        public void ToObserver_NotificationOnError()
        {
            var ex = new Exception();
            int i = 0;
            Action<Notification<int>> next = n =>
            {
                Assert.AreEqual(i++, 0);
                Assert.AreEqual(n.Kind, NotificationKind.OnError);
                Assert.AreSame(n.Exception, ex);
                Assert.IsFalse(n.HasValue);
            };
            next.ToObserver().OnError(ex);
        }

        [TestMethod]
        public void ToObserver_NotificationOnCompleted()
        {
            var ex = new Exception();
            int i = 0;
            Action<Notification<int>> next = n =>
            {
                Assert.AreEqual(i++, 0);
                Assert.AreEqual(n.Kind, NotificationKind.OnCompleted);
                Assert.IsFalse(n.HasValue);
            };
            next.ToObserver().OnCompleted();
        }

        [TestMethod]
        public void ToNotifier_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observer.ToNotifier<int>(default(IObserver<int>)));
        }

        [TestMethod]
        public void ToNotifier_Forwards()
        {
            var obsn = new MyObserver();
            obsn.ToNotifier()(new Notification<int>.OnNext(42));
            Assert.AreEqual(obsn.HasOnNext, 42);

            var ex = new Exception();
            var obse = new MyObserver();
            obse.ToNotifier()(new Notification<int>.OnError(ex));
            Assert.AreSame(ex, obse.HasOnError);

            var obsc = new MyObserver();
            obsc.ToNotifier()(new Notification<int>.OnCompleted());
            Assert.IsTrue(obsc.HasOnCompleted);
        }

        [TestMethod]
        public void Create_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observer.Create<int>(default(Action<int>)));
            Throws<ArgumentNullException>(() => Observer.Create<int>(default(Action<int>), () => { }));
            Throws<ArgumentNullException>(() => Observer.Create<int>(_ => { }, default(Action)));
            Throws<ArgumentNullException>(() => Observer.Create<int>(default(Action<int>), (Exception _) => { }));
            Throws<ArgumentNullException>(() => Observer.Create<int>(_ => { }, default(Action<Exception>)));
            Throws<ArgumentNullException>(() => Observer.Create<int>(default(Action<int>), (Exception _) => { }, () => { }));
            Throws<ArgumentNullException>(() => Observer.Create<int>(_ => { }, default(Action<Exception>), () => { }));
            Throws<ArgumentNullException>(() => Observer.Create<int>(_ => { }, (Exception _) => { }, default(Action)));
        }

        [TestMethod]
        public void Create_OnNext()
        {
            bool next = false;
            var res = Observer.Create<int>(x => { Assert.AreEqual(42, x); next = true; });
            res.OnNext(42);
            Assert.IsTrue(next);
            res.OnCompleted();
        }

        [TestMethod]
        public void Create_OnNext_HasError()
        {
            var ex = new Exception();
            var e_ = default(Exception);

            bool next = false;
            var res = Observer.Create<int>(x => { Assert.AreEqual(42, x); next = true; });
            res.OnNext(42);
            Assert.IsTrue(next);

            try
            {
                res.OnError(ex);
                Assert.Fail();
            }
            catch (Exception e)
            {
                e_ = e;
            }
            Assert.AreSame(ex, e_);
        }

        [TestMethod]
        public void Create_OnNextOnCompleted()
        {
            bool next = false;
            bool completed = false;
            var res = Observer.Create<int>(x => { Assert.AreEqual(42, x); next = true; }, () => { completed = true; });
            res.OnNext(42);
            Assert.IsTrue(next);
            Assert.IsFalse(completed);
            res.OnCompleted();
            Assert.IsTrue(completed);
        }

        [TestMethod]
        public void Create_OnNextOnCompleted_HasError()
        {
            var ex = new Exception();
            var e_ = default(Exception);

            bool next = false;
            bool completed = false;
            var res = Observer.Create<int>(x => { Assert.AreEqual(42, x); next = true; }, () => { completed = true; });
            res.OnNext(42);
            Assert.IsTrue(next);
            Assert.IsFalse(completed);
            try
            {
                res.OnError(ex);
                Assert.Fail();
            }
            catch (Exception e)
            {
                e_ = e;
            }
            Assert.AreSame(ex, e_);
            Assert.IsFalse(completed);
        }

        [TestMethod]
        public void Create_OnNextOnError()
        {
            var ex = new Exception();
            bool next = true;
            bool error = false;
            var res = Observer.Create<int>(x => { Assert.AreEqual(42, x); next = true; }, e => { Assert.AreSame(ex, e); error = true; });
            res.OnNext(42);
            Assert.IsTrue(next);
            Assert.IsFalse(error);
            res.OnError(ex);
            Assert.IsTrue(error);
        }

        [TestMethod]
        public void Create_OnNextOnError_HitCompleted()
        {
            var ex = new Exception();
            bool next = true;
            bool error = false;
            var res = Observer.Create<int>(x => { Assert.AreEqual(42, x); next = true; }, e => { Assert.AreSame(ex, e); error = true; });
            res.OnNext(42);
            Assert.IsTrue(next);
            Assert.IsFalse(error);
            res.OnCompleted();
            Assert.IsFalse(error);
        }

        [TestMethod]
        public void Create_OnNextOnErrorOnCompleted1()
        {
            var ex = new Exception();
            bool next = true;
            bool error = false;
            bool completed = false;
            var res = Observer.Create<int>(x => { Assert.AreEqual(42, x); next = true; }, e => { Assert.AreSame(ex, e); error = true; }, () => { completed = true; });
            res.OnNext(42);
            Assert.IsTrue(next);
            Assert.IsFalse(error);
            Assert.IsFalse(completed);
            res.OnCompleted();
            Assert.IsTrue(completed);
            Assert.IsFalse(error);
        }

        [TestMethod]
        public void Create_OnNextOnErrorOnCompleted2()
        {
            var ex = new Exception();
            bool next = true;
            bool error = false;
            bool completed = false;
            var res = Observer.Create<int>(x => { Assert.AreEqual(42, x); next = true; }, e => { Assert.AreSame(ex, e); error = true; }, () => { completed = true; });
            res.OnNext(42);
            Assert.IsTrue(next);
            Assert.IsFalse(error);
            Assert.IsFalse(completed);
            res.OnError(ex);
            Assert.IsTrue(error);
            Assert.IsFalse(completed);
        }

        [TestMethod]
        public void AsObserver_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observer.AsObserver<int>(default(IObserver<int>)));
        }

        [TestMethod]
        public void AsObserver_Hides()
        {
            var obs = new MyObserver();
            var res = obs.AsObserver();

            Assert.IsFalse(object.ReferenceEquals(obs, res));
        }

        [TestMethod]
        public void AsObserver_Forwards()
        {
            var obsn = new MyObserver();
            obsn.AsObserver().OnNext(42);
            Assert.AreEqual(obsn.HasOnNext, 42);

            var ex = new Exception();
            var obse = new MyObserver();
            obse.AsObserver().OnError(ex);
            Assert.AreSame(ex, obse.HasOnError);

            var obsc = new MyObserver();
            obsc.AsObserver().OnCompleted();
            Assert.IsTrue(obsc.HasOnCompleted);
        }

        class MyObserver : IObserver<int>
        {
            public void OnNext(int value)
            {
                HasOnNext = value;
            }

            public void OnError(Exception exception)
            {
                HasOnError = exception;
            }

            public void OnCompleted()
            {
                HasOnCompleted = true;
            }

            public int HasOnNext { get; set; }
            public Exception HasOnError { get; set; }
            public bool HasOnCompleted { get; set; }
        }
    }
}
