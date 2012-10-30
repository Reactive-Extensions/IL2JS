using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Collections.Generic;
using ReactiveTests.Mocks;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Linq;
using Reactive;
using ReactiveTests.Dummies;
using Reactive.Concurrency;

namespace ReactiveTests.Tests
{
    [TestClass]
    public class ObservableSingleTest : Test
    {
        [TestMethod]
        public void Materialize_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Materialize<int>(null));
        }

        [TestMethod]
        public void Materialize_Never()
        {
            var scheduler = new TestScheduler();
            var results = scheduler.Run(() => Observable.Never<int>().Materialize());

            results.AssertEqual();
        }

        [TestMethod]
        public void Materialize_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.Materialize()).ToArray();
            Assert.AreEqual(2, results.Length, "length");
            Assert.IsTrue(results[0].Value.Kind == NotificationKind.OnNext && results[0].Value.Value.Kind == NotificationKind.OnCompleted && results[0].Time == 250);
            Assert.IsTrue(results[1].Value.Kind == NotificationKind.OnCompleted && results[1].Time == 250);
        }

        [TestMethod]
        public void Materialize_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.Materialize()).ToArray();
            Assert.AreEqual(3, results.Length, "length");
            Assert.IsTrue(results[0].Value.Kind == NotificationKind.OnNext && results[0].Value.Value.Kind == NotificationKind.OnNext && results[0].Value.Value.Value == 2 && results[0].Time == 210);
            Assert.IsTrue(results[1].Value.Kind == NotificationKind.OnNext && results[1].Value.Value.Kind == NotificationKind.OnCompleted && results[1].Time == 250);
            Assert.IsTrue(results[2].Value.Kind == NotificationKind.OnCompleted && results[2].Time == 250);
        }

        [TestMethod]
        public void Materialize_Throw()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs = new[] {
                OnNext(150, 1),
                OnError<int>(250, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.Materialize()).ToArray();
            Assert.AreEqual(2, results.Length, "length");
            Assert.IsTrue(results[0].Value.Kind == NotificationKind.OnNext && results[0].Value.Value.Kind == NotificationKind.OnError && ((Notification<int>.OnError)results[0].Value.Value).Exception == ex);
            Assert.IsTrue(results[1].Value.Kind == NotificationKind.OnCompleted);
        }

        [TestMethod]
        public void Dematerialize_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Dematerialize<int>(null));
        }

        [TestMethod]
        public void Materialize_Dematerialize_Never()
        {
            var scheduler = new TestScheduler();
            var results = scheduler.Run(() => Observable.Never<int>().Materialize().Dematerialize());

            results.AssertEqual();
        }

        [TestMethod]
        public void Materialize_Dematerialize_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.Materialize().Dematerialize()).ToArray();
            Assert.AreEqual(1, results.Length, "length");
            Assert.IsTrue(results[0].Value.Kind == NotificationKind.OnCompleted && results[0].Time == 250);
        }

        [TestMethod]
        public void Materialize_Dematerialize_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.Materialize().Dematerialize()).ToArray();
            Assert.AreEqual(2, results.Length, "length");
            Assert.IsTrue(results[0].Value.Kind == NotificationKind.OnNext && results[0].Value.Value == 2 && results[0].Time == 210);
            Assert.IsTrue(results[1].Value.Kind == NotificationKind.OnCompleted);
        }

        [TestMethod]
        public void Materialize_Dematerialize_Throw()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs = new[] {
                OnNext(150, 1),
                OnError<int>(250, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.Materialize().Dematerialize()).ToArray();
            Assert.AreEqual(1, results.Length, "length");
            Assert.IsTrue(results[0].Value.Kind == NotificationKind.OnError && ((Notification<int>.OnError)results[0].Value).Exception == ex && results[0].Time == 250);
        }

        [TestMethod]
        public void StartWith_ArgumentChecking()
        {
            var scheduler = new TestScheduler();
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.StartWith(null, 1));
            Throws<ArgumentNullException>(() => Observable.StartWith(someObservable, null, 1));
            Throws<ArgumentNullException>(() => Observable.StartWith(null, scheduler, 1));
        }

        [TestMethod]
        public void StartWith()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(220, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.StartWith(1)).ToArray();
            Assert.AreEqual(3, results.Length, "length");
            Assert.IsTrue(results[0].Value.Kind == NotificationKind.OnNext && results[0].Value.Value == 1 && results[0].Time == 200);
            Assert.IsTrue(results[1].Value.Kind == NotificationKind.OnNext && results[1].Value.Value == 2 && results[1].Time == 220);
            Assert.IsTrue(results[2].Value.Kind == NotificationKind.OnCompleted);
        }

        [TestMethod]
        public void Buffer_Single_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.BufferWithCount(default(IObservable<int>), 1));
            Throws<ArgumentOutOfRangeException>(() => Observable.BufferWithCount(someObservable, 0));
            Throws<ArgumentOutOfRangeException>(() => Observable.BufferWithCount(someObservable, -1));
            Throws<ArgumentNullException>(() => Observable.BufferWithCount(default(IObservable<int>), 1, 1));
            Throws<ArgumentOutOfRangeException>(() => Observable.BufferWithCount(someObservable, 1, 0));
            Throws<ArgumentOutOfRangeException>(() => Observable.BufferWithCount(someObservable, 0, 1));
        }

        [TestMethod]
        public void Buffer_Count_PartialWindow()
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

            var results = scheduler.Run(() => xs.BufferWithCount(5)).ToArray();
            Assert.AreEqual(2, results.Length, "length");
            Assert.IsTrue(results[0].Value.Value.SequenceEqual(new int[] { 2, 3, 4, 5 }) && results[0].Time == 250, "first");
            Assert.IsTrue(results[1].Value is Notification<IList<int>>.OnCompleted && results[1].Time == 250, "completed");
        }

        [TestMethod]
        public void Buffer_Count_FullWindows()
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

            var results = scheduler.Run(() => xs.BufferWithCount(2)).ToArray();
            Assert.AreEqual(3, results.Length, "length");
            Assert.IsTrue(results[0].Value.Value.SequenceEqual(new int[] { 2, 3 }) && results[0].Time == 220, "first");
            Assert.IsTrue(results[1].Value.Value.SequenceEqual(new int[] { 4, 5 }) && results[1].Time == 240, "second");
            Assert.IsTrue(results[2].Value is Notification<IList<int>>.OnCompleted && results[2].Time == 250, "completed");
        }

        [TestMethod]
        public void Buffer_Count_FullAndPartialWindows()
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

            var results = scheduler.Run(() => xs.BufferWithCount(3)).ToArray();
            Assert.AreEqual(3, results.Length, "length");
            Assert.IsTrue(results[0].Value.Value.SequenceEqual(new int[] { 2, 3, 4 }) && results[0].Time == 230, "first");
            Assert.IsTrue(results[1].Value.Value.SequenceEqual(new int[] { 5 }) && results[1].Time == 250, "second");
            Assert.IsTrue(results[2].Value is Notification<IList<int>>.OnCompleted && results[2].Time == 250, "completed");
        }

        [TestMethod]
        public void Buffer_Count_Error()
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

            var results = scheduler.Run(() => xs.BufferWithCount(5)).ToArray();
            Assert.AreEqual(2, results.Length, "length");
            Assert.IsTrue(results[0].Value.Value.SequenceEqual(new int[] { 2, 3, 4, 5 }) && results[0].Time == 250, "first");
            Assert.IsTrue(results[1].Value is Notification<IList<int>>.OnError && results[1].Time == 250, "completed");
        }

        [TestMethod]
        public void Buffer_Count_Skip_Less()
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

            var results = scheduler.Run(() => xs.BufferWithCount(3, 1)).ToArray();
            Assert.AreEqual(4, results.Length, "length");
            Assert.IsTrue(results[0].Value.Value.SequenceEqual(new int[] { 2, 3, 4 }) && results[0].Time == 230, "first");
            Assert.IsTrue(results[1].Value.Value.SequenceEqual(new int[] { 3, 4, 5 }) && results[1].Time == 240, "second");
            Assert.IsTrue(results[2].Value.Value.SequenceEqual(new int[] { 4, 5 }) && results[2].Time == 250, "third");
            Assert.IsTrue(results[3].Value is Notification<IList<int>>.OnCompleted && results[3].Time == 250, "completed");
        }

        [TestMethod]
        public void Buffer_Count_Skip_More()
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

            var results = scheduler.Run(() => xs.BufferWithCount(2, 3)).ToArray();
            Assert.AreEqual(3, results.Length, "length");
            Assert.IsTrue(results[0].Value.Value.SequenceEqual(new int[] { 2, 3 }) && results[0].Time == 220, "first");
            Assert.IsTrue(results[1].Value.Value.SequenceEqual(new int[] { 5 }) && results[1].Time == 250, "second");
            Assert.IsTrue(results[2].Value is Notification<IList<int>>.OnCompleted && results[2].Time == 250, "completed");
        }

        [TestMethod]
        public void AsObservable_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.AsObservable<int>(null));
        }

        [TestMethod]
        public void AsObservable_Hides()
        {
            var someObservable = Observable.Empty<int>();
            Assert.IsFalse(object.ReferenceEquals(someObservable.AsObservable(), someObservable));
        }

        [TestMethod]
        public void AsObservable_Never()
        {
            var scheduler = new TestScheduler();
            var results = scheduler.Run(() => Observable.Never<int>().AsObservable());

            results.AssertEqual();
        }

        [TestMethod]
        public void AsObservable_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.AsObservable()).ToArray();
            Assert.AreEqual(1, results.Length, "length");
            Assert.IsTrue(results[0].Value is Notification<int>.OnCompleted && results[0].Time == 250, "completed");
        }

        [TestMethod]
        public void AsObservable_Throw()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();
            var msgs = new[] {
                OnNext(150, 1),
                OnError<int>(250, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.AsObservable()).ToArray();
            Assert.AreEqual(1, results.Length, "length");
            Assert.IsTrue(results[0].Value is Notification<int>.OnError && ((Notification<int>.OnError)results[0].Value).Exception == ex && results[0].Time == 250, "error");
        }

        [TestMethod]
        public void AsObservable_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(220, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.AsObservable()).ToArray();
            Assert.AreEqual(2, results.Length, "length");
            Assert.IsTrue(results[0].Value is Notification<int>.OnNext && results[0].Value.Value == 2 && results[0].Time == 220, "first");
            Assert.IsTrue(results[1].Value is Notification<int>.OnCompleted && results[1].Time == 250, "completed");
        }

        [TestMethod]
        public void AsObservable_IsNotEager()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(220, 2),
                OnCompleted<int>(250)
            };

            bool subscribed = false;
            var xs = Observable.Create<int>(obs =>
            {
                subscribed = true;
                var disp = scheduler.CreateHotObservable(msgs).Subscribe(obs);
                return disp.Dispose;
            });

            xs.AsObservable();
            Assert.IsFalse(subscribed);

            var results = scheduler.Run(() => xs.AsObservable());
            Assert.IsTrue(subscribed);
        }

        [TestMethod]
        public void Scan_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.Scan<int>(null, (_, __) => 0));
            Throws<ArgumentNullException>(() => Observable.Scan<int>(someObservable, null));
            Throws<ArgumentNullException>(() => Observable.Scan<int, int>(null, 0, (_, __) => 0));
            Throws<ArgumentNullException>(() => Observable.Scan<int, int>(someObservable, 0, null));
        }

        [TestMethod]
        public void Scan_Seed_Never()
        {
            var scheduler = new TestScheduler();

            var seed = 42;
            var results = scheduler.Run(() => Observable.Never<int>().Scan(seed, (acc, x) => acc + x)).ToArray();

            results.AssertEqual();
        }

        [TestMethod]
        public void Scan_Seed_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var seed = 42;
            var results = scheduler.Run(() => xs.Scan(seed, (acc, x) => acc + x)).ToArray();
            Assert.AreEqual(1, results.Length);
            Assert.IsTrue(results[0].Value.Kind == NotificationKind.OnCompleted && results[0].Time == 250);
        }

        [TestMethod]
        public void Scan_Seed_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(220, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var seed = 42;
            var results = scheduler.Run(() => xs.Scan(seed, (acc, x) => acc + x)).ToArray();
            Assert.AreEqual(2, results.Length);
            Assert.IsTrue(results[0].Value.Kind == NotificationKind.OnNext && results[0].Time == 220 && results[0].Value.Value == seed + 2);
            Assert.IsTrue(results[1].Value.Kind == NotificationKind.OnCompleted && results[1].Time == 250);
        }

        [TestMethod]
        public void Scan_Seed_Throw()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();
            var msgs = new[] {
                OnNext(150, 1),
                OnError<int>(250, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var seed = 42;
            var results = scheduler.Run(() => xs.Scan(seed, (acc, x) => acc + x)).ToArray();
            Assert.AreEqual(1, results.Length, "length");
            Assert.IsTrue(results[0].Value is Notification<int>.OnError && ((Notification<int>.OnError)results[0].Value).Exception == ex && results[0].Time == 250, "error");
        }

        [TestMethod]
        public void Scan_Seed_SomeData()
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

            var seed = 1;
            var results = scheduler.Run(() => xs.Scan(seed, (acc, x) => acc + x)).ToArray();
            Assert.AreEqual(5, results.Length);
            Assert.IsTrue(results[0].Value.Kind == NotificationKind.OnNext && results[0].Time == 210 && results[0].Value.Value == seed + 2);
            Assert.IsTrue(results[1].Value.Kind == NotificationKind.OnNext && results[1].Time == 220 && results[1].Value.Value == seed + 2 + 3);
            Assert.IsTrue(results[2].Value.Kind == NotificationKind.OnNext && results[2].Time == 230 && results[2].Value.Value == seed + 2 + 3 + 4);
            Assert.IsTrue(results[3].Value.Kind == NotificationKind.OnNext && results[3].Time == 240 && results[3].Value.Value == seed + 2 + 3 + 4 + 5);
            Assert.IsTrue(results[4].Value.Kind == NotificationKind.OnCompleted && results[4].Time == 250);
        }

        [TestMethod]
        public void Scan_NoSeed_Never()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Never<int>().Scan((acc, x) => acc + x)).ToArray();

            results.AssertEqual();
        }

        [TestMethod]
        public void Scan_NoSeed_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.Scan((acc, x) => acc + x)).ToArray();
            Assert.AreEqual(1, results.Length);
            Assert.IsTrue(results[0].Value.Kind == NotificationKind.OnCompleted && results[0].Time == 250);
        }

        [TestMethod]
        public void Scan_NoSeed_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(220, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.Scan((acc, x) => acc + x)).ToArray();
            Assert.AreEqual(2, results.Length);
            Assert.IsTrue(results[0].Value.Kind == NotificationKind.OnNext && results[0].Time == 220 && results[0].Value.Value == 2);
            Assert.IsTrue(results[1].Value.Kind == NotificationKind.OnCompleted && results[1].Time == 250);
        }

        [TestMethod]
        public void Scan_NoSeed_Throw()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();
            var msgs = new[] {
                OnNext(150, 1),
                OnError<int>(250, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.Scan((acc, x) => acc + x)).ToArray();
            Assert.AreEqual(1, results.Length, "length");
            Assert.IsTrue(results[0].Value is Notification<int>.OnError && ((Notification<int>.OnError)results[0].Value).Exception == ex && results[0].Time == 250, "error");
        }

        [TestMethod]
        public void Scan_NoSeed_SomeData()
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

            var results = scheduler.Run(() => xs.Scan((acc, x) => acc + x)).ToArray();
            Assert.AreEqual(5, results.Length);
            Assert.IsTrue(results[0].Value.Kind == NotificationKind.OnNext && results[0].Time == 210 && results[0].Value.Value == 2);
            Assert.IsTrue(results[1].Value.Kind == NotificationKind.OnNext && results[1].Time == 220 && results[1].Value.Value == 2 + 3);
            Assert.IsTrue(results[2].Value.Kind == NotificationKind.OnNext && results[2].Time == 230 && results[2].Value.Value == 2 + 3 + 4);
            Assert.IsTrue(results[3].Value.Kind == NotificationKind.OnNext && results[3].Time == 240 && results[3].Value.Value == 2 + 3 + 4 + 5);
            Assert.IsTrue(results[4].Value.Kind == NotificationKind.OnCompleted && results[4].Time == 250);
        }

        [TestMethod]
        public void DistinctUntilChanged_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.DistinctUntilChanged<int>(null));
            Throws<ArgumentNullException>(() => Observable.DistinctUntilChanged<int>(null, EqualityComparer<int>.Default));
            Throws<ArgumentNullException>(() => Observable.DistinctUntilChanged<int>(someObservable, null));
            Throws<ArgumentNullException>(() => Observable.DistinctUntilChanged<int, int>(null, _ => _));
            Throws<ArgumentNullException>(() => Observable.DistinctUntilChanged<int, int>(someObservable, null));
            Throws<ArgumentNullException>(() => Observable.DistinctUntilChanged<int, int>(someObservable, _ => _, null));
            Throws<ArgumentNullException>(() => Observable.DistinctUntilChanged<int, int>(null, _ => _, EqualityComparer<int>.Default));
            Throws<ArgumentNullException>(() => Observable.DistinctUntilChanged<int, int>(someObservable, null, EqualityComparer<int>.Default));
        }

        [TestMethod]
        public void DistinctUntilChanged_Never()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Never<int>().DistinctUntilChanged()).ToArray();

            results.AssertEqual();
        }

        [TestMethod]
        public void DistinctUntilChanged_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.DistinctUntilChanged()).ToArray();
            Assert.AreEqual(1, results.Length);
            Assert.IsTrue(results[0].Value.Kind == NotificationKind.OnCompleted && results[0].Time == 250);
        }

        [TestMethod]
        public void DistinctUntilChanged_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(220, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.DistinctUntilChanged()).ToArray();
            Assert.AreEqual(2, results.Length);
            Assert.IsTrue(results[0].Value.Kind == NotificationKind.OnNext && results[0].Time == 220 && results[0].Value.Value == 2);
            Assert.IsTrue(results[1].Value.Kind == NotificationKind.OnCompleted && results[1].Time == 250);
        }

        [TestMethod]
        public void DistinctUntilChanged_Throw()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();
            var msgs = new[] {
                OnNext(150, 1),
                OnError<int>(250, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.DistinctUntilChanged()).ToArray();
            Assert.AreEqual(1, results.Length, "length");
            Assert.IsTrue(results[0].Value is Notification<int>.OnError && ((Notification<int>.OnError)results[0].Value).Exception == ex && results[0].Time == 250, "error");
        }

        [TestMethod]
        public void DistinctUntilChanged_AllChanges()
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

            var results = scheduler.Run(() => xs.DistinctUntilChanged()).ToArray();
            Assert.AreEqual(5, results.Length);
            Assert.IsTrue(results[0].Value.Kind == NotificationKind.OnNext && results[0].Time == 210 && results[0].Value.Value == 2);
            Assert.IsTrue(results[1].Value.Kind == NotificationKind.OnNext && results[1].Time == 220 && results[1].Value.Value == 3);
            Assert.IsTrue(results[2].Value.Kind == NotificationKind.OnNext && results[2].Time == 230 && results[2].Value.Value == 4);
            Assert.IsTrue(results[3].Value.Kind == NotificationKind.OnNext && results[3].Time == 240 && results[3].Value.Value == 5);
            Assert.IsTrue(results[4].Value.Kind == NotificationKind.OnCompleted && results[4].Time == 250);
        }

        [TestMethod]
        public void DistinctUntilChanged_AllSame()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 2),
                OnNext(230, 2),
                OnNext(240, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.DistinctUntilChanged()).ToArray();
            Assert.AreEqual(2, results.Length);
            Assert.IsTrue(results[0].Value.Kind == NotificationKind.OnNext && results[0].Time == 210 && results[0].Value.Value == 2);
            Assert.IsTrue(results[1].Value.Kind == NotificationKind.OnCompleted && results[1].Time == 250);
        }

        [TestMethod]
        public void DistinctUntilChanged_SomeChanges()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2), //*
                OnNext(215, 3), //*
                OnNext(220, 3),
                OnNext(225, 2), //*
                OnNext(230, 2),
                OnNext(230, 1), //*
                OnNext(240, 2), //*
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.DistinctUntilChanged()).ToArray();
            Assert.AreEqual(6, results.Length);
            Assert.IsTrue(results[0].Value.Kind == NotificationKind.OnNext && results[0].Time == 210 && results[0].Value.Value == 2);
            Assert.IsTrue(results[1].Value.Kind == NotificationKind.OnNext && results[1].Time == 215 && results[1].Value.Value == 3);
            Assert.IsTrue(results[2].Value.Kind == NotificationKind.OnNext && results[2].Time == 225 && results[2].Value.Value == 2);
            Assert.IsTrue(results[3].Value.Kind == NotificationKind.OnNext && results[3].Time == 230 && results[3].Value.Value == 1);
            Assert.IsTrue(results[4].Value.Kind == NotificationKind.OnNext && results[4].Time == 240 && results[4].Value.Value == 2);
            Assert.IsTrue(results[5].Value.Kind == NotificationKind.OnCompleted && results[5].Time == 250);
        }

        [TestMethod]
        public void DistinctUntilChanged_Comparer_AllEqual()
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

            var results = scheduler.Run(() => xs.DistinctUntilChanged(new FuncComparer<int>((x, y) => true))).ToArray();
            Assert.AreEqual(2, results.Length);
            Assert.IsTrue(results[0].Value.Kind == NotificationKind.OnNext && results[0].Time == 210 && results[0].Value.Value == 2);
            Assert.IsTrue(results[1].Value.Kind == NotificationKind.OnCompleted && results[1].Time == 250);
        }

        [TestMethod]
        public void DistinctUntilChanged_Comparer_AllDifferent()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 2),
                OnNext(230, 2),
                OnNext(240, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.DistinctUntilChanged(new FuncComparer<int>((x, y) => false))).ToArray();
            Assert.AreEqual(5, results.Length);
            Assert.IsTrue(results[0].Value.Kind == NotificationKind.OnNext && results[0].Time == 210 && results[0].Value.Value == 2);
            Assert.IsTrue(results[1].Value.Kind == NotificationKind.OnNext && results[1].Time == 220 && results[1].Value.Value == 2);
            Assert.IsTrue(results[2].Value.Kind == NotificationKind.OnNext && results[2].Time == 230 && results[2].Value.Value == 2);
            Assert.IsTrue(results[3].Value.Kind == NotificationKind.OnNext && results[3].Time == 240 && results[3].Value.Value == 2);
            Assert.IsTrue(results[4].Value.Kind == NotificationKind.OnCompleted && results[4].Time == 250);
        }

        [TestMethod]
        public void DistinctUntilChanged_KeySelector_Div2()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2), //*
                OnNext(220, 4),
                OnNext(230, 3), //*
                OnNext(240, 5),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => xs.DistinctUntilChanged(x => x % 2)).ToArray();
            Assert.AreEqual(3, results.Length);
            Assert.IsTrue(results[0].Value.Kind == NotificationKind.OnNext && results[0].Time == 210 && results[0].Value.Value == 2);
            Assert.IsTrue(results[1].Value.Kind == NotificationKind.OnNext && results[1].Time == 230 && results[1].Value.Value == 3);
            Assert.IsTrue(results[2].Value.Kind == NotificationKind.OnCompleted && results[2].Time == 250);
        }

        class FuncComparer<T> : IEqualityComparer<T>
        {
            private Func<T, T, bool> _equals;

            public FuncComparer(Func<T, T, bool> equals)
            {
                _equals = equals;
            }

            public bool Equals(T x, T y)
            {
                return _equals(x, y);
            }

            public int GetHashCode(T obj)
            {
                return 0;
            }
        }

        [TestMethod]
        public void DistinctUntilChanged_KeySelectorThrows()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var ex = new Exception();
            var results = scheduler.Run(() => xs.DistinctUntilChanged(new Func<int, int>(x => { throw ex; }))).ToArray();

            results.AssertEqual(
                OnError<int>(210, ex)
                );
        }

        [TestMethod]
        public void DistinctUntilChanged_ComparerThrows()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var ex = new Exception();
            var results = scheduler.Run(() => xs.DistinctUntilChanged(new ThrowComparer<int>(ex))).ToArray();

            results.AssertEqual(
                OnNext(210, 2),
                OnError<int>(220, ex)
                );
        }

        class ThrowComparer<T> : IEqualityComparer<T>
        {
            private Exception _ex;

            public ThrowComparer(Exception ex)
            {
                _ex = ex;
            }

            public bool Equals(T x, T y)
            {
                throw _ex;
            }

            public int GetHashCode(T obj)
            {
                return 0;
            }
        }

        [TestMethod]
        public void Finally_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.Finally<int>(null, () => { }));
            Throws<ArgumentNullException>(() => Observable.Finally<int>(someObservable, null));
        }

        /* TODO: bug 1356
        [TestMethod]
        public void Finally_Never()
        {
            var scheduler = new TestScheduler();

            bool invoked = false;
            var results = scheduler.Run(() => Observable.Never<int>().Finally(() => { invoked = true; }));

            results.AssertEqual();

            Assert.IsTrue(invoked); // due to unsubscribe; see 1356
        }

        [TestMethod]
        public void Finally_OnlyCalledOnce_Never()
        {
            int invokeCount = 0;
            var someObservable = Observable.Never<int>().Finally(() => { invokeCount++; });
            var d = someObservable.Subscribe();
            d.Dispose();
            d.Dispose();

            Assert.AreEqual(1, invokeCount);
        }
        */

        [TestMethod]
        public void Finally_OnlyCalledOnce_Empty()
        {
            int invokeCount = 0;
            var someObservable = Observable.Empty<int>().Finally(() => { invokeCount++; });
            var d = someObservable.Subscribe();
            d.Dispose();
            d.Dispose();

            Assert.AreEqual(1, invokeCount);
        }

        [TestMethod]
        public void Finally_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            bool invoked = false;
            var results = scheduler.Run(() => xs.Finally(() => { invoked = true; })).ToArray();

            Assert.AreEqual(1, results.Length, "length");
            Assert.IsTrue(results[0].Value is Notification<int>.OnCompleted && results[0].Time == 250, "completed");

            Assert.IsTrue(invoked);
        }

        [TestMethod]
        public void Finally_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            bool invoked = false;
            var results = scheduler.Run(() => xs.Finally(() => { invoked = true; })).ToArray();

            Assert.AreEqual(2, results.Length, "length");
            Assert.IsTrue(results[0].Value is Notification<int>.OnNext && results[0].Value.Value == 2 && results[0].Time == 210, "first");
            Assert.IsTrue(results[1].Value is Notification<int>.OnCompleted && results[1].Time == 250, "completed");

            Assert.IsTrue(invoked);
        }

        [TestMethod]
        public void Finally_Throw()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();
            var msgs = new[] {
                OnNext(150, 1),
                OnError<int>(250, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            bool invoked = false;
            var results = scheduler.Run(() => xs.Finally(() => { invoked = true; })).ToArray();

            Assert.AreEqual(1, results.Length, "length");
            Assert.IsTrue(results[0].Value is Notification<int>.OnError && ((Notification<int>.OnError)results[0].Value).Exception == ex && results[0].Time == 250, "error");

            Assert.IsTrue(invoked);
        }

        [TestMethod]
        public void Do_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.Do<int>(someObservable, (Action<int>)null));
            Throws<ArgumentNullException>(() => Observable.Do<int>(null, _ => { }));
            Throws<ArgumentNullException>(() => Observable.Do<int>(someObservable, x => { }, (Action)null));
            Throws<ArgumentNullException>(() => Observable.Do<int>(someObservable, (Action<int>)null, () => { }));
            Throws<ArgumentNullException>(() => Observable.Do<int>(null, x => { }, () => { }));
            Throws<ArgumentNullException>(() => Observable.Do<int>(someObservable, x => { }, (Action<Exception>)null));
            Throws<ArgumentNullException>(() => Observable.Do<int>(someObservable, (Action<int>)null, (Exception _) => { }));
            Throws<ArgumentNullException>(() => Observable.Do<int>(null, x => { }, (Exception _) => { }));
            Throws<ArgumentNullException>(() => Observable.Do<int>(someObservable, x => { }, (Exception _) => { }, null));
            Throws<ArgumentNullException>(() => Observable.Do<int>(someObservable, x => { }, (Action<Exception>)null, () => { }));
            Throws<ArgumentNullException>(() => Observable.Do<int>(someObservable, (Action<int>)null, (Exception _) => { }, () => { }));
            Throws<ArgumentNullException>(() => Observable.Do<int>(null, x => { }, (Exception _) => { }, () => { }));
        }

        [TestMethod]
        public void Do_ShouldSeeAllValues()
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

            int i = 0;
            int sum = 2 + 3 + 4 + 5;
            scheduler.Run(() => xs.Do(x => { i++; sum -= x; })).ToArray();

            Assert.AreEqual(4, i);
            Assert.AreEqual(0, sum);
        }

        [TestMethod]
        public void Do_PlainAction()
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

            int i = 0;
            scheduler.Run(() => xs.Do(_ => { i++; })).ToArray();

            Assert.AreEqual(4, i);
        }

        [TestMethod]
        public void Do_NextCompleted()
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

            int i = 0;
            int sum = 2 + 3 + 4 + 5;
            bool completed = false;
            scheduler.Run(() => xs.Do(x => { i++; sum -= x; }, () => { completed = true; })).ToArray();

            Assert.AreEqual(4, i);
            Assert.AreEqual(0, sum);
            Assert.IsTrue(completed);
        }

        [TestMethod]
        public void Do_NextCompleted_Never()
        {
            var scheduler = new TestScheduler();

            int i = 0;
            bool completed = false;
            scheduler.Run(() => Observable.Never<int>().Do(x => { i++; }, () => { completed = true; })).ToArray();

            Assert.AreEqual(0, i);
            Assert.IsFalse(completed);
        }

        [TestMethod]
        public void Do_NextError()
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

            int i = 0;
            int sum = 2 + 3 + 4 + 5;
            bool sawError = false;
            scheduler.Run(() => xs.Do(x => { i++; sum -= x; }, e => { sawError = e == ex; })).ToArray();

            Assert.AreEqual(4, i);
            Assert.AreEqual(0, sum);
            Assert.IsTrue(sawError);
        }

        [TestMethod]
        public void Do_NextErrorNot()
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

            int i = 0;
            int sum = 2 + 3 + 4 + 5;
            bool sawError = false;
            scheduler.Run(() => xs.Do(x => { i++; sum -= x; }, _ => { sawError = true; })).ToArray();

            Assert.AreEqual(4, i);
            Assert.AreEqual(0, sum);
            Assert.IsFalse(sawError);
        }

        [TestMethod]
        public void Do_NextErrorCompleted()
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

            int i = 0;
            int sum = 2 + 3 + 4 + 5;
            bool sawError = false;
            bool hasCompleted = false;
            scheduler.Run(() => xs.Do(x => { i++; sum -= x; }, e => { sawError = true; }, () => { hasCompleted = true; })).ToArray();

            Assert.AreEqual(4, i);
            Assert.AreEqual(0, sum);
            Assert.IsFalse(sawError);
            Assert.IsTrue(hasCompleted);
        }

        [TestMethod]
        public void Do_NextErrorCompletedError()
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

            int i = 0;
            int sum = 2 + 3 + 4 + 5;
            bool sawError = false;
            bool hasCompleted = false;
            scheduler.Run(() => xs.Do(x => { i++; sum -= x; }, e => { sawError = e == ex; }, () => { hasCompleted = true; })).ToArray();

            Assert.AreEqual(4, i);
            Assert.AreEqual(0, sum);
            Assert.IsTrue(sawError);
            Assert.IsFalse(hasCompleted);
        }

        [TestMethod]
        public void Do_NextErrorCompletedNever()
        {
            var scheduler = new TestScheduler();

            int i = 0;
            bool sawError = false;
            bool hasCompleted = false;
            scheduler.Run(() => Observable.Never<int>().Do(x => { i++; }, e => { sawError = true; }, () => { hasCompleted = true; })).ToArray();

            Assert.AreEqual(0, i);
            Assert.IsFalse(sawError);
            Assert.IsFalse(hasCompleted);
        }

        [TestMethod]
        public void While_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.While(default(Func<bool>), DummyObservable<int>.Instance, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.While(DummyFunc<bool>.Instance, default(IObservable<int>), DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.While(DummyFunc<bool>.Instance, DummyObservable<int>.Instance, default(IScheduler)));
            Throws<ArgumentNullException>(() => Observable.While(default(Func<bool>), DummyObservable<int>.Instance));
            Throws<ArgumentNullException>(() => Observable.While(DummyFunc<bool>.Instance, default(IObservable<int>)));
        }

        [TestMethod]
        public void While_AlwaysFalse()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(50, 1),
                OnNext(100, 2),
                OnNext(150, 3),
                OnNext(200, 4),
                OnCompleted<int>(250)
                );

            var results = scheduler.Run(() => Observable.While(() => false, xs, scheduler));

            results.AssertEqual(
                OnCompleted<int>(201)
                );

            xs.Subscriptions.AssertEqual(
                );
        }

        [TestMethod]
        public void While_AlwaysTrue()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(50, 1),
                OnNext(100, 2),
                OnNext(150, 3),
                OnNext(200, 4),
                OnCompleted<int>(250)
                );

            var results = scheduler.Run(() => Observable.While(() => true, xs, scheduler));

            results.AssertEqual(
                OnNext(251, 1),
                OnNext(301, 2),
                OnNext(351, 3),
                OnNext(401, 4),
                OnNext(502, 1),
                OnNext(552, 2),
                OnNext(602, 3),
                OnNext(652, 4),
                OnNext(753, 1),
                OnNext(803, 2),
                OnNext(853, 3),
                OnNext(903, 4)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 452),
                Subscribe(452, 703),
                Subscribe(703, 954),
                Subscribe(954, 1000)
                );
        }

        [TestMethod]
        public void While_AlwaysTrue_Throw()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnError<int>(50, new MockException(1))
                );

            var results = scheduler.Run(() => Observable.While(() => true, xs, scheduler));

            results.AssertEqual(
                OnError<int>(251, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 251)
                );
        }

        [TestMethod]
        public void While_AlwaysTrue_Infinite()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(50, 1)
                );

            var results = scheduler.Run(() => Observable.While(() => true, xs, scheduler));

            results.AssertEqual(
                OnNext(251, 1)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 1000)
                );
        }

        [TestMethod]
        public void While_SometimesTrue()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(50, 1),
                OnNext(100, 2),
                OnNext(150, 3),
                OnNext(200, 4),
                OnCompleted<int>(250)
                );

            int n = 0;

            var results = scheduler.Run(() => Observable.While(() => ++n < 3, xs, scheduler));

            results.AssertEqual(
                OnNext(251, 1),
                OnNext(301, 2),
                OnNext(351, 3),
                OnNext(401, 4),
                OnNext(502, 1),
                OnNext(552, 2),
                OnNext(602, 3),
                OnNext(652, 4),
                OnCompleted<int>(703)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 452),
                Subscribe(452, 703)
                );
        }

        static T Throw<T>(Exception ex)
        {
            throw ex;
        }

        [TestMethod]
        public void While_SometimesThrows()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(50, 1),
                OnNext(100, 2),
                OnNext(150, 3),
                OnNext(200, 4),
                OnCompleted<int>(250)
                );

            int n = 0;

            var results = scheduler.Run(() => Observable.While(() => ++n < 3 ? true : Throw<bool>(new MockException(1)), xs, scheduler));

            results.AssertEqual(
                OnNext(251, 1),
                OnNext(301, 2),
                OnNext(351, 3),
                OnNext(401, 4),
                OnNext(502, 1),
                OnNext(552, 2),
                OnNext(602, 3),
                OnNext(652, 4),
                OnError<int>(703, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 452),
                Subscribe(452, 703)
                );
        }

        [TestMethod]
        public void While_CheckDefault()
        {
            var left = 0;
            var right = 0;
            Observable.While(() => ++left < 5, Observable.Range(1, 3), Scheduler.ThreadPool).AssertEqual(
                Observable.While(() => ++right < 5, Observable.Range(1, 3)));
        }

        [TestMethod]
        public void If_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.If(null, DummyObservable<int>.Instance, DummyObservable<int>.Instance));
            Throws<ArgumentNullException>(() => Observable.If(DummyFunc<bool>.Instance, null, DummyObservable<int>.Instance));
            Throws<ArgumentNullException>(() => Observable.If(DummyFunc<bool>.Instance, DummyObservable<int>.Instance, null));
        }

        [TestMethod]
        public void If_True()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(210, 1),
                OnNext(250, 2),
                OnCompleted<int>(300)
                );

            var ys = scheduler.CreateHotObservable(
                OnNext(310, 3),
                OnNext(350, 4),
                OnCompleted<int>(400)
                );

            var results = scheduler.Run(() => Observable.If(() => true, xs, ys));

            results.AssertEqual(
                OnNext(210, 1),
                OnNext(250, 2),
                OnCompleted<int>(300)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 300)
                );

            ys.Subscriptions.AssertEqual(
                );
        }

        [TestMethod]
        public void If_False()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(210, 1),
                OnNext(250, 2),
                OnCompleted<int>(300)
                );

            var ys = scheduler.CreateHotObservable(
                OnNext(310, 3),
                OnNext(350, 4),
                OnCompleted<int>(400)
                );

            var results = scheduler.Run(() => Observable.If(() => false, xs, ys));

            results.AssertEqual(
                OnNext(310, 3),
                OnNext(350, 4),
                OnCompleted<int>(400)
                );

            xs.Subscriptions.AssertEqual(
                );

            ys.Subscriptions.AssertEqual(
                Subscribe(200, 400)
                );
        }

        [TestMethod]
        public void If_Throw()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(210, 1),
                OnNext(250, 2),
                OnCompleted<int>(300)
                );

            var ys = scheduler.CreateHotObservable(
                OnNext(310, 3),
                OnNext(350, 4),
                OnCompleted<int>(400)
                );

            var results = scheduler.Run(() => Observable.If(() => Throw<bool>(new MockException(2)), xs, ys));

            results.AssertEqual(
                OnError<int>(200, new MockException(2))
                );

            xs.Subscriptions.AssertEqual(
                );

            ys.Subscriptions.AssertEqual(
                );
        }

        [TestMethod]
        public void If_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(210, 1),
                OnNext(250, 2)
                );

            var ys = scheduler.CreateHotObservable(
                OnNext(310, 3),
                OnNext(350, 4),
                OnCompleted<int>(400)
                );

            var results = scheduler.Run(() => Observable.If(() => true, xs, ys));

            results.AssertEqual(
                OnNext(210, 1),
                OnNext(250, 2)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 1000)
                );

            ys.Subscriptions.AssertEqual(
                );
        }

        [TestMethod]
        public void DoWhile_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.DoWhile(null, DummyObservable<int>.Instance, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.DoWhile(DummyFunc<bool>.Instance, default(IObservable<int>), DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.DoWhile(DummyFunc<bool>.Instance, DummyObservable<int>.Instance, null));
            Throws<ArgumentNullException>(() => Observable.DoWhile(null, DummyObservable<int>.Instance));
            Throws<ArgumentNullException>(() => Observable.DoWhile(DummyFunc<bool>.Instance, default(IObservable<int>)));
        }

        [TestMethod]
        public void DoWhile_AlwaysFalse()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(50, 1),
                OnNext(100, 2),
                OnNext(150, 3),
                OnNext(200, 4),
                OnCompleted<int>(250)
                );

            var results = scheduler.Run(() => Observable.DoWhile(() => false, xs, scheduler));

            results.AssertEqual(
                OnNext(251, 1),
                OnNext(301, 2),
                OnNext(351, 3),
                OnNext(401, 4),
                OnCompleted<int>(454)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 452)
                );
        }

        [TestMethod]
        public void DoWhile_AlwaysTrue()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(50, 1),
                OnNext(100, 2),
                OnNext(150, 3),
                OnNext(200, 4),
                OnCompleted<int>(250)
                );

            var results = scheduler.Run(() => Observable.DoWhile(() => true, xs, scheduler));

            results.AssertEqual(
                OnNext(251, 1),
                OnNext(301, 2),
                OnNext(351, 3),
                OnNext(401, 4),
                OnNext(503, 1),
                OnNext(553, 2),
                OnNext(603, 3),
                OnNext(653, 4),
                OnNext(754, 1),
                OnNext(804, 2),
                OnNext(854, 3),
                OnNext(904, 4)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 452),
                Subscribe(453, 704),
                Subscribe(704, 955),
                Subscribe(955, 1000)
                );
        }

        [TestMethod]
        public void DoWhile_AlwaysTrue_Throw()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnError<int>(50, new MockException(1))
                );

            var results = scheduler.Run(() => Observable.DoWhile(() => true, xs, scheduler));

            results.AssertEqual(
                OnError<int>(251, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 251)
                );
        }

        [TestMethod]
        public void DoWhile_AlwaysTrue_Infinite()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(50, 1)
                );

            var results = scheduler.Run(() => Observable.DoWhile(() => true, xs, scheduler));

            results.AssertEqual(
                OnNext(251, 1)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 1000)
                );
        }

        [TestMethod]
        public void DoWhile_SometimesTrue()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(50, 1),
                OnNext(100, 2),
                OnNext(150, 3),
                OnNext(200, 4),
                OnCompleted<int>(250)
                );

            int n = 0;

            var results = scheduler.Run(() => Observable.DoWhile(() => ++n < 3, xs, scheduler));

            results.AssertEqual(
                OnNext(251, 1),
                OnNext(301, 2),
                OnNext(351, 3),
                OnNext(401, 4),
                OnNext(503, 1),
                OnNext(553, 2),
                OnNext(603, 3),
                OnNext(653, 4),
                OnNext(754, 1),
                OnNext(804, 2),
                OnNext(854, 3),
                OnNext(904, 4),
                OnCompleted<int>(956)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 452),
                Subscribe(453, 704),
                Subscribe(704, 955)
                );
        }

        [TestMethod]
        public void DoWhile_SometimesThrows()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(50, 1),
                OnNext(100, 2),
                OnNext(150, 3),
                OnNext(200, 4),
                OnCompleted<int>(250)
                );

            int n = 0;

            var results = scheduler.Run(() => Observable.DoWhile(() => ++n < 3 ? true : Throw<bool>(new MockException(1)), xs, scheduler));

            results.AssertEqual(
                OnNext(251, 1),
                OnNext(301, 2),
                OnNext(351, 3),
                OnNext(401, 4),
                OnNext(503, 1),
                OnNext(553, 2),
                OnNext(603, 3),
                OnNext(653, 4),
                OnNext(754, 1),
                OnNext(804, 2),
                OnNext(854, 3),
                OnNext(904, 4),
                OnError<int>(955, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 452),
                Subscribe(453, 704),
                Subscribe(704, 955)
                );
        }

        [TestMethod]
        public void DoWhile_CheckDefault()
        {
            var left = 0;
            var right = 0;
            Observable.DoWhile(() => ++left < 5, Observable.Range(1, 3), Scheduler.ThreadPool).AssertEqual(
                Observable.DoWhile(() => ++right < 5, Observable.Range(1, 3)));
        }

        [TestMethod]
        public void Case_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Case(null, new Dictionary<int, IObservable<int>>(), DummyObservable<int>.Instance));
            Throws<ArgumentNullException>(() => Observable.Case(DummyFunc<int>.Instance, null, DummyObservable<int>.Instance));
            Throws<ArgumentNullException>(() => Observable.Case(DummyFunc<int>.Instance, new Dictionary<int, IObservable<int>>(), default(IObservable<int>)));

            Throws<ArgumentNullException>(() => Observable.Case(null, new Dictionary<int, IObservable<int>>(), DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.Case<int, int>(DummyFunc<int>.Instance, null, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.Case(DummyFunc<int>.Instance, new Dictionary<int, IObservable<int>>(), default(IScheduler)));

            Throws<ArgumentNullException>(() => Observable.Case(null, new Dictionary<int, IObservable<int>>()));
            Throws<ArgumentNullException>(() => Observable.Case<int, int>(DummyFunc<int>.Instance, null));
        }

        [TestMethod]
        public void Case_One()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(210, 1),
                OnNext(240, 2),
                OnNext(270, 3),
                OnCompleted<int>(300)
                );

            var ys = scheduler.CreateHotObservable(
                OnNext(220, 11),
                OnNext(250, 12),
                OnNext(280, 13),
                OnCompleted<int>(310)
                );

            var zs = scheduler.CreateHotObservable(
                OnNext(230, 21),
                OnNext(240, 22),
                OnNext(290, 23),
                OnCompleted<int>(320)
                );

            var map = new Dictionary<int, IObservable<int>>
            {
                { 1, xs },
                { 2, ys }
            };

            var results = scheduler.Run(() => Observable.Case(() => 1, map, zs));

            results.AssertEqual(
                OnNext(210, 1),
                OnNext(240, 2),
                OnNext(270, 3),
                OnCompleted<int>(300)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 300)
                );

            ys.Subscriptions.AssertEqual(
                );

            zs.Subscriptions.AssertEqual(
                );
        }

        [TestMethod]
        public void Case_Two()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(210, 1),
                OnNext(240, 2),
                OnNext(270, 3),
                OnCompleted<int>(300)
                );

            var ys = scheduler.CreateHotObservable(
                OnNext(220, 11),
                OnNext(250, 12),
                OnNext(280, 13),
                OnCompleted<int>(310)
                );

            var zs = scheduler.CreateHotObservable(
                OnNext(230, 21),
                OnNext(240, 22),
                OnNext(290, 23),
                OnCompleted<int>(320)
                );

            var map = new Dictionary<int, IObservable<int>>
            {
                { 1, xs },
                { 2, ys }
            };

            var results = scheduler.Run(() => Observable.Case(() => 2, map, zs));

            results.AssertEqual(
                OnNext(220, 11),
                OnNext(250, 12),
                OnNext(280, 13),
                OnCompleted<int>(310)
                );

            xs.Subscriptions.AssertEqual(
                );

            ys.Subscriptions.AssertEqual(
                Subscribe(200, 310)
                );

            zs.Subscriptions.AssertEqual(
                );
        }

        [TestMethod]
        public void Case_Three()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(210, 1),
                OnNext(240, 2),
                OnNext(270, 3),
                OnCompleted<int>(300)
                );

            var ys = scheduler.CreateHotObservable(
                OnNext(220, 11),
                OnNext(250, 12),
                OnNext(280, 13),
                OnCompleted<int>(310)
                );

            var zs = scheduler.CreateHotObservable(
                OnNext(230, 21),
                OnNext(240, 22),
                OnNext(290, 23),
                OnCompleted<int>(320)
                );

            var map = new Dictionary<int, IObservable<int>>
            {
                { 1, xs },
                { 2, ys }
            };

            var results = scheduler.Run(() => Observable.Case(() => 3, map, zs));

            results.AssertEqual(
                OnNext(230, 21),
                OnNext(240, 22),
                OnNext(290, 23),
                OnCompleted<int>(320)
                );

            xs.Subscriptions.AssertEqual(
                );

            ys.Subscriptions.AssertEqual(
                );

            zs.Subscriptions.AssertEqual(
                Subscribe(200, 320)
                );
        }

        [TestMethod]
        public void Case_Throw()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(210, 1),
                OnNext(240, 2),
                OnNext(270, 3),
                OnCompleted<int>(300)
                );

            var ys = scheduler.CreateHotObservable(
                OnNext(220, 11),
                OnNext(250, 12),
                OnNext(280, 13),
                OnCompleted<int>(310)
                );

            var zs = scheduler.CreateHotObservable(
                OnNext(230, 21),
                OnNext(240, 22),
                OnNext(290, 23),
                OnCompleted<int>(320)
                );

            var map = new Dictionary<int, IObservable<int>>
            {
                { 1, xs },
                { 2, ys }
            };

            var results = scheduler.Run(() => Observable.Case(() => Throw<int>(new MockException(1)), map, zs));

            results.AssertEqual(
                OnError<int>(200, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                );

            ys.Subscriptions.AssertEqual(
                );

            zs.Subscriptions.AssertEqual(
                );
        }

        [TestMethod]
        public void CaseWithDefault_One()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(210, 1),
                OnNext(240, 2),
                OnNext(270, 3),
                OnCompleted<int>(300)
                );

            var ys = scheduler.CreateHotObservable(
                OnNext(220, 11),
                OnNext(250, 12),
                OnNext(280, 13),
                OnCompleted<int>(310)
                );

            var map = new Dictionary<int, IObservable<int>>
            {
                { 1, xs },
                { 2, ys }
            };

            var results = scheduler.Run(() => Observable.Case(() => 1, map, scheduler));

            results.AssertEqual(
                OnNext(210, 1),
                OnNext(240, 2),
                OnNext(270, 3),
                OnCompleted<int>(300)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 300)
                );

            ys.Subscriptions.AssertEqual(
                );
        }

        [TestMethod]
        public void CaseWithDefault_Two()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(210, 1),
                OnNext(240, 2),
                OnNext(270, 3),
                OnCompleted<int>(300)
                );

            var ys = scheduler.CreateHotObservable(
                OnNext(220, 11),
                OnNext(250, 12),
                OnNext(280, 13),
                OnCompleted<int>(310)
                );

            var map = new Dictionary<int, IObservable<int>>
            {
                { 1, xs },
                { 2, ys }
            };

            var results = scheduler.Run(() => Observable.Case(() => 2, map, scheduler));

            results.AssertEqual(
                OnNext(220, 11),
                OnNext(250, 12),
                OnNext(280, 13),
                OnCompleted<int>(310)
                );

            xs.Subscriptions.AssertEqual(
                );

            ys.Subscriptions.AssertEqual(
                Subscribe(200, 310)
                );
        }

        [TestMethod]
        public void CaseWithDefault_Three()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(210, 1),
                OnNext(240, 2),
                OnNext(270, 3),
                OnCompleted<int>(300)
                );

            var ys = scheduler.CreateHotObservable(
                OnNext(220, 11),
                OnNext(250, 12),
                OnNext(280, 13),
                OnCompleted<int>(310)
                );

            var map = new Dictionary<int, IObservable<int>>
            {
                { 1, xs },
                { 2, ys }
            };

            var results = scheduler.Run(() => Observable.Case(() => 3, map, scheduler));

            results.AssertEqual(
                OnCompleted<int>(201)
                );

            xs.Subscriptions.AssertEqual(
                );

            ys.Subscriptions.AssertEqual(
                );
        }

        [TestMethod]
        public void CaseWithDefault_Throw()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(210, 1),
                OnNext(240, 2),
                OnNext(270, 3),
                OnCompleted<int>(300)
                );

            var ys = scheduler.CreateHotObservable(
                OnNext(220, 11),
                OnNext(250, 12),
                OnNext(280, 13),
                OnCompleted<int>(310)
                );

            var map = new Dictionary<int, IObservable<int>>
            {
                { 1, xs },
                { 2, ys }
            };

            var results = scheduler.Run(() => Observable.Case(() => Throw<int>(new MockException(1)), map, scheduler));

            results.AssertEqual(
                OnError<int>(200, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                );

            ys.Subscriptions.AssertEqual(
                );
        }

        [TestMethod]
        public void CaseWithDefault_CheckDefault()
        {
            Observable.Case(() => 1, new Dictionary<int, IObservable<int>>(), Scheduler.ThreadPool)
                .AssertEqual(Observable.Case(() => 1, new Dictionary<int, IObservable<int>>()));
        }

        [TestMethod]
        public void For_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.For(DummyEnumerable<int>.Instance, DummyFunc<int, IObservable<int>>.Instance, null));
            Throws<ArgumentNullException>(() => Observable.For(DummyEnumerable<int>.Instance, default(Func<int, IObservable<int>>), DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.For(null, DummyFunc<int, IObservable<int>>.Instance, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.For(DummyEnumerable<int>.Instance, default(Func<int, IObservable<int>>)));
            Throws<ArgumentNullException>(() => Observable.For(null, DummyFunc<int, IObservable<int>>.Instance));
        }

        [TestMethod]
        public void For_Basic()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.For(new[] { 1, 2, 3 }, x => scheduler.CreateColdObservable(
                OnNext<int>((ushort)(x * 100 + 10), x * 10 + 1),
                OnNext<int>((ushort)(x * 100 + 20), x * 10 + 2),
                OnNext<int>((ushort)(x * 100 + 30), x * 10 + 3),
                OnCompleted<int>((ushort)(x * 100 + 40))), scheduler));

            results.AssertEqual(
                OnNext(311, 11),
                OnNext(321, 12),
                OnNext(331, 13),
                OnNext(552, 21),
                OnNext(562, 22),
                OnNext(572, 23),
                OnNext(893, 31),
                OnNext(903, 32),
                OnNext(913, 33),
                OnCompleted<int>(924)
                );
        }

        IEnumerable<int> For_Error_Core()
        {
            yield return 1;
            yield return 2;
            yield return 3;
            throw new MockException(1);
        }

        [TestMethod]
        public void For_Error()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.For(For_Error_Core(), x => scheduler.CreateColdObservable(
                OnNext<int>((ushort)(x * 100 + 10), x * 10 + 1),
                OnNext<int>((ushort)(x * 100 + 20), x * 10 + 2),
                OnNext<int>((ushort)(x * 100 + 30), x * 10 + 3),
                OnCompleted<int>((ushort)(x * 100 + 40))), scheduler));

            results.AssertEqual(
                OnNext(311, 11),
                OnNext(321, 12),
                OnNext(331, 13),
                OnNext(552, 21),
                OnNext(562, 22),
                OnNext(572, 23),
                OnNext(893, 31),
                OnNext(903, 32),
                OnNext(913, 33),
                OnError<int>(924, new MockException(1))
                );
        }

        [TestMethod]
        public void For_Throws()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.For(new[] { 1, 2, 3 }, x => Throw<IObservable<int>>(new MockException(1)), scheduler));

            results.AssertEqual(
                OnError<int>(201, new MockException(1))
                );
        }

        [TestMethod]
        public void For_CheckDefault()
        {
            Observable.For(Enumerable.Range(1, 3), x => Observable.Range(1, 3), Scheduler.ThreadPool).AssertEqual(
                Observable.For(Enumerable.Range(1, 3), x => Observable.Range(1, 3))
                );
        }

        [TestMethod]
        public void Let_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Let(0, default(Func<int, IObservable<int>>)));
        }

        [TestMethod]
        public void Let_Return()
        {
            var scheduler = new TestScheduler();

            var n = 0;

            scheduler.Schedule(() => ++n, 90);
            scheduler.Schedule(() => ++n, 190);

            var results = scheduler.Run(() => Observable.Let(n, x => Observable.Return(x, scheduler)));

            results.AssertEqual(
                OnNext(201, 1),
                OnCompleted<int>(201)
                );
        }

        [TestMethod]
        public void Let_Error()
        {
            var scheduler = new TestScheduler();

            var n = 0;

            scheduler.Schedule(() => ++n, 90);
            scheduler.Schedule(() => ++n, 190);

            var results = scheduler.Run(() => Observable.Let(n, x => Observable.Throw<int>(new MockException(x), scheduler)));

            results.AssertEqual(
                OnError<int>(201, new MockException(1))
                );
        }

        [TestMethod]
        public void Let_Never()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<int>(
                );

            var n = 0;

            scheduler.Schedule(() => ++n, 90);
            scheduler.Schedule(() => ++n, 190);

            var results = scheduler.Run(() => Observable.Let(n, x => xs));

            results.AssertEqual(
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 1000)
                );
        }

        [TestMethod]
        public void Let_Throw()
        {
            var scheduler = new TestScheduler();

            var n = 0;

            scheduler.Schedule(() => ++n, 90);
            scheduler.Schedule(() => ++n, 190);

            var results = scheduler.Run(() => Observable.Let(n, x => Throw<IObservable<int>>(new MockException(x))));

            results.AssertEqual(
                OnError<int>(200, new MockException(1))
                );
        }
    }
}
