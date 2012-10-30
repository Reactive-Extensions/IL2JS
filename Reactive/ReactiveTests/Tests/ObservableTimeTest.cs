using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReactiveTests.Dummies;
using ReactiveTests.Mocks;
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
    public class ObservableTimeTest : Test
    {
        [TestMethod]
        public void OneShotTimer_TimeSpan_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Timer(TimeSpan.Zero, null));
            Throws<ArgumentNullException>(() => Observable.Timer(TimeSpan.Zero, DummyScheduler.Instance).Subscribe(null));
            Throws<ArgumentNullException>(() => Observable.Timer(DateTimeOffset.Now, null));
            Throws<ArgumentNullException>(() => Observable.Timer(TimeSpan.Zero, TimeSpan.Zero, null));
            Throws<ArgumentNullException>(() => Observable.Timer(DateTimeOffset.Now, TimeSpan.Zero, null));
        }

        [TestMethod]
        public void OneShotTimer_TimeSpan_Basic()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Timer(TimeSpan.FromTicks(300), scheduler));

            results.AssertEqual(
                OnNext(500, 0L),
                OnCompleted<long>(500));
        }

        [TestMethod]
        public void OneShotTimer_TimeSpan_Zero()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Timer(TimeSpan.FromTicks(0), scheduler));

            results.AssertEqual(
                OnNext(201, 0L),
                OnCompleted<long>(201));
        }

        [TestMethod]
        public void OneShotTimer_TimeSpan_Negative()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Timer(TimeSpan.FromTicks(-1), scheduler));

            results.AssertEqual(
                OnNext(201, 0L),
                OnCompleted<long>(201));
        }

        [TestMethod]
        public void OneShotTimer_TimeSpan_Disposed()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Timer(TimeSpan.FromTicks(1000), scheduler));

            results.AssertEqual();
        }

        [TestMethod]
        public void OneShotTimer_TimeSpan_Infinite()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Timer(TimeSpan.MaxValue, scheduler));

            results.AssertEqual();
        }

        [TestMethod]
        public void OneShotTimer_TimeSpan_ObserverThrows()
        {
            var scheduler1 = new TestScheduler();

            var xs = Observable.Timer(TimeSpan.FromTicks(1), scheduler1);

            xs.Subscribe(x => { throw new InvalidOperationException(); });

            Throws<InvalidOperationException>(() => scheduler1.Run());

            var scheduler2 = new TestScheduler();

            var ys = Observable.Timer(TimeSpan.FromTicks(1), scheduler2);

            ys.Subscribe(x => { }, ex => { }, () => { throw new InvalidOperationException(); });

            Throws<InvalidOperationException>(() => scheduler2.Run());
        }

        [TestMethod]
        public void OneShotTimer_TimeSpan_DefaultScheduler()
        {
            Assert.IsTrue(Observable.Timer(TimeSpan.FromMilliseconds(1)).ToEnumerable().SequenceEqual(new[] { 0L }));
        }

        [TestMethod]
        public void OneShotTimer_DateTimeOffset_DefaultScheduler()
        {
            Assert.IsTrue(Observable.Timer(DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1)).ToEnumerable().SequenceEqual(new[] { 0L }));
        }

        [TestMethod]
        public void OneShotTimer_TimeSpan_TimeSpan_DefaultScheduler()
        {
            Assert.IsTrue(Observable.Timer(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)).ToEnumerable().Take(2).SequenceEqual(new[] { 0L, 1L }));
        }

        [TestMethod]
        public void OneShotTimer_DateTimeOffset_TimeSpan_DefaultScheduler()
        {
            Assert.IsTrue(Observable.Timer(DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(1)).ToEnumerable().Take(2).SequenceEqual(new[] { 0L, 1L }));
        }

        [TestMethod]
        public void Interval_TimeSpan_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Interval(TimeSpan.Zero, null));
            Throws<ArgumentNullException>(() => Observable.Interval(TimeSpan.Zero, DummyScheduler.Instance).Subscribe(null));
        }

        [TestMethod]
        public void Interval_TimeSpan_Basic()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Interval(TimeSpan.FromTicks(100), scheduler));

            results.AssertEqual(
                OnNext(300, 0L),
                OnNext(400, 1L),
                OnNext(500, 2L),
                OnNext(600, 3L),
                OnNext(700, 4L),
                OnNext(800, 5L),
                OnNext(900, 6L)
                );
        }

        [TestMethod]
        public void Interval_TimeSpan_Zero()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Interval(TimeSpan.FromTicks(0), scheduler), 210);

            results.AssertEqual(
                OnNext(201, 0L),
                OnNext(202, 1L),
                OnNext(203, 2L),
                OnNext(204, 3L),
                OnNext(205, 4L),
                OnNext(206, 5L),
                OnNext(207, 6L),
                OnNext(208, 7L),
                OnNext(209, 8L)
                );
        }

        [TestMethod]
        public void Interval_TimeSpan_Negative()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Interval(TimeSpan.FromTicks(-1), scheduler), 210);

            results.AssertEqual(
                OnNext(201, 0L),
                OnNext(202, 1L),
                OnNext(203, 2L),
                OnNext(204, 3L),
                OnNext(205, 4L),
                OnNext(206, 5L),
                OnNext(207, 6L),
                OnNext(208, 7L),
                OnNext(209, 8L)
                );
        }

        [TestMethod]
        public void Interval_TimeSpan_Disposed()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Interval(TimeSpan.FromTicks(1000), scheduler));

            results.AssertEqual();
        }

        [TestMethod]
        public void Interval_TimeSpan_ObserverThrows()
        {
            var scheduler = new TestScheduler();

            var xs = Observable.Interval(TimeSpan.FromTicks(1), scheduler);

            xs.Subscribe(x => { throw new InvalidOperationException(); });

            Throws<InvalidOperationException>(() => scheduler.Run());
        }

        [TestMethod]
        public void Interval_TimeSpan_DefaultScheduler()
        {
            Assert.IsTrue(Observable.Interval(TimeSpan.FromMilliseconds(1)).ToEnumerable().Take(3).SequenceEqual(new[] { 0L, 1L, 2L }));
        }

        [TestMethod]
        public void Delay_ArgumentChecking()
        {
            var scheduler = new TestScheduler();
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.Delay(default(IObservable<int>), DateTimeOffset.Now));
            Throws<ArgumentNullException>(() => Observable.Delay(default(IObservable<int>), TimeSpan.Zero));
            Throws<ArgumentNullException>(() => Observable.Delay(default(IObservable<int>), DateTimeOffset.Now, scheduler));
            Throws<ArgumentNullException>(() => Observable.Delay(default(IObservable<int>), TimeSpan.Zero, scheduler));
            Throws<ArgumentNullException>(() => Observable.Delay(someObservable, DateTimeOffset.Now, null));
            Throws<ArgumentNullException>(() => Observable.Delay(someObservable, TimeSpan.Zero, null));
        }

        [TestMethod]
        public void Delay_TimeSpan_Zero()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(250, 2),
                OnNext(350, 3),
                OnNext(450, 4),
                OnCompleted<int>(550)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            const ushort delay = 1;

            var results = scheduler.Run(() => xs.Delay(TimeSpan.Zero, scheduler));
            var expected = from n in msgs
                           where n.Time > ObservableTest.Subscribed
                           select new Recorded<Notification<int>>((ushort)(n.Time + delay), n.Value);

            results.AssertEqual(expected);
        }

        [TestMethod]
        public void Delay_TimeSpan_Positive()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(250, 2),
                OnNext(350, 3),
                OnNext(450, 4),
                OnCompleted<int>(550)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            const ushort delay = 42;

            var results = scheduler.Run(() => xs.Delay(TimeSpan.FromTicks(delay), scheduler));
            var expected = from n in msgs
                           where n.Time > ObservableTest.Subscribed
                           select new Recorded<Notification<int>>((ushort)(n.Time + delay), n.Value);

            results.AssertEqual(expected);
        }

        [TestMethod]
        public void Delay_Empty()
        {
            var scheduler = new TestScheduler();

            const ushort delay = 10;
            var results = scheduler.Run(() => Observable.Empty<int>(scheduler).Delay(TimeSpan.FromTicks(delay), scheduler));

            results.AssertEqual(OnCompleted<int>(ObservableTest.Subscribed + delay + 1 /* CHECK */));
        }

        [TestMethod]
        public void Delay_Error()
        {
            var scheduler = new TestScheduler();

            const ushort delay = 10;
            var ex = new Exception("Oops");
            var results = scheduler.Run(() => Observable.Throw<int>(ex, scheduler).Delay(TimeSpan.FromTicks(delay), scheduler));

            results.AssertEqual(OnError<int>(201, ex));
        }

        [TestMethod]
        public void Delay_Never()
        {
            var scheduler = new TestScheduler();

            const ushort delay = 10;
            var results = scheduler.Run(() => Observable.Never<int>().Delay(TimeSpan.FromTicks(delay), scheduler));

            results.AssertEqual();
        }

        [TestMethod]
        public void Delay_TimeSpan_DefaultScheduler()
        {
            Assert.IsTrue(Observable.Return(1).Delay(TimeSpan.FromMilliseconds(1)).ToEnumerable().SequenceEqual(new[] { 1 }));
        }

        [TestMethod]
        public void Delay_DateTimeOffset_DefaultScheduler()
        {
            Assert.IsTrue(Observable.Return(1).Delay(DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1)).ToEnumerable().SequenceEqual(new[] { 1 }));
        }

        [TestMethod]
        public void Delay_CrossingMessages()
        {
            var lst = new List<int>();

            var evt = new ManualResetEvent(false);

            var s = new Subject<int>();
            s.Delay(TimeSpan.FromSeconds(0.01)).Subscribe(x =>
            {
                lst.Add(x);
                if (x < 9)
                    s.OnNext(x + 1);
                else
                    s.OnCompleted();
            }, () =>
            {
                evt.Set();
            });
            s.OnNext(0);

            evt.WaitOne();

            Assert.IsTrue(Enumerable.Range(0, 10).SequenceEqual(lst));
        }

        [TestMethod]
        public void Throttle_ArgumentChecking()
        {
            var scheduler = new TestScheduler();
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.Throttle(default(IObservable<int>), TimeSpan.Zero));
            Throws<ArgumentNullException>(() => Observable.Throttle(someObservable, TimeSpan.Zero, null));
            Throws<ArgumentNullException>(() => Observable.Throttle(default(IObservable<int>), TimeSpan.Zero, scheduler));
        }

        private IEnumerable<Recorded<Notification<T>>> Generate<T, S>(S seed, Func<S, bool> condition, Func<S, S> iterate, Func<S, Recorded<Notification<T>>> selector, Func<S, Recorded<Notification<T>>> final)
        {
            S s;
            for (s = seed; condition(s); s = iterate(s))
                yield return selector(s);

            yield return final(s);
        }

        [TestMethod]
        public void Throttle_TimeSpan_AllPass()
        {
            var scheduler = new TestScheduler();

            const ushort delta = 50;

            var msgs = Generate(
                new { value = 1, time = (ushort)150 /*(ObservableTest.Subscribed - delta)*/ },
                s => s.time <= delta * 10,
                s => new { value = s.value + 1, time = (ushort)(s.time + delta) },
                s => OnNext(s.time, s.value),
                s => OnCompleted<int>(s.time)
            ).ToArray();

            var xs = scheduler.CreateHotObservable(msgs);

            const ushort throttleLength = delta - 10; /* < delta */

            var results = scheduler.Run(() => xs.Throttle(TimeSpan.FromTicks(throttleLength), scheduler));
            var expected = from n in msgs
                           where n.Time > ObservableTest.Subscribed
                           let value = n.Value
                           let time = value is Notification<int>.OnNext ? n.Time + throttleLength : n.Time
                           select new Recorded<Notification<int>>((ushort)time, value);

            results.AssertEqual(expected);
        }

        [TestMethod]
        public void Throttle_TimeSpan_AllPass_ErrorEnd()
        {
            var scheduler = new TestScheduler();

            const ushort delta = 50;

            var msgs = Generate(
                new { value = 1, time = (ushort)150 /*(ObservableTest.Subscribed - delta)*/ },
                s => s.time <= delta * 10,
                s => new { value = s.value + 1, time = (ushort)(s.time + delta) },
                s => OnNext(s.time, s.value),
                s => OnError<int>(s.time, new Exception("End"))
            ).ToArray();

            var xs = scheduler.CreateHotObservable(msgs);

            const ushort throttleLength = delta - 10; /* < delta */

            var results = scheduler.Run(() => xs.Throttle(TimeSpan.FromTicks(throttleLength), scheduler));
            var expected = from n in msgs
                           where n.Time > ObservableTest.Subscribed
                           let value = n.Value
                           let time = value is Notification<int>.OnNext ? n.Time + throttleLength : n.Time
                           select new Recorded<Notification<int>>((ushort)time, value);

            results.AssertEqual(expected);
        }

        [TestMethod]
        public void Throttle_TimeSpan_AllDrop()
        {
            var scheduler = new TestScheduler();

            const ushort delta = 50;

            Recorded<Notification<int>> previous = null;
            Recorded<Notification<int>> final = null;
            var msgs = Generate(
                new { value = 1, time = (ushort)150 /*(ObservableTest.Subscribed - delta)*/ },
                s => s.time <= delta * 10,
                s => new { value = s.value + 1, time = (ushort)(s.time + delta) },
                s => { previous = OnNext(s.time, s.value); return previous; },
                s => final = OnCompleted<int>(s.time)
            ).ToArray();

            var xs = scheduler.CreateHotObservable(msgs);

            const ushort throttleLength = delta + 10; /* > delta */

            var results = scheduler.Run(() => xs.Throttle(TimeSpan.FromTicks(throttleLength), scheduler));
            var expected = new Recorded<Notification<int>>[] { OnNext(final.Time, previous.Value.Value), final };

            results.AssertEqual(expected);
        }

        [TestMethod]
        public void Throttle_TimeSpan_AllDrop_ErrorEnd()
        {
            var scheduler = new TestScheduler();

            const ushort delta = 50;

            Recorded<Notification<int>> final = null;
            var msgs = Generate(
                new { value = 1, time = (ushort)150 /*(ObservableTest.Subscribed - delta)*/ },
                s => s.time <= delta * 10,
                s => new { value = s.value + 1, time = (ushort)(s.time + delta) },
                s => OnNext(s.time, s.value),
                s => final = OnError<int>(s.time, new Exception("End"))
            ).ToArray();

            var xs = scheduler.CreateHotObservable(msgs);

            const ushort throttleLength = delta + 10; /* > delta */

            var results = scheduler.Run(() => xs.Throttle(TimeSpan.FromTicks(throttleLength), scheduler));
            var expected = new[] { final };

            results.AssertEqual(expected);
        }

        [TestMethod]
        public void Throttle_TimeSpan_SomeDrop()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(250, 2),
                OnNext(350, 3), // drop due to 370
                OnNext(370, 4), // new window till 420
                OnNext(421, 5),
                OnNext(480, 6), // drop due to 490
                OnNext(490, 7), // drop due to 500
                OnNext(500, 8), // new window till 550
                OnCompleted<int>(600)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            const int throttleLength = 50;

            var results = scheduler.Run(() => xs.Throttle(TimeSpan.FromTicks(throttleLength), scheduler));
            var expected = from n in msgs
                           where n.Time > ObservableTest.Subscribed && n.Time != 350 && n.Time != 480 && n.Time != 490
                           let value = n.Value
                           let time = value is Notification<int>.OnNext ? n.Time + throttleLength : n.Time
                           select new Recorded<Notification<int>>((ushort)time, value);

            results.AssertEqual(expected);
        }

        [TestMethod]
        public void Throttle_Empty()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Empty<int>(scheduler).Throttle(TimeSpan.FromTicks(10), scheduler));

            results.AssertEqual(OnCompleted<int>(ObservableTest.Subscribed + 1 /* CHECK */));
        }

        [TestMethod]
        public void Throttle_Error()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception("Oops");
            var results = scheduler.Run(() => Observable.Throw<int>(ex, scheduler).Throttle(TimeSpan.FromTicks(10), scheduler));

            results.AssertEqual(OnError<int>(ObservableTest.Subscribed + 1 /* CHECK */, ex));
        }

        [TestMethod]
        public void Throttle_Never()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Never<int>().Throttle(TimeSpan.FromTicks(10), scheduler));

            results.AssertEqual();
        }

        [TestMethod]
        public void Throttle_DefaultScheduler()
        {
            Assert.IsTrue(Observable.Return(1).Throttle(TimeSpan.FromMilliseconds(1)).ToEnumerable().SequenceEqual(new [] { 1 }));
        }

        [TestMethod]
        public void Buffer_Time_ArgumentChecking()
        {
            var scheduler = new TestScheduler();
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.BufferWithTime(default(IObservable<int>), TimeSpan.Zero));
            Throws<ArgumentNullException>(() => Observable.BufferWithTime(someObservable, TimeSpan.Zero, null));
            Throws<ArgumentNullException>(() => Observable.BufferWithTime(default(IObservable<int>), TimeSpan.Zero, scheduler));
            Throws<ArgumentNullException>(() => Observable.BufferWithTime(default(IObservable<int>), TimeSpan.Zero, TimeSpan.Zero));
            Throws<ArgumentNullException>(() => Observable.BufferWithTime(someObservable, TimeSpan.Zero, TimeSpan.Zero, null));
            Throws<ArgumentNullException>(() => Observable.BufferWithTime(default(IObservable<int>), TimeSpan.Zero, TimeSpan.Zero, scheduler));
        }

        [TestMethod]
        public void Buffer_TimeSpan_PartialWindow()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.BufferWithTime(TimeSpan.FromTicks(100), scheduler)).ToArray();
            Assert.AreEqual(2, results.Length, "length");
            Assert.IsTrue(results[0].Value.Value.SequenceEqual(new int[] { 2, 3, 4, 5 }) && results[0].Time == 250, "first");
            Assert.IsTrue(results[1].Value is Notification<IList<int>>.OnCompleted && results[1].Time == 250, "completed");
        }

        [TestMethod]
        public void Buffer_TimeSpan_Boundaries()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.BufferWithTime(TimeSpan.FromTicks(10), scheduler)).ToArray();
            Assert.AreEqual(6, results.Length, "length");
            for (int i = 0; i < 4; i++)
            {
                Assert.IsTrue(results[i].Value.Value.SequenceEqual(new int[] { i + 2 }) && results[i].Time == 200 + (i + 1) * 10, "element");
            }
            Assert.IsTrue(results[4].Value.Value.SequenceEqual(new int[] { }) && results[4].Time == 250, "empty");
            Assert.IsTrue(results[5].Value is Notification<IList<int>>.OnCompleted && results[5].Time == 250, "completed");
        }

        [TestMethod]
        public void Buffer_TimeSpan_FullWindows()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.BufferWithTime(TimeSpan.FromTicks(30), scheduler)).ToArray();
            Assert.AreEqual(3, results.Length, "length");
            Assert.IsTrue(results[0].Value.Value.SequenceEqual(new int[] { 2, 3, 4 }) && results[0].Time == 230, "first");
            Assert.IsTrue(results[1].Value.Value.SequenceEqual(new int[] { 5 }) && results[1].Time == 250, "second");
            Assert.IsTrue(results[2].Value is Notification<IList<int>>.OnCompleted && results[2].Time == 250, "completed");
        }

        [TestMethod]
        public void Buffer_TimeSpan_FullAndPartialWindows()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.BufferWithTime(TimeSpan.FromTicks(35), scheduler)).ToArray();
            Assert.AreEqual(3, results.Length, "length");
            Assert.IsTrue(results[0].Value.Value.SequenceEqual(new int[] { 2, 3, 4 }) && results[0].Time == 235, "first");
            Assert.IsTrue(results[1].Value.Value.SequenceEqual(new int[] { 5 }) && results[1].Time == 250, "second");
            Assert.IsTrue(results[2].Value is Notification<IList<int>>.OnCompleted && results[2].Time == 250, "completed");
        }

        [TestMethod]
        public void Buffer_TimeSpan_Error()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnError<int>(250, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.BufferWithTime(TimeSpan.FromTicks(50), scheduler)).ToArray();
            Assert.AreEqual(2, results.Length, "length");
            Assert.IsTrue(results[0].Value.Value.SequenceEqual(new int[] { 2, 3, 4, 5 }) && results[0].Time == 250, "first");
            Assert.IsTrue(results[1].Value is Notification<IList<int>>.OnError && results[1].Time == 250, "completed");
        }

        [TestMethod]
        public void Buffer_TimeSpan_Skip_Less()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.BufferWithTime(TimeSpan.FromTicks(35), TimeSpan.FromTicks(15), scheduler)).ToArray();
            Assert.AreEqual(3, results.Length, "length");
            Assert.IsTrue(results[0].Value.Value.SequenceEqual(new int[] { 2, 3, 4 }) && results[0].Time == 235, "first");
            Assert.IsTrue(results[1].Value.Value.SequenceEqual(new int[] { 3, 4, 5 }) && results[1].Time == 250, "second");
            Assert.IsTrue(results[2].Value is Notification<IList<int>>.OnCompleted && results[2].Time == 250, "completed");
        }

        [TestMethod]
        public void Buffer_TimeSpan_Skip_More()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.BufferWithTime(TimeSpan.FromTicks(25), TimeSpan.FromTicks(35), scheduler)).ToArray();
            Assert.AreEqual(3, results.Length, "length");
            Assert.IsTrue(results[0].Value.Value.SequenceEqual(new int[] { 2, 3 }) && results[0].Time == 225, "first");
            Assert.IsTrue(results[1].Value.Value.SequenceEqual(new int[] { 5 }) && results[1].Time == 250, "second");
            Assert.IsTrue(results[2].Value is Notification<IList<int>>.OnCompleted && results[2].Time == 250, "completed");
        }

        [TestMethod]
        public void Buffer_TimeSpan_DefaultScheduler()
        {
            Assert.IsTrue(Observable.Return(1).BufferWithTime(TimeSpan.FromSeconds(1)).ToEnumerable().Count() == 1);
        }

        [TestMethod]
        public void Buffer_TimeSpan_TimeSpan_DefaultScheduler()
        {
            Assert.IsTrue(Observable.Return(1).BufferWithTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)).ToEnumerable().Count() == 1);
        }

        [TestMethod]
        public void Buffer_Time_Count_ArgumentChecking()
        {
            var scheduler = new TestScheduler();
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.BufferWithTimeOrCount(default(IObservable<int>), TimeSpan.Zero, 1));
            Throws<ArgumentOutOfRangeException>(() => Observable.BufferWithTimeOrCount(someObservable, TimeSpan.Zero, 0));
            Throws<ArgumentOutOfRangeException>(() => Observable.BufferWithTimeOrCount(someObservable, TimeSpan.Zero, -1));
            Throws<ArgumentNullException>(() => Observable.BufferWithTimeOrCount(default(IObservable<int>), TimeSpan.Zero, 1, scheduler));
            Throws<ArgumentOutOfRangeException>(() => Observable.BufferWithTimeOrCount(someObservable, TimeSpan.Zero, 0, scheduler));
            Throws<ArgumentOutOfRangeException>(() => Observable.BufferWithTimeOrCount(someObservable, TimeSpan.Zero, -1, scheduler));
            Throws<ArgumentNullException>(() => Observable.BufferWithTimeOrCount(someObservable, TimeSpan.Zero, 1, default(IScheduler)));
        }

        [TestMethod]
        public void Buffer_Time_Count_TimeWins()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnNext(250, 6),
                OnNext(260, 7),
                OnNext(270, 8),
                OnNext(280, 9),
                OnCompleted<int>(290)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.BufferWithTimeOrCount(TimeSpan.FromTicks(50), 99, scheduler)).ToArray();
            Assert.AreEqual(3, results.Length, "length");
            Assert.IsTrue(results[0].Value.Value.SequenceEqual(new int[] { 2, 3, 4, 5, 6 }) && results[0].Time == 250, "first");
            Assert.IsTrue(results[1].Value.Value.SequenceEqual(new int[] { 7, 8, 9 }) && results[1].Time == 290, "partial");
            Assert.IsTrue(results[2].Value is Notification<IList<int>>.OnCompleted && results[2].Time == 290, "completed");
        }

        [TestMethod]
        public void Buffer_Time_Count_TimeWins_Error()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnNext(250, 6),
                OnNext(260, 7),
                OnNext(270, 8),
                OnNext(280, 9),
                OnError<int>(290, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.BufferWithTimeOrCount(TimeSpan.FromTicks(50), 99, scheduler)).ToArray();
            Assert.AreEqual(3, results.Length, "length");
            Assert.IsTrue(results[0].Value.Value.SequenceEqual(new int[] { 2, 3, 4, 5, 6 }) && results[0].Time == 250, "first");
            Assert.IsTrue(results[1].Value.Value.SequenceEqual(new int[] { 7, 8, 9 }) && results[1].Time == 290, "partial");
            Assert.IsTrue(results[2].Value.Exception == ex && results[2].Time == 290, "completed");
        }

        [TestMethod]
        public void Buffer_Time_Count_CountWins()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnNext(250, 6),
                OnNext(260, 7),
                OnNext(270, 8),
                OnNext(280, 9),
                OnCompleted<int>(290)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.BufferWithTimeOrCount(TimeSpan.FromTicks(999), 5, scheduler)).ToArray();
            Assert.AreEqual(3, results.Length, "length");
            Assert.IsTrue(results[0].Value.Value.SequenceEqual(new int[] { 2, 3, 4, 5, 6 }) && results[0].Time == 250, "first");
            Assert.IsTrue(results[1].Value.Value.SequenceEqual(new int[] { 7, 8, 9 }) && results[1].Time == 290, "partial");
            Assert.IsTrue(results[2].Value is Notification<IList<int>>.OnCompleted && results[2].Time == 290, "completed");
        }

        [TestMethod]
        public void Buffer_Time_Count_CountWins_Error()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnNext(250, 6),
                OnNext(260, 7),
                OnNext(270, 8),
                OnNext(280, 9),
                OnError<int>(290, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.BufferWithTimeOrCount(TimeSpan.FromTicks(999), 5, scheduler)).ToArray();
            Assert.AreEqual(3, results.Length, "length");
            Assert.IsTrue(results[0].Value.Value.SequenceEqual(new int[] { 2, 3, 4, 5, 6 }) && results[0].Time == 250, "first");
            Assert.IsTrue(results[1].Value.Value.SequenceEqual(new int[] { 7, 8, 9 }) && results[1].Time == 290, "partial");
            Assert.IsTrue(results[2].Value.Exception == ex && results[2].Time == 290, "completed");
        }

        [TestMethod]
        public void Buffer_Time_Count_DefaultScheduler()
        {
            var xs = Observable.GenerateWithTime(0, x => x < 10, x => x, x => TimeSpan.FromSeconds(0.02), x => x + 1).Timestamp();
            var res = xs.BufferWithTimeOrCount(TimeSpan.FromSeconds(1), 4).Aggregate((left, right) => left.Concat(right).ToList()).First().Select(x => x.Value).ToList();
            Assert.IsTrue(res.SequenceEqual(Enumerable.Range(0, 10)));
        }

        [TestMethod]
        public void TimeInterval_ArgumentChecking()
        {
            var scheduler = new TestScheduler();
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.TimeInterval(default(IObservable<int>)));
            Throws<ArgumentNullException>(() => Observable.TimeInterval(default(IObservable<int>), scheduler));
            Throws<ArgumentNullException>(() => Observable.TimeInterval(someObservable, null));
            Throws<ArgumentNullException>(() => Observable.RemoveTimeInterval(default(IObservable<TimeInterval<int>>)));
        }

        [TestMethod]
        public void TimeInterval_Regular()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(230, 3),
                OnNext(260, 4),
                OnNext(300, 5),
                OnNext(350, 6),
                OnCompleted<int>(400)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.TimeInterval(scheduler)).ToArray();
            results.AssertEqual(
                OnNext(210, new TimeInterval<int>(2, TimeSpan.FromTicks(10))),
                OnNext(230, new TimeInterval<int>(3, TimeSpan.FromTicks(20))),
                OnNext(260, new TimeInterval<int>(4, TimeSpan.FromTicks(30))),
                OnNext(300, new TimeInterval<int>(5, TimeSpan.FromTicks(40))),
                OnNext(350, new TimeInterval<int>(6, TimeSpan.FromTicks(50))),
                OnCompleted<TimeInterval<int>>(400)
            );
        }

        [TestMethod]
        public void TimeInterval_Empty()
        {
            var scheduler = new TestScheduler();
            var results = scheduler.Run(() => Observable.Empty<int>(scheduler).TimeInterval(scheduler));

            results.AssertEqual(OnCompleted<TimeInterval<int>>(ObservableTest.Subscribed + 1 /* CHECK */));
        }

        [TestMethod]
        public void TimeInterval_Error()
        {
            var scheduler = new TestScheduler();
            var ex = new Exception("Oops");
            var results = scheduler.Run(() => Observable.Throw<int>(ex, scheduler).TimeInterval(scheduler));

            results.AssertEqual(OnError<TimeInterval<int>>(ObservableTest.Subscribed + 1 /* CHECK */, ex));
        }

        [TestMethod]
        public void TimeInterval_Never()
        {
            var scheduler = new TestScheduler();
            var results = scheduler.Run(() => Observable.Never<int>().TimeInterval(scheduler));

            results.AssertEqual();
        }

        [TestMethod]
        public void TimeInterval_RegularAndRemove()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(230, 3),
                OnNext(260, 4),
                OnNext(300, 5),
                OnNext(350, 6),
                OnCompleted<int>(400)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.TimeInterval(scheduler).RemoveTimeInterval()).ToArray();
            results.AssertEqual(
                from n in msgs
                where n.Time > ObservableTest.Subscribed
                select n
            );
        }

        [TestMethod]
        public void TimeInterval_DefaultScheduler()
        {
            Assert.IsTrue(Observable.Return(1).TimeInterval().Count().First() == 1);
        }

        [TestMethod]
        public void Timestamp_ArgumentChecking()
        {
            var scheduler = new TestScheduler();
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.Timestamp(default(IObservable<int>)));
            Throws<ArgumentNullException>(() => Observable.Timestamp(default(IObservable<int>), scheduler));
            Throws<ArgumentNullException>(() => Observable.Timestamp(someObservable, null));
            Throws<ArgumentNullException>(() => Observable.RemoveTimestamp(default(IObservable<Timestamped<int>>)));
        }

        [TestMethod]
        public void Timestamp_Regular()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(230, 3),
                OnNext(260, 4),
                OnNext(300, 5),
                OnNext(350, 6),
                OnCompleted<int>(400)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.Timestamp(scheduler)).ToArray();
            results.AssertEqual(
                OnNext(210, new Timestamped<int>(2, new DateTimeOffset(210, TimeSpan.Zero))),
                OnNext(230, new Timestamped<int>(3, new DateTimeOffset(230, TimeSpan.Zero))),
                OnNext(260, new Timestamped<int>(4, new DateTimeOffset(260, TimeSpan.Zero))),
                OnNext(300, new Timestamped<int>(5, new DateTimeOffset(300, TimeSpan.Zero))),
                OnNext(350, new Timestamped<int>(6, new DateTimeOffset(350, TimeSpan.Zero))),
                OnCompleted<Timestamped<int>>(400)
            );
        }

        [TestMethod]
        public void Timestamp_Empty()
        {
            var scheduler = new TestScheduler();
            var results = scheduler.Run(() => Observable.Empty<int>(scheduler).Timestamp(scheduler));

            results.AssertEqual(OnCompleted<Timestamped<int>>(ObservableTest.Subscribed + 1 /* CHECK */));
        }

        [TestMethod]
        public void Timestamp_Error()
        {
            var scheduler = new TestScheduler();
            var ex = new Exception("Oops");
            var results = scheduler.Run(() => Observable.Throw<int>(ex, scheduler).Timestamp(scheduler));

            results.AssertEqual(OnError<Timestamped<int>>(ObservableTest.Subscribed + 1 /* CHECK */, ex));
        }

        [TestMethod]
        public void Timestamp_Never()
        {
            var scheduler = new TestScheduler();
            var results = scheduler.Run(() => Observable.Never<int>().Timestamp(scheduler));

            results.AssertEqual();
        }

        [TestMethod]
        public void Timestamp_RegularAndRemove()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(230, 3),
                OnNext(260, 4),
                OnNext(300, 5),
                OnNext(350, 6),
                OnCompleted<int>(400)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.Timestamp(scheduler).RemoveTimestamp()).ToArray();
            results.AssertEqual(
                from n in msgs
                where n.Time > ObservableTest.Subscribed
                select n
            );
        }

        [TestMethod]
        public void Timestamp_DefaultScheduler()
        {
            Assert.IsTrue(Observable.Return(1).Timestamp().Count().First() == 1);
        }

        [TestMethod]
        public void Sample_ArgumentChecking()
        {
            var scheduler = new TestScheduler();
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.Sample(default(IObservable<int>), TimeSpan.Zero));
            Throws<ArgumentNullException>(() => Observable.Sample(default(IObservable<int>), TimeSpan.Zero, scheduler));
            Throws<ArgumentNullException>(() => Observable.Sample(someObservable, TimeSpan.Zero, null));
        }

        [TestMethod]
        public void Sample_Regular()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(230, 3),
                OnNext(260, 4),
                OnNext(300, 5),
                OnNext(350, 6),
                OnCompleted<int>(400)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.Sample(TimeSpan.FromTicks(50), scheduler)).ToArray();
            results.AssertEqual(
                OnNext(250, 3),
                OnNext(300, 5), /* CHECK: boundary of sampling */
                OnNext(350, 6),
                OnCompleted<int>(400)
            );
        }

        [TestMethod]
        public void Sample_Empty()
        {
            var scheduler = new TestScheduler();
            var results = scheduler.Run(() => Observable.Empty<int>(scheduler).Sample(TimeSpan.Zero, scheduler));

            results.AssertEqual(OnCompleted<int>(ObservableTest.Subscribed + 1 /* CHECK */));
        }

        [TestMethod]
        public void Sample_Error()
        {
            var scheduler = new TestScheduler();
            var ex = new Exception("Oops");
            var results = scheduler.Run(() => Observable.Throw<int>(ex, scheduler).Sample(TimeSpan.Zero, scheduler));

            results.AssertEqual(OnError<int>(ObservableTest.Subscribed + 1 /* CHECK */, ex));
        }

        [TestMethod]
        public void Sample_Never()
        {
            var scheduler = new TestScheduler();
            var results = scheduler.Run(() => Observable.Never<int>().Sample(TimeSpan.Zero, scheduler));

            results.AssertEqual();
        }

        [TestMethod]
        public void Sample_DefaultScheduler()
        {
            Observable.Return(1).Sample(TimeSpan.FromMilliseconds(1)).ToEnumerable().Count();
        }

        [TestMethod]
        public void Timeout_ArgumentChecking()
        {
            var scheduler = new TestScheduler();
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.Timeout(default(IObservable<int>), TimeSpan.Zero));
            Throws<ArgumentNullException>(() => Observable.Timeout(default(IObservable<int>), TimeSpan.Zero, someObservable));
            Throws<ArgumentNullException>(() => Observable.Timeout(someObservable, TimeSpan.Zero, default(IObservable<int>)));
            Throws<ArgumentNullException>(() => Observable.Timeout(default(IObservable<int>), new DateTimeOffset()));
            Throws<ArgumentNullException>(() => Observable.Timeout(default(IObservable<int>), new DateTimeOffset(), someObservable));
            Throws<ArgumentNullException>(() => Observable.Timeout(someObservable, new DateTimeOffset(), default(IObservable<int>)));
            Throws<ArgumentNullException>(() => Observable.Timeout(default(IObservable<int>), TimeSpan.Zero, scheduler));
            Throws<ArgumentNullException>(() => Observable.Timeout(someObservable, TimeSpan.Zero, default(IScheduler)));
            Throws<ArgumentNullException>(() => Observable.Timeout(default(IObservable<int>), TimeSpan.Zero, someObservable, scheduler));
            Throws<ArgumentNullException>(() => Observable.Timeout(someObservable, TimeSpan.Zero, someObservable, null));
            Throws<ArgumentNullException>(() => Observable.Timeout(someObservable, TimeSpan.Zero, default(IObservable<int>), scheduler));
            Throws<ArgumentNullException>(() => Observable.Timeout(default(IObservable<int>), new DateTimeOffset(), scheduler));
            Throws<ArgumentNullException>(() => Observable.Timeout(someObservable, new DateTimeOffset(), default(IScheduler)));
            Throws<ArgumentNullException>(() => Observable.Timeout(default(IObservable<int>), new DateTimeOffset(), someObservable, scheduler));
            Throws<ArgumentNullException>(() => Observable.Timeout(someObservable, new DateTimeOffset(), someObservable, null));
            Throws<ArgumentNullException>(() => Observable.Timeout(someObservable, new DateTimeOffset(), default(IObservable<int>), scheduler));
        }

        [TestMethod]
        public void Timeout_InTime()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(230, 3),
                OnNext(260, 4),
                OnNext(300, 5),
                OnNext(350, 6),
                OnCompleted<int>(400)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.Timeout(TimeSpan.FromTicks(500), scheduler)).ToArray();
            results.AssertEqual(
                from n in msgs
                where n.Time > ObservableTest.Subscribed
                select n
            );
        }

        [TestMethod]
        public void Timeout_OutOfTime()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(230, 3),
                OnNext(260, 4),
                OnNext(300, 5),
                OnNext(350, 6),
                OnCompleted<int>(400)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.Timeout(TimeSpan.FromTicks(205), scheduler)).ToArray();
            results.AssertEqual(
                from n in msgs
                where n.Time > ObservableTest.Subscribed
                select n
            );
        }

        [TestMethod]
        public void Timeout_TimeSpan_DefaultScheduler()
        {
            Assert.IsTrue(Observable.Return(1).Timeout(TimeSpan.FromSeconds(10)).ToEnumerable().Single() == 1);
        }

        [TestMethod]
        public void Timeout_TimeSpan_Observable_DefaultScheduler()
        {
            Assert.IsTrue(Observable.Return(1).Timeout(TimeSpan.FromSeconds(10), Observable.Return(2)).ToEnumerable().Single() == 1);
        }

        [TestMethod]
        public void Timeout_DateTimeOffset_DefaultScheduler()
        {
            Assert.IsTrue(Observable.Return(1).Timeout(DateTimeOffset.UtcNow + TimeSpan.FromSeconds(10)).ToEnumerable().Single() == 1);
        }

        [TestMethod]
        public void Timeout_DateTimeOffset_Observable_DefaultScheduler()
        {
            Assert.IsTrue(Observable.Return(1).Timeout(DateTimeOffset.UtcNow + TimeSpan.FromSeconds(10), Observable.Return(2)).ToEnumerable().Single() == 1);
        }

        [TestMethod]
        public void Timeout_TimeoutOccurs_1()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext( 70, 1),
                OnNext(130, 2),
                OnNext(310, 3),
                OnNext(400, 4),
                OnCompleted<int>(500)
                );

            var ys = scheduler.CreateColdObservable(
                OnNext( 50, -1),
                OnNext(200, -2),
                OnNext(310, -3),
                OnCompleted<int>(320)
                );

            var results = scheduler.Run(() => xs.Timeout(TimeSpan.FromTicks(100), ys, scheduler));

            results.AssertEqual(
                OnNext(350, -1),
                OnNext(500, -2),
                OnNext(610, -3),
                OnCompleted<int>(620)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 300)
                );

            ys.Subscriptions.AssertEqual(
                Subscribe(300, 620)
                );
        }

        [TestMethod]
        public void Timeout_TimeoutOccurs_2()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext( 70, 1),
                OnNext(130, 2),
                OnNext(240, 3),
                OnNext(310, 4),
                OnNext(430, 5),
                OnCompleted<int>(500)
                );

            var ys = scheduler.CreateColdObservable(
                OnNext( 50, -1),
                OnNext(200, -2),
                OnNext(310, -3),
                OnCompleted<int>(320)
                );

            var results = scheduler.Run(() => xs.Timeout(TimeSpan.FromTicks(100), ys, scheduler));

            results.AssertEqual(
                OnNext(240, 3),
                OnNext(310, 4),
                OnNext(460, -1),
                OnNext(610, -2),
                OnNext(720, -3),
                OnCompleted<int>(730)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 410)
                );

            ys.Subscriptions.AssertEqual(
                Subscribe(410, 730)
                );
        }

        [TestMethod]
        public void Timeout_TimeoutOccurs_Never()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 1),
                OnNext(130, 2),
                OnNext(240, 3),
                OnNext(310, 4),
                OnNext(430, 5),
                OnCompleted<int>(500)
                );

            var ys = scheduler.CreateColdObservable<int>(
                );

            var results = scheduler.Run(() => xs.Timeout(TimeSpan.FromTicks(100), ys, scheduler));

            results.AssertEqual(
                OnNext(240, 3),
                OnNext(310, 4)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 410)
                );

            ys.Subscriptions.AssertEqual(
                Subscribe(410, 1000)
                );
        }

        [TestMethod]
        public void Timeout_TimeoutOccurs_Completed()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnCompleted<int>(500)
                );

            var ys = scheduler.CreateColdObservable(
                OnNext(100, -1)
                );

            var results = scheduler.Run(() => xs.Timeout(TimeSpan.FromTicks(100), ys, scheduler));

            results.AssertEqual(
                OnNext(400, -1)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 300)
                );

            ys.Subscriptions.AssertEqual(
                Subscribe(300, 1000)
                );
        }

        [TestMethod]
        public void Timeout_TimeoutOccurs_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnError<int>(500, new MockException(1))
                );

            var ys = scheduler.CreateColdObservable(
                OnNext(100, -1)
                );

            var results = scheduler.Run(() => xs.Timeout(TimeSpan.FromTicks(100), ys, scheduler));

            results.AssertEqual(
                OnNext(400, -1)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 300)
                );

            ys.Subscriptions.AssertEqual(
                Subscribe(300, 1000)
                );
        }

        [TestMethod]
        public void Timeout_TimeoutNotOccurs_Completed()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnCompleted<int>(250)
                );

            var ys = scheduler.CreateColdObservable(
                OnNext(100, -1)
                );

            var results = scheduler.Run(() => xs.Timeout(TimeSpan.FromTicks(100), ys, scheduler));

            results.AssertEqual(
                OnCompleted<int>(250)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 250)
                );

            ys.Subscriptions.AssertEqual(
                );
        }

        [TestMethod]
        public void Timeout_TimeoutNotOccurs_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnError<int>(250, new MockException(1))
                );

            var ys = scheduler.CreateColdObservable(
                OnNext(100, -1)
                );

            var results = scheduler.Run(() => xs.Timeout(TimeSpan.FromTicks(100), ys, scheduler));

            results.AssertEqual(
                OnError<int>(250, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 250)
                );

            ys.Subscriptions.AssertEqual(
                );
        }

        [TestMethod]
        public void Timeout_TimeoutDoesNotOccur()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext( 70, 1),
                OnNext(130, 2),
                OnNext(240, 3),
                OnNext(320, 4),
                OnNext(410, 5),
                OnCompleted<int>(500)
                );

            var ys = scheduler.CreateColdObservable(
                OnNext(50, -1),
                OnNext(200, -2),
                OnNext(310, -3),
                OnCompleted<int>(320)
                );

            var results = scheduler.Run(() => xs.Timeout(TimeSpan.FromTicks(100), ys, scheduler));

            results.AssertEqual(
                OnNext(240, 3),
                OnNext(320, 4),
                OnNext(410, 5),
                OnCompleted<int>(500)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 500)
                );

            ys.Subscriptions.AssertEqual(
                );
        }

        [TestMethod]
        public void Timeout_DateTimeOffset_TimeoutOccurs()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(410, 1)
                );

            var ys = scheduler.CreateColdObservable(
                OnNext(100, -1)
                );

            var results = scheduler.Run(() => xs.Timeout(new DateTimeOffset(new DateTime(400), TimeSpan.Zero), ys, scheduler));

            results.AssertEqual(
                OnNext(500, -1)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 400)
                );

            ys.Subscriptions.AssertEqual(
                Subscribe(400, 1000)
                );
        }

        [TestMethod]
        public void Timeout_DateTimeOffset_TimeoutDoesNotOccur_Completed()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(310, 1),
                OnCompleted<int>(390)
                );

            var ys = scheduler.CreateColdObservable(
                OnNext(100, -1)
                );

            var results = scheduler.Run(() => xs.Timeout(new DateTimeOffset(new DateTime(400), TimeSpan.Zero), ys, scheduler));

            results.AssertEqual(
                OnNext(310, 1),
                OnCompleted<int>(390)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 390)
                );

            ys.Subscriptions.AssertEqual(
                );
        }

        [TestMethod]
        public void Timeout_DateTimeOffset_TimeoutDoesNotOccur_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(310, 1),
                OnError<int>(390, new MockException(1))
                );

            var ys = scheduler.CreateColdObservable(
                OnNext(100, -1)
                );

            var results = scheduler.Run(() => xs.Timeout(new DateTimeOffset(new DateTime(400), TimeSpan.Zero), ys, scheduler));

            results.AssertEqual(
                OnNext(310, 1),
                OnError<int>(390, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 390)
                );

            ys.Subscriptions.AssertEqual(
                );
        }

        [TestMethod]
        public void Timeout_DateTimeOffset_TimeoutOccur_2()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(310, 1),
                OnNext(350, 2),
                OnNext(420, 3),
                OnCompleted<int>(450)
                );

            var ys = scheduler.CreateColdObservable(
                OnNext(100, -1)
                );

            var results = scheduler.Run(() => xs.Timeout(new DateTimeOffset(new DateTime(400), TimeSpan.Zero), ys, scheduler));

            results.AssertEqual(
                OnNext(310, 1),
                OnNext(350, 2),
                OnNext(500, -1)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 400)
                );

            ys.Subscriptions.AssertEqual(
                Subscribe(400, 1000)
                );
        }

        [TestMethod]
        public void Timeout_DateTimeOffset_TimeoutOccur_3()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(310, 1),
                OnNext(350, 2),
                OnNext(420, 3),
                OnCompleted<int>(450)
                );

            var ys = scheduler.CreateColdObservable<int>(
                );

            var results = scheduler.Run(() => xs.Timeout(new DateTimeOffset(new DateTime(400), TimeSpan.Zero), ys, scheduler));

            results.AssertEqual(
                OnNext(310, 1),
                OnNext(350, 2)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 400)
                );

            ys.Subscriptions.AssertEqual(
                Subscribe(400, 1000)
                );
        }

        [TestMethod]
        public void Generate_TimeSpan_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.GenerateWithTime(0, DummyFunc<int, bool>.Instance, DummyFunc<int, int>.Instance, DummyFunc<int, TimeSpan>.Instance, DummyFunc<int, int>.Instance, (IScheduler)null));
            Throws<ArgumentNullException>(() => Observable.GenerateWithTime(0, (Func<int, bool>)null, DummyFunc<int, int>.Instance, DummyFunc<int, TimeSpan>.Instance, DummyFunc<int, int>.Instance, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.GenerateWithTime(0, DummyFunc<int, bool>.Instance, (Func<int, int>)null, DummyFunc<int, TimeSpan>.Instance, DummyFunc<int, int>.Instance, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.GenerateWithTime(0, DummyFunc<int, bool>.Instance, DummyFunc<int, int>.Instance, DummyFunc<int, TimeSpan>.Instance, (Func<int, int>)null, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.GenerateWithTime(0, DummyFunc<int, bool>.Instance, DummyFunc<int, int>.Instance, (Func<int, TimeSpan>)null, DummyFunc<int, int>.Instance, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.GenerateWithTime(0, DummyFunc<int, bool>.Instance, DummyFunc<int, int>.Instance, DummyFunc<int, TimeSpan>.Instance, DummyFunc<int, int>.Instance, DummyScheduler.Instance).Subscribe(null));
        }

        [TestMethod]
        public void Generate_TimeSpan_Finite()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.GenerateWithTime(0, x => x <= 3, x => x, x => TimeSpan.FromTicks(x + 1), x => x + 1, scheduler));

            results.AssertEqual(
                OnNext(202, 0),
                OnNext(204, 1),
                OnNext(207, 2),
                OnNext(211, 3),
                OnCompleted<int>(211)
                );
        }

        [TestMethod]
        public void Generate_TimeSpan_Throw_Condition()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.GenerateWithTime(0, new Func<int, bool>(x => { throw new MockException(x); }),
                x => x,
                x => TimeSpan.FromTicks(x + 1),
                x => x + 1, scheduler));

            results.AssertEqual(
                OnError<int>(201, new MockException(0))
                );
        }

        [TestMethod]
        public void Generate_TimeSpan_Throw_ResultSelector()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.GenerateWithTime(0, x => true,
                new Func<int, int>(x => { throw new MockException(x); }),
                x => TimeSpan.FromTicks(x + 1),
                x => x + 1, scheduler));

            results.AssertEqual(
                OnError<int>(201, new MockException(0))
                );
        }

        [TestMethod]
        public void Generate_TimeSpan_Throw_Iterate()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.GenerateWithTime(0, x => true,
                x => x,
                x => TimeSpan.FromTicks(x + 1),
                new Func<int, int>(x => { throw new MockException(x); }), scheduler));

            results.AssertEqual(
                OnNext(202, 0),
                OnError<int>(202, new MockException(0))
                );
        }

        [TestMethod]
        public void Generate_TimeSpan_Throw_TimeSelector()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.GenerateWithTime(0, x => true,
                x => x,
                new Func<int, TimeSpan>(x => { throw new MockException(x); }),
                x => x + 1, scheduler));

            results.AssertEqual(
                OnError<int>(201, new MockException(0))
                );
        }

        [TestMethod]
        public void Generate_TimeSpan_Dispose()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.GenerateWithTime(0, x => true, x => x, x => TimeSpan.FromTicks(x + 1), x => x + 1, scheduler), 210);

            results.AssertEqual(
                OnNext(202, 0),
                OnNext(204, 1),
                OnNext(207, 2)
                );
        }

        [TestMethod]
        public void Generate_TimeSpan_DefaultScheduler_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.GenerateWithTime(0, (Func<int, bool>)null, DummyFunc<int, int>.Instance, DummyFunc<int, TimeSpan>.Instance, DummyFunc<int, int>.Instance));
            Throws<ArgumentNullException>(() => Observable.GenerateWithTime(0, DummyFunc<int, bool>.Instance, (Func<int, int>)null, DummyFunc<int, TimeSpan>.Instance, DummyFunc<int, int>.Instance));
            Throws<ArgumentNullException>(() => Observable.GenerateWithTime(0, DummyFunc<int, bool>.Instance, DummyFunc<int, int>.Instance, DummyFunc<int, TimeSpan>.Instance, (Func<int, int>)null));
            Throws<ArgumentNullException>(() => Observable.GenerateWithTime(0, DummyFunc<int, bool>.Instance, DummyFunc<int, int>.Instance, (Func<int, TimeSpan>)null, DummyFunc<int, int>.Instance));
            Throws<ArgumentNullException>(() => Observable.GenerateWithTime(0, DummyFunc<int, bool>.Instance, DummyFunc<int, int>.Instance, DummyFunc<int, TimeSpan>.Instance, DummyFunc<int, int>.Instance).Subscribe(null));
        }

        [TestMethod]
        public void Generate_TimeSpan_DefaultScheduler()
        {
            Observable.GenerateWithTime(0, x => x < 10, x => x, x => TimeSpan.FromMilliseconds(x), x => x + 1).AssertEqual(Observable.GenerateWithTime(0, x => x < 10, x => x, x => TimeSpan.FromMilliseconds(x), x => x + 1, Scheduler.ThreadPool));
        }

        [TestMethod]
        public void Generate_DateTimeOffset_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.GenerateWithTime(0, DummyFunc<int, bool>.Instance, DummyFunc<int, int>.Instance, DummyFunc<int, DateTimeOffset>.Instance, DummyFunc<int, int>.Instance, (IScheduler)null));
            Throws<ArgumentNullException>(() => Observable.GenerateWithTime(0, (Func<int, bool>)null, DummyFunc<int, int>.Instance, DummyFunc<int, DateTimeOffset>.Instance, DummyFunc<int, int>.Instance, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.GenerateWithTime(0, DummyFunc<int, bool>.Instance, (Func<int, int>)null, DummyFunc<int, DateTimeOffset>.Instance, DummyFunc<int, int>.Instance, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.GenerateWithTime(0, DummyFunc<int, bool>.Instance, DummyFunc<int, int>.Instance, DummyFunc<int, DateTimeOffset>.Instance, (Func<int, int>)null, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.GenerateWithTime(0, DummyFunc<int, bool>.Instance, DummyFunc<int, int>.Instance, (Func<int, DateTimeOffset>)null, DummyFunc<int, int>.Instance, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.GenerateWithTime(0, DummyFunc<int, bool>.Instance, DummyFunc<int, int>.Instance, DummyFunc<int, DateTimeOffset>.Instance, DummyFunc<int, int>.Instance, DummyScheduler.Instance).Subscribe(null));
        }

        [TestMethod]
        public void Generate_DateTimeOffset_Finite()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.GenerateWithTime(0, x => x <= 3, x => x, x => scheduler.Now.AddTicks(x + 1), x => x + 1, scheduler));

            results.AssertEqual(
                OnNext(202, 0),
                OnNext(204, 1),
                OnNext(207, 2),
                OnNext(211, 3),
                OnCompleted<int>(211)
                );
        }

        [TestMethod]
        public void Generate_DateTimeOffset_Throw_Condition()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.GenerateWithTime(0, new Func<int, bool>(x => { throw new MockException(x); }),
                x => x,
                x => scheduler.Now.AddTicks(x + 1),
                x => x + 1, scheduler));

            results.AssertEqual(
                OnError<int>(201, new MockException(0))
                );
        }

        [TestMethod]
        public void Generate_DateTimeOffset_Throw_ResultSelector()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.GenerateWithTime(0, x => true,
                new Func<int, int>(x => { throw new MockException(x); }),
                x => scheduler.Now.AddTicks(x + 1),
                x => x + 1, scheduler));

            results.AssertEqual(
                OnError<int>(201, new MockException(0))
                );
        }

        [TestMethod]
        public void Generate_DateTimeOffset_Throw_Iterate()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.GenerateWithTime(0, x => true,
                x => x,
                x => scheduler.Now.AddTicks(x + 1),
                new Func<int, int>(x => { throw new MockException(x); }), scheduler));

            results.AssertEqual(
                OnNext(202, 0),
                OnError<int>(202, new MockException(0))
                );
        }

        [TestMethod]
        public void Generate_DateTimeOffset_Throw_TimeSelector()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.GenerateWithTime(0, x => true,
                x => x,
                new Func<int, DateTimeOffset>(x => { throw new MockException(x); }),
                x => x + 1, scheduler));

            results.AssertEqual(
                OnError<int>(201, new MockException(0))
                );
        }

        [TestMethod]
        public void Generate_DateTimeOffset_Dispose()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.GenerateWithTime(0, x => true, x => x, x => scheduler.Now.AddTicks(x + 1), x => x + 1, scheduler), 210);

            results.AssertEqual(
                OnNext(202, 0),
                OnNext(204, 1),
                OnNext(207, 2)
                );
        }

        [TestMethod]
        public void Generate_DateTimeOffset_DefaultScheduler_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.GenerateWithTime(0, (Func<int, bool>)null, DummyFunc<int, int>.Instance, DummyFunc<int, DateTimeOffset>.Instance, DummyFunc<int, int>.Instance));
            Throws<ArgumentNullException>(() => Observable.GenerateWithTime(0, DummyFunc<int, bool>.Instance, (Func<int, int>)null, DummyFunc<int, DateTimeOffset>.Instance, DummyFunc<int, int>.Instance));
            Throws<ArgumentNullException>(() => Observable.GenerateWithTime(0, DummyFunc<int, bool>.Instance, DummyFunc<int, int>.Instance, DummyFunc<int, DateTimeOffset>.Instance, (Func<int, int>)null));
            Throws<ArgumentNullException>(() => Observable.GenerateWithTime(0, DummyFunc<int, bool>.Instance, DummyFunc<int, int>.Instance, (Func<int, DateTimeOffset>)null, DummyFunc<int, int>.Instance));
            Throws<ArgumentNullException>(() => Observable.GenerateWithTime(0, DummyFunc<int, bool>.Instance, DummyFunc<int, int>.Instance, DummyFunc<int, DateTimeOffset>.Instance, DummyFunc<int, int>.Instance).Subscribe(null));
        }

        [TestMethod]
        public void Generate_DateTimeOffset_DefaultScheduler()
        {
            Observable.GenerateWithTime(0, x => x < 10, x => x, x => DateTimeOffset.Now.AddMilliseconds(x), x => x + 1).AssertEqual(Observable.GenerateWithTime(0, x => x < 10, x => x, x => DateTimeOffset.Now.AddMilliseconds(x), x => x + 1, Scheduler.ThreadPool));
        }
    }
}
