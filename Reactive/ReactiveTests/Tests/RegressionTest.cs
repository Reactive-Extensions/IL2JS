using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
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
    [TestClass]
    public class RegressionTest : Test
    {
        [TestMethod]
        public void Bug_1283()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(100, 1),
                OnNext(220, 2),
                OnNext(240, 3),
                OnNext(300, 4),
                OnNext(310, 5),
                OnCompleted<int>(350)
                );

            var results = scheduler.Run(() => xs.BufferWithTime(TimeSpan.FromTicks(100), scheduler).Select(ys => string.Join(" ", ys.Select(y => y.ToString()).ToArray())));

            results.AssertEqual(
                OnNext(300, "2 3 4"),
                OnNext(350, "5"),
                OnCompleted<string>(350)
                );
        }

        [TestMethod]
        public void Bug_1261()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(205, 1),
                OnNext(210, 2),
                OnNext(215, 3),
                OnNext(220, 4),
                OnNext(225, 5),
                OnNext(230, 6),
                OnCompleted<int>(230));

            var results = scheduler.Run(() => xs.BufferWithTime(TimeSpan.FromTicks(10), scheduler).Select(ys => string.Join(" ", ys.Select(y => y.ToString()).ToArray())));

            results.AssertEqual(
                OnNext(210, "1 2"),
                OnNext(220, "3 4"),
                OnNext(230, "5 6"),
                OnCompleted<string>(230)
                );
        }

        [TestMethod]
        public void Bug_1130()
        {
            var xs = Observable.Start(() => 5);
            Assert.IsNull(xs as ISubject<int, int>);
        }

        [TestMethod]
        public void Bug_1286()
        {
            var infinite = Observable.Return(new { Name = "test", Value = 0d }).Repeat(Scheduler.ThreadPool);
            var grouped = infinite.GroupBy(x => x.Name, x => x.Value);
            var disp = grouped.Subscribe(_ => { });
            Thread.Sleep(1);
            //most of the time, this deadlocks
            disp.Dispose();
            disp = grouped.Subscribe(_ => { });
            Thread.Sleep(1);
            //if the first doesn't this one always
            disp.Dispose();
        }

        [TestMethod]
        public void Bug_1287()
        {
            var flag = false;
            var x = Observable.Return(1, Scheduler.CurrentThread).Concat(Observable.Never<int>()).OnDispose(() => flag = true).First();
            Assert.AreEqual(1, x);
            Assert.IsTrue(flag);
        }

#if !NETCF37
        static IEnumerable<int> Bug_1333_Enumerable(AsyncSubject<IDisposable> s, Semaphore sema)
        {
            var d = s.First();
            var t = new Thread(() => { d.Dispose(); sema.Release(); });
            t.Start();
            t.Join();
            yield return 1;
        }

        [TestMethod]
        [Timeout(1000)]
        public void Bug_1333()
        {
            var sema = new Semaphore(0, 1);
            var d = new AsyncSubject<IDisposable>();
            var e = Bug_1333_Enumerable(d, sema).ToObservable(Scheduler.ThreadPool).Subscribe();
            d.OnNext(e);
            d.OnCompleted();
            sema.WaitOne();
        }
