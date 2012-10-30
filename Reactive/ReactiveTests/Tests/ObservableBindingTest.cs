using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Collections.Generic;
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
    public partial class ObservableBindingTest : Test
    {
        [TestMethod]
        public void Let_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.Let(default(IObservable<int>), x => x));
            Throws<ArgumentNullException>(() => Observable.Let<int, int>(someObservable, null));
        }

        [TestMethod]
        public void Let_CallsFunctionImmediately()
        {
            bool called = false;
            Observable.Empty<int>().Let(x => { called = true; return x; });
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void RefCount_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.RefCount<int>(null));
        }

        [TestMethod]
        public void ConnectableObservable_Creation()
        {
            int y = 0;
            var co1 = new ConnectableObservable<int>(Observable.Return<int>(1));
            co1.Subscribe(x => y = x);
            Assert.AreNotEqual(1, y);
            co1.Connect();
            Assert.AreEqual(1, y);
            
            y = 0;
            var s = new Subject<int>();
            var co2 = new ConnectableObservable<int>(Observable.Return<int>(1), s);
            co2.Subscribe(x => y = x);
            Assert.AreNotEqual(1, y);
            co2.Connect();
            Assert.AreEqual(1, y);
        }

        [TestMethod]
        public void ConnectableObservable_Connected()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<int>(
                OnNext(210, 1),
                OnNext(220, 2),
                OnNext(230, 3),
                OnNext(240, 4),
                OnCompleted<int>(250)
            );

            var subject = new MySubject();

            var conn = new ConnectableObservable<int>(xs, subject);
            var disconnect = conn.Connect();

            var res = scheduler.Run(() => conn).ToArray();
            res.AssertEqual(
                OnNext(210, 1),
                OnNext(220, 2),
                OnNext(230, 3),
                OnNext(240, 4),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void ConnectableObservable_NotConnected()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<int>(
                OnNext(210, 1),
                OnNext(220, 2),
                OnNext(230, 3),
                OnNext(240, 4),
                OnCompleted<int>(250)
            );

            var subject = new MySubject();

            var conn = new ConnectableObservable<int>(xs, subject);

            var res = scheduler.Run(() => conn).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void ConnectableObservable_Disconnected()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<int>(
                OnNext(210, 1),
                OnNext(220, 2),
                OnNext(230, 3),
                OnNext(240, 4),
                OnCompleted<int>(250)
            );

            var subject = new MySubject();

            var conn = new ConnectableObservable<int>(xs, subject);
            var disconnect = conn.Connect();
            disconnect.Dispose();

            var res = scheduler.Run(() => conn).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void ConnectableObservable_DisconnectFuture()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<int>(
                OnNext(210, 1),
                OnNext(220, 2),
                OnNext(230, 3),
                OnNext(240, 4),
                OnCompleted<int>(250)
            );

            var subject = new MySubject();

            var conn = new ConnectableObservable<int>(xs, subject);
            subject.DisposeOn(3, conn.Connect());

            var res = scheduler.Run(() => conn).ToArray();
            res.AssertEqual(
                OnNext(210, 1),
                OnNext(220, 2),
                OnNext(230, 3)
            );
        }

        [TestMethod]
        public void RefCount_ConnectsOnFirst()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<int>(
                OnNext(210, 1),
                OnNext(220, 2),
                OnNext(230, 3),
                OnNext(240, 4),
                OnCompleted<int>(250)
            );

            var subject = new MySubject();

            var conn = new ConnectableObservable<int>(xs, subject);

            var res = scheduler.Run(() => conn.RefCount()).ToArray();
            res.AssertEqual(
                OnNext(210, 1),
                OnNext(220, 2),
                OnNext(230, 3),
                OnNext(240, 4),
                OnCompleted<int>(250)
            );

            Assert.IsTrue(subject.Disposed);
        }

        [TestMethod]
        public void RefCount_NotConnected()
        {
            var disconnected = false;
            var count = 0;
            var xs = Observable.Defer(() =>
            {
                count++;
                return Observable.Create<int>(obs =>
                {
                    return () => { disconnected = true; };
                });
            });

            var subject = new MySubject();

            var conn = new ConnectableObservable<int>(xs, subject);
            var refd = conn.RefCount();

            var dis1 = refd.Subscribe();
            Assert.AreEqual(1, count);
            Assert.AreEqual(1, subject.SubscribeCount);
            Assert.IsFalse(disconnected);

            var dis2 = refd.Subscribe();
            Assert.AreEqual(1, count);
            Assert.AreEqual(2, subject.SubscribeCount);
            Assert.IsFalse(disconnected);

            dis1.Dispose();
            Assert.IsFalse(disconnected);
            dis2.Dispose();
            Assert.IsTrue(disconnected);
            disconnected = false;

            var dis3 = refd.Subscribe();
            Assert.AreEqual(2, count);
            Assert.AreEqual(3, subject.SubscribeCount);
            Assert.IsFalse(disconnected);

            dis3.Dispose();
            Assert.IsTrue(disconnected);
        }

        class MySubject : ISubject<int>
        {
            private Dictionary<int, IDisposable> _disposeOn = new Dictionary<int, IDisposable>();

            public void DisposeOn(int value, IDisposable disposable)
            {
                _disposeOn[value] = disposable;
            }

            private IObserver<int> _observer;

            public void OnNext(int value)
            {
                _observer.OnNext(value);

                IDisposable disconnect;
                if (_disposeOn.TryGetValue(value, out disconnect))
                    disconnect.Dispose();
            }

            public void OnError(Exception exception)
            {
                _observer.OnError(exception);
            }

            public void OnCompleted()
            {
                _observer.OnCompleted();
            }

            public IDisposable Subscribe(IObserver<int> observer)
            {
                _subscribeCount++;
                _observer = observer;
                return Disposable.Create(() => { _disposed = true; });
            }

            private int _subscribeCount;
            private bool _disposed;

            public int SubscribeCount { get { return _subscribeCount; } }
            public bool Disposed { get { return _disposed; } }
        }

        [TestMethod]
        public void Publish_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.Publish(default(IObservable<int>)));
            Throws<ArgumentNullException>(() => Observable.Publish(default(IObservable<int>), x => x));
            Throws<ArgumentNullException>(() => Observable.Publish<int, int>(someObservable, null));
        }

        [TestMethod]
        public void Publish_Basic()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var ys = default(IConnectableObservable<int>);
            var subscription = default(IDisposable);
            var connection = default(IDisposable);
            var results = new MockObserver<int>(scheduler);

            scheduler.Schedule(() => ys = xs.Publish(), Created);
            scheduler.Schedule(() => subscription = ys.Subscribe(results), Subscribed);
            scheduler.Schedule(() => subscription.Dispose(), Disposed);

            scheduler.Schedule(() => connection = ys.Connect(), 300);
            scheduler.Schedule(() => connection.Dispose(), 400);

            scheduler.Schedule(() => connection = ys.Connect(), 500);
            scheduler.Schedule(() => connection.Dispose(), 550);

            scheduler.Schedule(() => connection = ys.Connect(), 650);
            scheduler.Schedule(() => connection.Dispose(), 800);

            scheduler.Run();

            results.AssertEqual(
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(520, 11)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(300, 400),
                Subscribe(500, 550),
                Subscribe(650, 800)
                );
        }

        [TestMethod]
        public void Publish_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnError<int>(600, new MockException(1))
                );

            var ys = default(IConnectableObservable<int>);
            var subscription = default(IDisposable);
            var connection = default(IDisposable);
            var results = new MockObserver<int>(scheduler);

            scheduler.Schedule(() => ys = xs.Publish(), Created);
            scheduler.Schedule(() => subscription = ys.Subscribe(results), Subscribed);
            scheduler.Schedule(() => subscription.Dispose(), Disposed);

            scheduler.Schedule(() => connection = ys.Connect(), 300);
            scheduler.Schedule(() => connection.Dispose(), 400);

            scheduler.Schedule(() => connection = ys.Connect(), 500);
            scheduler.Schedule(() => connection.Dispose(), 800);

            scheduler.Run();

            results.AssertEqual(
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(520, 11),
                OnNext(560, 20),
                OnError<int>(600, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(300, 400),
                Subscribe(500, 600)
                );
        }

        [TestMethod]
        public void Publish_Complete()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var ys = default(IConnectableObservable<int>);
            var subscription = default(IDisposable);
            var connection = default(IDisposable);
            var results = new MockObserver<int>(scheduler);

            scheduler.Schedule(() => ys = xs.Publish(), Created);
            scheduler.Schedule(() => subscription = ys.Subscribe(results), Subscribed);
            scheduler.Schedule(() => subscription.Dispose(), Disposed);

            scheduler.Schedule(() => connection = ys.Connect(), 300);
            scheduler.Schedule(() => connection.Dispose(), 400);

            scheduler.Schedule(() => connection = ys.Connect(), 500);
            scheduler.Schedule(() => connection.Dispose(), 800);

            scheduler.Run();

            results.AssertEqual(
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(300, 400),
                Subscribe(500, 600)
                );
        }

        [TestMethod]
        public void Publish_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var ys = default(IConnectableObservable<int>);
            var subscription = default(IDisposable);
            var connection = default(IDisposable);
            var results = new MockObserver<int>(scheduler);

            scheduler.Schedule(() => ys = xs.Publish(), Created);
            scheduler.Schedule(() => subscription = ys.Subscribe(results), Subscribed);
            scheduler.Schedule(() => subscription.Dispose(), 350);

            scheduler.Schedule(() => connection = ys.Connect(), 300);
            scheduler.Schedule(() => connection.Dispose(), 400);

            scheduler.Schedule(() => connection = ys.Connect(), 500);
            scheduler.Schedule(() => connection.Dispose(), 550);

            scheduler.Schedule(() => connection = ys.Connect(), 650);
            scheduler.Schedule(() => connection.Dispose(), 800);

            scheduler.Run();

            results.AssertEqual(
                OnNext(340, 8)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(300, 400),
                Subscribe(500, 550),
                Subscribe(650, 800)
                );
        }

        [TestMethod]
        public void Publish_MultipleConnections()
        {
            var xs = Observable.Never<int>();
            var ys = xs.Publish();

            var connection1 = ys.Connect();
            var connection2 = ys.Connect();

            Assert.AreSame(connection1, connection2);

            connection1.Dispose();
            connection2.Dispose();

            var connection3 = ys.Connect();

            Assert.AreNotSame(connection1, connection3);

            connection3.Dispose();
        }

        [TestMethod]
        public void PublishLambda_Zip_Complete()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var results = scheduler.Run(() => xs.Publish(_xs => _xs.Zip(_xs.Skip(1), (prev, cur) => cur + prev)));

            results.AssertEqual(
                OnNext(280, 7),
                OnNext(290, 5),
                OnNext(340, 9),
                OnNext(360, 13),
                OnNext(370, 11),
                OnNext(390, 13),
                OnNext(410, 20),
                OnNext(430, 15),
                OnNext(450, 11),
                OnNext(520, 20),
                OnNext(560, 31),
                OnCompleted<int>(600)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );
        }

        [TestMethod]
        public void PublishLambda_Zip_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnError<int>(600, new MockException(1))
                );

            var results = scheduler.Run(() => xs.Publish(_xs => _xs.Zip(_xs.Skip(1), (prev, cur) => cur + prev)));

            results.AssertEqual(
                OnNext(280, 7),
                OnNext(290, 5),
                OnNext(340, 9),
                OnNext(360, 13),
                OnNext(370, 11),
                OnNext(390, 13),
                OnNext(410, 20),
                OnNext(430, 15),
                OnNext(450, 11),
                OnNext(520, 20),
                OnNext(560, 31),
                OnError<int>(600, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );
        }

        [TestMethod]
        public void PublishLambda_Zip_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var results = scheduler.Run(() => xs.Publish(_xs => _xs.Zip(_xs.Skip(1), (prev, cur) => cur + prev)), 470);

            results.AssertEqual(
                OnNext(280, 7),
                OnNext(290, 5),
                OnNext(340, 9),
                OnNext(360, 13),
                OnNext(370, 11),
                OnNext(390, 13),
                OnNext(410, 20),
                OnNext(430, 15),
                OnNext(450, 11)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 470)
                );
        }

        [TestMethod]
        public void Prune_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();
            var scheduler = new TestScheduler();

            Throws<ArgumentNullException>(() => Observable.Prune(default(IObservable<int>)));
            Throws<ArgumentNullException>(() => Observable.Prune(default(IObservable<int>), scheduler));
            Throws<ArgumentNullException>(() => Observable.Prune(someObservable, default(IScheduler)));
            Throws<ArgumentNullException>(() => Observable.Prune(default(IObservable<int>), x => x));
            Throws<ArgumentNullException>(() => Observable.Prune<int, int>(someObservable, null));
            Throws<ArgumentNullException>(() => Observable.Prune(default(IObservable<int>), x => x, scheduler));
            Throws<ArgumentNullException>(() => Observable.Prune<int, int>(someObservable, null, scheduler));
            Throws<ArgumentNullException>(() => Observable.Prune<int, int>(someObservable, x => x, default(IScheduler)));
        }

        [TestMethod]
        public void Prune_Basic()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var ys = default(IConnectableObservable<int>);
            var subscription = default(IDisposable);
            var connection = default(IDisposable);
            var results = new MockObserver<int>(scheduler);

            scheduler.Schedule(() => ys = xs.Prune(scheduler), Created);
            scheduler.Schedule(() => subscription = ys.Subscribe(results), Subscribed);
            scheduler.Schedule(() => subscription.Dispose(), Disposed);

            scheduler.Schedule(() => connection = ys.Connect(), 300);
            scheduler.Schedule(() => connection.Dispose(), 400);

            scheduler.Schedule(() => connection = ys.Connect(), 500);
            scheduler.Schedule(() => connection.Dispose(), 550);

            scheduler.Schedule(() => connection = ys.Connect(), 650);
            scheduler.Schedule(() => connection.Dispose(), 800);

            scheduler.Run();

            results.AssertEqual(
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(300, 400),
                Subscribe(500, 550),
                Subscribe(650, 800)
                );
        }

        [TestMethod]
        public void Prune_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnError<int>(600, new MockException(1))
                );

            var ys = default(IConnectableObservable<int>);
            var subscription = default(IDisposable);
            var connection = default(IDisposable);
            var results = new MockObserver<int>(scheduler);

            scheduler.Schedule(() => ys = xs.Prune(scheduler), Created);
            scheduler.Schedule(() => subscription = ys.Subscribe(results), Subscribed);
            scheduler.Schedule(() => subscription.Dispose(), Disposed);

            scheduler.Schedule(() => connection = ys.Connect(), 300);
            scheduler.Schedule(() => connection.Dispose(), 400);

            scheduler.Schedule(() => connection = ys.Connect(), 500);
            scheduler.Schedule(() => connection.Dispose(), 800);

            scheduler.Run();

            results.AssertEqual(
                OnError<int>(600, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(300, 400),
                Subscribe(500, 600)
                );
        }

        [TestMethod]
        public void Prune_Complete()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var ys = default(IConnectableObservable<int>);
            var subscription = default(IDisposable);
            var connection = default(IDisposable);
            var results = new MockObserver<int>(scheduler);

            scheduler.Schedule(() => ys = xs.Prune(scheduler), Created);
            scheduler.Schedule(() => subscription = ys.Subscribe(results), Subscribed);
            scheduler.Schedule(() => subscription.Dispose(), Disposed);

            scheduler.Schedule(() => connection = ys.Connect(), 300);
            scheduler.Schedule(() => connection.Dispose(), 400);

            scheduler.Schedule(() => connection = ys.Connect(), 500);
            scheduler.Schedule(() => connection.Dispose(), 800);

            scheduler.Run();

            results.AssertEqual(
                OnNext(600, 20),
                OnCompleted<int>(600)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(300, 400),
                Subscribe(500, 600)
                );
        }

        [TestMethod]
        public void Prune_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var ys = default(IConnectableObservable<int>);
            var subscription = default(IDisposable);
            var connection = default(IDisposable);
            var results = new MockObserver<int>(scheduler);

            scheduler.Schedule(() => ys = xs.Prune(scheduler), Created);
            scheduler.Schedule(() => subscription = ys.Subscribe(results), Subscribed);
            scheduler.Schedule(() => subscription.Dispose(), 350);

            scheduler.Schedule(() => connection = ys.Connect(), 300);
            scheduler.Schedule(() => connection.Dispose(), 400);

            scheduler.Schedule(() => connection = ys.Connect(), 500);
            scheduler.Schedule(() => connection.Dispose(), 550);

            scheduler.Schedule(() => connection = ys.Connect(), 650);
            scheduler.Schedule(() => connection.Dispose(), 800);

            scheduler.Run();

            results.AssertEqual(
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(300, 400),
                Subscribe(500, 550),
                Subscribe(650, 800)
                );
        }

        [TestMethod]
        public void Prune_MultipleConnections()
        {
            var xs = Observable.Never<int>();
            var ys = xs.Prune(new TestScheduler());

            var connection1 = ys.Connect();
            var connection2 = ys.Connect();

            Assert.AreSame(connection1, connection2);

            connection1.Dispose();
            connection2.Dispose();

            var connection3 = ys.Connect();

            Assert.AreNotSame(connection1, connection3);

            connection3.Dispose();
        }

        [TestMethod]
        public void PruneLambda_Zip_Complete()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var results = scheduler.Run(() => xs.Prune(_xs => _xs.Zip(_xs, (x, y) => x + y), scheduler));

            results.AssertEqual(
                OnNext(600, 40),
                OnCompleted<int>(600)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );
        }

        [TestMethod]
        public void PruneLambda_Zip_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnError<int>(600, new MockException(1))
                );

            var results = scheduler.Run(() => xs.Prune(_xs => _xs.Zip(_xs, (x, y) => x + y), scheduler));

            results.AssertEqual(
                OnError<int>(600, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );
        }

        [TestMethod]
        public void PruneLambda_Zip_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var results = scheduler.Run(() => xs.Prune(_xs => _xs.Zip(_xs, (x, y) => x + y), scheduler), 470);

            results.AssertEqual(
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 470)
                );
        }

        [TestMethod]
        public void Replay_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();
            var scheduler = new TestScheduler();

            Throws<ArgumentNullException>(() => Observable.Replay(default(IObservable<int>)));
            Throws<ArgumentNullException>(() => Observable.Replay(default(IObservable<int>), x => x));
            Throws<ArgumentNullException>(() => Observable.Replay<int, int>(someObservable, null));
            Throws<ArgumentNullException>(() => Observable.Replay<int>(null, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.Replay<int>(DummyObservable<int>.Instance, (IScheduler)null));
            Throws<ArgumentNullException>(() => Observable.Replay<int, int>(null, DummyFunc<IObservable<int>, IObservable<int>>.Instance, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.Replay<int, int>(DummyObservable<int>.Instance, null, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.Replay<int, int>(DummyObservable<int>.Instance, DummyFunc<IObservable<int>, IObservable<int>>.Instance, null));
            Throws<ArgumentNullException>(() => Observable.Replay(default(IObservable<int>), TimeSpan.FromSeconds(1)));
            Throws<ArgumentOutOfRangeException>(() => Observable.Replay(someObservable, TimeSpan.FromSeconds(-1)));
            Throws<ArgumentNullException>(() => Observable.Replay(default(IObservable<int>), x => x, TimeSpan.FromSeconds(1)));
            Throws<ArgumentNullException>(() => Observable.Replay<int, int>(someObservable, null, TimeSpan.FromSeconds(1)));
            Throws<ArgumentOutOfRangeException>(() => Observable.Replay<int, int>(someObservable, x => x, TimeSpan.FromSeconds(-1)));
            Throws<ArgumentNullException>(() => Observable.Replay(default(IObservable<int>), TimeSpan.FromSeconds(1), scheduler));
            Throws<ArgumentOutOfRangeException>(() => Observable.Replay(someObservable, TimeSpan.FromSeconds(-1), scheduler));
            Throws<ArgumentNullException>(() => Observable.Replay(someObservable, TimeSpan.FromSeconds(1), default(IScheduler)));
            Throws<ArgumentNullException>(() => Observable.Replay(default(IObservable<int>), x => x, TimeSpan.FromSeconds(1), scheduler));
            Throws<ArgumentNullException>(() => Observable.Replay<int, int>(someObservable, null, TimeSpan.FromSeconds(1), scheduler));
            Throws<ArgumentOutOfRangeException>(() => Observable.Replay(someObservable, x => x, TimeSpan.FromSeconds(-1), scheduler));
            Throws<ArgumentNullException>(() => Observable.Replay(someObservable, x => x, TimeSpan.FromSeconds(1), default(IScheduler)));
            Throws<ArgumentNullException>(() => Observable.Replay(default(IObservable<int>), 1, scheduler));
            Throws<ArgumentOutOfRangeException>(() => Observable.Replay(someObservable, -2, scheduler));
            Throws<ArgumentNullException>(() => Observable.Replay(someObservable, 1, default(IScheduler)));
            Throws<ArgumentNullException>(() => Observable.Replay(default(IObservable<int>), x => x, 1, scheduler));
            Throws<ArgumentNullException>(() => Observable.Replay<int, int>(someObservable, null, -2, scheduler));
            Throws<ArgumentOutOfRangeException>(() => Observable.Replay(someObservable, x => x, -2, scheduler));
            Throws<ArgumentNullException>(() => Observable.Replay(someObservable, x => x, 1, default(IScheduler)));
            Throws<ArgumentNullException>(() => Observable.Replay(default(IObservable<int>), 1));
            Throws<ArgumentOutOfRangeException>(() => Observable.Replay(someObservable, -2));
            Throws<ArgumentNullException>(() => Observable.Replay(default(IObservable<int>), x => x, 1));
            Throws<ArgumentNullException>(() => Observable.Replay<int, int>(someObservable, null, 1));
            Throws<ArgumentOutOfRangeException>(() => Observable.Replay(someObservable, x => x, -2));
            Throws<ArgumentNullException>(() => Observable.Replay(default(IObservable<int>), 1, TimeSpan.FromSeconds(1)));
            Throws<ArgumentOutOfRangeException>(() => Observable.Replay(someObservable, -2, TimeSpan.FromSeconds(1)));
            Throws<ArgumentOutOfRangeException>(() => Observable.Replay(someObservable, 1, TimeSpan.FromSeconds(-1)));
            Throws<ArgumentNullException>(() => Observable.Replay(default(IObservable<int>), x => x, 1, TimeSpan.FromSeconds(1)));
            Throws<ArgumentNullException>(() => Observable.Replay<int, int>(someObservable, null, 1, TimeSpan.FromSeconds(1)));
            Throws<ArgumentOutOfRangeException>(() => Observable.Replay(someObservable, x => x, -2, TimeSpan.FromSeconds(1)));
            Throws<ArgumentOutOfRangeException>(() => Observable.Replay(someObservable, x => x, 1, TimeSpan.FromSeconds(-1)));
            Throws<ArgumentNullException>(() => Observable.Replay(default(IObservable<int>), 1, TimeSpan.FromSeconds(1), scheduler));
            Throws<ArgumentOutOfRangeException>(() => Observable.Replay(someObservable, -2, TimeSpan.FromSeconds(1), scheduler));
            Throws<ArgumentOutOfRangeException>(() => Observable.Replay(someObservable, 1, TimeSpan.FromSeconds(-1), scheduler));
            Throws<ArgumentNullException>(() => Observable.Replay(someObservable, 1, TimeSpan.FromSeconds(1), null));
            Throws<ArgumentNullException>(() => Observable.Replay(default(IObservable<int>), x => x, 1, TimeSpan.FromSeconds(1), scheduler));
            Throws<ArgumentNullException>(() => Observable.Replay<int, int>(someObservable, null, 1, TimeSpan.FromSeconds(1), scheduler));
            Throws<ArgumentOutOfRangeException>(() => Observable.Replay(someObservable, x => x, -2, TimeSpan.FromSeconds(1), scheduler));
            Throws<ArgumentOutOfRangeException>(() => Observable.Replay(someObservable, x => x, 1, TimeSpan.FromSeconds(-1), scheduler));
            Throws<ArgumentNullException>(() => Observable.Replay(someObservable, x => x, 1, TimeSpan.FromSeconds(1), null));
        }

        [TestMethod]
        public void ReplayCount_Basic()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var ys = default(IConnectableObservable<int>);
            var subscription = default(IDisposable);
            var connection = default(IDisposable);
            var results = new MockObserver<int>(scheduler);

            scheduler.Schedule(() => ys = xs.Replay(3, scheduler), Created);
            scheduler.Schedule(() => subscription = ys.Subscribe(results), 450);
            scheduler.Schedule(() => subscription.Dispose(), Disposed);

            scheduler.Schedule(() => connection = ys.Connect(), 300);
            scheduler.Schedule(() => connection.Dispose(), 400);

            scheduler.Schedule(() => connection = ys.Connect(), 500);
            scheduler.Schedule(() => connection.Dispose(), 550);

            scheduler.Schedule(() => connection = ys.Connect(), 650);
            scheduler.Schedule(() => connection.Dispose(), 800);

            scheduler.Run();

            results.AssertEqual(
                OnNext(451, 5),
                OnNext(452, 6),
                OnNext(453, 7),
                OnNext(520, 11)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(300, 400),
                Subscribe(500, 550),
                Subscribe(650, 800)
                );
        }

        [TestMethod]
        public void ReplayCount_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnError<int>(600, new MockException(1))
                );

            var ys = default(IConnectableObservable<int>);
            var subscription = default(IDisposable);
            var connection = default(IDisposable);
            var results = new MockObserver<int>(scheduler);

            scheduler.Schedule(() => ys = xs.Replay(3, scheduler), Created);
            scheduler.Schedule(() => subscription = ys.Subscribe(results), 450);
            scheduler.Schedule(() => subscription.Dispose(), Disposed);

            scheduler.Schedule(() => connection = ys.Connect(), 300);
            scheduler.Schedule(() => connection.Dispose(), 400);

            scheduler.Schedule(() => connection = ys.Connect(), 500);
            scheduler.Schedule(() => connection.Dispose(), 800);

            scheduler.Run();

            results.AssertEqual(
                OnNext(451, 5),
                OnNext(452, 6),
                OnNext(453, 7),
                OnNext(520, 11),
                OnNext(560, 20),
                OnError<int>(600, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(300, 400),
                Subscribe(500, 600)
                );
        }

        [TestMethod]
        public void ReplayCount_Complete()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var ys = default(IConnectableObservable<int>);
            var subscription = default(IDisposable);
            var connection = default(IDisposable);
            var results = new MockObserver<int>(scheduler);

            scheduler.Schedule(() => ys = xs.Replay(3, scheduler), Created);
            scheduler.Schedule(() => subscription = ys.Subscribe(results), 450);
            scheduler.Schedule(() => subscription.Dispose(), Disposed);

            scheduler.Schedule(() => connection = ys.Connect(), 300);
            scheduler.Schedule(() => connection.Dispose(), 400);

            scheduler.Schedule(() => connection = ys.Connect(), 500);
            scheduler.Schedule(() => connection.Dispose(), 800);

            scheduler.Run();

            results.AssertEqual(
                OnNext(451, 5),
                OnNext(452, 6),
                OnNext(453, 7),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(300, 400),
                Subscribe(500, 600)
                );
        }

        [TestMethod]
        public void ReplayCount_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var ys = default(IConnectableObservable<int>);
            var subscription = default(IDisposable);
            var connection = default(IDisposable);
            var results = new MockObserver<int>(scheduler);

            scheduler.Schedule(() => ys = xs.Replay(3, scheduler), Created);
            scheduler.Schedule(() => subscription = ys.Subscribe(results), 450);
            scheduler.Schedule(() => subscription.Dispose(), 475);

            scheduler.Schedule(() => connection = ys.Connect(), 300);
            scheduler.Schedule(() => connection.Dispose(), 400);

            scheduler.Schedule(() => connection = ys.Connect(), 500);
            scheduler.Schedule(() => connection.Dispose(), 550);

            scheduler.Schedule(() => connection = ys.Connect(), 650);
            scheduler.Schedule(() => connection.Dispose(), 800);

            scheduler.Run();

            results.AssertEqual(
                OnNext(451, 5),
                OnNext(452, 6),
                OnNext(453, 7)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(300, 400),
                Subscribe(500, 550),
                Subscribe(650, 800)
                );
        }

        [TestMethod]
        public void ReplayCount_MultipleConnections()
        {
            var xs = Observable.Never<int>();
            var ys = xs.Replay(3, new TestScheduler());

            var connection1 = ys.Connect();
            var connection2 = ys.Connect();

            Assert.AreSame(connection1, connection2);

            connection1.Dispose();
            connection2.Dispose();

            var connection3 = ys.Connect();

            Assert.AreNotSame(connection1, connection3);

            connection3.Dispose();
        }

        [TestMethod]
        public void ReplayCountLambda_Zip_Complete()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var results = scheduler.Run(() => xs.Replay(_xs => _xs.Take(6).Repeat(scheduler), 3, scheduler), 610);

            results.AssertEqual(
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(372, 8),
                OnNext(373, 5),
                OnNext(374, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(432, 7),
                OnNext(433, 13),
                OnNext(434, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnNext(562, 9),
                OnNext(563, 11),
                OnNext(564, 20),
                OnNext(602, 11),
                OnNext(603, 20),
                OnNext(606, 11),
                OnNext(607, 20)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );
        }

        [TestMethod]
        public void ReplayCountLambda_Zip_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnError<int>(600, new MockException(1))
                );

            var results = scheduler.Run(() => xs.Replay(_xs => _xs.Take(6).Repeat(scheduler), 3, scheduler));

            results.AssertEqual(
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(372, 8),
                OnNext(373, 5),
                OnNext(374, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(432, 7),
                OnNext(433, 13),
                OnNext(434, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnNext(562, 9),
                OnNext(563, 11),
                OnNext(564, 20),
                OnError<int>(600, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );
        }

        [TestMethod]
        public void ReplayCountLambda_Zip_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var results = scheduler.Run(() => xs.Replay(_xs => _xs.Take(6).Repeat(scheduler), 3, scheduler), 470);

            results.AssertEqual(
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(372, 8),
                OnNext(373, 5),
                OnNext(374, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(432, 7),
                OnNext(433, 13),
                OnNext(434, 2),
                OnNext(450, 9)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 470)
                );
        }

        [TestMethod]
        public void ReplayTime_Basic()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var ys = default(IConnectableObservable<int>);
            var subscription = default(IDisposable);
            var connection = default(IDisposable);
            var results = new MockObserver<int>(scheduler);

            scheduler.Schedule(() => ys = xs.Replay(TimeSpan.FromTicks(150), scheduler), Created);
            scheduler.Schedule(() => subscription = ys.Subscribe(results), 450);
            scheduler.Schedule(() => subscription.Dispose(), Disposed);

            scheduler.Schedule(() => connection = ys.Connect(), 300);
            scheduler.Schedule(() => connection.Dispose(), 400);

            scheduler.Schedule(() => connection = ys.Connect(), 500);
            scheduler.Schedule(() => connection.Dispose(), 550);

            scheduler.Schedule(() => connection = ys.Connect(), 650);
            scheduler.Schedule(() => connection.Dispose(), 800);

            scheduler.Run();

            results.AssertEqual(
                OnNext(451, 8),
                OnNext(452, 5),
                OnNext(453, 6),
                OnNext(454, 7),
                OnNext(520, 11)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(300, 400),
                Subscribe(500, 550),
                Subscribe(650, 800)
                );
        }

        [TestMethod]
        public void ReplayTime_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnError<int>(600, new MockException(1))
                );

            var ys = default(IConnectableObservable<int>);
            var subscription = default(IDisposable);
            var connection = default(IDisposable);
            var results = new MockObserver<int>(scheduler);

            scheduler.Schedule(() => ys = xs.Replay(TimeSpan.FromTicks(75), scheduler), Created);
            scheduler.Schedule(() => subscription = ys.Subscribe(results), 450);
            scheduler.Schedule(() => subscription.Dispose(), Disposed);

            scheduler.Schedule(() => connection = ys.Connect(), 300);
            scheduler.Schedule(() => connection.Dispose(), 400);

            scheduler.Schedule(() => connection = ys.Connect(), 500);
            scheduler.Schedule(() => connection.Dispose(), 800);

            scheduler.Run();

            results.AssertEqual(
                OnNext(451, 7),
                OnNext(520, 11),
                OnNext(560, 20),
                OnError<int>(600, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(300, 400),
                Subscribe(500, 600)
                );
        }

        [TestMethod]
        public void ReplayTime_Complete()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var ys = default(IConnectableObservable<int>);
            var subscription = default(IDisposable);
            var connection = default(IDisposable);
            var results = new MockObserver<int>(scheduler);

            scheduler.Schedule(() => ys = xs.Replay(TimeSpan.FromTicks(85), scheduler), Created);
            scheduler.Schedule(() => subscription = ys.Subscribe(results), 450);
            scheduler.Schedule(() => subscription.Dispose(), Disposed);

            scheduler.Schedule(() => connection = ys.Connect(), 300);
            scheduler.Schedule(() => connection.Dispose(), 400);

            scheduler.Schedule(() => connection = ys.Connect(), 500);
            scheduler.Schedule(() => connection.Dispose(), 800);

            scheduler.Run();

            results.AssertEqual(
                OnNext(451, 6),
                OnNext(452, 7),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(300, 400),
                Subscribe(500, 600)
                );
        }

        [TestMethod]
        public void ReplayTime_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var ys = default(IConnectableObservable<int>);
            var subscription = default(IDisposable);
            var connection = default(IDisposable);
            var results = new MockObserver<int>(scheduler);

            scheduler.Schedule(() => ys = xs.Replay(TimeSpan.FromTicks(100), scheduler), Created);
            scheduler.Schedule(() => subscription = ys.Subscribe(results), 450);
            scheduler.Schedule(() => subscription.Dispose(), 475);

            scheduler.Schedule(() => connection = ys.Connect(), 300);
            scheduler.Schedule(() => connection.Dispose(), 400);

            scheduler.Schedule(() => connection = ys.Connect(), 500);
            scheduler.Schedule(() => connection.Dispose(), 550);

            scheduler.Schedule(() => connection = ys.Connect(), 650);
            scheduler.Schedule(() => connection.Dispose(), 800);

            scheduler.Run();

            results.AssertEqual(
                OnNext(451, 5),
                OnNext(452, 6),
                OnNext(453, 7)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(300, 400),
                Subscribe(500, 550),
                Subscribe(650, 800)
                );
        }

        [TestMethod]
        public void ReplayTime_MultipleConnections()
        {
            var xs = Observable.Never<int>();
            var ys = xs.Replay(TimeSpan.FromTicks(100), new TestScheduler());

            var connection1 = ys.Connect();
            var connection2 = ys.Connect();

            Assert.AreSame(connection1, connection2);

            connection1.Dispose();
            connection2.Dispose();

            var connection3 = ys.Connect();

            Assert.AreNotSame(connection1, connection3);

            connection3.Dispose();
        }

        [TestMethod]
        public void ReplayTimeLambda_Zip_Complete()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var results = scheduler.Run(() => xs.Replay(_xs => _xs.Take(6).Repeat(scheduler), TimeSpan.FromTicks(50), scheduler), 610);

            results.AssertEqual(
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(372, 8),
                OnNext(373, 5),
                OnNext(374, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(432, 7),
                OnNext(433, 13),
                OnNext(434, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnNext(562, 11),
                OnNext(563, 20),
                OnNext(602, 20),
                OnNext(605, 20),
                OnNext(608, 20)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );
        }

        [TestMethod]
        public void ReplayTimeLambda_Zip_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnError<int>(600, new MockException(1))
                );

            var results = scheduler.Run(() => xs.Replay(_xs => _xs.Take(6).Repeat(scheduler), TimeSpan.FromTicks(50), scheduler));

            results.AssertEqual(
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(372, 8),
                OnNext(373, 5),
                OnNext(374, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(432, 7),
                OnNext(433, 13),
                OnNext(434, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnNext(562, 11),
                OnNext(563, 20),
                OnError<int>(600, new MockException(1))
                );
            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );
        }

        [TestMethod]
        public void ReplayTimeLambda_Zip_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var results = scheduler.Run(() => xs.Replay(_xs => _xs.Take(6).Repeat(scheduler), TimeSpan.FromTicks(50), scheduler), 470);

            results.AssertEqual(
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(372, 8),
                OnNext(373, 5),
                OnNext(374, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(432, 7),
                OnNext(433, 13),
                OnNext(434, 2),
                OnNext(450, 9)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 470)
                );
        }

        [TestMethod]
        public void Prune_Default1()
        {
            var s = new Subject<int>();
            var xs = s.Prune(Scheduler.ThreadPool);
            var ys = s.Prune();

            xs.Connect();
            ys.Connect();

            s.OnNext(1);
            s.OnNext(2);
            s.OnCompleted();

            xs.AssertEqual(ys);
        }

        [TestMethod]
        public void PruneLambda_Default1()
        {
            var xs = Observable.Range(1, 10).Prune(_xs => _xs, Scheduler.ThreadPool);
            var ys = Observable.Range(1, 10).Prune(_xs => _xs);

            xs.AssertEqual(ys);
        }

        [TestMethod]
        public void Replay_Default1()
        {
            var s = new Subject<int>();
            var xs = s.Replay(100, Scheduler.ThreadPool);
            var ys = s.Replay(100);

            xs.Connect();
            ys.Connect();

            s.OnNext(1);
            s.OnNext(2);
            s.OnCompleted();

            xs.AssertEqual(ys);
        }

        [TestMethod]
        public void Replay_Default2()
        {
            var s = new Subject<int>();
            var xs = s.Replay(TimeSpan.FromHours(1), Scheduler.ThreadPool);
            var ys = s.Replay(TimeSpan.FromHours(1));

            xs.Connect();
            ys.Connect();

            s.OnNext(1);
            s.OnNext(2);
            s.OnCompleted();

            xs.AssertEqual(ys);
        }

        [TestMethod]
        public void Replay_Default3()
        {
            var s = new Subject<int>();
            var xs = s.Replay(100, TimeSpan.FromHours(1), Scheduler.ThreadPool);
            var ys = s.Replay(100, TimeSpan.FromHours(1));

            xs.Connect();
            ys.Connect();

            s.OnNext(1);
            s.OnNext(2);
            s.OnCompleted();

            xs.AssertEqual(ys);
        }

        [TestMethod]
        public void Replay_Default4()
        {
            var s = new Subject<int>();
            var xs = s.Replay(Scheduler.ThreadPool);
            var ys = s.Replay();

            xs.Connect();
            ys.Connect();

            s.OnNext(1);
            s.OnNext(2);
            s.OnCompleted();

            xs.AssertEqual(ys);
        }

        [TestMethod]
        public void ReplayLambda_Default1()
        {
            var xs = Observable.Range(1, 10).Replay(_xs => _xs, 100, Scheduler.ThreadPool);
            var ys = Observable.Range(1, 10).Replay(_xs => _xs, 100);

            xs.AssertEqual(ys);
        }

        [TestMethod]
        public void ReplayLambda_Default2()
        {
            var xs = Observable.Range(1, 10).Replay(_xs => _xs, TimeSpan.FromHours(1), Scheduler.ThreadPool);
            var ys = Observable.Range(1, 10).Replay(_xs => _xs, TimeSpan.FromHours(1));

            xs.AssertEqual(ys);
        }

        [TestMethod]
        public void ReplayLambda_Default3()
        {
            var xs = Observable.Range(1, 10).Replay(_xs => _xs, 100, TimeSpan.FromHours(1), Scheduler.ThreadPool);
            var ys = Observable.Range(1, 10).Replay(_xs => _xs, 100, TimeSpan.FromHours(1));

            xs.AssertEqual(ys);
        }

        [TestMethod]
        public void ReplayLambda_Default4()
        {
            var xs = Observable.Range(1, 10).Replay(_xs => _xs, Scheduler.ThreadPool);
            var ys = Observable.Range(1, 10).Replay(_xs => _xs);

            xs.AssertEqual(ys);
        }


        [TestMethod]
        public void PublishWithInitialValue_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.Publish(default(IObservable<int>), 1));
            Throws<ArgumentNullException>(() => Observable.Publish(someObservable, 1, null));
            Throws<ArgumentNullException>(() => Observable.Publish(default(IObservable<int>), 1, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.Publish(default(IObservable<int>), x => x, 1));
            Throws<ArgumentNullException>(() => Observable.Publish(someObservable, default(Func<IObservable<int>, IObservable<int>>), 1));
            Throws<ArgumentNullException>(() => Observable.Publish<int, int>(someObservable, null, 1, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.Publish<int, int>(someObservable, x=> x , 1, null));
            Throws<ArgumentNullException>(() => Observable.Publish<int, int>(default(IObservable<int>), x => x, 1, DummyScheduler.Instance));
        }

        [TestMethod]
        public void PublishWithInitialValue_SanityCheck()
        {
            var someObservable = Observable.Empty<int>();

            Observable.Publish(Observable.Range(1, 10), x => x, 0).AssertEqual(Observable.Range(0, 11));
        }

        [TestMethod]
        public void PublishWithInitialValue_Basic()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var ys = default(IConnectableObservable<int>);
            var subscription = default(IDisposable);
            var connection = default(IDisposable);
            var results = new MockObserver<int>(scheduler);

            scheduler.Schedule(() => ys = xs.Publish(1979, scheduler), Created);
            scheduler.Schedule(() => subscription = ys.Subscribe(results), Subscribed);
            scheduler.Schedule(() => subscription.Dispose(), Disposed);

            scheduler.Schedule(() => connection = ys.Connect(), 300);
            scheduler.Schedule(() => connection.Dispose(), 400);

            scheduler.Schedule(() => connection = ys.Connect(), 500);
            scheduler.Schedule(() => connection.Dispose(), 550);

            scheduler.Schedule(() => connection = ys.Connect(), 650);
            scheduler.Schedule(() => connection.Dispose(), 800);

            scheduler.Run();

            results.AssertEqual(
                OnNext(201, 1979),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(520, 11)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(300, 400),
                Subscribe(500, 550),
                Subscribe(650, 800)
                );
        }

        [TestMethod]
        public void PublishWithInitialValue_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnError<int>(600, new MockException(1))
                );

            var ys = default(IConnectableObservable<int>);
            var subscription = default(IDisposable);
            var connection = default(IDisposable);
            var results = new MockObserver<int>(scheduler);

            scheduler.Schedule(() => ys = xs.Publish(1979, scheduler), Created);
            scheduler.Schedule(() => subscription = ys.Subscribe(results), Subscribed);
            scheduler.Schedule(() => subscription.Dispose(), Disposed);

            scheduler.Schedule(() => connection = ys.Connect(), 300);
            scheduler.Schedule(() => connection.Dispose(), 400);

            scheduler.Schedule(() => connection = ys.Connect(), 500);
            scheduler.Schedule(() => connection.Dispose(), 800);

            scheduler.Run();

            results.AssertEqual(
                OnNext(201, 1979),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(520, 11),
                OnNext(560, 20),
                OnError<int>(600, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(300, 400),
                Subscribe(500, 600)
                );
        }

        [TestMethod]
        public void PublishWithInitialValue_Complete()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var ys = default(IConnectableObservable<int>);
            var subscription = default(IDisposable);
            var connection = default(IDisposable);
            var results = new MockObserver<int>(scheduler);

            scheduler.Schedule(() => ys = xs.Publish(1979, scheduler), Created);
            scheduler.Schedule(() => subscription = ys.Subscribe(results), Subscribed);
            scheduler.Schedule(() => subscription.Dispose(), Disposed);

            scheduler.Schedule(() => connection = ys.Connect(), 300);
            scheduler.Schedule(() => connection.Dispose(), 400);

            scheduler.Schedule(() => connection = ys.Connect(), 500);
            scheduler.Schedule(() => connection.Dispose(), 800);

            scheduler.Run();

            results.AssertEqual(
                OnNext(201, 1979),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(300, 400),
                Subscribe(500, 600)
                );
        }

        [TestMethod]
        public void PublishWithInitialValue_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var ys = default(IConnectableObservable<int>);
            var subscription = default(IDisposable);
            var connection = default(IDisposable);
            var results = new MockObserver<int>(scheduler);

            scheduler.Schedule(() => ys = xs.Publish(1979, scheduler), Created);
            scheduler.Schedule(() => subscription = ys.Subscribe(results), Subscribed);
            scheduler.Schedule(() => subscription.Dispose(), 350);

            scheduler.Schedule(() => connection = ys.Connect(), 300);
            scheduler.Schedule(() => connection.Dispose(), 400);

            scheduler.Schedule(() => connection = ys.Connect(), 500);
            scheduler.Schedule(() => connection.Dispose(), 550);

            scheduler.Schedule(() => connection = ys.Connect(), 650);
            scheduler.Schedule(() => connection.Dispose(), 800);

            scheduler.Run();

            results.AssertEqual(
                OnNext(201, 1979),
                OnNext(340, 8)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(300, 400),
                Subscribe(500, 550),
                Subscribe(650, 800)
                );
        }

        [TestMethod]
        public void PublishWithInitialValue_MultipleConnections()
        {
            var xs = Observable.Never<int>();
            var ys = xs.Publish(1979);

            var connection1 = ys.Connect();
            var connection2 = ys.Connect();

            Assert.AreSame(connection1, connection2);

            connection1.Dispose();
            connection2.Dispose();

            var connection3 = ys.Connect();

            Assert.AreNotSame(connection1, connection3);

            connection3.Dispose();
        }

        [TestMethod]
        public void PublishWithInitialValueLambda_Zip_Complete()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var results = scheduler.Run(() => xs.Publish(_xs => _xs.Zip(_xs.Skip(1), (prev, cur) => cur + prev), 1979, scheduler));

            results.AssertEqual(
                OnNext(220, 1982),
                OnNext(280, 7),
                OnNext(290, 5),
                OnNext(340, 9),
                OnNext(360, 13),
                OnNext(370, 11),
                OnNext(390, 13),
                OnNext(410, 20),
                OnNext(430, 15),
                OnNext(450, 11),
                OnNext(520, 20),
                OnNext(560, 31),
                OnCompleted<int>(600)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );
        }

        [TestMethod]
        public void PublishWithInitialValueLambda_Zip_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnError<int>(600, new MockException(1))
                );

            var results = scheduler.Run(() => xs.Publish(_xs => _xs.Zip(_xs.Skip(1), (prev, cur) => cur + prev), 1979, scheduler));

            results.AssertEqual(
                OnNext(220, 1982),
                OnNext(280, 7),
                OnNext(290, 5),
                OnNext(340, 9),
                OnNext(360, 13),
                OnNext(370, 11),
                OnNext(390, 13),
                OnNext(410, 20),
                OnNext(430, 15),
                OnNext(450, 11),
                OnNext(520, 20),
                OnNext(560, 31),
                OnError<int>(600, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );
        }

        [TestMethod]
        public void PublishWithInitialValueLambda_Zip_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(110, 7),
                OnNext(220, 3),
                OnNext(280, 4),
                OnNext(290, 1),
                OnNext(340, 8),
                OnNext(360, 5),
                OnNext(370, 6),
                OnNext(390, 7),
                OnNext(410, 13),
                OnNext(430, 2),
                OnNext(450, 9),
                OnNext(520, 11),
                OnNext(560, 20),
                OnCompleted<int>(600)
                );

            var results = scheduler.Run(() => xs.Publish(_xs => _xs.Zip(_xs.Skip(1), (prev, cur) => cur + prev), 1979, scheduler), 470);

            results.AssertEqual(
                OnNext(220, 1982),
                OnNext(280, 7),
                OnNext(290, 5),
                OnNext(340, 9),
                OnNext(360, 13),
                OnNext(370, 11),
                OnNext(390, 13),
                OnNext(410, 20),
                OnNext(430, 15),
                OnNext(450, 11)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 470)
                );
        }

    }
}
