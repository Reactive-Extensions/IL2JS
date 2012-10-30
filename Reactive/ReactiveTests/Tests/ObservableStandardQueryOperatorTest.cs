using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReactiveTests.Mocks;
using ReactiveTests.Dummies;


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
    public class ObservableStandardQueryOperatorTest : Test
    {
        [TestMethod]
        public void Select_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => ((IObservable<int>)null).Select<int, int>(DummyFunc<int, int>.Instance));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.Select<int, int>((Func<int, int>)null));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.Select<int, int>(DummyFunc<int, int>.Instance).Subscribe(null));
            Throws<ArgumentNullException>(() => NullErrorObservable<int>.Instance.Select<int, int>(DummyFunc<int, int>.Instance).Subscribe(DummyObserver<int>.Instance));
        }

        [TestMethod]
        public void Select_Throws()
        {
            Throws<InvalidOperationException>(() =>
                new MockObservable<int>(new Notification<int>.OnNext(1)).Select<int, int>(x => x).Subscribe(
                 x =>
                 {
                     throw new InvalidOperationException();
                 }));
            Throws<InvalidOperationException>(() =>
                new MockObservable<int>(new Notification<int>.OnError(new Exception())).Select<int, int>(x => x).Subscribe(
                 x => { },
                 exception =>
                 {
                     throw new InvalidOperationException();
                 }));
            Throws<InvalidOperationException>(() =>
                new MockObservable<int>(new Notification<int>.OnCompleted()).Select<int, int>(x => x).Subscribe(
                 x => { },
                 exception => { },
                 () =>
                 {
                     throw new InvalidOperationException();
                 }));
            Throws<InvalidOperationException>(() => new SubscribeThrowsObservable<int>().Select(x => x).Subscribe());
        }

        [TestMethod]
        public void Select_DisposeInsideSelector()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(100, 1),
                OnNext(200, 2),
                OnNext(500, 3),
                OnNext(600, 4)
                );

            var invoked = 0;

            var results = new MockObserver<int>(scheduler);

            var d = new MutableDisposable();
            d.Disposable = xs.Select(x =>
            {
                invoked++;
                if (scheduler.Ticks > 400)
                    d.Dispose();
                return x;
            }).Subscribe(results);

            scheduler.Schedule(d.Dispose, Disposed);

            scheduler.Run();

            results.AssertEqual(
                OnNext(100, 1),
                OnNext(200, 2)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(0, 500)
                );

            Assert.AreEqual(3, invoked);
        }

        [TestMethod]
        public void Select_Completed()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(180, 1),
                OnNext(210, 2),
                OnNext(240, 3),
                OnNext(290, 4),
                OnNext(350, 5),
                OnCompleted<int>(400),
                OnNext(410, -1),
                OnCompleted<int>(420),
                OnError<int>(430, new MockException(-1)));

            var results = scheduler.Run(() => xs.Select(x =>
            {
                invoked++;
                return x + 1;
            }));

            results.AssertEqual(
                OnNext(210, 3),
                OnNext(240, 4),
                OnNext(290, 5),
                OnNext(350, 6),
                OnCompleted<int>(400));

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 400));

            Assert.AreEqual(4, invoked);
        }

        [TestMethod]
        public void Select_NotCompleted()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(180, 1),
                OnNext(210, 2),
                OnNext(240, 3),
                OnNext(290, 4),
                OnNext(350, 5));

            var results = scheduler.Run(() => xs.Select(x =>
            {
                invoked++;
                return x + 1;
            }));

            results.AssertEqual(
                OnNext(210, 3),
                OnNext(240, 4),
                OnNext(290, 5),
                OnNext(350, 6));

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 1000));

            Assert.AreEqual(4, invoked);
        }

        [TestMethod]
        public void Select_Error()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(180, 1),
                OnNext(210, 2),
                OnNext(240, 3),
                OnNext(290, 4),
                OnNext(350, 5),
                OnError<int>(400, new MockException(6)),
                OnNext(410, -1),
                OnCompleted<int>(420),
                OnError<int>(430, new MockException(-1)));

            var results = scheduler.Run(() => xs.Select(x =>
            {
                invoked++;
                return x + 1;
            }));

            results.AssertEqual(
                OnNext(210, 3),
                OnNext(240, 4),
                OnNext(290, 5),
                OnNext(350, 6),
                OnError<int>(400, new MockException(6)));

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 400));

            Assert.AreEqual(4, invoked);
        }

        [TestMethod]
        public void Select_SelectorThrows()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(180, 1),
                OnNext(210, 2),
                OnNext(240, 3),
                OnNext(290, 4),
                OnNext(350, 5),
                OnCompleted<int>(400),
                OnNext(410, -1),
                OnCompleted<int>(420),
                OnError<int>(430, new MockException(-1)));

            var results = scheduler.Run(() => xs.Select(x =>
            {
                invoked++;
                if (invoked == 3)
                    throw new MockException(x);
                return x + 1;
            }));

            results.AssertEqual(
                OnNext(210, 3),
                OnNext(240, 4),
                OnError<int>(290, new MockException(4)));

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 290));

            Assert.AreEqual(3, invoked);
        }

        [TestMethod]
        public void SelectWithIndex_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => ((IObservable<int>)null).Select<int, int>(DummyFunc<int, int, int>.Instance));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.Select<int, int>((Func<int, int, int>)null));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.Select<int, int>(DummyFunc<int, int, int>.Instance).Subscribe(null));
            Throws<ArgumentNullException>(() => NullErrorObservable<int>.Instance.Select<int, int>(DummyFunc<int, int, int>.Instance).Subscribe(DummyObserver<int>.Instance));
        }

        [TestMethod]
        public void SelectWithIndex_Throws()
        {
            Throws<InvalidOperationException>(() =>
                new MockObservable<int>(new Notification<int>.OnNext(1)).Select<int, int>((x, index) => x).Subscribe(
                 x =>
                 {
                     throw new InvalidOperationException();
                 }));
            Throws<InvalidOperationException>(() =>
                new MockObservable<int>(new Notification<int>.OnError(new Exception())).Select<int, int>((x, index) => x).Subscribe(
                 x => { },
                 exception =>
                 {
                     throw new InvalidOperationException();
                 }));
            Throws<InvalidOperationException>(() =>
                new MockObservable<int>(new Notification<int>.OnCompleted()).Select<int, int>((x, index) => x).Subscribe(
                 x => { },
                 exception => { },
                 () =>
                 {
                     throw new InvalidOperationException();
                 }));
            Throws<InvalidOperationException>(() => new SubscribeThrowsObservable<int>().Select((x, index) => x).Subscribe());
        }

        [TestMethod]
        public void SelectWithIndex_DisposeInsideSelector()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(100, 4),
                OnNext(200, 3),
                OnNext(500, 2),
                OnNext(600, 1)
                );

            var invoked = 0;

            var results = new MockObserver<int>(scheduler);

            var d = new MutableDisposable();
            d.Disposable = xs.Select((x, index) =>
            {
                invoked++;
                if (scheduler.Ticks > 400)
                    d.Dispose();
                return x + index * 10;
            }).Subscribe(results);

            scheduler.Schedule(d.Dispose, Disposed);

            scheduler.Run();

            results.AssertEqual(
                OnNext(100, 4),
                OnNext(200, 13)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(0, 500)
                );

            Assert.AreEqual(3, invoked);
        }

        [TestMethod]
        public void SelectWithIndex_Completed()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(180, 5),
                OnNext(210, 4),
                OnNext(240, 3),
                OnNext(290, 2),
                OnNext(350, 1),
                OnCompleted<int>(400),
                OnNext(410, -1),
                OnCompleted<int>(420),
                OnError<int>(430, new MockException(-1)));

            var results = scheduler.Run(() => xs.Select((x, index) =>
            {
                invoked++;
                return (x + 1) + (index * 10);
            }));

            results.AssertEqual(
                OnNext(210, 5),
                OnNext(240, 14),
                OnNext(290, 23),
                OnNext(350, 32),
                OnCompleted<int>(400));

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 400));

            Assert.AreEqual(4, invoked);
        }

        [TestMethod]
        public void SelectWithIndex_NotCompleted()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(180, 5),
                OnNext(210, 4),
                OnNext(240, 3),
                OnNext(290, 2),
                OnNext(350, 1));

            var results = scheduler.Run(() => xs.Select((x, index) =>
            {
                invoked++;
                return (x + 1) + (index * 10);
            }));

            results.AssertEqual(
                OnNext(210, 5),
                OnNext(240, 14),
                OnNext(290, 23),
                OnNext(350, 32));

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 1000));

            Assert.AreEqual(4, invoked);
        }

        [TestMethod]
        public void SelectWithIndex_Error()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(180, 5),
                OnNext(210, 4),
                OnNext(240, 3),
                OnNext(290, 2),
                OnNext(350, 1),
                OnError<int>(400, new MockException(6)),
                OnNext(410, -1),
                OnCompleted<int>(420),
                OnError<int>(430, new MockException(-1)));

            var results = scheduler.Run(() => xs.Select((x, index) =>
            {
                invoked++;
                return (x + 1) + (index * 10);
            }));

            results.AssertEqual(
                OnNext(210, 5),
                OnNext(240, 14),
                OnNext(290, 23),
                OnNext(350, 32),
                OnError<int>(400, new MockException(6)));

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 400));

            Assert.AreEqual(4, invoked);
        }

        [TestMethod]
        public void SelectWithIndex_SelectorThrows()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(180, 5),
                OnNext(210, 4),
                OnNext(240, 3),
                OnNext(290, 2),
                OnNext(350, 1),
                OnCompleted<int>(400),
                OnNext(410, -1),
                OnCompleted<int>(420),
                OnError<int>(430, new MockException(-1)));

            var results = scheduler.Run(() => xs.Select((x, index) =>
            {
                invoked++;
                if (invoked == 3)
                    throw new MockException(x);
                return (x + 1) + (index * 10);
            }));

            results.AssertEqual(
                OnNext(210, 5),
                OnNext(240, 14),
                OnError<int>(290, new MockException(2)));

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 290));

            Assert.AreEqual(3, invoked);
        }

        [TestMethod]
        public void Where_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => ((IObservable<int>)null).Where<int>(DummyFunc<int, bool>.Instance));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.Where<int>((Func<int, bool>)null));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.Where<int>(DummyFunc<int, bool>.Instance).Subscribe(null));
            Throws<ArgumentNullException>(() => NullErrorObservable<int>.Instance.Where<int>(DummyFunc<int, bool>.Instance).Subscribe(DummyObserver<int>.Instance));
        }

        static bool IsPrime(int i)
        {
            if (i <= 1)
                return false;

            var max = (int)Math.Sqrt(i);
            for (var j = 2; j <= max; ++j)
                if (i % j == 0)
                    return false;

            return true;
        }

        [TestMethod]
        public void Where_Complete()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 1),
                OnNext(180, 2),
                OnNext(230, 3),
                OnNext(270, 4),
                OnNext(340, 5),
                OnNext(380, 6),
                OnNext(390, 7),
                OnNext(450, 8),
                OnNext(470, 9),
                OnNext(560, 10),
                OnNext(580, 11),
                OnCompleted<int>(600),
                OnNext(610, 12),
                OnError<int>(620, new MockException(1)),
                OnCompleted<int>(630)
                );

            var results = scheduler.Run(() => xs.Where(x =>
                {
                    invoked++;
                    return IsPrime(x);
                }));

            results.AssertEqual(
                OnNext(230, 3),
                OnNext(340, 5),
                OnNext(390, 7),
                OnNext(580, 11),
                OnCompleted<int>(600)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );

            Assert.AreEqual(9, invoked);
        }

        [TestMethod]
        public void Where_True()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 1),
                OnNext(180, 2),
                OnNext(230, 3),
                OnNext(270, 4),
                OnNext(340, 5),
                OnNext(380, 6),
                OnNext(390, 7),
                OnNext(450, 8),
                OnNext(470, 9),
                OnNext(560, 10),
                OnNext(580, 11),
                OnCompleted<int>(600)
                );

            var results = scheduler.Run(() => xs.Where(x =>
            {
                invoked++;
                return true;
            }));

            results.AssertEqual(
                OnNext(230, 3),
                OnNext(270, 4),
                OnNext(340, 5),
                OnNext(380, 6),
                OnNext(390, 7),
                OnNext(450, 8),
                OnNext(470, 9),
                OnNext(560, 10),
                OnNext(580, 11),
                OnCompleted<int>(600)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );

            Assert.AreEqual(9, invoked);
        }

        [TestMethod]
        public void Where_False()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 1),
                OnNext(180, 2),
                OnNext(230, 3),
                OnNext(270, 4),
                OnNext(340, 5),
                OnNext(380, 6),
                OnNext(390, 7),
                OnNext(450, 8),
                OnNext(470, 9),
                OnNext(560, 10),
                OnNext(580, 11),
                OnCompleted<int>(600)
                );

            var results = scheduler.Run(() => xs.Where(x =>
            {
                invoked++;
                return false;
            }));

            results.AssertEqual(
                OnCompleted<int>(600)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );

            Assert.AreEqual(9, invoked);
        }

        [TestMethod]
        public void Where_Dispose()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 1),
                OnNext(180, 2),
                OnNext(230, 3),
                OnNext(270, 4),
                OnNext(340, 5),
                OnNext(380, 6),
                OnNext(390, 7),
                OnNext(450, 8),
                OnNext(470, 9),
                OnNext(560, 10),
                OnNext(580, 11),
                OnCompleted<int>(600)
                );

            var results = scheduler.Run(() => xs.Where(x =>
            {
                invoked++;
                return IsPrime(x);
            }), 400);

            results.AssertEqual(
                OnNext(230, 3),
                OnNext(340, 5),
                OnNext(390, 7)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 400)
                );

            Assert.AreEqual(5, invoked);
        }

        [TestMethod]
        public void Where_Error()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 1),
                OnNext(180, 2),
                OnNext(230, 3),
                OnNext(270, 4),
                OnNext(340, 5),
                OnNext(380, 6),
                OnNext(390, 7),
                OnNext(450, 8),
                OnNext(470, 9),
                OnNext(560, 10),
                OnNext(580, 11),
                OnError<int>(600, new MockException(1)),
                OnNext(610, 12),
                OnError<int>(620, new MockException(1)),
                OnCompleted<int>(630)
                );

            var results = scheduler.Run(() => xs.Where(x =>
            {
                invoked++;
                return IsPrime(x);
            }));

            results.AssertEqual(
                OnNext(230, 3),
                OnNext(340, 5),
                OnNext(390, 7),
                OnNext(580, 11),
                OnError<int>(600, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );

            Assert.AreEqual(9, invoked);
        }

        [TestMethod]
        public void Where_Throw()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 1),
                OnNext(180, 2),
                OnNext(230, 3),
                OnNext(270, 4),
                OnNext(340, 5),
                OnNext(380, 6),
                OnNext(390, 7),
                OnNext(450, 8),
                OnNext(470, 9),
                OnNext(560, 10),
                OnNext(580, 11),
                OnCompleted<int>(600),
                OnNext(610, 12),
                OnError<int>(620, new MockException(1)),
                OnCompleted<int>(630)
                );

            var results = scheduler.Run(() => xs.Where(x =>
            {
                invoked++;
                if (x > 5)
                    throw new MockException(x);
                return IsPrime(x);
            }));

            results.AssertEqual(
                OnNext(230, 3),
                OnNext(340, 5),
                OnError<int>(380, new MockException(6))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 380)
                );

            Assert.AreEqual(4, invoked);
        }

        [TestMethod]
        public void Where_DisposeInPredicate()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 1),
                OnNext(180, 2),
                OnNext(230, 3),
                OnNext(270, 4),
                OnNext(340, 5),
                OnNext(380, 6),
                OnNext(390, 7),
                OnNext(450, 8),
                OnNext(470, 9),
                OnNext(560, 10),
                OnNext(580, 11),
                OnCompleted<int>(600),
                OnNext(610, 12),
                OnError<int>(620, new MockException(1)),
                OnCompleted<int>(630)
                );

            var results = new MockObserver<int>(scheduler);

            var d = new MutableDisposable();
            var ys = default(IObservable<int>);


            scheduler.Schedule(() => ys = xs.Where(x =>
                {
                    invoked++;
                    if (x == 8)
                        d.Dispose();
                    return IsPrime(x);
                }), Created);

            scheduler.Schedule(() => d.Disposable = ys.Subscribe(results), Subscribed);

            scheduler.Schedule(() => d.Dispose(), Disposed);

            scheduler.Run();

            results.AssertEqual(
                OnNext(230, 3),
                OnNext(340, 5),
                OnNext(390, 7)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 450)
                );

            Assert.AreEqual(6, invoked);

        }


        [TestMethod]
        public void WhereIndex_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => ((IObservable<int>)null).Where<int>(DummyFunc<int, int, bool>.Instance));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.Where<int>((Func<int, int, bool>)null));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.Where<int>(DummyFunc<int, int, bool>.Instance).Subscribe(null));
            Throws<ArgumentNullException>(() => NullErrorObservable<int>.Instance.Where<int>(DummyFunc<int, int, bool>.Instance).Subscribe(DummyObserver<int>.Instance));
        }

        [TestMethod]
        public void WhereIndex_Complete()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 1),
                OnNext(180, 2),
                OnNext(230, 3),
                OnNext(270, 4),
                OnNext(340, 5),
                OnNext(380, 6),
                OnNext(390, 7),
                OnNext(450, 8),
                OnNext(470, 9),
                OnNext(560, 10),
                OnNext(580, 11),
                OnCompleted<int>(600),
                OnNext(610, 12),
                OnError<int>(620, new MockException(1)),
                OnCompleted<int>(630)
                );

            var results = scheduler.Run(() => xs.Where((x, i) =>
            {
                invoked++;
                return IsPrime(x + i * 10);
            }));

            results.AssertEqual(
                OnNext(230, 3),
                OnNext(390, 7),
                OnCompleted<int>(600)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );

            Assert.AreEqual(9, invoked);
        }

        [TestMethod]
        public void WhereIndex_True()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 1),
                OnNext(180, 2),
                OnNext(230, 3),
                OnNext(270, 4),
                OnNext(340, 5),
                OnNext(380, 6),
                OnNext(390, 7),
                OnNext(450, 8),
                OnNext(470, 9),
                OnNext(560, 10),
                OnNext(580, 11),
                OnCompleted<int>(600)
                );

            var results = scheduler.Run(() => xs.Where((x, i) =>
            {
                invoked++;
                return true;
            }));

            results.AssertEqual(
                OnNext(230, 3),
                OnNext(270, 4),
                OnNext(340, 5),
                OnNext(380, 6),
                OnNext(390, 7),
                OnNext(450, 8),
                OnNext(470, 9),
                OnNext(560, 10),
                OnNext(580, 11),
                OnCompleted<int>(600)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );

            Assert.AreEqual(9, invoked);
        }

        [TestMethod]
        public void WhereIndex_False()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 1),
                OnNext(180, 2),
                OnNext(230, 3),
                OnNext(270, 4),
                OnNext(340, 5),
                OnNext(380, 6),
                OnNext(390, 7),
                OnNext(450, 8),
                OnNext(470, 9),
                OnNext(560, 10),
                OnNext(580, 11),
                OnCompleted<int>(600)
                );

            var results = scheduler.Run(() => xs.Where((x, i) =>
            {
                invoked++;
                return false;
            }));

            results.AssertEqual(
                OnCompleted<int>(600)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );

            Assert.AreEqual(9, invoked);
        }

        [TestMethod]
        public void WhereIndex_Dispose()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 1),
                OnNext(180, 2),
                OnNext(230, 3),
                OnNext(270, 4),
                OnNext(340, 5),
                OnNext(380, 6),
                OnNext(390, 7),
                OnNext(450, 8),
                OnNext(470, 9),
                OnNext(560, 10),
                OnNext(580, 11),
                OnCompleted<int>(600)
                );

            var results = scheduler.Run(() => xs.Where((x, i) =>
            {
                invoked++;
                return IsPrime(x + i * 10);
            }), 400);

            results.AssertEqual(
                OnNext(230, 3),
                OnNext(390, 7)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 400)
                );

            Assert.AreEqual(5, invoked);
        }

        [TestMethod]
        public void WhereIndex_Error()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 1),
                OnNext(180, 2),
                OnNext(230, 3),
                OnNext(270, 4),
                OnNext(340, 5),
                OnNext(380, 6),
                OnNext(390, 7),
                OnNext(450, 8),
                OnNext(470, 9),
                OnNext(560, 10),
                OnNext(580, 11),
                OnError<int>(600, new MockException(1)),
                OnNext(610, 12),
                OnError<int>(620, new MockException(1)),
                OnCompleted<int>(630)
                );

            var results = scheduler.Run(() => xs.Where((x, i) =>
            {
                invoked++;
                return IsPrime(x + i * 10);
            }));

            results.AssertEqual(
                OnNext(230, 3),
                OnNext(390, 7),
                OnError<int>(600, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );

            Assert.AreEqual(9, invoked);
        }

        [TestMethod]
        public void WhereIndex_Throw()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 1),
                OnNext(180, 2),
                OnNext(230, 3),
                OnNext(270, 4),
                OnNext(340, 5),
                OnNext(380, 6),
                OnNext(390, 7),
                OnNext(450, 8),
                OnNext(470, 9),
                OnNext(560, 10),
                OnNext(580, 11),
                OnCompleted<int>(600),
                OnNext(610, 12),
                OnError<int>(620, new MockException(1)),
                OnCompleted<int>(630)
                );

            var results = scheduler.Run(() => xs.Where((x, i) =>
            {
                invoked++;
                if (x > 5)
                    throw new MockException(x);
                return IsPrime(x + i * 10);
            }));

            results.AssertEqual(
                OnNext(230, 3),
                OnError<int>(380, new MockException(6))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 380)
                );

            Assert.AreEqual(4, invoked);
        }

        [TestMethod]
        public void WhereIndex_DisposeInPredicate()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 1),
                OnNext(180, 2),
                OnNext(230, 3),
                OnNext(270, 4),
                OnNext(340, 5),
                OnNext(380, 6),
                OnNext(390, 7),
                OnNext(450, 8),
                OnNext(470, 9),
                OnNext(560, 10),
                OnNext(580, 11),
                OnCompleted<int>(600),
                OnNext(610, 12),
                OnError<int>(620, new MockException(1)),
                OnCompleted<int>(630)
                );

            var results = new MockObserver<int>(scheduler);

            var d = new MutableDisposable();
            var ys = default(IObservable<int>);


            scheduler.Schedule(() => ys = xs.Where((x, i) =>
            {
                invoked++;
                if (x == 8)
                    d.Dispose();
                return IsPrime(x + i * 10);
            }), Created);

            scheduler.Schedule(() => d.Disposable = ys.Subscribe(results), Subscribed);

            scheduler.Schedule(() => d.Dispose(), Disposed);

            scheduler.Run();

            results.AssertEqual(
                OnNext(230, 3),
                OnNext(390, 7)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 450)
                );

            Assert.AreEqual(6, invoked);

        }

        class GroupByComparer : IEqualityComparer<string>
        {
            TestScheduler scheduler;
            int equalsThrowsAfter;
            ushort getHashCodeThrowsAfter;

            public GroupByComparer(TestScheduler scheduler, ushort equalsThrowsAfter, ushort getHashCodeThrowsAfter)
            {
                this.scheduler = scheduler;
                this.equalsThrowsAfter = equalsThrowsAfter;
                this.getHashCodeThrowsAfter = getHashCodeThrowsAfter;
            }

            public GroupByComparer(TestScheduler scheduler)
                : this(scheduler, ushort.MaxValue, ushort.MaxValue)
            {
            }

            public bool Equals(string x, string y)
            {
                if (scheduler.Ticks > equalsThrowsAfter)
                    throw new MockException(1111);

                return x.Equals(y, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(string obj)
            {
                if (scheduler.Ticks > getHashCodeThrowsAfter)
                    throw new MockException(999);

                return StringComparer.OrdinalIgnoreCase.GetHashCode(obj);
            }
        }

        static string Reverse(string s)
        {
            var sb = new StringBuilder();

            for (var i = s.Length - 1; i >= 0; i--)
                sb.Append(s[i]);

            return sb.ToString();
        }


        [TestMethod]
        public void GroupBy_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => ((IObservable<int>)null).GroupBy(DummyFunc<int, int>.Instance, DummyFunc<int, int>.Instance, EqualityComparer<int>.Default));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.GroupBy((Func<int, int>)null, DummyFunc<int, int>.Instance, EqualityComparer<int>.Default));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.GroupBy(DummyFunc<int, int>.Instance, (Func<int, int>)null, EqualityComparer<int>.Default));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.GroupBy(DummyFunc<int, int>.Instance, DummyFunc<int, int>.Instance, null));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.GroupBy(DummyFunc<int, int>.Instance, DummyFunc<int, int>.Instance, EqualityComparer<int>.Default).Subscribe(null));
            Throws<ArgumentNullException>(() => NullErrorObservable<int>.Instance.GroupBy(DummyFunc<int, int>.Instance, DummyFunc<int, int>.Instance, EqualityComparer<int>.Default).Subscribe());
        }

        [TestMethod]
        public void GroupBy_KeyEle_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => ((IObservable<int>)null).GroupBy(DummyFunc<int, int>.Instance, DummyFunc<int, int>.Instance));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.GroupBy((Func<int, int>)null, DummyFunc<int, int>.Instance));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.GroupBy(DummyFunc<int, int>.Instance, (Func<int, int>)null));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.GroupBy(DummyFunc<int, int>.Instance, DummyFunc<int, int>.Instance).Subscribe(null));
            Throws<ArgumentNullException>(() => NullErrorObservable<int>.Instance.GroupBy(DummyFunc<int, int>.Instance, DummyFunc<int, int>.Instance).Subscribe());
        }

        [TestMethod]
        public void GroupBy_KeyComparer_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => ((IObservable<int>)null).GroupBy(DummyFunc<int, int>.Instance, EqualityComparer<int>.Default));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.GroupBy((Func<int, int>)null, EqualityComparer<int>.Default));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.GroupBy(DummyFunc<int, int>.Instance, (IEqualityComparer<int>)null));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.GroupBy(DummyFunc<int, int>.Instance, EqualityComparer<int>.Default).Subscribe(null));
            Throws<ArgumentNullException>(() => NullErrorObservable<int>.Instance.GroupBy(DummyFunc<int, int>.Instance, EqualityComparer<int>.Default).Subscribe());
        }

        [TestMethod]
        public void GroupBy_Key_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => ((IObservable<int>)null).GroupBy(DummyFunc<int, int>.Instance));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.GroupBy((Func<int, int>)null));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.GroupBy(DummyFunc<int, int>.Instance).Subscribe(null));
            Throws<ArgumentNullException>(() => NullErrorObservable<int>.Instance.GroupBy(DummyFunc<int, int>.Instance).Subscribe());
        }

        [TestMethod]
        public void GroupBy_WithKeyComparer()
        {
            var scheduler = new TestScheduler();

            var keyInvoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(90, "error"),
                OnNext(110, "error"),
                OnNext(130, "error"),
                OnNext(220, "  foo"),
                OnNext(240, " FoO "),
                OnNext(270, "baR  "),
                OnNext(310, "foO "),
                OnNext(350, " Baz   "),
                OnNext(360, "  qux "),
                OnNext(390, "   bar"),
                OnNext(420, " BAR  "),
                OnNext(470, "FOO "),
                OnNext(480, "baz  "),
                OnNext(510, " bAZ "),
                OnNext(530, "    fOo    "),
                OnCompleted<string>(570),
                OnNext(580, "error"),
                OnCompleted<string>(600),
                OnError<string>(650, new MockException(1))
                );

            var comparer = new GroupByComparer(scheduler);

            var results = scheduler.Run(() => xs.GroupBy(x =>
            {
                keyInvoked++;
                return x.Trim();
            }, comparer).Select(g => g.Key));

            results.AssertEqual(
                OnNext(220, "foo"),
                OnNext(270, "baR"),
                OnNext(350, "Baz"),
                OnNext(360, "qux"),
                OnCompleted<string>(570)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 570)
                );

            Assert.AreEqual(12, keyInvoked);
        }

        [TestMethod]
        public void GroupBy_Outer_Complete()
        {
            var scheduler = new TestScheduler();

            var keyInvoked = 0;
            var eleInvoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(90, "error"),
                OnNext(110, "error"),
                OnNext(130, "error"),
                OnNext(220, "  foo"),
                OnNext(240, " FoO "),
                OnNext(270, "baR  "),
                OnNext(310, "foO "),
                OnNext(350, " Baz   "),
                OnNext(360, "  qux "),
                OnNext(390, "   bar"),
                OnNext(420, " BAR  "),
                OnNext(470, "FOO "),
                OnNext(480, "baz  "),
                OnNext(510, " bAZ "),
                OnNext(530, "    fOo    "),
                OnCompleted<string>(570),
                OnNext(580, "error"),
                OnCompleted<string>(600),
                OnError<string>(650, new MockException(1))
                );

            var comparer = new GroupByComparer(scheduler);

            var results = scheduler.Run(() => xs.GroupBy(x =>
                {
                    keyInvoked++;
                    return x.Trim();
                }, x =>
                {
                    eleInvoked++;
                    return Reverse(x);
                }, comparer).Select(g => g.Key));

            results.AssertEqual(
                OnNext(220, "foo"),
                OnNext(270, "baR"),
                OnNext(350, "Baz"),
                OnNext(360, "qux"),
                OnCompleted<string>(570)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 570)
                );

            Assert.AreEqual(12, keyInvoked);
            Assert.AreEqual(12, eleInvoked);
        }

        [TestMethod]
        public void GroupBy_Outer_Error()
        {
            var scheduler = new TestScheduler();

            var keyInvoked = 0;
            var eleInvoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(90, "error"),
                OnNext(110, "error"),
                OnNext(130, "error"),
                OnNext(220, "  foo"),
                OnNext(240, " FoO "),
                OnNext(270, "baR  "),
                OnNext(310, "foO "),
                OnNext(350, " Baz   "),
                OnNext(360, "  qux "),
                OnNext(390, "   bar"),
                OnNext(420, " BAR  "),
                OnNext(470, "FOO "),
                OnNext(480, "baz  "),
                OnNext(510, " bAZ "),
                OnNext(530, "    fOo    "),
                OnError<string>(570, new MockException(42)),
                OnNext(580, "error"),
                OnCompleted<string>(600),
                OnError<string>(650, new MockException(1))
                );

            var comparer = new GroupByComparer(scheduler);

            var results = scheduler.Run(() => xs.GroupBy(x =>
            {
                keyInvoked++;
                return x.Trim();
            }, x =>
            {
                eleInvoked++;
                return Reverse(x);
            }, comparer).Select(g => g.Key));

            results.AssertEqual(
                OnNext(220, "foo"),
                OnNext(270, "baR"),
                OnNext(350, "Baz"),
                OnNext(360, "qux"),
                OnError<string>(570, new MockException(42))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 570)
                );

            Assert.AreEqual(12, keyInvoked);
            Assert.AreEqual(12, eleInvoked);
        }

        [TestMethod]
        public void GroupBy_Outer_Dispose()
        {
            var scheduler = new TestScheduler();

            var keyInvoked = 0;
            var eleInvoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(90, "error"),
                OnNext(110, "error"),
                OnNext(130, "error"),
                OnNext(220, "  foo"),
                OnNext(240, " FoO "),
                OnNext(270, "baR  "),
                OnNext(310, "foO "),
                OnNext(350, " Baz   "),
                OnNext(360, "  qux "),
                OnNext(390, "   bar"),
                OnNext(420, " BAR  "),
                OnNext(470, "FOO "),
                OnNext(480, "baz  "),
                OnNext(510, " bAZ "),
                OnNext(530, "    fOo    "),
                OnCompleted<string>(570),
                OnNext(580, "error"),
                OnCompleted<string>(600),
                OnError<string>(650, new MockException(1))
                );

            var comparer = new GroupByComparer(scheduler);

            var results = scheduler.Run(() => xs.GroupBy(x =>
            {
                keyInvoked++;
                return x.Trim();
            }, x =>
            {
                eleInvoked++;
                return Reverse(x);
            }, comparer).Select(g => g.Key), 355);

            results.AssertEqual(
                OnNext(220, "foo"),
                OnNext(270, "baR"),
                OnNext(350, "Baz")
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 355)
                );

            Assert.AreEqual(5, keyInvoked);
            Assert.AreEqual(5, eleInvoked);
        }

        [TestMethod]
        public void GroupBy_Outer_KeyThrow()
        {
            var scheduler = new TestScheduler();

            var keyInvoked = 0;
            var eleInvoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(90, "error"),
                OnNext(110, "error"),
                OnNext(130, "error"),
                OnNext(220, "  foo"),
                OnNext(240, " FoO "),
                OnNext(270, "baR  "),
                OnNext(310, "foO "),
                OnNext(350, " Baz   "),
                OnNext(360, "  qux "),
                OnNext(390, "   bar"),
                OnNext(420, " BAR  "),
                OnNext(470, "FOO "),
                OnNext(480, "baz  "),
                OnNext(510, " bAZ "),
                OnNext(530, "    fOo    "),
                OnCompleted<string>(570),
                OnNext(580, "error"),
                OnCompleted<string>(600),
                OnError<string>(650, new MockException(1))
                );

            var comparer = new GroupByComparer(scheduler);

            var results = scheduler.Run(() => xs.GroupBy(x =>
            {
                keyInvoked++;
                if (keyInvoked == 10)
                    throw new MockException(20);
                return x.Trim();
            }, x =>
            {
                eleInvoked++;
                return Reverse(x);
            }, comparer).Select(g => g.Key));

            results.AssertEqual(
                OnNext(220, "foo"),
                OnNext(270, "baR"),
                OnNext(350, "Baz"),
                OnNext(360, "qux"),
                OnError<string>(480, new MockException(20))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 480)
                );

            Assert.AreEqual(10, keyInvoked);
            Assert.AreEqual(9, eleInvoked);
        }

        [TestMethod]
        public void GroupBy_Outer_EleThrow()
        {
            var scheduler = new TestScheduler();

            var keyInvoked = 0;
            var eleInvoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(90, "error"),
                OnNext(110, "error"),
                OnNext(130, "error"),
                OnNext(220, "  foo"),
                OnNext(240, " FoO "),
                OnNext(270, "baR  "),
                OnNext(310, "foO "),
                OnNext(350, " Baz   "),
                OnNext(360, "  qux "),
                OnNext(390, "   bar"),
                OnNext(420, " BAR  "),
                OnNext(470, "FOO "),
                OnNext(480, "baz  "),
                OnNext(510, " bAZ "),
                OnNext(530, "    fOo    "),
                OnCompleted<string>(570),
                OnNext(580, "error"),
                OnCompleted<string>(600),
                OnError<string>(650, new MockException(1))
                );

            var comparer = new GroupByComparer(scheduler);

            var results = scheduler.Run(() => xs.GroupBy(x =>
            {
                keyInvoked++;
                return x.Trim();
            }, x =>
            {
                eleInvoked++;
                if (eleInvoked == 10)
                    throw new MockException(10);
                return Reverse(x);
            }, comparer).Select(g => g.Key));

            results.AssertEqual(
                OnNext(220, "foo"),
                OnNext(270, "baR"),
                OnNext(350, "Baz"),
                OnNext(360, "qux"),
                OnError<string>(480, new MockException(10))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 480)
                );

            Assert.AreEqual(10, keyInvoked);
            Assert.AreEqual(10, eleInvoked);
        }

        [TestMethod]
        public void GroupBy_Outer_ComparerEqualsThrow()
        {
            var scheduler = new TestScheduler();

            var keyInvoked = 0;
            var eleInvoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(90, "error"),
                OnNext(110, "error"),
                OnNext(130, "error"),
                OnNext(220, "  foo"),
                OnNext(240, " FoO "),
                OnNext(270, "baR  "),
                OnNext(310, "foO "),
                OnNext(350, " Baz   "),
                OnNext(360, "  qux "),
                OnNext(390, "   bar"),
                OnNext(420, " BAR  "),
                OnNext(470, "FOO "),
                OnNext(480, "baz  "),
                OnNext(510, " bAZ "),
                OnNext(530, "    fOo    "),
                OnCompleted<string>(570),
                OnNext(580, "error"),
                OnCompleted<string>(600),
                OnError<string>(650, new MockException(1))
                );

            var comparer = new GroupByComparer(scheduler, 250, ushort.MaxValue);

            var results = scheduler.Run(() => xs.GroupBy(x =>
            {
                keyInvoked++;
                return x.Trim();
            }, x =>
            {
                eleInvoked++;
                return Reverse(x);
            }, comparer).Select(g => g.Key));

            results.AssertEqual(
                OnNext(220, "foo"),
                OnNext(270, "baR"),
                OnError<string>(310, new MockException(1111))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 310)
                );

            Assert.AreEqual(4, keyInvoked);
            Assert.AreEqual(3, eleInvoked);
        }

        [TestMethod]
        public void GroupBy_Outer_ComparerGetHashCodeThrow()
        {
            var scheduler = new TestScheduler();

            var keyInvoked = 0;
            var eleInvoked = 0;

            var xs = scheduler.CreateHotObservable(
                OnNext(90, "error"),
                OnNext(110, "error"),
                OnNext(130, "error"),
                OnNext(220, "  foo"),
                OnNext(240, " FoO "),
                OnNext(270, "baR  "),
                OnNext(310, "foO "),
                OnNext(350, " Baz   "),
                OnNext(360, "  qux "),
                OnNext(390, "   bar"),
                OnNext(420, " BAR  "),
                OnNext(470, "FOO "),
                OnNext(480, "baz  "),
                OnNext(510, " bAZ "),
                OnNext(530, "    fOo    "),
                OnCompleted<string>(570),
                OnNext(580, "error"),
                OnCompleted<string>(600),
                OnError<string>(650, new MockException(1))
                );

            var comparer = new GroupByComparer(scheduler, ushort.MaxValue, 410);

            var results = scheduler.Run(() => xs.GroupBy(x =>
            {
                keyInvoked++;
                return x.Trim();
            }, x =>
            {
                eleInvoked++;
                return Reverse(x);
            }, comparer).Select(g => g.Key));

            results.AssertEqual(
                OnNext(220, "foo"),
                OnNext(270, "baR"),
                OnNext(350, "Baz"),
                OnNext(360, "qux"),
                OnError<string>(420, new MockException(999))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 420)
                );

            Assert.AreEqual(8, keyInvoked);
            Assert.AreEqual(7, eleInvoked);
        }

        [TestMethod]
        public void GroupBy_Inner_Complete()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, "error"),
                OnNext(110, "error"),
                OnNext(130, "error"),
                OnNext(220, "  foo"),
                OnNext(240, " FoO "),
                OnNext(270, "baR  "),
                OnNext(310, "foO "),
                OnNext(350, " Baz   "),
                OnNext(360, "  qux "),
                OnNext(390, "   bar"),
                OnNext(420, " BAR  "),
                OnNext(470, "FOO "),
                OnNext(480, "baz  "),
                OnNext(510, " bAZ "),
                OnNext(530, "    fOo    "),
                OnCompleted<string>(570),
                OnNext(580, "error"),
                OnCompleted<string>(600),
                OnError<string>(650, new MockException(1))
                );

            var comparer = new GroupByComparer(scheduler);
            var outer = default(IObservable<IGroupedObservable<string, string>>);
            var outerSubscription = default(IDisposable);
            var inners = new Dictionary<string, IObservable<string>>();
            var innerSubscriptions = new Dictionary<string, IDisposable>();
            var results = new Dictionary<string, MockObserver<string>>();

            scheduler.Schedule(() => outer = xs.GroupBy(x => x.Trim(), x => Reverse(x), comparer), Created);

            scheduler.Schedule(() => outerSubscription = outer.Subscribe(group =>
                {
                    var result = new MockObserver<string>(scheduler);
                    inners[group.Key] = group;
                    results[group.Key] = result;
                    scheduler.Schedule(() => innerSubscriptions[group.Key] = group.Subscribe(result), 100);
                }), Subscribed);

            scheduler.Schedule(() =>
                {
                    outerSubscription.Dispose();
                    foreach (var d in innerSubscriptions.Values)
                        d.Dispose();
                }, Disposed);

            scheduler.Run();

            Assert.AreEqual(4, inners.Count);

            results["foo"].AssertEqual(
                OnNext(470, " OOF"),
                OnNext(530, "    oOf    "),
                OnCompleted<string>(570)
                );

            results["baR"].AssertEqual(
                OnNext(390, "rab   "),
                OnNext(420, "  RAB "),
                OnCompleted<string>(570)
                );

            results["Baz"].AssertEqual(
                OnNext(480, "  zab"),
                OnNext(510, " ZAb "),
                OnCompleted<string>(570)
                );

            results["qux"].AssertEqual(
                OnCompleted<string>(570)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 570)
                );
        }

        [TestMethod]
        public void GroupBy_Inner_Complete_All()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, "error"),
                OnNext(110, "error"),
                OnNext(130, "error"),
                OnNext(220, "  foo"),
                OnNext(240, " FoO "),
                OnNext(270, "baR  "),
                OnNext(310, "foO "),
                OnNext(350, " Baz   "),
                OnNext(360, "  qux "),
                OnNext(390, "   bar"),
                OnNext(420, " BAR  "),
                OnNext(470, "FOO "),
                OnNext(480, "baz  "),
                OnNext(510, " bAZ "),
                OnNext(530, "    fOo    "),
                OnCompleted<string>(570),
                OnNext(580, "error"),
                OnCompleted<string>(600),
                OnError<string>(650, new MockException(1))
                );

            var comparer = new GroupByComparer(scheduler);
            var outer = default(IObservable<IGroupedObservable<string, string>>);
            var outerSubscription = default(IDisposable);
            var inners = new Dictionary<string, IObservable<string>>();
            var innerSubscriptions = new Dictionary<string, IDisposable>();
            var results = new Dictionary<string, MockObserver<string>>();

            scheduler.Schedule(() => outer = xs.GroupBy(x => x.Trim(), x => Reverse(x), comparer), Created);

            scheduler.Schedule(() => outerSubscription = outer.Subscribe(group =>
            {
                var result = new MockObserver<string>(scheduler);
                inners[group.Key] = group;
                results[group.Key] = result;
                innerSubscriptions[group.Key] = group.Subscribe(result);
            }), Subscribed);

            scheduler.Schedule(() =>
            {
                outerSubscription.Dispose();
                foreach (var d in innerSubscriptions.Values)
                    d.Dispose();
            }, Disposed);

            scheduler.Run();

            Assert.AreEqual(4, inners.Count);

            results["foo"].AssertEqual(
                OnNext(220, "oof  "),
                OnNext(240, " OoF "),
                OnNext(310, " Oof"),
                OnNext(470, " OOF"),
                OnNext(530, "    oOf    "),
                OnCompleted<string>(570)
                );

            results["baR"].AssertEqual(
                OnNext(270, "  Rab"),
                OnNext(390, "rab   "),
                OnNext(420, "  RAB "),
                OnCompleted<string>(570)
                );

            results["Baz"].AssertEqual(
                OnNext(350, "   zaB "),
                OnNext(480, "  zab"),
                OnNext(510, " ZAb "),
                OnCompleted<string>(570)
                );

            results["qux"].AssertEqual(
                OnNext(360, " xuq  "),
                OnCompleted<string>(570)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 570)
                );
        }

        [TestMethod]
        public void GroupBy_Inner_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, "error"),
                OnNext(110, "error"),
                OnNext(130, "error"),
                OnNext(220, "  foo"),
                OnNext(240, " FoO "),
                OnNext(270, "baR  "),
                OnNext(310, "foO "),
                OnNext(350, " Baz   "),
                OnNext(360, "  qux "),
                OnNext(390, "   bar"),
                OnNext(420, " BAR  "),
                OnNext(470, "FOO "),
                OnNext(480, "baz  "),
                OnNext(510, " bAZ "),
                OnNext(530, "    fOo    "),
                OnError<string>(570, new MockException(42)),
                OnNext(580, "error"),
                OnCompleted<string>(600),
                OnError<string>(650, new MockException(1))
                );

            var comparer = new GroupByComparer(scheduler);
            var outer = default(IObservable<IGroupedObservable<string, string>>);
            var outerSubscription = default(IDisposable);
            var inners = new Dictionary<string, IObservable<string>>();
            var innerSubscriptions = new Dictionary<string, IDisposable>();
            var results = new Dictionary<string, MockObserver<string>>();

            scheduler.Schedule(() => outer = xs.GroupBy(x => x.Trim(), x => Reverse(x), comparer), Created);

            scheduler.Schedule(() => outerSubscription = outer.Subscribe(group =>
            {
                var result = new MockObserver<string>(scheduler);
                inners[group.Key] = group;
                results[group.Key] = result;
                scheduler.Schedule(() => innerSubscriptions[group.Key] = group.Subscribe(result), 100);
            }, ex => { }), Subscribed);

            scheduler.Schedule(() =>
            {
                outerSubscription.Dispose();
                foreach (var d in innerSubscriptions.Values)
                    d.Dispose();
            }, Disposed);

            scheduler.Run();

            Assert.AreEqual(4, inners.Count);

            results["foo"].AssertEqual(
                OnNext(470, " OOF"),
                OnNext(530, "    oOf    "),
                OnError<string>(570, new MockException(42))
                );

            results["baR"].AssertEqual(
                OnNext(390, "rab   "),
                OnNext(420, "  RAB "),
                OnError<string>(570, new MockException(42))
                );

            results["Baz"].AssertEqual(
                OnNext(480, "  zab"),
                OnNext(510, " ZAb "),
                OnError<string>(570, new MockException(42))
                );

            results["qux"].AssertEqual(
                OnError<string>(570, new MockException(42))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 570)
                );
        }

        [TestMethod]
        public void GroupBy_Inner_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, "error"),
                OnNext(110, "error"),
                OnNext(130, "error"),
                OnNext(220, "  foo"),
                OnNext(240, " FoO "),
                OnNext(270, "baR  "),
                OnNext(310, "foO "),
                OnNext(350, " Baz   "),
                OnNext(360, "  qux "),
                OnNext(390, "   bar"),
                OnNext(420, " BAR  "),
                OnNext(470, "FOO "),
                OnNext(480, "baz  "),
                OnNext(510, " bAZ "),
                OnNext(530, "    fOo    "),
                OnCompleted<string>(570),
                OnNext(580, "error"),
                OnCompleted<string>(600),
                OnError<string>(650, new MockException(1))
                );

            var comparer = new GroupByComparer(scheduler);
            var outer = default(IObservable<IGroupedObservable<string, string>>);
            var outerSubscription = default(IDisposable);
            var inners = new Dictionary<string, IObservable<string>>();
            var innerSubscriptions = new Dictionary<string, IDisposable>();
            var results = new Dictionary<string, MockObserver<string>>();

            scheduler.Schedule(() => outer = xs.GroupBy(x => x.Trim(), x => Reverse(x), comparer), Created);

            scheduler.Schedule(() => outerSubscription = outer.Subscribe(group =>
            {
                var result = new MockObserver<string>(scheduler);
                inners[group.Key] = group;
                results[group.Key] = result;
                innerSubscriptions[group.Key] = group.Subscribe(result);
            }), Subscribed);

            scheduler.Schedule(() =>
            {
                outerSubscription.Dispose();
                foreach (var d in innerSubscriptions.Values)
                    d.Dispose();
            }, 400);

            scheduler.Run();

            Assert.AreEqual(4, inners.Count);

            results["foo"].AssertEqual(
                OnNext(220, "oof  "),
                OnNext(240, " OoF "),
                OnNext(310, " Oof")
                );

            results["baR"].AssertEqual(
                OnNext(270, "  Rab"),
                OnNext(390, "rab   ")
                );

            results["Baz"].AssertEqual(
                OnNext(350, "   zaB ")
                );

            results["qux"].AssertEqual(
                OnNext(360, " xuq  ")
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 400)
                );
        }

        [TestMethod]
        public void GroupBy_Inner_KeyThrow()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, "error"),
                OnNext(110, "error"),
                OnNext(130, "error"),
                OnNext(220, "  foo"),
                OnNext(240, " FoO "),
                OnNext(270, "baR  "),
                OnNext(310, "foO "),
                OnNext(350, " Baz   "),
                OnNext(360, "  qux "),
                OnNext(390, "   bar"),
                OnNext(420, " BAR  "),
                OnNext(470, "FOO "),
                OnNext(480, "baz  "),
                OnNext(510, " bAZ "),
                OnNext(530, "    fOo    "),
                OnCompleted<string>(570),
                OnNext(580, "error"),
                OnCompleted<string>(600),
                OnError<string>(650, new MockException(1))
                );

            var comparer = new GroupByComparer(scheduler);
            var outer = default(IObservable<IGroupedObservable<string, string>>);
            var outerSubscription = default(IDisposable);
            var inners = new Dictionary<string, IObservable<string>>();
            var innerSubscriptions = new Dictionary<string, IDisposable>();
            var results = new Dictionary<string, MockObserver<string>>();

            var keyInvoked = 0;

            scheduler.Schedule(() => outer = xs.GroupBy(x =>
                {
                    keyInvoked++;
                    if (keyInvoked == 6)
                        throw new MockException(42);
                    return x.Trim();
                }, x => Reverse(x), comparer), Created);

            scheduler.Schedule(() => outerSubscription = outer.Subscribe(group =>
            {
                var result = new MockObserver<string>(scheduler);
                inners[group.Key] = group;
                results[group.Key] = result;
                innerSubscriptions[group.Key] = group.Subscribe(result);
            }, _ => { }), Subscribed);

            scheduler.Schedule(() =>
            {
                outerSubscription.Dispose();
                foreach (var d in innerSubscriptions.Values)
                    d.Dispose();
            }, Disposed);

            scheduler.Run();

            Assert.AreEqual(3, inners.Count);

            results["foo"].AssertEqual(
                OnNext(220, "oof  "),
                OnNext(240, " OoF "),
                OnNext(310, " Oof"),
                OnError<string>(360, new MockException(42))
                );

            results["baR"].AssertEqual(
                OnNext(270, "  Rab"),
                OnError<string>(360, new MockException(42))
                );

            results["Baz"].AssertEqual(
                OnNext(350, "   zaB "),
                OnError<string>(360, new MockException(42))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 360)
                );
        }

        [TestMethod]
        public void GroupBy_Inner_EleThrow()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, "error"),
                OnNext(110, "error"),
                OnNext(130, "error"),
                OnNext(220, "  foo"),
                OnNext(240, " FoO "),
                OnNext(270, "baR  "),
                OnNext(310, "foO "),
                OnNext(350, " Baz   "),
                OnNext(360, "  qux "),
                OnNext(390, "   bar"),
                OnNext(420, " BAR  "),
                OnNext(470, "FOO "),
                OnNext(480, "baz  "),
                OnNext(510, " bAZ "),
                OnNext(530, "    fOo    "),
                OnCompleted<string>(570),
                OnNext(580, "error"),
                OnCompleted<string>(600),
                OnError<string>(650, new MockException(1))
                );

            var comparer = new GroupByComparer(scheduler);
            var outer = default(IObservable<IGroupedObservable<string, string>>);
            var outerSubscription = default(IDisposable);
            var inners = new Dictionary<string, IObservable<string>>();
            var innerSubscriptions = new Dictionary<string, IDisposable>();
            var results = new Dictionary<string, MockObserver<string>>();

            var eleInvoked = 0;

            scheduler.Schedule(() => outer = xs.GroupBy(x => x.Trim(), x =>
                {
                    eleInvoked++;
                    if (eleInvoked == 6)
                        throw new MockException(42);
                    return Reverse(x);
                }, comparer), Created);

            scheduler.Schedule(() => outerSubscription = outer.Subscribe(group =>
            {
                var result = new MockObserver<string>(scheduler);
                inners[group.Key] = group;
                results[group.Key] = result;
                innerSubscriptions[group.Key] = group.Subscribe(result);
            }, _ => { }), Subscribed);

            scheduler.Schedule(() =>
            {
                outerSubscription.Dispose();
                foreach (var d in innerSubscriptions.Values)
                    d.Dispose();
            }, Disposed);

            scheduler.Run();

            Assert.AreEqual(4, inners.Count);

            results["foo"].AssertEqual(
                OnNext(220, "oof  "),
                OnNext(240, " OoF "),
                OnNext(310, " Oof"),
                OnError<string>(360, new MockException(42))
                );

            results["baR"].AssertEqual(
                OnNext(270, "  Rab"),
                OnError<string>(360, new MockException(42))
                );

            results["Baz"].AssertEqual(
                OnNext(350, "   zaB "),
                OnError<string>(360, new MockException(42))
                );

            results["qux"].AssertEqual(
                OnError<string>(360, new MockException(42))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 360)
                );
        }

        [TestMethod]
        public void GroupBy_Inner_Comparer_EqualsThrow()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, "error"),
                OnNext(110, "error"),
                OnNext(130, "error"),
                OnNext(220, "  foo"),
                OnNext(240, " FoO "),
                OnNext(270, "baR  "),
                OnNext(310, "foO "),
                OnNext(350, " Baz   "),
                OnNext(360, "  qux "),
                OnNext(390, "   bar"),
                OnNext(420, " BAR  "),
                OnNext(470, "FOO "),
                OnNext(480, "baz  "),
                OnNext(510, " bAZ "),
                OnNext(530, "    fOo    "),
                OnCompleted<string>(570),
                OnNext(580, "error"),
                OnCompleted<string>(600),
                OnError<string>(650, new MockException(1))
                );

            var comparer = new GroupByComparer(scheduler, 400, ushort.MaxValue);
            var outer = default(IObservable<IGroupedObservable<string, string>>);
            var outerSubscription = default(IDisposable);
            var inners = new Dictionary<string, IObservable<string>>();
            var innerSubscriptions = new Dictionary<string, IDisposable>();
            var results = new Dictionary<string, MockObserver<string>>();

            scheduler.Schedule(() => outer = xs.GroupBy(x => x.Trim(), x => Reverse(x), comparer), Created);

            scheduler.Schedule(() => outerSubscription = outer.Subscribe(group =>
            {
                var result = new MockObserver<string>(scheduler);
                inners[group.Key] = group;
                results[group.Key] = result;
                innerSubscriptions[group.Key] = group.Subscribe(result);
            }, _ => { }), Subscribed);

            scheduler.Schedule(() =>
            {
                outerSubscription.Dispose();
                foreach (var d in innerSubscriptions.Values)
                    d.Dispose();
            }, Disposed);

            scheduler.Run();

            Assert.AreEqual(4, inners.Count);

            results["foo"].AssertEqual(
                OnNext(220, "oof  "),
                OnNext(240, " OoF "),
                OnNext(310, " Oof"),
                OnError<string>(420, new MockException(1111))
                );

            results["baR"].AssertEqual(
                OnNext(270, "  Rab"),
                OnNext(390, "rab   "),
                OnError<string>(420, new MockException(1111))
                );

            results["Baz"].AssertEqual(
                OnNext(350, "   zaB "),
                OnError<string>(420, new MockException(1111))
                );

            results["qux"].AssertEqual(
                OnNext(360, " xuq  "),
                OnError<string>(420, new MockException(1111))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 420)
                );
        }

        [TestMethod]
        public void GroupBy_Inner_Comparer_GetHashCodeThrow()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, "error"),
                OnNext(110, "error"),
                OnNext(130, "error"),
                OnNext(220, "  foo"),
                OnNext(240, " FoO "),
                OnNext(270, "baR  "),
                OnNext(310, "foO "),
                OnNext(350, " Baz   "),
                OnNext(360, "  qux "),
                OnNext(390, "   bar"),
                OnNext(420, " BAR  "),
                OnNext(470, "FOO "),
                OnNext(480, "baz  "),
                OnNext(510, " bAZ "),
                OnNext(530, "    fOo    "),
                OnCompleted<string>(570),
                OnNext(580, "error"),
                OnCompleted<string>(600),
                OnError<string>(650, new MockException(1))
                );

            var comparer = new GroupByComparer(scheduler, ushort.MaxValue, 400);
            var outer = default(IObservable<IGroupedObservable<string, string>>);
            var outerSubscription = default(IDisposable);
            var inners = new Dictionary<string, IObservable<string>>();
            var innerSubscriptions = new Dictionary<string, IDisposable>();
            var results = new Dictionary<string, MockObserver<string>>();

            scheduler.Schedule(() => outer = xs.GroupBy(x => x.Trim(), x => Reverse(x), comparer), Created);

            scheduler.Schedule(() => outerSubscription = outer.Subscribe(group =>
            {
                var result = new MockObserver<string>(scheduler);
                inners[group.Key] = group;
                results[group.Key] = result;
                innerSubscriptions[group.Key] = group.Subscribe(result);
            }, _ => { }), Subscribed);

            scheduler.Schedule(() =>
            {
                outerSubscription.Dispose();
                foreach (var d in innerSubscriptions.Values)
                    d.Dispose();
            }, Disposed);

            scheduler.Run();

            Assert.AreEqual(4, inners.Count);

            results["foo"].AssertEqual(
                OnNext(220, "oof  "),
                OnNext(240, " OoF "),
                OnNext(310, " Oof"),
                OnError<string>(420, new MockException(999))
                );

            results["baR"].AssertEqual(
                OnNext(270, "  Rab"),
                OnNext(390, "rab   "),
                OnError<string>(420, new MockException(999))
                );

            results["Baz"].AssertEqual(
                OnNext(350, "   zaB "),
                OnError<string>(420, new MockException(999))
                );

            results["qux"].AssertEqual(
                OnNext(360, " xuq  "),
                OnError<string>(420, new MockException(999))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 420)
                );
        }

        [TestMethod]
        public void GroupBy_Outer_Independence()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, "error"),
                OnNext(110, "error"),
                OnNext(130, "error"),
                OnNext(220, "  foo"),
                OnNext(240, " FoO "),
                OnNext(270, "baR  "),
                OnNext(310, "foO "),
                OnNext(350, " Baz   "),
                OnNext(360, "  qux "),
                OnNext(390, "   bar"),
                OnNext(420, " BAR  "),
                OnNext(470, "FOO "),
                OnNext(480, "baz  "),
                OnNext(510, " bAZ "),
                OnNext(530, "    fOo    "),
                OnCompleted<string>(570),
                OnNext(580, "error"),
                OnCompleted<string>(600),
                OnError<string>(650, new MockException(1))
                );

            var comparer = new GroupByComparer(scheduler);
            var outer = default(IObservable<IGroupedObservable<string, string>>);
            var outerSubscription = default(IDisposable);
            var inners = new Dictionary<string, IObservable<string>>();
            var innerSubscriptions = new Dictionary<string, IDisposable>();
            var results = new Dictionary<string, MockObserver<string>>();
            var outerResults = new MockObserver<string>(scheduler);

            scheduler.Schedule(() => outer = xs.GroupBy(x => x.Trim(), x => Reverse(x), comparer), Created);

            scheduler.Schedule(() => outerSubscription = outer.Subscribe(group =>
            {
                outerResults.OnNext(group.Key);
                var result = new MockObserver<string>(scheduler);
                inners[group.Key] = group;
                results[group.Key] = result;
                innerSubscriptions[group.Key] = group.Subscribe(result);
            }, outerResults.OnError, outerResults.OnCompleted), Subscribed);

            scheduler.Schedule(() =>
            {
                outerSubscription.Dispose();
                foreach (var d in innerSubscriptions.Values)
                    d.Dispose();
            }, Disposed);

            scheduler.Schedule(() => outerSubscription.Dispose(), 320);

            scheduler.Run();

            Assert.AreEqual(2, inners.Count);

            outerResults.AssertEqual(
                OnNext(220, "foo"),
                OnNext(270, "baR")
                );

            results["foo"].AssertEqual(
                OnNext(220, "oof  "),
                OnNext(240, " OoF "),
                OnNext(310, " Oof"),
                OnNext(470, " OOF"),
                OnNext(530, "    oOf    "),
                OnCompleted<string>(570)
                );

            results["baR"].AssertEqual(
                OnNext(270, "  Rab"),
                OnNext(390, "rab   "),
                OnNext(420, "  RAB "),
                OnCompleted<string>(570)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 570)
                );
        }

        [TestMethod]
        public void GroupBy_Inner_Independence()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, "error"),
                OnNext(110, "error"),
                OnNext(130, "error"),
                OnNext(220, "  foo"),
                OnNext(240, " FoO "),
                OnNext(270, "baR  "),
                OnNext(310, "foO "),
                OnNext(350, " Baz   "),
                OnNext(360, "  qux "),
                OnNext(390, "   bar"),
                OnNext(420, " BAR  "),
                OnNext(470, "FOO "),
                OnNext(480, "baz  "),
                OnNext(510, " bAZ "),
                OnNext(530, "    fOo    "),
                OnCompleted<string>(570),
                OnNext(580, "error"),
                OnCompleted<string>(600),
                OnError<string>(650, new MockException(1))
                );

            var comparer = new GroupByComparer(scheduler);
            var outer = default(IObservable<IGroupedObservable<string, string>>);
            var outerSubscription = default(IDisposable);
            var inners = new Dictionary<string, IObservable<string>>();
            var innerSubscriptions = new Dictionary<string, IDisposable>();
            var results = new Dictionary<string, MockObserver<string>>();
            var outerResults = new MockObserver<string>(scheduler);

            scheduler.Schedule(() => outer = xs.GroupBy(x => x.Trim(), x => Reverse(x), comparer), Created);

            scheduler.Schedule(() => outerSubscription = outer.Subscribe(group =>
            {
                outerResults.OnNext(group.Key);
                var result = new MockObserver<string>(scheduler);
                inners[group.Key] = group;
                results[group.Key] = result;
                innerSubscriptions[group.Key] = group.Subscribe(result);
            }, outerResults.OnError, outerResults.OnCompleted), Subscribed);

            scheduler.Schedule(() =>
            {
                outerSubscription.Dispose();
                foreach (var d in innerSubscriptions.Values)
                    d.Dispose();
            }, Disposed);

            scheduler.Schedule(() => innerSubscriptions["foo"].Dispose(), 320);

            scheduler.Run();

            Assert.AreEqual(4, inners.Count);

            results["foo"].AssertEqual(
                OnNext(220, "oof  "),
                OnNext(240, " OoF "),
                OnNext(310, " Oof")
                );

            results["baR"].AssertEqual(
                OnNext(270, "  Rab"),
                OnNext(390, "rab   "),
                OnNext(420, "  RAB "),
                OnCompleted<string>(570)
                );

            results["Baz"].AssertEqual(
                OnNext(350, "   zaB "),
                OnNext(480, "  zab"),
                OnNext(510, " ZAb "),
                OnCompleted<string>(570)
                );

            results["qux"].AssertEqual(
                OnNext(360, " xuq  "),
                OnCompleted<string>(570)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 570)
                );
        }


        [TestMethod]
        public void GroupBy_Inner_Multiple_Independence()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, "error"),
                OnNext(110, "error"),
                OnNext(130, "error"),
                OnNext(220, "  foo"),
                OnNext(240, " FoO "),
                OnNext(270, "baR  "),
                OnNext(310, "foO "),
                OnNext(350, " Baz   "),
                OnNext(360, "  qux "),
                OnNext(390, "   bar"),
                OnNext(420, " BAR  "),
                OnNext(470, "FOO "),
                OnNext(480, "baz  "),
                OnNext(510, " bAZ "),
                OnNext(530, "    fOo    "),
                OnCompleted<string>(570),
                OnNext(580, "error"),
                OnCompleted<string>(600),
                OnError<string>(650, new MockException(1))
                );

            var comparer = new GroupByComparer(scheduler);
            var outer = default(IObservable<IGroupedObservable<string, string>>);
            var outerSubscription = default(IDisposable);
            var inners = new Dictionary<string, IObservable<string>>();
            var innerSubscriptions = new Dictionary<string, IDisposable>();
            var results = new Dictionary<string, MockObserver<string>>();
            var outerResults = new MockObserver<string>(scheduler);

            scheduler.Schedule(() => outer = xs.GroupBy(x => x.Trim(), x => Reverse(x), comparer), Created);

            scheduler.Schedule(() => outerSubscription = outer.Subscribe(group =>
            {
                outerResults.OnNext(group.Key);
                var result = new MockObserver<string>(scheduler);
                inners[group.Key] = group;
                results[group.Key] = result;
                innerSubscriptions[group.Key] = group.Subscribe(result);
            }, outerResults.OnError, outerResults.OnCompleted), Subscribed);

            scheduler.Schedule(() =>
            {
                outerSubscription.Dispose();
                foreach (var d in innerSubscriptions.Values)
                    d.Dispose();
            }, Disposed);

            scheduler.Schedule(() => innerSubscriptions["foo"].Dispose(), 320);
            scheduler.Schedule(() => innerSubscriptions["baR"].Dispose(), 280);
            scheduler.Schedule(() => innerSubscriptions["Baz"].Dispose(), 355);
            scheduler.Schedule(() => innerSubscriptions["qux"].Dispose(), 400);

            scheduler.Run();

            Assert.AreEqual(4, inners.Count);

            results["foo"].AssertEqual(
                OnNext(220, "oof  "),
                OnNext(240, " OoF "),
                OnNext(310, " Oof")
                );

            results["baR"].AssertEqual(
                OnNext(270, "  Rab")
                );

            results["Baz"].AssertEqual(
                OnNext(350, "   zaB ")
                );

            results["qux"].AssertEqual(
                OnNext(360, " xuq  ")
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 570)
                );
        }

        [TestMethod]
        public void GroupBy_Inner_Escape_Complete()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(220, "  foo"),
                OnNext(240, " FoO "),
                OnNext(310, "foO "),
                OnNext(470, "FOO "),
                OnNext(530, "    fOo    "),
                OnCompleted<string>(570)
                );

            var outer = default(IObservable<IGroupedObservable<string, string>>);
            var outerSubscription = default(IDisposable);
            var inner = default(IObservable<string>);
            var innerSubscription = default(IDisposable);
            var results = new MockObserver<string>(scheduler);

            scheduler.Schedule(() => outer = xs.GroupBy(x => x.Trim()), Created);

            scheduler.Schedule(() => outerSubscription = outer.Subscribe(group =>
            {
                inner = group;
            }), Subscribed);

            scheduler.Schedule(() => innerSubscription = inner.Subscribe(results), 600);

            scheduler.Schedule(() =>
            {
                outerSubscription.Dispose();
                innerSubscription.Dispose();
            }, Disposed);

            scheduler.Run();

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 570)
                );

            results.AssertEqual(
                );
        }

        [TestMethod]
        public void GroupBy_Inner_Escape_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(220, "  foo"),
                OnNext(240, " FoO "),
                OnNext(310, "foO "),
                OnNext(470, "FOO "),
                OnNext(530, "    fOo    "),
                OnError<string>(570, new MockException(30))
                );

            var outer = default(IObservable<IGroupedObservable<string, string>>);
            var outerSubscription = default(IDisposable);
            var inner = default(IObservable<string>);
            var innerSubscription = default(IDisposable);
            var results = new MockObserver<string>(scheduler);

            scheduler.Schedule(() => outer = xs.GroupBy(x => x.Trim()), Created);

            scheduler.Schedule(() => outerSubscription = outer.Subscribe(group =>
            {
                inner = group;
            }, _ => { }), Subscribed);

            scheduler.Schedule(() => innerSubscription = inner.Subscribe(results), 600);

            scheduler.Schedule(() =>
            {
                outerSubscription.Dispose();
                innerSubscription.Dispose();
            }, Disposed);

            scheduler.Run();

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 570)
                );

            results.AssertEqual(
                );
        }

        [TestMethod]
        public void GroupBy_Inner_Escape_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(220, "  foo"),
                OnNext(240, " FoO "),
                OnNext(310, "foO "),
                OnNext(470, "FOO "),
                OnNext(530, "    fOo    "),
                OnError<string>(570, new MockException(30))
                );

            var outer = default(IObservable<IGroupedObservable<string, string>>);
            var outerSubscription = default(IDisposable);
            var inner = default(IObservable<string>);
            var innerSubscription = default(IDisposable);
            var results = new MockObserver<string>(scheduler);

            scheduler.Schedule(() => outer = xs.GroupBy(x => x.Trim()), Created);

            scheduler.Schedule(() => outerSubscription = outer.Subscribe(group =>
            {
                inner = group;
            }), Subscribed);

            scheduler.Schedule(() => outerSubscription.Dispose(), 400);

            scheduler.Schedule(() => innerSubscription = inner.Subscribe(results), 600);

            scheduler.Schedule(() =>
            {
                innerSubscription.Dispose();
            }, Disposed);

            scheduler.Run();

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 400)
                );

            results.AssertEqual(
                );
        }

        [TestMethod]
        public void Take_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => ((IObservable<int>)null).Take(0));
            Throws<ArgumentOutOfRangeException>(() => DummyObservable<int>.Instance.Take(-1));
            Throws<ArgumentNullException>(() => NullErrorObservable<int>.Instance.Take(1).Subscribe(DummyObserver<int>.Instance));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.Take(1).Subscribe(null));
        }

        [TestMethod]
        public void Take_Complete_After()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 6),
                OnNext(150, 4),
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10),
                OnCompleted<int>(690)
                );

            var results = scheduler.Run(() => xs.Take(20));

            results.AssertEqual(
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10),
                OnCompleted<int>(690)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 690)
                );
        }

        [TestMethod]
        public void Take_Complete_Same()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 6),
                OnNext(150, 4),
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10),
                OnCompleted<int>(690)
                );

            var results = scheduler.Run(() => xs.Take(17));

            results.AssertEqual(
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10),
                OnCompleted<int>(630)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 630)
                );
        }

        [TestMethod]
        public void Take_Complete_Before()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 6),
                OnNext(150, 4),
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10),
                OnCompleted<int>(690)
                );

            var results = scheduler.Run(() => xs.Take(10));

            results.AssertEqual(
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnCompleted<int>(415)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 415)
                );
        }

        [TestMethod]
        public void Take_Complete_Zero()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 6),
                OnNext(150, 4),
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10),
                OnCompleted<int>(690)
                );

            var results = scheduler.Run(() => xs.Take(0));

            results.AssertEqual(
                OnCompleted<int>(200)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 200)
                );
        }

        [TestMethod]
        public void Take_Error_After()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 6),
                OnNext(150, 4),
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10),
                OnError<int>(690, new MockException(1))
                );

            var results = scheduler.Run(() => xs.Take(20));

            results.AssertEqual(
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10),
                OnError<int>(690, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 690)
                );
        }

        [TestMethod]
        public void Take_Error_Same()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 6),
                OnNext(150, 4),
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10),
                OnError<int>(690, new MockException(1))
                );

            var results = scheduler.Run(() => xs.Take(17));

            results.AssertEqual(
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10),
                OnCompleted<int>(630)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 630)
                );
        }

        [TestMethod]
        public void Take_Error_Before()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 6),
                OnNext(150, 4),
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10),
                OnError<int>(690, new MockException(1))
                );

            var results = scheduler.Run(() => xs.Take(3));

            results.AssertEqual(
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnCompleted<int>(270)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 270)
                );
        }

        [TestMethod]
        public void Take_Dispose_Before()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 6),
                OnNext(150, 4),
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10)
                );

            var results = scheduler.Run(() => xs.Take(3), 250);

            results.AssertEqual(
                OnNext(210, 9),
                OnNext(230, 13)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 250)
                );
        }

        [TestMethod]
        public void Take_Dispose_After()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 6),
                OnNext(150, 4),
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10)
                );

            var results = scheduler.Run(() => xs.Take(3), 400);

            results.AssertEqual(
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnCompleted<int>(270)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 270)
                );
        }

        [TestMethod]
        public void Take_Zero()
        {
            var scheduler = new TestScheduler();
            
            var ok = new ImmediateFireObservable(false);
            scheduler.Run(() => ok.Take(0));
            Assert.IsTrue(ok.Disposed);

            var err = new ImmediateFireObservable(true);
            scheduler.Run(() => err.Take(0));
            Assert.IsTrue(err.Disposed);
        }

        class ImmediateFireObservable : IObservable<int>
        {
            private bool _fail;
            private bool _disposed;

            public ImmediateFireObservable(bool fail)
            {
                _fail = fail;
            }

            public bool Disposed { get { return _disposed; } }

            public IDisposable Subscribe(IObserver<int> observer)
            {
                observer.OnNext(1);
                if (_fail)
                    observer.OnError(new Exception());
                else
                    observer.OnCompleted();

                return Disposable.Create(() => { _disposed = true; });
            }
        }

        [TestMethod]
        public void Skip_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => ((IObservable<int>)null).Skip(0));
            Throws<ArgumentOutOfRangeException>(() => DummyObservable<int>.Instance.Skip(-1));
            Throws<ArgumentNullException>(() => NullErrorObservable<int>.Instance.Skip(1).Subscribe(DummyObserver<int>.Instance));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.Skip(0).Subscribe(null));
        }

        [TestMethod]
        public void Skip_Complete_After()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 6),
                OnNext(150, 4),
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10),
                OnCompleted<int>(690)
                );

            var results = scheduler.Run(() => xs.Skip(20));

            results.AssertEqual(
                OnCompleted<int>(690)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 690)
                );
        }

        [TestMethod]
        public void Skip_Complete_Same()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 6),
                OnNext(150, 4),
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10),
                OnCompleted<int>(690)
                );

            var results = scheduler.Run(() => xs.Skip(17));

            results.AssertEqual(
                OnCompleted<int>(690)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 690)
                );
        }

        [TestMethod]
        public void Skip_Complete_Before()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 6),
                OnNext(150, 4),
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10),
                OnCompleted<int>(690)
                );

            var results = scheduler.Run(() => xs.Skip(10));

            results.AssertEqual(
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10),
                OnCompleted<int>(690)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 690)
                );
        }

        [TestMethod]
        public void Skip_Complete_Zero()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 6),
                OnNext(150, 4),
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10),
                OnCompleted<int>(690)
                );

            var results = scheduler.Run(() => xs.Skip(0));

            results.AssertEqual(
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10),
                OnCompleted<int>(690)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 690)
                );
        }

        [TestMethod]
        public void Skip_Error_After()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 6),
                OnNext(150, 4),
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10),
                OnError<int>(690, new MockException(1))
                );

            var results = scheduler.Run(() => xs.Skip(20));

            results.AssertEqual(
                OnError<int>(690, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 690)
                );
        }

        [TestMethod]
        public void Skip_Error_Same()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 6),
                OnNext(150, 4),
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10),
                OnError<int>(690, new MockException(1))
                );

            var results = scheduler.Run(() => xs.Skip(17));

            results.AssertEqual(
                OnError<int>(690, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 690)
                );
        }

        [TestMethod]
        public void Skip_Error_Before()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 6),
                OnNext(150, 4),
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10),
                OnError<int>(690, new MockException(1))
                );

            var results = scheduler.Run(() => xs.Skip(3));

            results.AssertEqual(
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10),
                OnError<int>(690, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 690)
                );
        }

        [TestMethod]
        public void Skip_Dispose_Before()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 6),
                OnNext(150, 4),
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10)
                );

            var results = scheduler.Run(() => xs.Skip(3), 250);

            results.AssertEqual(
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 250)
                );
        }

        [TestMethod]
        public void Skip_Dispose_After()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 6),
                OnNext(150, 4),
                OnNext(210, 9),
                OnNext(230, 13),
                OnNext(270, 7),
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11),
                OnNext(410, 15),
                OnNext(415, 16),
                OnNext(460, 72),
                OnNext(510, 76),
                OnNext(560, 32),
                OnNext(570, -100),
                OnNext(580, -3),
                OnNext(590, 5),
                OnNext(630, 10)
                );

            var results = scheduler.Run(() => xs.Skip(3), 400);

            results.AssertEqual(
                OnNext(280, 1),
                OnNext(300, -1),
                OnNext(310, 3),
                OnNext(340, 8),
                OnNext(370, 11)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 400)
                );
        }

        [TestMethod]
        public void TakeWhile_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => ((IObservable<int>)null).TakeWhile(DummyFunc<int, bool>.Instance));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.TakeWhile(null));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.TakeWhile(DummyFunc<int, bool>.Instance).Subscribe(null));
            Throws<ArgumentNullException>(() => NullErrorObservable<int>.Instance.TakeWhile(DummyFunc<int, bool>.Instance).Subscribe());
        }

        [TestMethod]
        public void TakeWhile_Complete_Before()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, -1),
                OnNext(110, -1),
                OnNext(210, 2),
                OnNext(260, 5),
                OnNext(290, 13),
                OnNext(320, 3),
                OnCompleted<int>(330),
                OnNext(350, 7),
                OnNext(390, 4),
                OnNext(410, 17),
                OnNext(450, 8),
                OnNext(500, 23),
                OnCompleted<int>(600)
                );

            var invoked = 0;

            var results = scheduler.Run(() => xs.TakeWhile(x =>
            {
                invoked++;
                return IsPrime(x);
            }));

            results.AssertEqual(
                OnNext(210, 2),
                OnNext(260, 5),
                OnNext(290, 13),
                OnNext(320, 3),
                OnCompleted<int>(330)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 330)
                );

            Assert.AreEqual(4, invoked);
        }

        [TestMethod]
        public void TakeWhile_Complete_After()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, -1),
                OnNext(110, -1),
                OnNext(210, 2),
                OnNext(260, 5),
                OnNext(290, 13),
                OnNext(320, 3),
                OnNext(350, 7),
                OnNext(390, 4),
                OnNext(410, 17),
                OnNext(450, 8),
                OnNext(500, 23),
                OnCompleted<int>(600)
                );

            var invoked = 0;

            var results = scheduler.Run(() => xs.TakeWhile(x =>
                {
                    invoked++;
                    return IsPrime(x);
                }));

            results.AssertEqual(
                OnNext(210, 2),
                OnNext(260, 5),
                OnNext(290, 13),
                OnNext(320, 3),
                OnNext(350, 7),
                OnCompleted<int>(390)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 390)
                );

            Assert.AreEqual(6, invoked);
        }

        [TestMethod]
        public void TakeWhile_Error_Before()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, -1),
                OnNext(110, -1),
                OnNext(210, 2),
                OnNext(260, 5),
                OnError<int>(270, new MockException(4)),
                OnNext(290, 13),
                OnNext(320, 3),
                OnNext(350, 7),
                OnNext(390, 4),
                OnNext(410, 17),
                OnNext(450, 8),
                OnNext(500, 23)
                );

            var invoked = 0;

            var results = scheduler.Run(() => xs.TakeWhile(x =>
            {
                invoked++;
                return IsPrime(x);
            }));

            results.AssertEqual(
                OnNext(210, 2),
                OnNext(260, 5),
                OnError<int>(270, new MockException(4))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 270)
                );

            Assert.AreEqual(2, invoked);
        }

        [TestMethod]
        public void TakeWhile_Error_After()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, -1),
                OnNext(110, -1),
                OnNext(210, 2),
                OnNext(260, 5),
                OnNext(290, 13),
                OnNext(320, 3),
                OnNext(350, 7),
                OnNext(390, 4),
                OnNext(410, 17),
                OnNext(450, 8),
                OnNext(500, 23),
                OnError<int>(600, new MockException(4))
                );

            var invoked = 0;

            var results = scheduler.Run(() => xs.TakeWhile(x =>
            {
                invoked++;
                return IsPrime(x);
            }));

            results.AssertEqual(
                OnNext(210, 2),
                OnNext(260, 5),
                OnNext(290, 13),
                OnNext(320, 3),
                OnNext(350, 7),
                OnCompleted<int>(390)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 390)
                );

            Assert.AreEqual(6, invoked);
        }

        [TestMethod]
        public void TakeWhile_Dispose_Before()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, -1),
                OnNext(110, -1),
                OnNext(210, 2),
                OnNext(260, 5),
                OnNext(290, 13),
                OnNext(320, 3),
                OnNext(350, 7),
                OnNext(390, 4),
                OnNext(410, 17),
                OnNext(450, 8),
                OnNext(500, 23),
                OnCompleted<int>(600)
                );

            var invoked = 0;

            var results = scheduler.Run(() => xs.TakeWhile(x =>
            {
                invoked++;
                return IsPrime(x);
            }), 300);

            results.AssertEqual(
                OnNext(210, 2),
                OnNext(260, 5),
                OnNext(290, 13)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 300)
                );

            Assert.AreEqual(3, invoked);
        }

        [TestMethod]
        public void TakeWhile_Dispose_After()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, -1),
                OnNext(110, -1),
                OnNext(210, 2),
                OnNext(260, 5),
                OnNext(290, 13),
                OnNext(320, 3),
                OnNext(350, 7),
                OnNext(390, 4),
                OnNext(410, 17),
                OnNext(450, 8),
                OnNext(500, 23),
                OnCompleted<int>(600)
                );

            var invoked = 0;

            var results = scheduler.Run(() => xs.TakeWhile(x =>
            {
                invoked++;
                return IsPrime(x);
            }), 400);

            results.AssertEqual(
                OnNext(210, 2),
                OnNext(260, 5),
                OnNext(290, 13),
                OnNext(320, 3),
                OnNext(350, 7),
                OnCompleted<int>(390)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 390)
                );

            Assert.AreEqual(6, invoked);
        }

        [TestMethod]
        public void TakeWhile_Zero()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, -1),
                OnNext(110, -1),
                OnNext(205, 100),
                OnNext(210, 2),
                OnNext(260, 5),
                OnNext(290, 13),
                OnNext(320, 3),
                OnNext(350, 7),
                OnNext(390, 4),
                OnNext(410, 17),
                OnNext(450, 8),
                OnNext(500, 23),
                OnCompleted<int>(600)
                );

            var invoked = 0;

            var results = scheduler.Run(() => xs.TakeWhile(x =>
            {
                invoked++;
                return IsPrime(x);
            }), 300);

            results.AssertEqual(
                OnCompleted<int>(205)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 205)
                );

            Assert.AreEqual(1, invoked);
        }

        [TestMethod]
        public void TakeWhile_Throw()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, -1),
                OnNext(110, -1),
                OnNext(210, 2),
                OnNext(260, 5),
                OnNext(290, 13),
                OnNext(320, 3),
                OnNext(350, 7),
                OnNext(390, 4),
                OnNext(410, 17),
                OnNext(450, 8),
                OnNext(500, 23),
                OnCompleted<int>(600)
                );

            var invoked = 0;

            var results = scheduler.Run(() => xs.TakeWhile(x =>
            {
                invoked++;
                if (invoked == 3)
                    throw new MockException(3);
                return IsPrime(x);
            }));

            results.AssertEqual(
                OnNext(210, 2),
                OnNext(260, 5),
                OnError<int>(290, new MockException(3))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 290)
                );

            Assert.AreEqual(3, invoked);
        }

        [TestMethod]
        public void SkipWhile_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => ((IObservable<int>)null).SkipWhile(DummyFunc<int, bool>.Instance));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.SkipWhile(null));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.SkipWhile(DummyFunc<int, bool>.Instance).Subscribe(null));
            Throws<ArgumentNullException>(() => NullErrorObservable<int>.Instance.SkipWhile(DummyFunc<int, bool>.Instance).Subscribe());
        }

        [TestMethod]
        public void SkipWhile_Complete_Before()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, -1),
                OnNext(110, -1),
                OnNext(210, 2),
                OnNext(260, 5),
                OnNext(290, 13),
                OnNext(320, 3),
                OnCompleted<int>(330),
                OnNext(350, 7),
                OnNext(390, 4),
                OnNext(410, 17),
                OnNext(450, 8),
                OnNext(500, 23),
                OnCompleted<int>(600)
                );

            var invoked = 0;

            var results = scheduler.Run(() => xs.SkipWhile(x =>
            {
                invoked++;
                return IsPrime(x);
            }));

            results.AssertEqual(
                OnCompleted<int>(330)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 330)
                );

            Assert.AreEqual(4, invoked);
        }

        [TestMethod]
        public void SkipWhile_Complete_After()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, -1),
                OnNext(110, -1),
                OnNext(210, 2),
                OnNext(260, 5),
                OnNext(290, 13),
                OnNext(320, 3),
                OnNext(350, 7),
                OnNext(390, 4),
                OnNext(410, 17),
                OnNext(450, 8),
                OnNext(500, 23),
                OnCompleted<int>(600)
                );

            var invoked = 0;

            var results = scheduler.Run(() => xs.SkipWhile(x =>
            {
                invoked++;
                return IsPrime(x);
            }));

            results.AssertEqual(
                OnNext(390, 4),
                OnNext(410, 17),
                OnNext(450, 8),
                OnNext(500, 23),
                OnCompleted<int>(600)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );

            Assert.AreEqual(6, invoked);
        }

        [TestMethod]
        public void SkipWhile_Error_Before()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, -1),
                OnNext(110, -1),
                OnNext(210, 2),
                OnNext(260, 5),
                OnError<int>(270, new MockException(4)),
                OnNext(290, 13),
                OnNext(320, 3),
                OnNext(350, 7),
                OnNext(390, 4),
                OnNext(410, 17),
                OnNext(450, 8),
                OnNext(500, 23)
                );

            var invoked = 0;

            var results = scheduler.Run(() => xs.SkipWhile(x =>
            {
                invoked++;
                return IsPrime(x);
            }));

            results.AssertEqual(
                OnError<int>(270, new MockException(4))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 270)
                );

            Assert.AreEqual(2, invoked);
        }

        [TestMethod]
        public void SkipWhile_Error_After()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, -1),
                OnNext(110, -1),
                OnNext(210, 2),
                OnNext(260, 5),
                OnNext(290, 13),
                OnNext(320, 3),
                OnNext(350, 7),
                OnNext(390, 4),
                OnNext(410, 17),
                OnNext(450, 8),
                OnNext(500, 23),
                OnError<int>(600, new MockException(4))
                );

            var invoked = 0;

            var results = scheduler.Run(() => xs.SkipWhile(x =>
            {
                invoked++;
                return IsPrime(x);
            }));

            results.AssertEqual(
                OnNext(390, 4),
                OnNext(410, 17),
                OnNext(450, 8),
                OnNext(500, 23),
                OnError<int>(600, new MockException(4))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );

            Assert.AreEqual(6, invoked);
        }

        [TestMethod]
        public void SkipWhile_Dispose_Before()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, -1),
                OnNext(110, -1),
                OnNext(210, 2),
                OnNext(260, 5),
                OnNext(290, 13),
                OnNext(320, 3),
                OnNext(350, 7),
                OnNext(390, 4),
                OnNext(410, 17),
                OnNext(450, 8),
                OnNext(500, 23),
                OnCompleted<int>(600)
                );

            var invoked = 0;

            var results = scheduler.Run(() => xs.SkipWhile(x =>
            {
                invoked++;
                return IsPrime(x);
            }), 300);

            results.AssertEqual(
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 300)
                );

            Assert.AreEqual(3, invoked);
        }

        [TestMethod]
        public void SkipWhile_Dispose_After()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, -1),
                OnNext(110, -1),
                OnNext(210, 2),
                OnNext(260, 5),
                OnNext(290, 13),
                OnNext(320, 3),
                OnNext(350, 7),
                OnNext(390, 4),
                OnNext(410, 17),
                OnNext(450, 8),
                OnNext(500, 23),
                OnCompleted<int>(600)
                );

            var invoked = 0;

            var results = scheduler.Run(() => xs.SkipWhile(x =>
            {
                invoked++;
                return IsPrime(x);
            }), 470);

            results.AssertEqual(
                OnNext(390, 4),
                OnNext(410, 17),
                OnNext(450, 8)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 470)
                );

            Assert.AreEqual(6, invoked);
        }

        [TestMethod]
        public void SkipWhile_Zero()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, -1),
                OnNext(110, -1),
                OnNext(205, 100),
                OnNext(210, 2),
                OnNext(260, 5),
                OnNext(290, 13),
                OnNext(320, 3),
                OnNext(350, 7),
                OnNext(390, 4),
                OnNext(410, 17),
                OnNext(450, 8),
                OnNext(500, 23),
                OnCompleted<int>(600)
                );

            var invoked = 0;

            var results = scheduler.Run(() => xs.SkipWhile(x =>
            {
                invoked++;
                return IsPrime(x);
            }));

            results.AssertEqual(
                OnNext(205, 100),
                OnNext(210, 2),
                OnNext(260, 5),
                OnNext(290, 13),
                OnNext(320, 3),
                OnNext(350, 7),
                OnNext(390, 4),
                OnNext(410, 17),
                OnNext(450, 8),
                OnNext(500, 23),
                OnCompleted<int>(600)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );

            Assert.AreEqual(1, invoked);
        }

        [TestMethod]
        public void SkipWhile_Throw()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, -1),
                OnNext(110, -1),
                OnNext(210, 2),
                OnNext(260, 5),
                OnNext(290, 13),
                OnNext(320, 3),
                OnNext(350, 7),
                OnNext(390, 4),
                OnNext(410, 17),
                OnNext(450, 8),
                OnNext(500, 23),
                OnCompleted<int>(600)
                );

            var invoked = 0;

            var results = scheduler.Run(() => xs.SkipWhile(x =>
            {
                invoked++;
                if (invoked == 3)
                    throw new MockException(3);
                return IsPrime(x);
            }));

            results.AssertEqual(
                OnError<int>(290, new MockException(3))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 290)
                );

            Assert.AreEqual(3, invoked);
        }

        [TestMethod]
        public void SelectMany_Then_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => ((IObservable<int>)null).SelectMany(DummyObservable<string>.Instance));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.SelectMany(((IObservable<string>)null)));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.SelectMany(DummyObservable<string>.Instance).Subscribe(null));
            Throws<ArgumentNullException>(() => NullErrorObservable<int>.Instance.SelectMany(DummyObservable<string>.Instance).Subscribe(DummyObserver<string>.Instance));
        }

        [TestMethod]
        public void SelectMany_Then_Complete_Complete()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(100, 4),
                OnNext(200, 2),
                OnNext(300, 3),
                OnNext(400, 1),
                OnCompleted<int>(500)
                );

            var ys = scheduler.CreateColdObservable(
                OnNext(50, "foo"),
                OnNext(100, "bar"),
                OnNext(150, "baz"),
                OnNext(200, "qux"),
                OnCompleted<string>(250)
                );

            var results = scheduler.Run(() => xs.SelectMany(ys));

            results.AssertEqual(
                OnNext(350, "foo"),
                OnNext(400, "bar"),
                OnNext(450, "baz"),
                OnNext(450, "foo"),
                OnNext(500, "qux"),
                OnNext(500, "bar"),
                OnNext(550, "baz"),
                OnNext(550, "foo"),
                OnNext(600, "qux"),
                OnNext(600, "bar"),
                OnNext(650, "baz"),
                OnNext(650, "foo"),
                OnNext(700, "qux"),
                OnNext(700, "bar"),
                OnNext(750, "baz"),
                OnNext(800, "qux"),
                OnCompleted<string>(850)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 700)
                );

            ys.Subscriptions.AssertEqual(
                Subscribe(300, 550),
                Subscribe(400, 650),
                Subscribe(500, 750),
                Subscribe(600, 850)
                );
        }

        [TestMethod]
        public void SelectMany_Then_Complete_Complete_2()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(100, 4),
                OnNext(200, 2),
                OnNext(300, 3),
                OnNext(400, 1),
                OnCompleted<int>(700)
                );

            var ys = scheduler.CreateColdObservable(
                OnNext(50, "foo"),
                OnNext(100, "bar"),
                OnNext(150, "baz"),
                OnNext(200, "qux"),
                OnCompleted<string>(250)
                );

            var results = scheduler.Run(() => xs.SelectMany(ys));

            results.AssertEqual(
                OnNext(350, "foo"),
                OnNext(400, "bar"),
                OnNext(450, "baz"),
                OnNext(450, "foo"),
                OnNext(500, "qux"),
                OnNext(500, "bar"),
                OnNext(550, "baz"),
                OnNext(550, "foo"),
                OnNext(600, "qux"),
                OnNext(600, "bar"),
                OnNext(650, "baz"),
                OnNext(650, "foo"),
                OnNext(700, "qux"),
                OnNext(700, "bar"),
                OnNext(750, "baz"),
                OnNext(800, "qux"),
                OnCompleted<string>(900)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 900)
                );

            ys.Subscriptions.AssertEqual(
                Subscribe(300, 550),
                Subscribe(400, 650),
                Subscribe(500, 750),
                Subscribe(600, 850)
                );
        }

        [TestMethod]
        public void SelectMany_Then_Never_Complete()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(100, 4),
                OnNext(200, 2),
                OnNext(300, 3),
                OnNext(400, 1),
                OnNext(500, 5),
                OnNext(700, 0)
                );

            var ys = scheduler.CreateColdObservable(
                OnNext(50, "foo"),
                OnNext(100, "bar"),
                OnNext(150, "baz"),
                OnNext(200, "qux"),
                OnCompleted<string>(250)
                );

            var results = scheduler.Run(() => xs.SelectMany(ys));

            results.AssertEqual(
                OnNext(350, "foo"),
                OnNext(400, "bar"),
                OnNext(450, "baz"),
                OnNext(450, "foo"),
                OnNext(500, "qux"),
                OnNext(500, "bar"),
                OnNext(550, "baz"),
                OnNext(550, "foo"),
                OnNext(600, "qux"),
                OnNext(600, "bar"),
                OnNext(650, "baz"),
                OnNext(650, "foo"),
                OnNext(700, "qux"),
                OnNext(700, "bar"),
                OnNext(750, "baz"),
                OnNext(750, "foo"),
                OnNext(800, "qux"),
                OnNext(800, "bar"),
                OnNext(850, "baz"),
                OnNext(900, "qux"),
                OnNext(950, "foo")
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 1000)
                );

            ys.Subscriptions.AssertEqual(
                Subscribe(300, 550),
                Subscribe(400, 650),
                Subscribe(500, 750),
                Subscribe(600, 850),
                Subscribe(700, 950),
                Subscribe(900, 1000)
                );
        }

        [TestMethod]
        public void SelectMany_Then_Complete_Never()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(100, 4),
                OnNext(200, 2),
                OnNext(300, 3),
                OnNext(400, 1),
                OnCompleted<int>(500)
                );

            var ys = scheduler.CreateColdObservable(
                OnNext(50, "foo"),
                OnNext(100, "bar"),
                OnNext(150, "baz"),
                OnNext(200, "qux")
                );

            var results = scheduler.Run(() => xs.SelectMany(ys));

            results.AssertEqual(
                OnNext(350, "foo"),
                OnNext(400, "bar"),
                OnNext(450, "baz"),
                OnNext(450, "foo"),
                OnNext(500, "qux"),
                OnNext(500, "bar"),
                OnNext(550, "baz"),
                OnNext(550, "foo"),
                OnNext(600, "qux"),
                OnNext(600, "bar"),
                OnNext(650, "baz"),
                OnNext(650, "foo"),
                OnNext(700, "qux"),
                OnNext(700, "bar"),
                OnNext(750, "baz"),
                OnNext(800, "qux")
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 700)
                );

            ys.Subscriptions.AssertEqual(
                Subscribe(300, 1000),
                Subscribe(400, 1000),
                Subscribe(500, 1000),
                Subscribe(600, 1000)
                );
        }

        [TestMethod]
        public void SelectMany_Then_Complete_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(100, 4),
                OnNext(200, 2),
                OnNext(300, 3),
                OnNext(400, 1),
                OnCompleted<int>(500)
                );

            var ys = scheduler.CreateColdObservable(
                OnNext(50, "foo"),
                OnNext(100, "bar"),
                OnNext(150, "baz"),
                OnNext(200, "qux"),
                OnError<string>(300, new MockException(42))
                );

            var results = scheduler.Run(() => xs.SelectMany(ys));

            results.AssertEqual(
                OnNext(350, "foo"),
                OnNext(400, "bar"),
                OnNext(450, "baz"),
                OnNext(450, "foo"),
                OnNext(500, "qux"),
                OnNext(500, "bar"),
                OnNext(550, "baz"),
                OnNext(550, "foo"),
                OnError<string>(600, new MockException(42))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );

            ys.Subscriptions.AssertEqual(
                Subscribe(300, 600),
                Subscribe(400, 600),
                Subscribe(500, 600),
                Subscribe(600, 600)
                );
        }

        [TestMethod]
        public void SelectMany_Then_Error_Complete()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(100, 4),
                OnNext(200, 2),
                OnNext(300, 3),
                OnNext(400, 1),
                OnError<int>(500, new MockException(2))
                );

            var ys = scheduler.CreateColdObservable(
                OnNext(50, "foo"),
                OnNext(100, "bar"),
                OnNext(150, "baz"),
                OnNext(200, "qux"),
                OnCompleted<string>(250)
                );

            var results = scheduler.Run(() => xs.SelectMany(ys));

            results.AssertEqual(
                OnNext(350, "foo"),
                OnNext(400, "bar"),
                OnNext(450, "baz"),
                OnNext(450, "foo"),
                OnNext(500, "qux"),
                OnNext(500, "bar"),
                OnNext(550, "baz"),
                OnNext(550, "foo"),
                OnNext(600, "qux"),
                OnNext(600, "bar"),
                OnNext(650, "baz"),
                OnNext(650, "foo"),
                OnError<string>(700, new MockException(2))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 700)
                );

            ys.Subscriptions.AssertEqual(
                Subscribe(300, 550),
                Subscribe(400, 650),
                Subscribe(500, 700),
                Subscribe(600, 700)
                );
        }

        [TestMethod]
        public void SelectMany_Then_Error_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(100, 4),
                OnNext(200, 2),
                OnNext(300, 3),
                OnNext(400, 1),
                OnError<int>(500, new MockException(2))
                );

            var ys = scheduler.CreateColdObservable(
                OnNext(50, "foo"),
                OnNext(100, "bar"),
                OnNext(150, "baz"),
                OnNext(200, "qux"),
                OnError<string>(250, new MockException(3))
                );

            var results = scheduler.Run(() => xs.SelectMany(ys));

            results.AssertEqual(
                OnNext(350, "foo"),
                OnNext(400, "bar"),
                OnNext(450, "baz"),
                OnNext(450, "foo"),
                OnNext(500, "qux"),
                OnNext(500, "bar"),
                OnError<string>(550, new MockException(3))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 550)
                );

            ys.Subscriptions.AssertEqual(
                Subscribe(300, 550),
                Subscribe(400, 550),
                Subscribe(500, 550)
                );
        }

        [TestMethod]
        public void SelectMany_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => ((IObservable<int>)null).SelectMany<int, int>(DummyFunc<int, IObservable<int>>.Instance));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.SelectMany((Func<int, IObservable<int>>)null));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.SelectMany(DummyFunc<int, IObservable<int>>.Instance).Subscribe(null));
            Throws<ArgumentNullException>(() => NullErrorObservable<int>.Instance.SelectMany(DummyFunc<int, IObservable<int>>.Instance).Subscribe(DummyObserver<int>.Instance));
            Throws<ArgumentNullException>(() => new MockObservable<int>(new Notification<int>.OnNext(default(int))).SelectMany(x => (IObservable<int>)null).Subscribe(DummyObserver<int>.Instance));
        }

        [TestMethod]
        public void SelectMany_Complete()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                    OnNext(5, scheduler.CreateColdObservable(
                        OnError<int>(1, new InvalidOperationException()))),
                    OnNext(105, scheduler.CreateColdObservable(
                        OnError<int>(1, new InvalidOperationException()))),
                    OnNext(300, scheduler.CreateColdObservable(
                        OnNext(10, 102),
                        OnNext(90, 103),
                        OnNext(110, 104),
                        OnNext(190, 105),
                        OnNext(440, 106),
                        OnCompleted<int>(460))),
                    OnNext(400, scheduler.CreateColdObservable(
                        OnNext(180, 202),
                        OnNext(190, 203),
                        OnCompleted<int>(205))),
                    OnNext(550, scheduler.CreateColdObservable(
                        OnNext(10, 301),
                        OnNext(50, 302),
                        OnNext(70, 303),
                        OnNext(260, 304),
                        OnNext(310, 305),
                        OnCompleted<int>(410))),
                    OnNext(750, scheduler.CreateColdObservable(
                        OnCompleted<int>(40))),
                    OnNext(850, scheduler.CreateColdObservable(
                        OnNext(80, 401),
                        OnNext(90, 402),
                        OnCompleted<int>(100))),
                    OnCompleted<ColdObservable<int>>(900)
                );

            var results = scheduler.Run(() => xs.SelectMany(x => x));

            results.AssertEqual(
                OnNext(310, 102),
                OnNext(390, 103),
                OnNext(410, 104),
                OnNext(490, 105),
                OnNext(560, 301),
                OnNext(580, 202),
                OnNext(590, 203),
                OnNext(600, 302),
                OnNext(620, 303),
                OnNext(740, 106),
                OnNext(810, 304),
                OnNext(860, 305),
                OnNext(930, 401),
                OnNext(940, 402),
                OnCompleted<int>(960)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 900));

            xs.Messages[2].Value.Value.Subscriptions.AssertEqual(
                Subscribe(300, 760));

            xs.Messages[3].Value.Value.Subscriptions.AssertEqual(
                Subscribe(400, 605));

            xs.Messages[4].Value.Value.Subscriptions.AssertEqual(
                Subscribe(550, 960));

            xs.Messages[5].Value.Value.Subscriptions.AssertEqual(
                Subscribe(750, 790));

            xs.Messages[6].Value.Value.Subscriptions.AssertEqual(
                Subscribe(850, 950));
        }

        [TestMethod]
        public void SelectMany_Complete_InnerNotComplete()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                    OnNext(5, scheduler.CreateColdObservable(
                        OnError<int>(1, new InvalidOperationException()))),
                    OnNext(105, scheduler.CreateColdObservable(
                        OnError<int>(1, new InvalidOperationException()))),
                    OnNext(300, scheduler.CreateColdObservable(
                        OnNext(10, 102),
                        OnNext(90, 103),
                        OnNext(110, 104),
                        OnNext(190, 105),
                        OnNext(440, 106),
                        OnCompleted<int>(460))),
                    OnNext(400, scheduler.CreateColdObservable(
                        OnNext(180, 202),
                        OnNext(190, 203))),
                    OnNext(550, scheduler.CreateColdObservable(
                        OnNext(10, 301),
                        OnNext(50, 302),
                        OnNext(70, 303),
                        OnNext(260, 304),
                        OnNext(310, 305),
                        OnCompleted<int>(410))),
                    OnNext(750, scheduler.CreateColdObservable(
                        OnCompleted<int>(40))),
                    OnNext(850, scheduler.CreateColdObservable(
                        OnNext(80, 401),
                        OnNext(90, 402),
                        OnCompleted<int>(100))),
                    OnCompleted<ColdObservable<int>>(900)
                );

            var results = scheduler.Run(() => xs.SelectMany(x => x));

            results.AssertEqual(
                OnNext(310, 102),
                OnNext(390, 103),
                OnNext(410, 104),
                OnNext(490, 105),
                OnNext(560, 301),
                OnNext(580, 202),
                OnNext(590, 203),
                OnNext(600, 302),
                OnNext(620, 303),
                OnNext(740, 106),
                OnNext(810, 304),
                OnNext(860, 305),
                OnNext(930, 401),
                OnNext(940, 402)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 900));

            xs.Messages[2].Value.Value.Subscriptions.AssertEqual(
                Subscribe(300, 760));

            xs.Messages[3].Value.Value.Subscriptions.AssertEqual(
                Subscribe(400, 1000));

            xs.Messages[4].Value.Value.Subscriptions.AssertEqual(
                Subscribe(550, 960));

            xs.Messages[5].Value.Value.Subscriptions.AssertEqual(
                Subscribe(750, 790));

            xs.Messages[6].Value.Value.Subscriptions.AssertEqual(
                Subscribe(850, 950));
        }

        [TestMethod]
        public void SelectMany_Complete_OuterNotComplete()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                    OnNext(5, scheduler.CreateColdObservable(
                        OnError<int>(1, new InvalidOperationException()))),
                    OnNext(105, scheduler.CreateColdObservable(
                        OnError<int>(1, new InvalidOperationException()))),
                    OnNext(300, scheduler.CreateColdObservable(
                        OnNext(10, 102),
                        OnNext(90, 103),
                        OnNext(110, 104),
                        OnNext(190, 105),
                        OnNext(440, 106),
                        OnCompleted<int>(460))),
                    OnNext(400, scheduler.CreateColdObservable(
                        OnNext(180, 202),
                        OnNext(190, 203),
                        OnCompleted<int>(205))),
                    OnNext(550, scheduler.CreateColdObservable(
                        OnNext(10, 301),
                        OnNext(50, 302),
                        OnNext(70, 303),
                        OnNext(260, 304),
                        OnNext(310, 305),
                        OnCompleted<int>(410))),
                    OnNext(750, scheduler.CreateColdObservable(
                        OnCompleted<int>(40))),
                    OnNext(850, scheduler.CreateColdObservable(
                        OnNext(80, 401),
                        OnNext(90, 402),
                        OnCompleted<int>(100)))
                );

            var results = scheduler.Run(() => xs.SelectMany(x => x));

            results.AssertEqual(
                OnNext(310, 102),
                OnNext(390, 103),
                OnNext(410, 104),
                OnNext(490, 105),
                OnNext(560, 301),
                OnNext(580, 202),
                OnNext(590, 203),
                OnNext(600, 302),
                OnNext(620, 303),
                OnNext(740, 106),
                OnNext(810, 304),
                OnNext(860, 305),
                OnNext(930, 401),
                OnNext(940, 402)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 1000));

            xs.Messages[2].Value.Value.Subscriptions.AssertEqual(
                Subscribe(300, 760));

            xs.Messages[3].Value.Value.Subscriptions.AssertEqual(
                Subscribe(400, 605));

            xs.Messages[4].Value.Value.Subscriptions.AssertEqual(
                Subscribe(550, 960));

            xs.Messages[5].Value.Value.Subscriptions.AssertEqual(
                Subscribe(750, 790));

            xs.Messages[6].Value.Value.Subscriptions.AssertEqual(
                Subscribe(850, 950));
        }

        [TestMethod]
        public void SelectMany_Error_Outer()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                    OnNext(5, scheduler.CreateColdObservable(
                        OnError<int>(1, new InvalidOperationException()))),
                    OnNext(105, scheduler.CreateColdObservable(
                        OnError<int>(1, new InvalidOperationException()))),
                    OnNext(300, scheduler.CreateColdObservable(
                        OnNext(10, 102),
                        OnNext(90, 103),
                        OnNext(110, 104),
                        OnNext(190, 105),
                        OnNext(440, 106),
                        OnCompleted<int>(460))),
                    OnNext(400, scheduler.CreateColdObservable(
                        OnNext(180, 202),
                        OnNext(190, 203),
                        OnCompleted<int>(205))),
                    OnNext(550, scheduler.CreateColdObservable(
                        OnNext(10, 301),
                        OnNext(50, 302),
                        OnNext(70, 303),
                        OnNext(260, 304),
                        OnNext(310, 305),
                        OnCompleted<int>(410))),
                    OnNext(750, scheduler.CreateColdObservable(
                        OnCompleted<int>(40))),
                    OnNext(850, scheduler.CreateColdObservable(
                        OnNext(80, 401),
                        OnNext(90, 402),
                        OnCompleted<int>(100))),
                    OnError<ColdObservable<int>>(900, new MockException(1))
                );

            var results = scheduler.Run(() => xs.SelectMany(x => x));

            results.AssertEqual(
                OnNext(310, 102),
                OnNext(390, 103),
                OnNext(410, 104),
                OnNext(490, 105),
                OnNext(560, 301),
                OnNext(580, 202),
                OnNext(590, 203),
                OnNext(600, 302),
                OnNext(620, 303),
                OnNext(740, 106),
                OnNext(810, 304),
                OnNext(860, 305),
                OnError<int>(900, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 900));

            xs.Messages[2].Value.Value.Subscriptions.AssertEqual(
                Subscribe(300, 760));

            xs.Messages[3].Value.Value.Subscriptions.AssertEqual(
                Subscribe(400, 605));

            xs.Messages[4].Value.Value.Subscriptions.AssertEqual(
                Subscribe(550, 900));

            xs.Messages[5].Value.Value.Subscriptions.AssertEqual(
                Subscribe(750, 790));

            xs.Messages[6].Value.Value.Subscriptions.AssertEqual(
                Subscribe(850, 900));
        }

        [TestMethod]
        public void SelectMany_Error_Inner()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                    OnNext(5, scheduler.CreateColdObservable(
                        OnError<int>(1, new InvalidOperationException()))),
                    OnNext(105, scheduler.CreateColdObservable(
                        OnError<int>(1, new InvalidOperationException()))),
                    OnNext(300, scheduler.CreateColdObservable(
                        OnNext(10, 102),
                        OnNext(90, 103),
                        OnNext(110, 104),
                        OnNext(190, 105),
                        OnNext(440, 106),
                        OnError<int>(460, new MockException(0)))),
                    OnNext(400, scheduler.CreateColdObservable(
                        OnNext(180, 202),
                        OnNext(190, 203),
                        OnCompleted<int>(205))),
                    OnNext(550, scheduler.CreateColdObservable(
                        OnNext(10, 301),
                        OnNext(50, 302),
                        OnNext(70, 303),
                        OnNext(260, 304),
                        OnNext(310, 305),
                        OnCompleted<int>(410))),
                    OnNext(750, scheduler.CreateColdObservable(
                        OnCompleted<int>(40))),
                    OnNext(850, scheduler.CreateColdObservable(
                        OnNext(80, 401),
                        OnNext(90, 402),
                        OnCompleted<int>(100))),
                    OnCompleted<ColdObservable<int>>(900)
                );

            var results = scheduler.Run(() => xs.SelectMany(x => x));

            results.AssertEqual(
                OnNext(310, 102),
                OnNext(390, 103),
                OnNext(410, 104),
                OnNext(490, 105),
                OnNext(560, 301),
                OnNext(580, 202),
                OnNext(590, 203),
                OnNext(600, 302),
                OnNext(620, 303),
                OnNext(740, 106),
                OnError<int>(760, new MockException(0))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 760));

            xs.Messages[2].Value.Value.Subscriptions.AssertEqual(
                Subscribe(300, 760));

            xs.Messages[3].Value.Value.Subscriptions.AssertEqual(
                Subscribe(400, 605));

            xs.Messages[4].Value.Value.Subscriptions.AssertEqual(
                Subscribe(550, 760));

            xs.Messages[5].Value.Value.Subscriptions.AssertEqual(
                Subscribe(750, 760));

            xs.Messages[6].Value.Value.Subscriptions.AssertEqual(
                );
        }

        [TestMethod]
        public void SelectMany_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                    OnNext(5, scheduler.CreateColdObservable(
                        OnError<int>(1, new InvalidOperationException()))),
                    OnNext(105, scheduler.CreateColdObservable(
                        OnError<int>(1, new InvalidOperationException()))),
                    OnNext(300, scheduler.CreateColdObservable(
                        OnNext(10, 102),
                        OnNext(90, 103),
                        OnNext(110, 104),
                        OnNext(190, 105),
                        OnNext(440, 106),
                        OnCompleted<int>(460))),
                    OnNext(400, scheduler.CreateColdObservable(
                        OnNext(180, 202),
                        OnNext(190, 203),
                        OnCompleted<int>(205))),
                    OnNext(550, scheduler.CreateColdObservable(
                        OnNext(10, 301),
                        OnNext(50, 302),
                        OnNext(70, 303),
                        OnNext(260, 304),
                        OnNext(310, 305),
                        OnCompleted<int>(410))),
                    OnNext(750, scheduler.CreateColdObservable(
                        OnCompleted<int>(40))),
                    OnNext(850, scheduler.CreateColdObservable(
                        OnNext(80, 401),
                        OnNext(90, 402),
                        OnCompleted<int>(100))),
                    OnCompleted<ColdObservable<int>>(900)
                );

            var results = scheduler.Run(() => xs.SelectMany(x => x), 700);

            results.AssertEqual(
                OnNext(310, 102),
                OnNext(390, 103),
                OnNext(410, 104),
                OnNext(490, 105),
                OnNext(560, 301),
                OnNext(580, 202),
                OnNext(590, 203),
                OnNext(600, 302),
                OnNext(620, 303)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 700));

            xs.Messages[2].Value.Value.Subscriptions.AssertEqual(
                Subscribe(300, 700));

            xs.Messages[3].Value.Value.Subscriptions.AssertEqual(
                Subscribe(400, 605));

            xs.Messages[4].Value.Value.Subscriptions.AssertEqual(
                Subscribe(550, 700));

            xs.Messages[5].Value.Value.Subscriptions.AssertEqual(
                );

            xs.Messages[6].Value.Value.Subscriptions.AssertEqual(
                );
        }

        [TestMethod]
        public void SelectMany_Throw()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                    OnNext(5, scheduler.CreateColdObservable(
                        OnError<int>(1, new InvalidOperationException()))),
                    OnNext(105, scheduler.CreateColdObservable(
                        OnError<int>(1, new InvalidOperationException()))),
                    OnNext(300, scheduler.CreateColdObservable(
                        OnNext(10, 102),
                        OnNext(90, 103),
                        OnNext(110, 104),
                        OnNext(190, 105),
                        OnNext(440, 106),
                        OnCompleted<int>(460))),
                    OnNext(400, scheduler.CreateColdObservable(
                        OnNext(180, 202),
                        OnNext(190, 203),
                        OnCompleted<int>(205))),
                    OnNext(550, scheduler.CreateColdObservable(
                        OnNext(10, 301),
                        OnNext(50, 302),
                        OnNext(70, 303),
                        OnNext(260, 304),
                        OnNext(310, 305),
                        OnCompleted<int>(410))),
                    OnNext(750, scheduler.CreateColdObservable(
                        OnCompleted<int>(40))),
                    OnNext(850, scheduler.CreateColdObservable(
                        OnNext(80, 401),
                        OnNext(90, 402),
                        OnCompleted<int>(100))),
                    OnCompleted<ColdObservable<int>>(900)
                );

            var invoked = 0;

            var results = scheduler.Run(() => xs.SelectMany(x =>
                {
                    invoked++;
                    if (invoked == 3)
                        throw new MockException(3);
                    return x;
                }));

            results.AssertEqual(
                OnNext(310, 102),
                OnNext(390, 103),
                OnNext(410, 104),
                OnNext(490, 105),
                OnError<int>(550, new MockException(3))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 550));

            xs.Messages[2].Value.Value.Subscriptions.AssertEqual(
                Subscribe(300, 550));

            xs.Messages[3].Value.Value.Subscriptions.AssertEqual(
                Subscribe(400, 550));

            xs.Messages[4].Value.Value.Subscriptions.AssertEqual(
                );

            xs.Messages[5].Value.Value.Subscriptions.AssertEqual(
                );

            xs.Messages[6].Value.Value.Subscriptions.AssertEqual(
                );

            Assert.AreEqual(3, invoked);
        }

        [TestMethod]
        public void SelectMany_UseFunction()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(210, 4),
                OnNext(220, 3),
                OnNext(250, 5),
                OnNext(270, 1),
                OnCompleted<int>(290)
                );

            var results = scheduler.Run(() => xs.SelectMany(x => Observable.Interval(TimeSpan.FromTicks(10), scheduler).Select(_ => x).Take(x)));

            results.AssertEqual(
                OnNext(220, 4),
                OnNext(230, 3),
                OnNext(230, 4),
                OnNext(240, 3),
                OnNext(240, 4),
                OnNext(250, 3),
                OnNext(250, 4),
                OnNext(260, 5),
                OnNext(270, 5),
                OnNext(280, 1),
                OnNext(280, 5),
                OnNext(290, 5),
                OnNext(300, 5),
                OnCompleted<int>(300)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 290)
                );
        }

        [TestMethod]
        public void Cast_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => ((IObservable<object>)null).Cast<bool>());
            Throws<ArgumentNullException>(() => DummyObservable<object>.Instance.Cast<bool>().Subscribe(null));
            Throws<ArgumentNullException>(() => NullErrorObservable<object>.Instance.Cast<bool>().Subscribe());
        }

        class A : IEquatable<A>
        {
            int id;

            public A(int id)
            {
                this.id = id;
            }

            public bool Equals(A other)
            {
                if (other == null)
                    return false;
                return id == other.id && GetType().Equals(other.GetType());
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as A);
            }

            public override int GetHashCode()
            {
                return id;
            }
        }

        class B : A
        {
            public B(int id)
                : base(id)
            {
            }
        }

        class C : A
        {
            public C(int id)
                : base(id)
            {
            }
        }

        class D : B
        {
            public D(int id)
                : base(id)
            {
            }
        }

        class E : IEquatable<E>
        {
            int id;

            public E(int id)
            {
                this.id = id;
            }

            public bool Equals(E other)
            {
                if (other == null)
                    return false;
                return id == other.id && GetType().Equals(other.GetType());
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as E);
            }

            public override int GetHashCode()
            {
                return id;
            }
        }

        [TestMethod]
        public void Cast_Complete()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<object>(
                OnNext<object>(210, new B(0)),
                OnNext<object>(220, new D(1)),
                OnNext<object>(240, new B(2)),
                OnNext<object>(270, new D(3)),
                OnCompleted<object>(300)
                );

            var results = scheduler.Run(() => xs.Cast<B>());

            results.AssertEqual(
                OnNext<B>(210, new B(0)),
                OnNext<B>(220, new D(1)),
                OnNext<B>(240, new B(2)),
                OnNext<B>(270, new D(3)),
                OnCompleted<B>(300)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 300)
                );
        }

        [TestMethod]
        public void Cast_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<object>(
                OnNext<object>(210, new B(0)),
                OnNext<object>(220, new D(1)),
                OnNext<object>(240, new B(2)),
                OnNext<object>(270, new D(3)),
                OnError<object>(300, new MockException(4))
                );

            var results = scheduler.Run(() => xs.Cast<B>());

            results.AssertEqual(
                OnNext<B>(210, new B(0)),
                OnNext<B>(220, new D(1)),
                OnNext<B>(240, new B(2)),
                OnNext<B>(270, new D(3)),
                OnError<B>(300, new MockException(4))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 300)
                );
        }

        [TestMethod]
        public void Cast_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<object>(
                OnNext<object>(210, new B(0)),
                OnNext<object>(220, new D(1)),
                OnNext<object>(240, new B(2)),
                OnNext<object>(270, new D(3)),
                OnCompleted<object>(300)
                );

            var results = scheduler.Run(() => xs.Cast<B>(), 250);

            results.AssertEqual(
                OnNext<B>(210, new B(0)),
                OnNext<B>(220, new D(1)),
                OnNext<B>(240, new B(2))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 250)
                );
        }

        [TestMethod]
        public void Cast_NotValid()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<object>(
                OnNext<object>(210, new B(0)),
                OnNext<object>(220, new D(1)),
                OnNext<object>(240, new B(2)),
                OnNext<object>(250, new A(-1)),
                OnNext<object>(270, new D(3)),
                OnCompleted<object>(300)
                );

            var results = (MockObserver<B>)scheduler.Run(() => xs.Cast<B>());

            results.Take(3).AssertEqual(
                OnNext<B>(210, new B(0)),
                OnNext<B>(220, new D(1)),
                OnNext<B>(240, new B(2))
                );

            Assert.AreEqual(4, results.Count);

            Assert.AreEqual(NotificationKind.OnError, results[3].Value.Kind);

            Assert.AreEqual(250, results[3].Time);

            Assert.AreEqual(typeof(InvalidCastException), results[3].Value.Exception.GetType());

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 250)
                );
        }

        [TestMethod]
        public void OfType_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => ((IObservable<object>)null).OfType<bool>());
            Throws<ArgumentNullException>(() => DummyObservable<object>.Instance.OfType<bool>().Subscribe(null));
            Throws<ArgumentNullException>(() => NullErrorObservable<object>.Instance.OfType<bool>().Subscribe());
        }

        [TestMethod]
        public void OfType_Complete()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<object>(
                OnNext<object>(210, new B(0)),
                OnNext<object>(220, new A(1)),
                OnNext<object>(230, new E(2)),
                OnNext<object>(240, new D(3)),
                OnNext<object>(250, new C(4)),
                OnNext<object>(260, new B(5)),
                OnNext<object>(270, new B(6)),
                OnNext<object>(280, new D(7)),
                OnNext<object>(290, new A(8)),
                OnNext<object>(300, new E(9)),
                OnNext<object>(310, 3),
                OnNext<object>(320, "foo"),
                OnNext<object>(330, true),
                OnNext<object>(340, new B(10)),
                OnCompleted<object>(350)
                );

            var results = scheduler.Run(() => xs.OfType<B>());

            results.AssertEqual(
                OnNext<B>(210, new B(0)),
                OnNext<B>(240, new D(3)),
                OnNext<B>(260, new B(5)),
                OnNext<B>(270, new B(6)),
                OnNext<B>(280, new D(7)),
                OnNext<B>(340, new B(10)),
                OnCompleted<B>(350)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 350)
                );
        }

        [TestMethod]
        public void OfType_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<object>(
                OnNext<object>(210, new B(0)),
                OnNext<object>(220, new A(1)),
                OnNext<object>(230, new E(2)),
                OnNext<object>(240, new D(3)),
                OnNext<object>(250, new C(4)),
                OnNext<object>(260, new B(5)),
                OnNext<object>(270, new B(6)),
                OnNext<object>(280, new D(7)),
                OnNext<object>(290, new A(8)),
                OnNext<object>(300, new E(9)),
                OnNext<object>(310, 3),
                OnNext<object>(320, "foo"),
                OnNext<object>(330, true),
                OnNext<object>(340, new B(10)),
                OnError<object>(350, new MockException(200))
                );

            var results = scheduler.Run(() => xs.OfType<B>());

            results.AssertEqual(
                OnNext<B>(210, new B(0)),
                OnNext<B>(240, new D(3)),
                OnNext<B>(260, new B(5)),
                OnNext<B>(270, new B(6)),
                OnNext<B>(280, new D(7)),
                OnNext<B>(340, new B(10)),
                OnError<B>(350, new MockException(200))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 350)
                );
        }

        [TestMethod]
        public void OfType_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<object>(
                OnNext<object>(210, new B(0)),
                OnNext<object>(220, new A(1)),
                OnNext<object>(230, new E(2)),
                OnNext<object>(240, new D(3)),
                OnNext<object>(250, new C(4)),
                OnNext<object>(260, new B(5)),
                OnNext<object>(270, new B(6)),
                OnNext<object>(280, new D(7)),
                OnNext<object>(290, new A(8)),
                OnNext<object>(300, new E(9)),
                OnNext<object>(310, 3),
                OnNext<object>(320, "foo"),
                OnNext<object>(330, true),
                OnNext<object>(340, new B(10)),
                OnError<object>(350, new MockException(200))
                );

            var results = scheduler.Run(() => xs.OfType<B>(), 275);

            results.AssertEqual(
                OnNext<B>(210, new B(0)),
                OnNext<B>(240, new D(3)),
                OnNext<B>(260, new B(5)),
                OnNext<B>(270, new B(6))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 275)
                );
        }

        [TestMethod]
        public void SelectMany_Enumerable_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => ((IObservable<int>)null).SelectMany<int, int>(DummyFunc<int, IEnumerable<int>>.Instance));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.SelectMany((Func<int, IEnumerable<int>>)null));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.SelectMany(DummyFunc<int, IEnumerable<int>>.Instance).Subscribe(null));
            Throws<ArgumentNullException>(() => NullErrorObservable<int>.Instance.SelectMany(DummyFunc<int, IEnumerable<int>>.Instance).Subscribe(DummyObserver<int>.Instance));
            Throws<NullReferenceException>(() => new MockObservable<int>(new Notification<int>.OnNext(default(int))).SelectMany(x => (IEnumerable<int>)null).Subscribe(DummyObserver<int>.Instance));
        }

        [TestMethod]
        public void SelectMany_Enumerable_Complete()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(210, 2),
                OnNext(340, 4),
                OnNext(420, 3),
                OnNext(510, 2),
                OnCompleted<int>(600)
                );

            var results = scheduler.Run(() => xs.SelectMany(x => Enumerable.Repeat(x, x)));

            results.AssertEqual(
                OnNext(210, 2),
                OnNext(210, 2),
                OnNext(340, 4),
                OnNext(340, 4),
                OnNext(340, 4),
                OnNext(340, 4),
                OnNext(420, 3),
                OnNext(420, 3),
                OnNext(420, 3),
                OnNext(510, 2),
                OnNext(510, 2),
                OnCompleted<int>(600)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );
        }

        [TestMethod]
        public void SelectMany_Enumerable_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(210, 2),
                OnNext(340, 4),
                OnNext(420, 3),
                OnNext(510, 2),
                OnError<int>(600, new MockException(300))
                );

            var results = scheduler.Run(() => xs.SelectMany(x => Enumerable.Repeat(x, x)));

            results.AssertEqual(
                OnNext(210, 2),
                OnNext(210, 2),
                OnNext(340, 4),
                OnNext(340, 4),
                OnNext(340, 4),
                OnNext(340, 4),
                OnNext(420, 3),
                OnNext(420, 3),
                OnNext(420, 3),
                OnNext(510, 2),
                OnNext(510, 2),
                OnError<int>(600, new MockException(300))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );
        }

        [TestMethod]
        public void SelectMany_Enumerable_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(210, 2),
                OnNext(340, 4),
                OnNext(420, 3),
                OnNext(510, 2),
                OnCompleted<int>(600)
                );

            var results = scheduler.Run(() => xs.SelectMany(x => Enumerable.Repeat(x, x)), 350);

            results.AssertEqual(
                OnNext(210, 2),
                OnNext(210, 2),
                OnNext(340, 4),
                OnNext(340, 4),
                OnNext(340, 4),
                OnNext(340, 4)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 350)
                );
        }

        [TestMethod]
        public void SelectMany_Enumerable_SelectorThrows()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(210, 2),
                OnNext(340, 4),
                OnNext(420, 3),
                OnNext(510, 2),
                OnCompleted<int>(600)
                );

            var invoked = 0;

            var results = scheduler.Run(() => xs.SelectMany(x =>
                {
                    invoked++;
                    if (invoked == 3)
                        throw new MockException(22);

                    return Enumerable.Repeat(x, x);
                }));

            results.AssertEqual(
                OnNext(210, 2),
                OnNext(210, 2),
                OnNext(340, 4),
                OnNext(340, 4),
                OnNext(340, 4),
                OnNext(340, 4),
                OnError<int>(420, new MockException(22))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 420)
                );

            Assert.AreEqual(3, invoked);
        }

        class CurrentThrowsEnumerable<T> : IEnumerable<T>
        {
            IEnumerable<T> e;

            public CurrentThrowsEnumerable(IEnumerable<T> e)
            {
                this.e = e;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return new Enumerator(e.GetEnumerator());
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            class Enumerator : IEnumerator<T>
            {
                IEnumerator<T> e;

                public Enumerator(IEnumerator<T> e)
                {
                    this.e = e;
                }

                public T Current
                {
                    get { throw new MockException(1); }
                }

                public void Dispose()
                {
                    e.Dispose();
                }

                object System.Collections.IEnumerator.Current
                {
                    get { return Current; }
                }

                public bool MoveNext()
                {
                    return e.MoveNext();
                }

                public void Reset()
                {
                    e.Reset();
                }
            }
        }

        [TestMethod]
        public void SelectMany_Enumerable_CurrentThrows()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(210, 2),
                OnNext(340, 4),
                OnNext(420, 3),
                OnNext(510, 2),
                OnCompleted<int>(600)
                );

            var results = scheduler.Run(() => xs.SelectMany(x => new CurrentThrowsEnumerable<int>(Enumerable.Repeat(x, x))));

            results.AssertEqual(
                OnError<int>(210, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 210)
                );
        }

        class MoveNextThrowsEnumerable<T> : IEnumerable<T>
        {
            IEnumerable<T> e;

            public MoveNextThrowsEnumerable(IEnumerable<T> e)
            {
                this.e = e;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return new Enumerator(e.GetEnumerator());
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            class Enumerator : IEnumerator<T>
            {
                IEnumerator<T> e;

                public Enumerator(IEnumerator<T> e)
                {
                    this.e = e;
                }

                public T Current
                {
                    get { return e.Current; }
                }

                public void Dispose()
                {
                    e.Dispose();
                }

                object System.Collections.IEnumerator.Current
                {
                    get { return Current; }
                }

                public bool MoveNext()
                {
                    throw new MockException(1);
                }

                public void Reset()
                {
                    e.Reset();
                }
            }
        }

        [TestMethod]
        public void SelectMany_Enumerable_MoveNextThrows()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(210, 2),
                OnNext(340, 4),
                OnNext(420, 3),
                OnNext(510, 2),
                OnCompleted<int>(600)
                );

            var results = scheduler.Run(() => xs.SelectMany(x => new MoveNextThrowsEnumerable<int>(Enumerable.Repeat(x, x))));

            results.AssertEqual(
                OnError<int>(210, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 210)
                );
        }

        [TestMethod]
        public void SelectMany_QueryOperator_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => ((IObservable<int>)null).SelectMany<int, int, int>(DummyFunc<int, IObservable<int>>.Instance, DummyFunc<int, int, int>.Instance));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.SelectMany((Func<int, IObservable<int>>)null, DummyFunc<int, int, int>.Instance));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.SelectMany(DummyFunc<int, IObservable<int>>.Instance, ((Func<int, int, int>)null)));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.SelectMany(DummyFunc<int, IObservable<int>>.Instance, DummyFunc<int, int, int>.Instance).Subscribe(null));
            Throws<ArgumentNullException>(() => NullErrorObservable<int>.Instance.SelectMany(DummyFunc<int, IObservable<int>>.Instance, DummyFunc<int, int, int>.Instance).Subscribe(DummyObserver<int>.Instance));
            Throws<ArgumentNullException>(() => new MockObservable<int>(new Notification<int>.OnNext(default(int))).SelectMany(x => (IObservable<int>)null, DummyFunc<int, int, int>.Instance).Subscribe());
        }

        [TestMethod]
        public void SelectMany_QueryOperator_CompleteOuterFirst()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(220, 4),
                OnNext(221, 3),
                OnNext(222, 2),
                OnNext(223, 5),
                OnCompleted<int>(224)
                );

            var results = scheduler.Run(() => from x in xs
                                              from y in Observable.Interval(TimeSpan.FromTicks(1), scheduler).Take(x)
                                              select x * 10 + (int)y);

            results.AssertEqual(
                OnNext(221, 40),
                OnNext(222, 30),
                OnNext(222, 41),
                OnNext(223, 20),
                OnNext(223, 31),
                OnNext(223, 42),
                OnNext(224, 50),
                OnNext(224, 21),
                OnNext(224, 32),
                OnNext(224, 43),
                OnNext(225, 51),
                OnNext(226, 52),
                OnNext(227, 53),
                OnNext(228, 54),
                OnCompleted<int>(228)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 224)
                );
        }

        [TestMethod]
        public void SelectMany_QueryOperator_CompleteInnerFirst()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(220, 4),
                OnNext(221, 3),
                OnNext(222, 2),
                OnNext(223, 5),
                OnCompleted<int>(300)
                );

            var results = scheduler.Run(() => from x in xs
                                              from y in Observable.Interval(TimeSpan.FromTicks(1), scheduler).Take(x)
                                              select x * 10 + (int)y);

            results.AssertEqual(
                OnNext(221, 40),
                OnNext(222, 30),
                OnNext(222, 41),
                OnNext(223, 20),
                OnNext(223, 31),
                OnNext(223, 42),
                OnNext(224, 50),
                OnNext(224, 21),
                OnNext(224, 32),
                OnNext(224, 43),
                OnNext(225, 51),
                OnNext(226, 52),
                OnNext(227, 53),
                OnNext(228, 54),
                OnCompleted<int>(300)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 300)
                );
        }

        [TestMethod]
        public void SelectMany_QueryOperator_ErrorOuter()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(220, 4),
                OnNext(221, 3),
                OnNext(222, 2),
                OnNext(223, 5),
                OnError<int>(224, new MockException(2))
                );

            var results = scheduler.Run(() => from x in xs
                                              from y in Observable.Interval(TimeSpan.FromTicks(1), scheduler).Take(x)
                                              select x * 10 + (int)y);

            results.AssertEqual(
                OnNext(221, 40),
                OnNext(222, 30),
                OnNext(222, 41),
                OnNext(223, 20),
                OnNext(223, 31),
                OnNext(223, 42),
                OnError<int>(224, new MockException(2))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 224)
                );
        }

        [TestMethod]
        public void SelectMany_QueryOperator_ErrorInner()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(220, 4),
                OnNext(221, 3),
                OnNext(222, 2),
                OnNext(223, 5),
                OnCompleted<int>(224)
                );

            var results = scheduler.Run(() => from x in xs
                                              from y in x == 2 ? Observable.Throw<long>(new MockException(2), scheduler)
                                                : Observable.Interval(TimeSpan.FromTicks(1), scheduler).Take(x)
                                              select x * 10 + (int)y);

            results.AssertEqual(
                OnNext(221, 40),
                OnNext(222, 30),
                OnNext(222, 41),
                OnError<int>(223, new MockException(2))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 223)
                );
        }

        [TestMethod]
        public void SelectMany_QueryOperator_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(220, 4),
                OnNext(221, 3),
                OnNext(222, 2),
                OnNext(223, 5),
                OnCompleted<int>(224)
                );

            var results = scheduler.Run(() => from x in xs
                                              from y in Observable.Interval(TimeSpan.FromTicks(1), scheduler).Take(x)
                                              select x * 10 + (int)y, 223);

            results.AssertEqual(
                OnNext(221, 40),
                OnNext(222, 30),
                OnNext(222, 41)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 223)
                );
        }

        static T Throw<T>(int id)
        {
            throw new MockException(id);
        }


        [TestMethod]
        public void SelectMany_QueryOperator_ThrowSelector()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(220, 4),
                OnNext(221, 3),
                OnNext(222, 2),
                OnNext(223, 5),
                OnCompleted<int>(224)
                );

            var results = scheduler.Run(() => from x in xs
                                              from y in Throw<IObservable<long>>(1)
                                              select x * 10 + (int)y);

            results.AssertEqual(
                OnError<int>(220, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 220)
                );
        }

        [TestMethod]
        public void SelectMany_QueryOperator_ThrowResult()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(220, 4),
                OnNext(221, 3),
                OnNext(222, 2),
                OnNext(223, 5),
                OnCompleted<int>(224)
                );

            var results = scheduler.Run(() => from x in xs
                                              from y in Observable.Interval(TimeSpan.FromTicks(1), scheduler).Take(x)
                                              select Throw<int>(1));

            results.AssertEqual(
                OnError<int>(221, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 221)
                );
        }

        [TestMethod]
        public void SelectMany_Triple_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.SelectMany(null, DummyFunc<int, IObservable<int>>.Instance, DummyFunc<Exception, IObservable<int>>.Instance, DummyFunc<IObservable<int>>.Instance));
            Throws<ArgumentNullException>(() => Observable.SelectMany(DummyObservable<int>.Instance, null, DummyFunc<Exception, IObservable<int>>.Instance, DummyFunc<IObservable<int>>.Instance));
            Throws<ArgumentNullException>(() => Observable.SelectMany(DummyObservable<int>.Instance, DummyFunc<int, IObservable<int>>.Instance, null, DummyFunc<IObservable<int>>.Instance));
            Throws<ArgumentNullException>(() => Observable.SelectMany(DummyObservable<int>.Instance, DummyFunc<int, IObservable<int>>.Instance, DummyFunc<Exception, IObservable<int>>.Instance, null));
            Throws<ArgumentNullException>(() => Observable.SelectMany(DummyObservable<int>.Instance, DummyFunc<int, IObservable<int>>.Instance, DummyFunc<Exception, IObservable<int>>.Instance, DummyFunc<IObservable<int>>.Instance).Subscribe(null));
            Throws<ArgumentNullException>(() => Observable.SelectMany(NullErrorObservable<int>.Instance, DummyFunc<int, IObservable<int>>.Instance, DummyFunc<Exception, IObservable<int>>.Instance, DummyFunc<IObservable<int>>.Instance).Subscribe());
            Throws<ArgumentNullException>(() => Observable.SelectMany<int, int>(Observable.Return<int>(1, Scheduler.Immediate), x => null, ex => null, () => null).Subscribe());
            Throws<ArgumentNullException>(() => Observable.SelectMany<int, int>(Observable.Throw<int>(new Exception(), Scheduler.Immediate), x => null, ex => null, () => null).Subscribe());
            Throws<ArgumentNullException>(() => Observable.SelectMany<int, int>(Observable.Empty<int>(Scheduler.Immediate), x => null, ex => null, () => null).Subscribe());
        }

        [TestMethod]
        public void SelectMany_Triple_Identity()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(300, 0),
                OnNext(301, 1),
                OnNext(302, 2),
                OnNext(303, 3),
                OnNext(304, 4),
                OnCompleted<int>(305)
                );

            var results = scheduler.Run(() => xs.SelectMany(
                x => Observable.Return(x, scheduler),
                ex => Observable.Throw<int>(ex, scheduler),
                () => Observable.Empty<int>(scheduler)));

            results.AssertEqual(
                OnNext(301, 0),
                OnNext(302, 1),
                OnNext(303, 2),
                OnNext(304, 3),
                OnNext(305, 4),
                OnCompleted<int>(306)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 305)
                );
        }

        [TestMethod]
        public void SelectMany_Triple_Error_Identity()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(300, 0),
                OnNext(301, 1),
                OnNext(302, 2),
                OnNext(303, 3),
                OnNext(304, 4),
                OnError<int>(305, new MockException(1))
                );

            var results = scheduler.Run(() => xs.SelectMany(
                x => Observable.Return(x, scheduler),
                ex => Observable.Throw<int>(ex, scheduler),
                () => Observable.Empty<int>(scheduler)));

            results.AssertEqual(
                OnNext(301, 0),
                OnNext(302, 1),
                OnNext(303, 2),
                OnNext(304, 3),
                OnNext(305, 4),
                OnError<int>(306, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 305)
                );
        }

        [TestMethod]
        public void SelectMany_Triple_SelectMany()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(300, 0),
                OnNext(301, 1),
                OnNext(302, 2),
                OnNext(303, 3),
                OnNext(304, 4),
                OnCompleted<int>(305)
                );

            var results = scheduler.Run(() => xs.SelectMany(
                x => Observable.Repeat(x, x, scheduler),
                ex => Observable.Throw<int>(ex, scheduler),
                () => Observable.Empty<int>(scheduler)));

            results.AssertEqual(
                OnNext(302, 1),
                OnNext(303, 2),
                OnNext(304, 3),
                OnNext(304, 2),
                OnNext(305, 4),
                OnNext(305, 3),
                OnNext(306, 4),
                OnNext(306, 3),
                OnNext(307, 4),
                OnNext(308, 4),
                OnCompleted<int>(309)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 305)
                );
        }


        [TestMethod]
        public void SelectMany_Triple_Concat()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(300, 0),
                OnNext(301, 1),
                OnNext(302, 2),
                OnNext(303, 3),
                OnNext(304, 4),
                OnCompleted<int>(305)
                );

            var results = scheduler.Run(() => xs.SelectMany(
                x => Observable.Return(x, scheduler),
                ex => Observable.Throw<int>(ex, scheduler),
                () => Observable.Range(1, 3, scheduler)));

            results.AssertEqual(
                OnNext(301, 0),
                OnNext(302, 1),
                OnNext(303, 2),
                OnNext(304, 3),
                OnNext(305, 4),
                OnNext(306, 1),
                OnNext(307, 2),
                OnNext(308, 3),
                OnCompleted<int>(309)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 305)
                );
        }

        [TestMethod]
        public void SelectMany_Triple_Catch()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(300, 0),
                OnNext(301, 1),
                OnNext(302, 2),
                OnNext(303, 3),
                OnNext(304, 4),
                OnCompleted<int>(305)
                );

            var results = scheduler.Run(() => xs.SelectMany(
                x => Observable.Return(x, scheduler),
                ex => Observable.Range(1, 3, scheduler),
                () => Observable.Empty<int>(scheduler)));

            results.AssertEqual(
                OnNext(301, 0),
                OnNext(302, 1),
                OnNext(303, 2),
                OnNext(304, 3),
                OnNext(305, 4),
                OnCompleted<int>(306)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 305)
                );
        }

        [TestMethod]
        public void SelectMany_Triple_Error_Catch()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(300, 0),
                OnNext(301, 1),
                OnNext(302, 2),
                OnNext(303, 3),
                OnNext(304, 4),
                OnError<int>(305, new MockException(1))
                );

            var results = scheduler.Run(() => xs.SelectMany(
                x => Observable.Return(x, scheduler),
                ex => Observable.Range(1, 3, scheduler),
                () => Observable.Empty<int>(scheduler)));

            results.AssertEqual(
                OnNext(301, 0),
                OnNext(302, 1),
                OnNext(303, 2),
                OnNext(304, 3),
                OnNext(305, 4),
                OnNext(306, 1),
                OnNext(307, 2),
                OnNext(308, 3),
                OnCompleted<int>(309)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 305)
                );
        }

        [TestMethod]
        public void SelectMany_Triple_All()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(300, 0),
                OnNext(301, 1),
                OnNext(302, 2),
                OnNext(303, 3),
                OnNext(304, 4),
                OnCompleted<int>(305)
                );

            var results = scheduler.Run(() => xs.SelectMany(
                x => Observable.Repeat(x, x, scheduler),
                ex => Observable.Repeat(0, 2, scheduler),
                () => Observable.Repeat(-1, 2, scheduler)));

            results.AssertEqual(
                OnNext(302, 1),
                OnNext(303, 2),
                OnNext(304, 3),
                OnNext(304, 2),
                OnNext(305, 4),
                OnNext(305, 3),
                OnNext(306, -1),
                OnNext(306, 4),
                OnNext(306, 3),
                OnNext(307, -1),
                OnNext(307, 4),
                OnNext(308, 4),
                OnCompleted<int>(309)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 305)
                );
        }

        [TestMethod]
        public void SelectMany_Triple_Error_All()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(300, 0),
                OnNext(301, 1),
                OnNext(302, 2),
                OnNext(303, 3),
                OnNext(304, 4),
                OnError<int>(305, new MockException(1))
                );

            var results = scheduler.Run(() => xs.SelectMany(
                x => Observable.Repeat(x, x, scheduler),
                ex => Observable.Repeat(0, 2, scheduler),
                () => Observable.Repeat(-1, 2, scheduler)));

            results.AssertEqual(
                OnNext(302, 1),
                OnNext(303, 2),
                OnNext(304, 3),
                OnNext(304, 2),
                OnNext(305, 4),
                OnNext(305, 3),
                OnNext(306, 0),
                OnNext(306, 4),
                OnNext(306, 3),
                OnNext(307, 0),
                OnNext(307, 4),
                OnNext(308, 4),
                OnCompleted<int>(309)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 305)
                );
        }

        [TestMethod]
        public void SelectMany_Triple_All_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(300, 0),
                OnNext(301, 1),
                OnNext(302, 2),
                OnNext(303, 3),
                OnNext(304, 4),
                OnCompleted<int>(305)
                );

            var results = scheduler.Run(() => xs.SelectMany(
                x => Observable.Repeat(x, x, scheduler),
                ex => Observable.Repeat(0, 2, scheduler),
                () => Observable.Repeat(-1, 2, scheduler)), 307);

            results.AssertEqual(
                OnNext(302, 1),
                OnNext(303, 2),
                OnNext(304, 3),
                OnNext(304, 2),
                OnNext(305, 4),
                OnNext(305, 3),
                OnNext(306, -1),
                OnNext(306, 4),
                OnNext(306, 3)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 305)
                );
        }

        [TestMethod]
        public void SelectMany_Triple_All_Dispose_Before_First()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(300, 0),
                OnNext(301, 1),
                OnNext(302, 2),
                OnNext(303, 3),
                OnNext(304, 4),
                OnCompleted<int>(305)
                );

            var results = scheduler.Run(() => xs.SelectMany(
                x => Observable.Repeat(x, x, scheduler),
                ex => Observable.Repeat(0, 2, scheduler),
                () => Observable.Repeat(-1, 2, scheduler)), 304);

            results.AssertEqual(
                OnNext(302, 1),
                OnNext(303, 2)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 304)
                );
        }

        [TestMethod]
        public void SelectMany_Triple_OnNextThrow()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(300, 0),
                OnNext(301, 1),
                OnNext(302, 2),
                OnNext(303, 3),
                OnNext(304, 4),
                OnCompleted<int>(305)
                );

            var results = scheduler.Run(() => xs.SelectMany(
                x => Throw<IObservable<int>>(1),
                ex => Observable.Repeat(0, 2, scheduler),
                () => Observable.Repeat(-1, 2, scheduler)));

            results.AssertEqual(
                OnError<int>(300, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 300)
                );
        }

        [TestMethod]
        public void SelectMany_Triple_OnErrorThrow()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(300, 0),
                OnNext(301, 1),
                OnNext(302, 2),
                OnNext(303, 3),
                OnNext(304, 4),
                OnError<int>(305, new MockException(2))
                );

            var results = scheduler.Run(() => xs.SelectMany(
                x => Observable.Repeat(x, x, scheduler),
                ex => Throw<IObservable<int>>(1),
                () => Observable.Repeat(-1, 2, scheduler)));

            results.AssertEqual(
                OnNext(302, 1),
                OnNext(303, 2),
                OnNext(304, 3),
                OnNext(304, 2),
                OnError<int>(305, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 305)
                );
        }


        [TestMethod]
        public void SelectMany_Triple_OnCompletedThrow()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(300, 0),
                OnNext(301, 1),
                OnNext(302, 2),
                OnNext(303, 3),
                OnNext(304, 4),
                OnCompleted<int>(305)
                );

            var results = scheduler.Run(() => xs.SelectMany(
                x => Observable.Repeat(x, x, scheduler),
                ex => Observable.Repeat(0, 2, scheduler),
                () => Throw<IObservable<int>>(1)));

            results.AssertEqual(
                OnNext(302, 1),
                OnNext(303, 2),
                OnNext(304, 3),
                OnNext(304, 2),
                OnError<int>(305, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 305)
                );
        }
    }
}