#endif
        [TestMethod]
        public void Bug_1263()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Interval(TimeSpan.FromTicks(100), scheduler).Do(_ => scheduler.Sleep(TimeSpan.FromTicks(50))));

            results.AssertEqual(
                OnNext(350, 0L),
                OnNext(450, 1L),
                OnNext(550, 2L),
                OnNext(650, 3L),
                OnNext(750, 4L),
                OnNext(850, 5L),
                OnNext(950, 6L)
                );
        }

        [TestMethod]
        public void Bug_1295_Completed()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(300, 1),
                OnNext(350, 2),
                OnNext(500, 3),
                OnCompleted<int>(550)
                );

            var results = scheduler.Run(() => xs.Throttle(TimeSpan.FromTicks(100), scheduler));

            results.AssertEqual(
                OnNext(450, 2),
                OnNext(550, 3),
                OnCompleted<int>(550)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 550)
                );
        }

        [TestMethod]
        public void Bug_1295_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(300, 1),
                OnNext(350, 2),
                OnNext(500, 3),
                OnError<int>(550, new MockException(1))
                );

            var results = scheduler.Run(() => xs.Throttle(TimeSpan.FromTicks(100), scheduler));

            results.AssertEqual(
                OnNext(450, 2),
                OnError<int>(550, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 550)
                );
        }

        [TestMethod]
        public void Bug_1297_Catch_None()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Catch<int>(scheduler));

            results.AssertEqual(
                OnCompleted<int>(201)
                );
        }

        [TestMethod]
        public void Bug_1297_OnErrorResumeNext_None()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.OnErrorResumeNext<int>(scheduler));

            results.AssertEqual(
                OnCompleted<int>(201)
                );
        }

        [TestMethod]
        public void Bug_1297_Catch_Single()
        {
            var scheduler = new TestScheduler();

            var xs = Observable.Throw<int>(new MockException(1), scheduler);

            var results = scheduler.Run(() => Observable.Catch(scheduler, xs));

            results.AssertEqual(
                OnError<int>(203, new MockException(1))
                );
        }

        [TestMethod]
        public void Bug_1297_OnErrorResumeNext_Single()
        {
            var scheduler = new TestScheduler();

            var xs = Observable.Throw<int>(new MockException(1), scheduler);

            var results = scheduler.Run(() => Observable.OnErrorResumeNext(scheduler, xs));

            results.AssertEqual(
                OnCompleted<int>(203)
                );
        }

        [TestMethod]
        public void Bug_1297_Catch_Multi()
        {
            var scheduler = new TestScheduler();

            var xs = Observable.Throw<int>(new MockException(1), scheduler);
            var ys = Observable.Throw<int>(new MockException(2), scheduler);
            var zs = Observable.Throw<int>(new MockException(3), scheduler);

            var results = scheduler.Run(() => Observable.Catch(scheduler, xs, ys, zs));

            results.AssertEqual(
                OnError<int>(207, new MockException(3))
                );
        }

        [TestMethod]
        public void Bug_1297_OnErrorResumeNext_Multi()
        {
            var scheduler = new TestScheduler();

            var xs = Observable.Throw<int>(new MockException(1), scheduler);
            var ys = Observable.Throw<int>(new MockException(2), scheduler);
            var zs = Observable.Throw<int>(new MockException(3), scheduler);

            var results = scheduler.Run(() => Observable.OnErrorResumeNext(scheduler, xs, ys, zs));

            results.AssertEqual(
                OnCompleted<int>(207)
                );
        }

        [TestMethod]
        public void Bug_1380()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(220, 1),
                OnNext(250, 2),
                OnNext(270, 3),
                OnNext(290, 4),
                OnNext(310, 5),
                OnNext(340, 6),
                OnNext(360, 7),
                OnError<int>(380, new MockException(1))
                );

            var results = scheduler.Run(() => xs.Delay(TimeSpan.FromTicks(100), scheduler));

            results.AssertEqual(
                OnNext(320, 1),
                OnNext(350, 2),
                OnNext(370, 3),
                OnError<int>(380, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 380)
                );
        }

        [TestMethod]
        public void Bug_1304_Completed()
        {
            var scheduler = new TestScheduler();

            var t = 0L;

            var xs = scheduler.CreateHotObservable(
                OnCompleted<int>(300)
                );

            var results = scheduler.Run(() => xs.Finally(() =>
                {
                    scheduler.Sleep(TimeSpan.FromTicks(100));
                    t = scheduler.Ticks;
                }));

            results.AssertEqual(
                OnCompleted<int>(300)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 300)
                );

            Assert.AreEqual(400, t);
        }

        [TestMethod]
        public void Bug_1304_Error()
        {
            var scheduler = new TestScheduler();

            var t = 0L;

            var xs = scheduler.CreateHotObservable(
                OnError<int>(300, new MockException(1))
                );

            var results = scheduler.Run(() => xs.Finally(() =>
            {
                scheduler.Sleep(TimeSpan.FromTicks(100));
                t = scheduler.Ticks;
            }));

            results.AssertEqual(
                OnError<int>(300, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 300)
                );

            Assert.AreEqual(400, t);
        }

        [TestMethod]
        public void Bug_1356()
        {
            var run = false;
            Observable.Range(0, 10).Finally(() => run = true).Take(5).Run();
            Assert.IsTrue(run);
        }

        [TestMethod]
        public void Bug_1381()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext( 90, 1),
                OnNext(110, 2),
                OnNext(250, 3),
                OnNext(270, 4),
                OnNext(280, 5),
                OnNext(301, 6),
                OnNext(302, 7),
                OnNext(400, 8),
                OnNext(401, 9),
                OnNext(510, 10)
                );

            var results = new MockObserver<int>(scheduler);
            var ys = default(IConnectableObservable<int>);
            var connection = default(IDisposable);
            var subscription = default(IDisposable);

            scheduler.Schedule(() => ys = xs.Replay(scheduler), 100);
            scheduler.Schedule(() => connection = ys.Connect(), 200);
            scheduler.Schedule(() => subscription = ys.Subscribe(results), 300);
            scheduler.Schedule(() => subscription.Dispose(), 500);
            scheduler.Schedule(() => connection.Dispose(), 600);

            scheduler.Run();

            results.AssertEqual(
                OnNext(301, 3),
                OnNext(302, 4),
                OnNext(303, 5),
                OnNext(304, 6),
                OnNext(305, 7),
                OnNext(400, 8),
                OnNext(401, 9)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 600)
                );
        }

        [TestMethod]
        public void Bug_1302_SelectorThrows_LeftLast()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(210, 1),
                OnCompleted<int>(220)
                );

            var ys = scheduler.CreateHotObservable(
                OnNext(215, 2),
                OnCompleted<int>(217)
                );

            var results = scheduler.Run(() => xs.ForkJoin<int, int, int>(ys, (x, y) => { throw new MockException(1); }));

            results.AssertEqual(
                OnError<int>(220, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 220)
                );

            ys.Subscriptions.AssertEqual(
                Subscribe(200, 217)
                );
        }

        [TestMethod]
        public void Bug_1302_SelectorThrows_RightLast()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(210, 1),
                OnCompleted<int>(217)
                );

            var ys = scheduler.CreateHotObservable(
                OnNext(215, 2),
                OnCompleted<int>(220)
                );

            var results = scheduler.Run(() => xs.ForkJoin<int, int, int>(ys, (x, y) => { throw new MockException(1); }));

            results.AssertEqual(
                OnError<int>(220, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 217)
                );

            ys.Subscriptions.AssertEqual(
                Subscribe(200, 220)
                );
        }

        [TestMethod]
        public void Bug_1302_RightLast_NoLeft()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnCompleted<int>(217)
                );

            var ys = scheduler.CreateHotObservable(
                OnNext(215, 2),
                OnCompleted<int>(220)
                );

            var results = scheduler.Run(() => xs.ForkJoin<int, int, int>(ys, (x, y) => x + y));

            results.AssertEqual(
                OnCompleted<int>(220)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 217)
                );

            ys.Subscriptions.AssertEqual(
                Subscribe(200, 220)
                );
        }

        [TestMethod]
        public void Bug_1302_RightLast_NoRight()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(215, 2),
                OnCompleted<int>(217)
                );

            var ys = scheduler.CreateHotObservable(
                OnCompleted<int>(220)
                );

            var results = scheduler.Run(() => xs.ForkJoin<int, int, int>(ys, (x, y) => x + y));

            results.AssertEqual(
                OnCompleted<int>(220)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 217)
                );

            ys.Subscriptions.AssertEqual(
                Subscribe(200, 220)
                );
        }
    }
}
