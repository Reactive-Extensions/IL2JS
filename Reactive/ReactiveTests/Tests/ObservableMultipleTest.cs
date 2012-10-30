using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
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
    public class ObservableMultipleTest : Test
    {
        [TestMethod]
        public void TakeUntil_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.TakeUntil<int, int>(null, someObservable));
            Throws<ArgumentNullException>(() => Observable.TakeUntil<int, int>(someObservable, null));
        }

        [TestMethod]
        public void TakeUntil_Preempt_SomeData_Next()
        {
            var scheduler = new TestScheduler();

            var lMsgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            };

            var rMsgs = new[] {
                OnNext(150, 1),
                OnNext(225, 99),
                OnCompleted<int>(230)
            };

            var l = scheduler.CreateHotObservable(lMsgs);
            var r = scheduler.CreateHotObservable(rMsgs);

            var results = scheduler.Run(() => l.TakeUntil(r));
            results.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 3),
                OnCompleted<int>(225)
            );
        }

        [TestMethod]
        public void TakeUntil_Preempt_SomeData_Error()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var lMsgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            };

            var rMsgs = new[] {
                OnNext(150, 1),
                OnError<int>(225, ex)
            };

            var l = scheduler.CreateHotObservable(lMsgs);
            var r = scheduler.CreateHotObservable(rMsgs);

            var results = scheduler.Run(() => l.TakeUntil(r));
            results.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 3),
                OnError<int>(225, ex)
            );
        }

        [TestMethod]
        public void TakeUntil_NoPreempt_SomeData_Empty()
        {
            var scheduler = new TestScheduler();

            var lMsgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            };

            var rMsgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(225)
            };

            var l = scheduler.CreateHotObservable(lMsgs);
            var r = scheduler.CreateHotObservable(rMsgs);

            var results = scheduler.Run(() => l.TakeUntil(r));
            results.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void TakeUntil_NoPreempt_SomeData_Never()
        {
            var scheduler = new TestScheduler();

            var lMsgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            };

            var l = scheduler.CreateHotObservable(lMsgs);
            var r = Observable.Never<int>();

            var results = scheduler.Run(() => l.TakeUntil(r));
            results.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void TakeUntil_Preempt_Never_Next()
        {
            var scheduler = new TestScheduler();

            var rMsgs = new[] {
                OnNext(150, 1),
                OnNext(225, 2), //!
                OnCompleted<int>(250)
            };

            var l = Observable.Never<int>();
            var r = scheduler.CreateHotObservable(rMsgs);

            var results = scheduler.Run(() => l.TakeUntil(r));
            results.AssertEqual(
                OnCompleted<int>(225)
            );
        }

        [TestMethod]
        public void TakeUntil_Preempt_Never_Error()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var rMsgs = new[] {
                OnNext(150, 1),
                OnError<int>(225, ex)
            };

            var l = Observable.Never<int>();
            var r = scheduler.CreateHotObservable(rMsgs);

            var results = scheduler.Run(() => l.TakeUntil(r));
            results.AssertEqual(
                OnError<int>(225, ex)
            );
        }

        [TestMethod]
        public void TakeUntil_NoPreempt_Never_Empty()
        {
            var scheduler = new TestScheduler();

            var rMsgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(225)
            };

            var l = Observable.Never<int>();
            var r = scheduler.CreateHotObservable(rMsgs);

            var results = scheduler.Run(() => l.TakeUntil(r));
            results.AssertEqual();
        }

        [TestMethod]
        public void TakeUntil_NoPreempt_Never_Never()
        {
            var scheduler = new TestScheduler();

            var l = Observable.Never<int>();
            var r = Observable.Never<int>();

            var results = scheduler.Run(() => l.TakeUntil(r));
            results.AssertEqual();
        }

        [TestMethod]
        public void TakeUntil_Preempt_BeforeFirstProduced()
        {
            var scheduler = new TestScheduler();

            var lMsgs = new[] {
                OnNext(150, 1),
                OnNext(230, 2),
                OnCompleted<int>(240)
            };

            var rMsgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2), //!
                OnCompleted<int>(220)
            };

            var l = scheduler.CreateHotObservable(lMsgs);
            var r = scheduler.CreateHotObservable(rMsgs);

            var results = scheduler.Run(() => l.TakeUntil(r));
            results.AssertEqual(
                OnCompleted<int>(210)
            );
        }

        [TestMethod]
        public void TakeUntil_Preempt_BeforeFirstProduced_RemainSilentAndProperDisposed()
        {
            var scheduler = new TestScheduler();

            var lMsgs = new[] {
                OnNext(150, 1),
                OnError<int>(215, new Exception()), // should not come
                OnCompleted<int>(240)
            };

            var rMsgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2), //!
                OnCompleted<int>(220)
            };

            bool sourceNotDisposed = false;

            var l = scheduler.CreateHotObservable(lMsgs).Do(_ => sourceNotDisposed = true);
            var r = scheduler.CreateHotObservable(rMsgs);

            var results = scheduler.Run(() => l.TakeUntil(r));
            results.AssertEqual(
                OnCompleted<int>(210)
            );

            Assert.IsFalse(sourceNotDisposed);
        }

        [TestMethod]
        public void TakeUntil_NoPreempt_AfterLastProduced_ProperDisposedSignal()
        {
            var scheduler = new TestScheduler();

            var lMsgs = new[] {
                OnNext(150, 1),
                OnNext(230, 2),
                OnCompleted<int>(240)
            };

            var rMsgs = new[] {
                OnNext(150, 1),
                OnNext(250, 2),
                OnCompleted<int>(260)
            };

            bool signalNotDisposed = false;

            var l = scheduler.CreateHotObservable(lMsgs);
            var r = scheduler.CreateHotObservable(rMsgs).Do(_ => signalNotDisposed = true);

            var results = scheduler.Run(() => l.TakeUntil(r));
            results.AssertEqual(
                OnNext(230, 2),
                OnCompleted<int>(240)
            );

            Assert.IsFalse(signalNotDisposed);
        }

        [TestMethod]
        public void SkipUntil_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.SkipUntil<int, int>(null, someObservable));
            Throws<ArgumentNullException>(() => Observable.SkipUntil<int, int>(someObservable, null));
        }

        [TestMethod]
        public void SkipUntil_SomeData_Next()
        {
            var scheduler = new TestScheduler();

            var lMsgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4), //!
                OnNext(240, 5), //!
                OnCompleted<int>(250)
            };

            var rMsgs = new[] {
                OnNext(150, 1),
                OnNext(225, 99),
                OnCompleted<int>(230)
            };

            var l = scheduler.CreateHotObservable(lMsgs);
            var r = scheduler.CreateHotObservable(rMsgs);

            var results = scheduler.Run(() => l.SkipUntil(r));
            results.AssertEqual(
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void SkipUntil_SomeData_Error()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var lMsgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            };

            var rMsgs = new[] {
                OnNext(150, 1),
                OnError<int>(225, ex)
            };

            var l = scheduler.CreateHotObservable(lMsgs);
            var r = scheduler.CreateHotObservable(rMsgs);

            var results = scheduler.Run(() => l.SkipUntil(r));
            results.AssertEqual(
                OnError<int>(225, ex)
            );
        }

        [TestMethod]
        public void SkipUntil_SomeData_Empty()
        {
            var scheduler = new TestScheduler();

            var lMsgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            };

            var rMsgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(225)
            };

            var l = scheduler.CreateHotObservable(lMsgs);
            var r = scheduler.CreateHotObservable(rMsgs);

            var results = scheduler.Run(() => l.SkipUntil(r));
            results.AssertEqual();
        }

        [TestMethod]
        public void SkipUntil_Never_Next()
        {
            var scheduler = new TestScheduler();

            var rMsgs = new[] {
                OnNext(150, 1),
                OnNext(225, 2), //!
                OnCompleted<int>(250)
            };

            var l = Observable.Never<int>();
            var r = scheduler.CreateHotObservable(rMsgs);

            var results = scheduler.Run(() => l.SkipUntil(r));
            results.AssertEqual();
        }

        [TestMethod]
        public void SkipUntil_Never_Error()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var rMsgs = new[] {
                OnNext(150, 1),
                OnError<int>(225, ex)
            };

            var l = Observable.Never<int>();
            var r = scheduler.CreateHotObservable(rMsgs);

            var results = scheduler.Run(() => l.SkipUntil(r));
            results.AssertEqual(
                OnError<int>(225, ex)
            );
        }

        [TestMethod]
        public void SkipUntil_SomeData_Never()
        {
            var scheduler = new TestScheduler();

            var lMsgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            };

            var l = scheduler.CreateHotObservable(lMsgs);
            var r = Observable.Never<int>();

            var results = scheduler.Run(() => l.SkipUntil(r));
            results.AssertEqual();
        }

        [TestMethod]
        public void SkipUntil_Never_Empty()
        {
            var scheduler = new TestScheduler();

            var rMsgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(225)
            };

            var l = Observable.Never<int>();
            var r = scheduler.CreateHotObservable(rMsgs);

            var results = scheduler.Run(() => l.SkipUntil(r));
            results.AssertEqual();
        }

        [TestMethod]
        public void SkipUntil_Never_Never()
        {
            var scheduler = new TestScheduler();

            var l = Observable.Never<int>();
            var r = Observable.Never<int>();

            var results = scheduler.Run(() => l.SkipUntil(r));
            results.AssertEqual();
        }

        [TestMethod]
        public void SkipUntil_HasCompletedCausesDisposal()
        {
            var scheduler = new TestScheduler();

            var lMsgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            };

            bool disposed = false;

            var l = scheduler.CreateHotObservable(lMsgs);
            var r = Observable.Create<int>(obs => () => { disposed = true; });

            var results = scheduler.Run(() => l.SkipUntil(r));
            results.AssertEqual();

            Assert.IsTrue(disposed, "disposed");
        }

        [TestMethod]
        public void Merge_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.Merge(default(IScheduler), someObservable, someObservable));
            Throws<ArgumentNullException>(() => Observable.Merge(someObservable, someObservable, default(IScheduler)));
            Throws<ArgumentNullException>(() => Observable.Merge(someObservable, null));
            Throws<ArgumentNullException>(() => Observable.Merge(default(IObservable<int>), someObservable));
            Throws<ArgumentNullException>(() => Observable.Merge((IObservable<int>[])null));
            Throws<ArgumentNullException>(() => Observable.Merge((IEnumerable<IObservable<int>>)null));
            Throws<ArgumentNullException>(() => ((IObservable<int>)null).Merge(someObservable, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => someObservable.Merge(default(IObservable<int>), DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.Merge((IEnumerable<IObservable<int>>)null, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.Merge(new IObservable<int>[0], default(IScheduler)));
            Throws<ArgumentNullException>(() => Observable.Merge((IObservable<IObservable<int>>)null));
            Throws<ArgumentNullException>(() => Observable.Merge(DummyScheduler.Instance, (IObservable<int>[])null));
            Throws<ArgumentNullException>(() => Observable.Merge(Scheduler.CurrentThread, new IObservable<int>[] { someObservable, null, someObservable }).Subscribe());
        }

        [TestMethod]
        public void Merge_Never2()
        {
            var n1 = Observable.Never<int>();
            var n2 = Observable.Never<int>();

            var scheduler = new TestScheduler();
            var results = scheduler.Run(() => Observable.Merge(scheduler, n1, n2));
            results.AssertEqual();
        }

        [TestMethod]
        public void Merge_Never3()
        {
            var n1 = Observable.Never<int>();
            var n2 = Observable.Never<int>();
            var n3 = Observable.Never<int>();

            var scheduler = new TestScheduler();
            var results = scheduler.Run(() => Observable.Merge(scheduler, n1, n2, n3));
            results.AssertEqual();
        }

        [TestMethod]
        public void Merge_Empty2()
        {
            var scheduler = new TestScheduler();
            var e1 = Observable.Empty<int>(scheduler);
            var e2 = Observable.Empty<int>(scheduler);
            var results = scheduler.Run(() => Observable.Merge(scheduler, e1, e2));
            results.AssertEqual(
                OnCompleted<int>(203)
            );
        }

        [TestMethod]
        public void Merge_Empty3()
        {
            var e1 = Observable.Empty<int>();
            var e2 = Observable.Empty<int>();
            var e3 = Observable.Empty<int>();

            var scheduler = new TestScheduler();
            var results = scheduler.Run(() => Observable.Merge(scheduler, e1, e2, e3));
            results.AssertEqual(
                OnCompleted<int>(204)
            );
        }

        [TestMethod]
        public void Merge_EmptyDelayed2_RightLast()
        {
            var scheduler = new TestScheduler();

            var lMsgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(240)
            };

            var rMsgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var e1 = scheduler.CreateHotObservable(lMsgs);
            var e2 = scheduler.CreateHotObservable(rMsgs);

            var results = scheduler.Run(() => Observable.Merge(scheduler, e1, e2));
            results.AssertEqual(
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void Merge_EmptyDelayed2_LeftLast()
        {
            var scheduler = new TestScheduler();

            var lMsgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var rMsgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(240)
            };

            var e1 = scheduler.CreateHotObservable(lMsgs);
            var e2 = scheduler.CreateHotObservable(rMsgs);

            var results = scheduler.Run(() => Observable.Merge(scheduler, e1, e2));
            results.AssertEqual(
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void Merge_EmptyDelayed3_MiddleLast()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(245)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var msgs3 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(240)
            };

            var e1 = scheduler.CreateHotObservable(msgs1);
            var e2 = scheduler.CreateHotObservable(msgs2);
            var e3 = scheduler.CreateHotObservable(msgs3);

            var results = scheduler.Run(() => Observable.Merge(scheduler, e1, e2, e3));
            results.AssertEqual(
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void Merge_EmptyNever()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(245)
            };

            var e1 = scheduler.CreateHotObservable(msgs1);
            var n1 = Observable.Never<int>();

            var results = scheduler.Run(() => Observable.Merge(scheduler, e1, n1));
            results.AssertEqual();
        }

        [TestMethod]
        public void Merge_NeverEmpty()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(245)
            };

            var e1 = scheduler.CreateHotObservable(msgs1);
            var n1 = Observable.Never<int>();

            var results = scheduler.Run(() => Observable.Merge(scheduler, n1, e1));
            results.AssertEqual();
        }

        [TestMethod]
        public void Merge_ReturnNever()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(245)
            };

            var r1 = scheduler.CreateHotObservable(msgs1);
            var n1 = Observable.Never<int>();

            var results = scheduler.Run(() => Observable.Merge(scheduler, r1, n1));
            results.AssertEqual(
                OnNext(210, 2)
            );
        }

        [TestMethod]
        public void Merge_NeverReturn()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(245)
            };

            var r1 = scheduler.CreateHotObservable(msgs1);
            var n1 = Observable.Never<int>();

            var results = scheduler.Run(() => Observable.Merge(scheduler, n1, r1));
            results.AssertEqual(
                OnNext(210, 2)
            );
        }

        [TestMethod]
        public void Merge_ErrorNever()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnError<int>(245, ex)
            };

            var e1 = scheduler.CreateHotObservable(msgs1);
            var n1 = Observable.Never<int>();

            var results = scheduler.Run(() => Observable.Merge(scheduler, e1, n1));
            results.AssertEqual(
                OnNext(210, 2),
                OnError<int>(245, ex)
            );
        }

        [TestMethod]
        public void Merge_NeverError()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnError<int>(245, ex)
            };

            var e1 = scheduler.CreateHotObservable(msgs1);
            var n1 = Observable.Never<int>();

            var results = scheduler.Run(() => Observable.Merge(scheduler, n1, e1));
            results.AssertEqual(
                OnNext(210, 2),
                OnError<int>(245, ex)
            );
        }

        [TestMethod]
        public void Merge_EmptyReturn()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(245)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(250)
            };

            var e1 = scheduler.CreateHotObservable(msgs1);
            var r1 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => Observable.Merge(scheduler, e1, r1));
            results.AssertEqual(
                OnNext(210, 2),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void Merge_ReturnEmpty()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(245)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(250)
            };

            var e1 = scheduler.CreateHotObservable(msgs1);
            var r1 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => Observable.Merge(scheduler, r1, e1));
            results.AssertEqual(
                OnNext(210, 2),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void Merge_Lots2()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 4),
                OnNext(230, 6),
                OnNext(240, 8),
                OnCompleted<int>(245)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(215, 3),
                OnNext(225, 5),
                OnNext(235, 7),
                OnNext(245, 9),
                OnCompleted<int>(250)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => Observable.Merge(scheduler, o1, o2)).ToArray();
            Assert.AreEqual(9, results.Length, "length");
            for (int i = 0; i < 8; i++)
            {
                Assert.IsTrue(results[i].Value.Kind == NotificationKind.OnNext && results[i].Time == (ushort)(210 + i * 5) && results[i].Value.Value == i + 2);
            }
            Assert.IsTrue(results[8].Value.Kind == NotificationKind.OnCompleted && results[8].Time == 250, "complete");
        }

        [TestMethod]
        public void Merge_Lots3()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(225, 5),
                OnNext(240, 8),
                OnCompleted<int>(245)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(215, 3),
                OnNext(230, 6),
                OnNext(245, 9),
                OnCompleted<int>(250)
            };

            var msgs3 = new[] {
                OnNext(150, 1),
                OnNext(220, 4),
                OnNext(235, 7),
                OnCompleted<int>(240)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);
            var o3 = scheduler.CreateHotObservable(msgs3);

            var results = scheduler.Run(() => new [] { o1, o2, o3 }.Merge(scheduler)).ToArray();
            Assert.AreEqual(9, results.Length, "length");
            for (int i = 0; i < 8; i++)
            {
                Assert.IsTrue(results[i].Value.Kind == NotificationKind.OnNext && results[i].Time == (ushort)(210 + i * 5) && results[i].Value.Value == i + 2);
            }
            Assert.IsTrue(results[8].Value.Kind == NotificationKind.OnCompleted && results[8].Time == 250, "complete");
        }

        [TestMethod]
        public void Merge_LotsMore()
        {
            var inputs = new List<List<Recorded<Notification<int>>>>();

            const int N = 10;
            for (int i = 0; i < N; i++)
            {
                var lst = new List<Recorded<Notification<int>>> { OnNext(150, 1) };
                inputs.Add(lst);

                ushort start = (ushort)(301 + i);
                for (int j = 0; j < i; j++)
                {
                    var onNext = OnNext(start += (ushort)(j * 5), j + i + 2);
                    lst.Add(onNext);
                }

                lst.Add(OnCompleted<int>((ushort)(start + N - i)));
            }

            var inputsFlat = inputs.Aggregate((l, r) => l.Concat(r).ToList()).ToArray();

            var resOnNext = (from n in inputsFlat
                             where n.Time >= 200
                             where n.Value.Kind == NotificationKind.OnNext
                             orderby n.Time
                             select n).ToList();

            var lastCompleted = (from n in inputsFlat
                                 where n.Time >= 200
                                 where n.Value.Kind == NotificationKind.OnCompleted
                                 orderby n.Time descending
                                 select n).First();

            var scheduler = new TestScheduler();

            // Last ToArray: got to create the hot observables *now*
            var xss = inputs.Select(lst => (IObservable<int>)scheduler.CreateHotObservable(lst.ToArray())).ToArray();

            var results = scheduler.Run(() => xss.Merge(scheduler)).ToArray();

            Assert.AreEqual(resOnNext.Count + 1, results.Length, "length");
            for (int i = 0; i < resOnNext.Count; i++)
            {
                Assert.IsTrue(results[i].Value.Kind == NotificationKind.OnNext && results[i].Time == resOnNext[i].Time && results[i].Value.Value == resOnNext[i].Value.Value);
            }
            Assert.IsTrue(results[resOnNext.Count].Value.Kind == NotificationKind.OnCompleted && results[resOnNext.Count].Time == lastCompleted.Time, "complete");
        }

        [TestMethod]
        public void Merge_ErrorLeft()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnError<int>(245, ex)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(215, 3),
                OnCompleted<int>(250)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => Observable.Merge(o1, o2, scheduler));
            results.AssertEqual(
                OnNext(210, 2),
                OnNext(215, 3),
                OnError<int>(245, ex)
            );
        }

        [TestMethod]
        public void Merge_ErrorCausesDisposal()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnError<int>(210, ex) //!
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(220, 1), // should not come
                OnCompleted<int>(230)
            };

            bool sourceNotDisposed = false;

            var e1 = scheduler.CreateHotObservable(msgs1);
            var o1 = scheduler.CreateHotObservable(msgs2).Do(_ => sourceNotDisposed = true);

            var results = scheduler.Run(() => Observable.Merge(e1, o1, scheduler));
            results.AssertEqual(
                OnError<int>(210, ex) //!
            );

            Assert.IsFalse(sourceNotDisposed);
        }

        [TestMethod]
        public void Merge_ObservableOfObservable_Data()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                    OnNext<IObservable<int>>(300, scheduler.CreateColdObservable(
                        OnNext(10, 101),
                        OnNext(20, 102),
                        OnNext(110, 103),
                        OnNext(120, 104),
                        OnNext(210, 105),
                        OnNext(220, 106),
                        OnCompleted<int>(230))),
                    OnNext<IObservable<int>>(400, scheduler.CreateColdObservable(
                        OnNext(10, 201),
                        OnNext(20, 202),
                        OnNext(30, 203),
                        OnNext(40, 204),
                        OnCompleted<int>(50))),
                    OnNext<IObservable<int>>(500, scheduler.CreateColdObservable(
                        OnNext(10, 301),
                        OnNext(20, 302),
                        OnNext(30, 303),
                        OnNext(40, 304),
                        OnNext(120, 305),
                        OnCompleted<int>(150))),
                    OnCompleted<IObservable<int>>(600)
                );

            var results = scheduler.Run(() => xs.Merge());

            results.AssertEqual(
                OnNext(310, 101),
                OnNext(320, 102),
                OnNext(410, 103),
                OnNext(410, 201),
                OnNext(420, 104),
                OnNext(420, 202),
                OnNext(430, 203),
                OnNext(440, 204),
                OnNext(510, 105),
                OnNext(510, 301),
                OnNext(520, 106),
                OnNext(520, 302),
                OnNext(530, 303),
                OnNext(540, 304),
                OnNext(620, 305),
                OnCompleted<int>(650)
                );
        }

        [TestMethod]
        public void Merge_ObservableOfObservable_Data_NonOverlapped()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                    OnNext<IObservable<int>>(300, scheduler.CreateColdObservable(
                        OnNext(10, 101),
                        OnNext(20, 102),
                        OnCompleted<int>(230))),
                    OnNext<IObservable<int>>(400, scheduler.CreateColdObservable(
                        OnNext(10, 201),
                        OnNext(20, 202),
                        OnNext(30, 203),
                        OnNext(40, 204),
                        OnCompleted<int>(50))),
                    OnNext<IObservable<int>>(500, scheduler.CreateColdObservable(
                        OnNext(10, 301),
                        OnNext(20, 302),
                        OnNext(30, 303),
                        OnNext(40, 304),
                        OnCompleted<int>(50))),
                    OnCompleted<IObservable<int>>(600)
                );

            var results = scheduler.Run(() => xs.Merge());

            results.AssertEqual(
                OnNext(310, 101),
                OnNext(320, 102),
                OnNext(410, 201),
                OnNext(420, 202),
                OnNext(430, 203),
                OnNext(440, 204),
                OnNext(510, 301),
                OnNext(520, 302),
                OnNext(530, 303),
                OnNext(540, 304),
                OnCompleted<int>(600)
                );
        }

        [TestMethod]
        public void Merge_ObservableOfObservable_InnerThrows()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var xs = scheduler.CreateHotObservable(
                    OnNext<IObservable<int>>(300, scheduler.CreateColdObservable(
                        OnNext(10, 101),
                        OnNext(20, 102),
                        OnNext(110, 103),
                        OnNext(120, 104),
                        OnNext(210, 105),
                        OnNext(220, 106),
                        OnCompleted<int>(230))),
                    OnNext<IObservable<int>>(400, scheduler.CreateColdObservable(
                        OnNext(10, 201),
                        OnNext(20, 202),
                        OnNext(30, 203),
                        OnNext(40, 204),
                        OnError<int>(50, ex))),
                    OnNext<IObservable<int>>(500, scheduler.CreateColdObservable(
                        OnNext(10, 301),
                        OnNext(20, 302),
                        OnNext(30, 303),
                        OnNext(40, 304),
                        OnCompleted<int>(150))),
                    OnCompleted<IObservable<int>>(600)
                );

            var results = scheduler.Run(() => xs.Merge());

            results.AssertEqual(
                OnNext(310, 101),
                OnNext(320, 102),
                OnNext(410, 103),
                OnNext(410, 201),
                OnNext(420, 104),
                OnNext(420, 202),
                OnNext(430, 203),
                OnNext(440, 204),
                OnError<int>(450, ex)
                );
        }

        [TestMethod]
        public void Merge_ObservableOfObservable_OuterThrows()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var xs = scheduler.CreateHotObservable(
                    OnNext<IObservable<int>>(300, scheduler.CreateColdObservable(
                        OnNext(10, 101),
                        OnNext(20, 102),
                        OnNext(110, 103),
                        OnNext(120, 104),
                        OnNext(210, 105),
                        OnNext(220, 106),
                        OnCompleted<int>(230))),
                    OnNext<IObservable<int>>(400, scheduler.CreateColdObservable(
                        OnNext(10, 201),
                        OnNext(20, 202),
                        OnNext(30, 203),
                        OnNext(40, 204),
                        OnCompleted<int>(50))),
                    OnError<IObservable<int>>(500, ex)
                );

            var results = scheduler.Run(() => xs.Merge());

            results.AssertEqual(
                OnNext(310, 101),
                OnNext(320, 102),
                OnNext(410, 103),
                OnNext(410, 201),
                OnNext(420, 104),
                OnNext(420, 202),
                OnNext(430, 203),
                OnNext(440, 204),
                OnError<int>(500, ex)
                );
        }

        [TestMethod]
        public void Merge_Binary_DefaultScheduler()
        {
            Assert.IsTrue(Observable.Return(1).Merge(Observable.Return(2)).ToEnumerable().OrderBy(x => x).SequenceEqual(new[] { 1, 2 }));
        }

        [TestMethod]
        public void Merge_Params_DefaultScheduler()
        {
            Assert.IsTrue(Observable.Merge(Observable.Return(1), Observable.Return(2)).ToEnumerable().OrderBy(x => x).SequenceEqual(new[] { 1, 2 }));
        }

        [TestMethod]
        public void Merge_IEnumerableOfIObservable_DefaultScheduler()
        {
            Assert.IsTrue(Observable.Merge((IEnumerable<IObservable<int>>)new[] { Observable.Return(1), Observable.Return(2) }).ToEnumerable().OrderBy(x => x).SequenceEqual(new[] { 1, 2 }));
        }

        [TestMethod]
        public void Switch_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Switch((IObservable<IObservable<int>>)null));
        }

        [TestMethod]
        public void Switch_Data()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                    OnNext<IObservable<int>>(300, scheduler.CreateColdObservable(
                        OnNext(10, 101),
                        OnNext(20, 102),
                        OnNext(110, 103),
                        OnNext(120, 104),
                        OnNext(210, 105),
                        OnNext(220, 106),
                        OnCompleted<int>(230))),
                    OnNext<IObservable<int>>(400, scheduler.CreateColdObservable(
                        OnNext(10, 201),
                        OnNext(20, 202),
                        OnNext(30, 203),
                        OnNext(40, 204),
                        OnCompleted<int>(50))),
                    OnNext<IObservable<int>>(500, scheduler.CreateColdObservable(
                        OnNext(10, 301),
                        OnNext(20, 302),
                        OnNext(30, 303),
                        OnNext(40, 304),
                        OnCompleted<int>(150))),
                    OnCompleted<IObservable<int>>(600)
                );

            var results = scheduler.Run(() => xs.Switch());

            results.AssertEqual(
                OnNext(310, 101),
                OnNext(320, 102),
                OnNext(410, 201),
                OnNext(420, 202),
                OnNext(430, 203),
                OnNext(440, 204),
                OnNext(510, 301),
                OnNext(520, 302),
                OnNext(530, 303),
                OnNext(540, 304),
                OnCompleted<int>(650)
                );
        }

        [TestMethod]
        public void Switch_InnerThrows()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var xs = scheduler.CreateHotObservable(
                    OnNext<IObservable<int>>(300, scheduler.CreateColdObservable(
                        OnNext(10, 101),
                        OnNext(20, 102),
                        OnNext(110, 103),
                        OnNext(120, 104),
                        OnNext(210, 105),
                        OnNext(220, 106),
                        OnCompleted<int>(230))),
                    OnNext<IObservable<int>>(400, scheduler.CreateColdObservable(
                        OnNext(10, 201),
                        OnNext(20, 202),
                        OnNext(30, 203),
                        OnNext(40, 204),
                        OnError<int>(50, ex))),
                    OnNext<IObservable<int>>(500, scheduler.CreateColdObservable(
                        OnNext(10, 301),
                        OnNext(20, 302),
                        OnNext(30, 303),
                        OnNext(40, 304),
                        OnCompleted<int>(150))),
                    OnCompleted<IObservable<int>>(600)
                );

            var results = scheduler.Run(() => xs.Switch());

            results.AssertEqual(
                OnNext(310, 101),
                OnNext(320, 102),
                OnNext(410, 201),
                OnNext(420, 202),
                OnNext(430, 203),
                OnNext(440, 204),
                OnError<int>(450, ex)
                );
        }

        [TestMethod]
        public void Switch_OuterThrows()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var xs = scheduler.CreateHotObservable(
                    OnNext<IObservable<int>>(300, scheduler.CreateColdObservable(
                        OnNext(10, 101),
                        OnNext(20, 102),
                        OnNext(110, 103),
                        OnNext(120, 104),
                        OnNext(210, 105),
                        OnNext(220, 106),
                        OnCompleted<int>(230))),
                    OnNext<IObservable<int>>(400, scheduler.CreateColdObservable(
                        OnNext(10, 201),
                        OnNext(20, 202),
                        OnNext(30, 203),
                        OnNext(40, 204),
                        OnCompleted<int>(50))),
                    OnError<IObservable<int>>(500, ex)
                );

            var results = scheduler.Run(() => xs.Switch());

            results.AssertEqual(
                OnNext(310, 101),
                OnNext(320, 102),
                OnNext(410, 201),
                OnNext(420, 202),
                OnNext(430, 203),
                OnNext(440, 204),
                OnError<int>(500, ex)
                );
        }

        [TestMethod]
        public void Switch_NoInner()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                    OnCompleted<IObservable<int>>(500)
                );

            var results = scheduler.Run(() => xs.Switch());

            results.AssertEqual(
                OnCompleted<int>(500)
                );
        }

        [TestMethod]
        public void Switch_InnerCompletes()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                    OnNext<IObservable<int>>(300, scheduler.CreateColdObservable(
                        OnNext(10, 101),
                        OnNext(20, 102),
                        OnNext(110, 103),
                        OnNext(120, 104),
                        OnNext(210, 105),
                        OnNext(220, 106),
                        OnCompleted<int>(230))),
                    OnCompleted<IObservable<int>>(540)
                );

            var results = scheduler.Run(() => xs.Switch());

            results.AssertEqual(
                OnNext(310, 101),
                OnNext(320, 102),
                OnNext(410, 103),
                OnNext(420, 104),
                OnNext(510, 105),
                OnNext(520, 106),
                OnCompleted<int>(540)
                );
        }

        [TestMethod]
        public void Amb_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.Amb((IObservable<int>[])null));
            Throws<ArgumentNullException>(() => Observable.Amb((IEnumerable<IObservable<int>>)null));
            Throws<ArgumentNullException>(() => Observable.Amb(null, someObservable));
            Throws<ArgumentNullException>(() => Observable.Amb(someObservable, null));
        }

        [TestMethod]
        public void Amb_Never2()
        {
            var scheduler = new TestScheduler();

            var l = Observable.Never<int>();
            var r = Observable.Never<int>();

            var results = scheduler.Run(() => l.Amb(r));
            results.AssertEqual();
        }

        [TestMethod]
        public void Amb_Never3()
        {
            var scheduler = new TestScheduler();

            var n1 = Observable.Never<int>();
            var n2 = Observable.Never<int>();
            var n3 = Observable.Never<int>();

            var results = scheduler.Run(() => new[] { n1, n2, n3 }.Amb());
            results.AssertEqual();
        }

        [TestMethod]
        public void Amb_Never3_Params()
        {
            var scheduler = new TestScheduler();

            var n1 = Observable.Never<int>();
            var n2 = Observable.Never<int>();
            var n3 = Observable.Never<int>();

            var results = scheduler.Run(() => Observable.Amb(n1, n2, n3));
            results.AssertEqual();
        }

        [TestMethod]
        public void Amb_NeverEmpty()
        {
            var scheduler = new TestScheduler();

            var rMsgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(225)
            };

            var n = Observable.Never<int>();
            var e = scheduler.CreateHotObservable(rMsgs);

            var results = scheduler.Run(() => n.Amb(e));
            results.AssertEqual(
                OnCompleted<int>(225)
            );
        }

        [TestMethod]
        public void Amb_EmptyNever()
        {
            var scheduler = new TestScheduler();

            var lMsgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(225)
            };

            var n = Observable.Never<int>();
            var e = scheduler.CreateHotObservable(lMsgs);

            var results = scheduler.Run(() => e.Amb(n));
            results.AssertEqual(
                OnCompleted<int>(225)
            );
        }

        [TestMethod]
        public void Amb_RegularShouldDisposeLoser()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(240)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(220, 3),
                OnCompleted<int>(250)
            };

            bool sourceNotDisposed = false;

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2).Do(_ => sourceNotDisposed = true);

            var results = scheduler.Run(() => o1.Amb(o2));
            results.AssertEqual(
                OnNext(210, 2),
                OnCompleted<int>(240)
            );

            Assert.IsFalse(sourceNotDisposed);
        }

        [TestMethod]
        public void Amb_WinnerThrows()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnError<int>(220, ex)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(220, 3),
                OnCompleted<int>(250)
            };

            bool sourceNotDisposed = false;

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2).Do(_ => sourceNotDisposed = true);

            var results = scheduler.Run(() => o1.Amb(o2));
            results.AssertEqual(
                OnNext(210, 2),
                OnError<int>(220, ex)
            );

            Assert.IsFalse(sourceNotDisposed);
        }

        [TestMethod]
        public void Amb_LoserThrows()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(220, 2),
                OnError<int>(230, ex)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(210, 3),
                OnCompleted<int>(250)
            };

            bool sourceNotDisposed = false;

            var o1 = scheduler.CreateHotObservable(msgs1).Do(_ => sourceNotDisposed = true);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.Amb(o2));
            results.AssertEqual(
                OnNext(210, 3),
                OnCompleted<int>(250)
            );

            Assert.IsFalse(sourceNotDisposed);
        }

        [TestMethod]
        public void Amb_ThrowsBeforeElection()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnError<int>(210, ex)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(220, 3),
                OnCompleted<int>(250)
            };

            bool sourceNotDisposed = false;

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2).Do(_ => sourceNotDisposed = true);

            var results = scheduler.Run(() => o1.Amb(o2));
            results.AssertEqual(
                OnError<int>(210, ex)
            );

            Assert.IsFalse(sourceNotDisposed);
        }

        [TestMethod]
        public void Catch_ArgumentChecking()
        {
            var scheduler = new TestScheduler();
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.Catch<int>((IObservable<int>[])null));
            Throws<ArgumentNullException>(() => Observable.Catch<int>((IEnumerable<IObservable<int>>)null));
            Throws<ArgumentNullException>(() => Observable.Catch<int>((IEnumerable<IObservable<int>>)null, scheduler));
            Throws<ArgumentNullException>(() => Observable.Catch<int>(new IObservable<int>[0], null));
            Throws<ArgumentNullException>(() => Observable.Catch<int>(someObservable, null));
            Throws<ArgumentNullException>(() => Observable.Catch<int>((IObservable<int>)null, someObservable));
            Throws<ArgumentNullException>(() => Observable.Catch<int>(someObservable, null, scheduler));
            Throws<ArgumentNullException>(() => Observable.Catch<int>(someObservable, someObservable, (IScheduler)null));
            Throws<ArgumentNullException>(() => Observable.Catch<int>(null, someObservable, scheduler));
            Throws<ArgumentNullException>(() => Observable.Catch<int>((IScheduler)null, new IObservable<int>[0]));
            Throws<ArgumentNullException>(() => Observable.Catch<int>(scheduler, (IObservable<int>[])null));
            Throws<ArgumentNullException>(() => Observable.Catch<int, Exception>(null, _ => someObservable));
            Throws<ArgumentNullException>(() => Observable.Catch<int, Exception>(someObservable, null));
        }

        [TestMethod]
        public void Catch_NoErrors()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(240, 4),
                OnCompleted<int>(250)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.Catch(o2, scheduler));
            results.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 3),
                OnCompleted<int>(230)
            );
        }

        [TestMethod]
        public void Catch_Never()
        {
            var scheduler = new TestScheduler();

            var msgs2 = new[] {
                OnNext(240, 4),
                OnCompleted<int>(250)
            };

            var o1 = Observable.Never<int>();
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.Catch(o2, scheduler));
            results.AssertEqual();
        }

        [TestMethod]
        public void Catch_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(240, 4),
                OnCompleted<int>(250)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.Catch(o2, scheduler));
            results.AssertEqual(
                OnCompleted<int>(230)
            );
        }

        [TestMethod]
        public void Catch_Return()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(240, 4),
                OnCompleted<int>(250)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.Catch(o2, scheduler));
            results.AssertEqual(
                OnNext(210, 2),
                OnCompleted<int>(230)
            );
        }

        [TestMethod]
        public void Catch_Error()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnError<int>(230, ex)
            };

            var msgs2 = new[] {
                OnNext(240, 4),
                OnCompleted<int>(250)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.Catch(o2, scheduler));
            results.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(240, 4),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void Catch_Error_Never()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnError<int>(230, ex)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = Observable.Never<int>();

            var results = scheduler.Run(() => o1.Catch(o2, scheduler));
            results.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 3)
            );
        }

        [TestMethod]
        public void Catch_Error_Error()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnError<int>(230, new Exception())
            };

            var ex = new Exception();

            var msgs2 = new[] {
                OnNext(240, 4),
                OnError<int>(250, ex)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.Catch(o2, scheduler));
            results.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(240, 4),
                OnError<int>(251, ex)
            );
        }

        [TestMethod]
        public void Catch_Multiple()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnError<int>(215, ex)
            };

            var msgs2 = new[] {
                OnNext(220, 3),
                OnError<int>(225, ex)
            };

            var msgs3 = new[] {
                OnNext(230, 4),
                OnCompleted<int>(235)
            };

            var msgs4 = new[] {
                OnNext(240, 5),
                OnCompleted<int>(245)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);
            var o3 = scheduler.CreateHotObservable(msgs3);

            var results = scheduler.Run(() => Observable.Catch(scheduler, o1, o2, o3));
            results.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnCompleted<int>(235)
            );
        }

        [TestMethod]
        public void Catch_ErrorSpecific_Caught()
        {
            var scheduler = new TestScheduler();

            var ex = new ArgumentException("x");

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnError<int>(230, ex)
            };

            var msgs2 = new[] {
                OnNext(240, 4),
                OnCompleted<int>(250)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            bool handlerCalled = false;

            var results = scheduler.Run(() => o1.Catch((ArgumentException ex_) => { handlerCalled = true; return o2; }));

            Assert.IsTrue(handlerCalled, "handler called");

            results.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(240, 4),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void Catch_ErrorSpecific_Uncaught()
        {
            var scheduler = new TestScheduler();

            var ex = new InvalidOperationException("x");

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnError<int>(230, ex)
            };

            var msgs2 = new[] {
                OnNext(240, 4),
                OnCompleted<int>(250)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            bool handlerCalled = false;

            var results = scheduler.Run(() => o1.Catch((ArgumentException ex_) => { handlerCalled = true; return o2; }));

            Assert.IsFalse(handlerCalled, "handler called");

            results.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 3),
                OnError<int>(230, ex)
            );
        }

        [TestMethod]
        public void Catch_HandlerThrows()
        {
            var scheduler = new TestScheduler();

            var ex = new ArgumentException("x");

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnError<int>(230, ex)
            };

            var msgs2 = new[] {
                OnNext(240, 4),
                OnCompleted<int>(250)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            bool handlerCalled = false;

            var ex2 = new Exception();
            var results = scheduler.Run(() => o1.Catch((ArgumentException ex_) => { handlerCalled = true; throw ex2; }));

            Assert.IsTrue(handlerCalled, "handler called");

            results.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 3),
                OnError<int>(230, ex2)
            );
        }

        [TestMethod]
        public void Catch_Nested_OuterCatches()
        {
            var scheduler = new TestScheduler();

            var ex = new ArgumentException("x");

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnError<int>(215, ex)
            };

            var msgs2 = new[] {
                OnNext(220, 3),
                OnCompleted<int>(225)
            };

            var msgs3 = new[] {
                OnNext(220, 4), //!
                OnCompleted<int>(225)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);
            var o3 = scheduler.CreateHotObservable(msgs3);

            bool firstHandlerCalled = false;
            bool secondHandlerCalled = false;

            var results = scheduler.Run(() =>
                o1
                .Catch((InvalidOperationException ex_) => { firstHandlerCalled = true; return o2; })
                .Catch((ArgumentException ex_) => { secondHandlerCalled = true; return o3; })
            );

            Assert.IsFalse(firstHandlerCalled, "first handler called");
            Assert.IsTrue(secondHandlerCalled, "second handler called");

            results.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 4),
                OnCompleted<int>(225)
            );
        }

        [TestMethod]
        public void Catch_Nested_InnerCatches()
        {
            var scheduler = new TestScheduler();

            var ex = new ArgumentException("x");

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnError<int>(215, ex)
            };

            var msgs2 = new[] {
                OnNext(220, 3), //!
                OnCompleted<int>(225)
            };

            var msgs3 = new[] {
                OnNext(220, 4),
                OnCompleted<int>(225)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);
            var o3 = scheduler.CreateHotObservable(msgs3);

            bool firstHandlerCalled = false;
            bool secondHandlerCalled = false;

            var results = scheduler.Run(() =>
                o1
                .Catch((ArgumentException ex_) => { firstHandlerCalled = true; return o2; })
                .Catch((InvalidOperationException ex_) => { secondHandlerCalled = true; return o3; })
            );

            Assert.IsTrue(firstHandlerCalled, "first handler called");
            Assert.IsFalse(secondHandlerCalled, "second handler called");

            results.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 3),
                OnCompleted<int>(225)
            );
        }

        [TestMethod]
        public void Catch_ThrowFromNestedCatch()
        {
            var scheduler = new TestScheduler();

            var ex1 = new ArgumentException("x1");
            var ex2 = new ArgumentException("x2");

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnError<int>(215, ex1)
            };

            var msgs2 = new[] {
                OnNext(220, 3), //!
                OnError<int>(225, ex2)
            };

            var msgs3 = new[] {
                OnNext(230, 4),
                OnCompleted<int>(235)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);
            var o3 = scheduler.CreateHotObservable(msgs3);

            bool firstHandlerCalled = false;
            bool secondHandlerCalled = false;

            var results = scheduler.Run(() =>
                o1
                .Catch((ArgumentException ex_) => { firstHandlerCalled = true; Assert.IsTrue(ex1 == ex_, "Expected ex1"); return o2; })
                .Catch((ArgumentException ex_) => { secondHandlerCalled = true; Assert.IsTrue(ex2 == ex_, "Expected ex2"); return o3; })
            );

            Assert.IsTrue(firstHandlerCalled, "first handler called");
            Assert.IsTrue(secondHandlerCalled, "second handler called");

            results.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnCompleted<int>(235)
            );
        }

        [TestMethod]
        public void Catch_DefaultScheduler_Binary()
        {
            var evt = new ManualResetEvent(false);

            int res = 0;
            Observable.Return(1).Catch(Observable.Return(2)).Subscribe(x =>
            {
                res = x;
                evt.Set();
            });

            evt.WaitOne();
            Assert.AreEqual(1, res);
        }

        [TestMethod]
        public void Catch_DefaultScheduler_Nary()
        {
            var evt = new ManualResetEvent(false);

            int res = 0;
            Observable.Catch(Observable.Return(1), Observable.Return(2), Observable.Return(3)).Subscribe(x =>
            {
                res = x;
                evt.Set();
            });

            evt.WaitOne();
            Assert.AreEqual(1, res);
        }

        [TestMethod]
        public void Catch_DefaultScheduler_NaryEnumerable()
        {
            var evt = new ManualResetEvent(false);

            IEnumerable<IObservable<int>> sources = new[] { Observable.Return(1), Observable.Return(2), Observable.Return(3) };

            int res = 0;
            Observable.Catch(sources).Subscribe(x =>
            {
                res = x;
                evt.Set();
            });

            evt.WaitOne();
            Assert.AreEqual(1, res);
        }

        [TestMethod]
        public void Catch_IteratorThrows()
        {
            var scheduler = new TestScheduler();
            var ex = new Exception();

            var res = scheduler.Run(() => Observable.Catch<int>(Catch_IteratorThrows_Source(ex, true), scheduler)).ToArray();
            res.AssertEqual(
                OnError<int>(201, ex)
            );
        }

        private IEnumerable<IObservable<int>> Catch_IteratorThrows_Source(Exception ex, bool b)
        {
            if (b)
                throw ex;
            else
                yield break;
        }

        [TestMethod]
        public void Catch_EmptyIterator()
        {
            var scheduler = new TestScheduler();

            var res = scheduler.Run(() => Observable.Catch<int>((IEnumerable<IObservable<int>>)new IObservable<int>[0], scheduler)).ToArray();
            res.AssertEqual(
                OnCompleted<int>(201)
            );
        }

        [TestMethod]
        public void OnErrorResumeNext_ArgumentChecking()
        {
            var scheduler = new TestScheduler();
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.OnErrorResumeNext<int>((IObservable<int>[])null));
            Throws<ArgumentNullException>(() => Observable.OnErrorResumeNext<int>((IEnumerable<IObservable<int>>)null));
            Throws<ArgumentNullException>(() => Observable.OnErrorResumeNext<int>((IObservable<int>)null, someObservable));
            Throws<ArgumentNullException>(() => Observable.OnErrorResumeNext<int>(someObservable, (IObservable<int>)null));
            Throws<ArgumentNullException>(() => Observable.OnErrorResumeNext<int>(null, someObservable, scheduler));
            Throws<ArgumentNullException>(() => Observable.OnErrorResumeNext<int>(someObservable, null, scheduler));
            Throws<ArgumentNullException>(() => Observable.OnErrorResumeNext<int>(someObservable, someObservable, (IScheduler)null));
            Throws<ArgumentNullException>(() => Observable.OnErrorResumeNext<int>(scheduler, (IObservable<int>[])null));
            Throws<ArgumentNullException>(() => Observable.OnErrorResumeNext<int>((IScheduler)null, new IObservable<int>[0]));
            Throws<ArgumentNullException>(() => Observable.OnErrorResumeNext<int>((IEnumerable<IObservable<int>>)null, scheduler));
            Throws<ArgumentNullException>(() => Observable.OnErrorResumeNext<int>(new IObservable<int>[0], null));
        }

        [TestMethod]
        public void OnErrorResumeNext_NoErrors()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(240, 4),
                OnCompleted<int>(250)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.OnErrorResumeNext(o2, scheduler));
            results.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(240, 4),
                OnCompleted<int>(251)
            );
        }

        [TestMethod]
        public void OnErrorResumeNext_Error()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnError<int>(230, new Exception())
            };

            var msgs2 = new[] {
                OnNext(240, 4),
                OnCompleted<int>(250)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.OnErrorResumeNext(o2, scheduler));
            results.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(240, 4),
                OnCompleted<int>(251)
            );
        }

        [TestMethod]
        public void OnErrorResumeNext_ErrorMultiple()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnError<int>(220, new Exception())
            };

            var msgs2 = new[] {
                OnNext(230, 3),
                OnError<int>(240, new Exception())
            };

            var msgs3 = new[] {
                OnCompleted<int>(250)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);
            var o3 = scheduler.CreateHotObservable(msgs3);

            var results = scheduler.Run(() => Observable.OnErrorResumeNext(scheduler, o1, o2, o3));
            results.AssertEqual(
                OnNext(210, 2),
                OnNext(230, 3),
                OnCompleted<int>(251)
            );
        }

        [TestMethod]
        public void OnErrorResumeNext_EmptyReturnThrowAndMore()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(205)
            };

            var msgs2 = new[] {
                OnNext(215, 2),
                OnCompleted<int>(220)
            };

            var msgs3 = new[] {
                OnNext(225, 3),
                OnNext(230, 4),
                OnCompleted<int>(235)
            };

            var msgs4 = new[] {
                OnError<int>(240, new Exception()),
            };

            var msgs5 = new[] {
                OnNext<int>(245, 5),
                OnCompleted<int>(250)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);
            var o3 = scheduler.CreateHotObservable(msgs3);
            var o4 = scheduler.CreateHotObservable(msgs4);
            var o5 = scheduler.CreateHotObservable(msgs5);

            var results = scheduler.Run(() => new[] { o1, o2, o3, o4, o5 }.OnErrorResumeNext(scheduler));
            results.AssertEqual(
                OnNext(215, 2),
                OnNext(225, 3),
                OnNext(230, 4),
                OnNext(245, 5),
                OnCompleted<int>(251)
            );
        }

        [TestMethod]
        public void OnErrorResumeNext_LastThrows()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(220)
            };

            var msgs2 = new[] {
                OnError<int>(230, ex)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.OnErrorResumeNext(o2, scheduler));
            results.AssertEqual(
                OnNext(210, 2),
                OnCompleted<int>(231)
            );
        }

        [TestMethod]
        public void OnErrorResumeNext_SingleSourceThrows()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnError<int>(230, ex)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);

            var results = scheduler.Run(() => Observable.OnErrorResumeNext(scheduler, o1));
            results.AssertEqual(
                OnCompleted<int>(231)
            );
        }

        [TestMethod]
        public void OnErrorResumeNext_EndWithNever()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(220)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = Observable.Never<int>();

            var results = scheduler.Run(() => Observable.OnErrorResumeNext(scheduler, o1, o2));
            results.AssertEqual(
                OnNext(210, 2)
            );
        }

        [TestMethod]
        public void OnErrorResumeNext_StartWithNever()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(220)
            };

            var o1 = Observable.Never<int>();
            var o2 = scheduler.CreateHotObservable(msgs1);

            var results = scheduler.Run(() => Observable.OnErrorResumeNext(scheduler, o1, o2));
            results.AssertEqual();
        }

        [TestMethod]
        public void OnErrorResumeNext_DefaultScheduler_Binary()
        {
            var evt = new ManualResetEvent(false);

            int sum = 0;
            Observable.Return(1).OnErrorResumeNext(Observable.Return(2)).Subscribe(x =>
            {
                sum += x;
            }, () => evt.Set());

            evt.WaitOne();
            Assert.AreEqual(3, sum);
        }

        [TestMethod]
        public void OnErrorResumeNext_DefaultScheduler_Nary()
        {
            var evt = new ManualResetEvent(false);

            int sum = 0;
            Observable.OnErrorResumeNext(Observable.Return(1), Observable.Return(2), Observable.Return(3)).Subscribe(x =>
            {
                sum += x;
            }, () => evt.Set());

            evt.WaitOne();
            Assert.AreEqual(6, sum);
        }

        [TestMethod]
        public void OnErrorResumeNext_DefaultScheduler_NaryEnumerable()
        {
            var evt = new ManualResetEvent(false);

            IEnumerable<IObservable<int>> sources = new[] { Observable.Return(1), Observable.Return(2), Observable.Return(3) };

            int sum = 0;
            Observable.OnErrorResumeNext(sources).Subscribe(x =>
            {
                sum += x;
            }, () => evt.Set());

            evt.WaitOne();
            Assert.AreEqual(6, sum);
        }

        [TestMethod]
        public void OnErrorResumeNext_IteratorThrows()
        {
            var scheduler = new TestScheduler();
            var ex = new Exception();

            var res = scheduler.Run(() => Observable.OnErrorResumeNext<int>(Catch_IteratorThrows_Source(ex, true), scheduler)).ToArray();
            res.AssertEqual(
                OnError<int>(201, ex)
            );
        }

        [TestMethod]
        public void Zip_ArgumentChecking()
        {
            var scheduler = new TestScheduler();
            var someObservable = Observable.Empty<int>();
            var someEnumerable = Enumerable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.Zip<int, int, int>(someObservable, someObservable, null));
            Throws<ArgumentNullException>(() => Observable.Zip<int, int, int>(null, someObservable, (_, __) => 0));
            Throws<ArgumentNullException>(() => Observable.Zip<int, int, int>(someObservable, default(IObservable<int>), (_, __) => 0));
            Throws<ArgumentNullException>(() => Observable.Zip<int, int, int>(someObservable, someEnumerable, null));
            Throws<ArgumentNullException>(() => Observable.Zip<int, int, int>(null, someEnumerable, (_, __) => 0));
            Throws<ArgumentNullException>(() => Observable.Zip<int, int, int>(someObservable, default(IEnumerable<int>), (_, __) => 0));
        }

        [TestMethod]
        public void Zip_NeverNever()
        {
            var scheduler = new TestScheduler();

            var n1 = Observable.Never<int>();
            var n2 = Observable.Never<int>();

            var results = scheduler.Run(() => n1.Zip(n2, (x, y) => x + y));
            results.AssertEqual();
        }

        [TestMethod]
        public void Zip_NeverEmpty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(210)
            };

            var n = Observable.Never<int>();
            var e = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => n.Zip(e, (x, y) => x + y));
            results.AssertEqual();
        }

        [TestMethod]
        public void Zip_EmptyNever()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(210)
            };

            var e = scheduler.CreateHotObservable(msgs);
            var n = Observable.Never<int>();

            var results = scheduler.Run(() => e.Zip(n, (x, y) => x + y));
            results.AssertEqual();
        }

        [TestMethod]
        public void Zip_EmptyEmpty()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(210)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(210)
            };

            var e1 = scheduler.CreateHotObservable(msgs1);
            var e2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e1.Zip(e2, (x, y) => x + y));
            results.AssertEqual(
                OnCompleted<int>(210)
            );
        }

        [TestMethod]
        public void Zip_EmptyNonEmpty()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(210)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(215, 2), // Intended behavior - will only know here there was no error and we can complete gracefully
                OnCompleted<int>(220)
            };

            var e = scheduler.CreateHotObservable(msgs1);
            var o = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e.Zip(o, (x, y) => x + y));
            results.AssertEqual(
                OnCompleted<int>(215)
            );
        }

        [TestMethod]
        public void Zip_NonEmptyEmpty()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(210)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnCompleted<int>(220)
            };

            var e = scheduler.CreateHotObservable(msgs1);
            var o = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o.Zip(e, (x, y) => x + y));
            results.AssertEqual(
                OnCompleted<int>(215)
            );
        }

        [TestMethod]
        public void Zip_NeverNonEmpty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnCompleted<int>(220)
            };

            var o = scheduler.CreateHotObservable(msgs);
            var n = Observable.Never<int>();

            var results = scheduler.Run(() => n.Zip(o, (x, y) => x + y));
            results.AssertEqual();
        }

        [TestMethod]
        public void Zip_NonEmptyNever()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnCompleted<int>(220)
            };

            var o = scheduler.CreateHotObservable(msgs);
            var n = Observable.Never<int>();

            var results = scheduler.Run(() => o.Zip(n, (x, y) => x + y));
            results.AssertEqual();
        }

        [TestMethod]
        public void Zip_NonEmptyNonEmpty()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(220, 3),
                OnCompleted<int>(240) // Intended behavior - will only know here there was no error and we can complete gracefully
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.Zip(o2, (x, y) => x + y));
            results.AssertEqual(
                OnNext(220, 2 + 3),
                OnCompleted<int>(240)
            );
        }

        [TestMethod]
        public void Zip_EmptyError()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(220, ex),
            };

            var e = scheduler.CreateHotObservable(msgs1);
            var f = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e.Zip(f, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex)
            );
        }

        [TestMethod]
        public void Zip_ErrorEmpty()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(220, ex),
            };

            var e = scheduler.CreateHotObservable(msgs1);
            var f = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => f.Zip(e, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex)
            );
        }

        [TestMethod]
        public void Zip_NeverError()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(220, ex),
            };

            var n = Observable.Never<int>();
            var f = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => n.Zip(f, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex)
            );
        }

        [TestMethod]
        public void Zip_ErrorNever()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(220, ex),
            };

            var n = Observable.Never<int>();
            var f = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => f.Zip(n, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex)
            );
        }

        [TestMethod]
        public void Zip_ErrorError()
        {
            var scheduler = new TestScheduler();

            var ex1 = new Exception();
            var ex2 = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnError<int>(230, ex1),
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(220, ex2),
            };

            var f1 = scheduler.CreateHotObservable(msgs1);
            var f2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => f1.Zip(f2, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex2)
            );
        }

        [TestMethod]
        public void Zip_SomeError()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(220, ex),
            };

            var o = scheduler.CreateHotObservable(msgs1);
            var e = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o.Zip(e, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex)
            );
        }

        [TestMethod]
        public void Zip_ErrorSome()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(220, ex),
            };

            var o = scheduler.CreateHotObservable(msgs1);
            var e = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e.Zip(o, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex)
            );
        }

        [TestMethod]
        public void Zip_SomeDataAsymmetric1()
        {
            var scheduler = new TestScheduler();

            var msgs1 = Enumerable.Range(0, 5).Select((x, i) => OnNext((ushort)(205 + i * 5), x)).ToArray();
            var msgs2 = Enumerable.Range(0, 10).Select((x, i) => OnNext((ushort)(202 + i * 8), x)).ToArray();

            int len = Math.Min(msgs1.Length, msgs2.Length);

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.Zip(o2, (x, y) => x + y)).ToArray();
            Assert.AreEqual(len, results.Length, "length");
            for (int i = 0; i < len; i++)
            {
                var sum = msgs1[i].Value.Value + msgs2[i].Value.Value;
                var time = Math.Max(msgs1[i].Time, msgs2[i].Time);
                Assert.IsTrue(results[i].Value.Kind == NotificationKind.OnNext && results[i].Time == time && results[i].Value.Value == sum, i.ToString());
            }
        }

        [TestMethod]
        public void Zip_SomeDataAsymmetric2()
        {
            var scheduler = new TestScheduler();

            var msgs1 = Enumerable.Range(0, 10).Select((x, i) => OnNext((ushort)(205 + i * 5), x)).ToArray();
            var msgs2 = Enumerable.Range(0, 5).Select((x, i) => OnNext((ushort)(202 + i * 8), x)).ToArray();

            int len = Math.Min(msgs1.Length, msgs2.Length);

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.Zip(o2, (x, y) => x + y)).ToArray();
            Assert.AreEqual(len, results.Length, "length");
            for (int i = 0; i < len; i++)
            {
                var sum = msgs1[i].Value.Value + msgs2[i].Value.Value;
                var time = Math.Max(msgs1[i].Time, msgs2[i].Time);
                Assert.IsTrue(results[i].Value.Kind == NotificationKind.OnNext && results[i].Time == time && results[i].Value.Value == sum, i.ToString());
            }
        }

        [TestMethod]
        public void Zip_SomeDataSymmetric()
        {
            var scheduler = new TestScheduler();

            var msgs1 = Enumerable.Range(0, 10).Select((x, i) => OnNext((ushort)(205 + i * 5), x)).ToArray();
            var msgs2 = Enumerable.Range(0, 10).Select((x, i) => OnNext((ushort)(202 + i * 8), x)).ToArray();

            int len = Math.Min(msgs1.Length, msgs2.Length);

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.Zip(o2, (x, y) => x + y)).ToArray();
            Assert.AreEqual(len, results.Length, "length");
            for (int i = 0; i < len; i++)
            {
                var sum = msgs1[i].Value.Value + msgs2[i].Value.Value;
                var time = Math.Max(msgs1[i].Time, msgs2[i].Time);
                Assert.IsTrue(results[i].Value.Kind == NotificationKind.OnNext && results[i].Time == time && results[i].Value.Value == sum, i.ToString());
            }
        }

        [TestMethod]
        public void Zip_SelectorThrows()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnNext(225, 4),
                OnCompleted<int>(240)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(220, 3),
                OnNext(230, 5), //!
                OnCompleted<int>(250)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var ex = new Exception("Bang");

            var results = scheduler.Run(() => o1.Zip(o2, (x, y) =>
            {
                if (y == 5)
                    throw ex;
                return x + y;
            }));
            results.AssertEqual(
                OnNext(220, 2 + 3),
                OnError<int>(230, ex)
            );
        }

        [TestMethod]
        public void ZipWithEnumerable_NeverNever()
        {
            var evt = new ManualResetEvent(false);
            var scheduler = new TestScheduler();

            var n1 = Observable.Never<int>();
            var n2 = EnumerableNever(evt);

            var results = scheduler.Run(() => n1.Zip(n2, (x, y) => x + y));
            results.AssertEqual();
            evt.Set();
        }

        private IEnumerable<int> EnumerableNever(ManualResetEvent evt)
        {
            evt.WaitOne();
            yield break;
        }

        [TestMethod]
        public void ZipWithEnumerable_NeverEmpty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(210)
            };

            var n = Observable.Never<int>();
            var e = Enumerable.Empty<int>();

            var results = scheduler.Run(() => n.Zip(e, (x, y) => x + y));
            results.AssertEqual();
        }

        [TestMethod]
        public void ZipWithEnumerable_EmptyNever()
        {
            var evt = new ManualResetEvent(false);

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(210)
            };

            var e = scheduler.CreateHotObservable(msgs);
            var n = EnumerableNever(evt);

            var results = scheduler.Run(() => e.Zip(n, (x, y) => x + y));
            results.AssertEqual(
                OnCompleted<int>(210)
            );
            evt.Set();
        }

        [TestMethod]
        public void ZipWithEnumerable_EmptyEmpty()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(210)
            };

            var e1 = scheduler.CreateHotObservable(msgs1);
            var e2 = Enumerable.Empty<int>();

            var results = scheduler.Run(() => e1.Zip(e2, (x, y) => x + y));
            results.AssertEqual(
                OnCompleted<int>(210)
            );
        }

        [TestMethod]
        public void ZipWithEnumerable_EmptyNonEmpty()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(210)
            };

            var e = scheduler.CreateHotObservable(msgs1);
            var o = new[] { 2 };

            var results = scheduler.Run(() => e.Zip(o, (x, y) => x + y));
            results.AssertEqual(
                OnCompleted<int>(210)
            );
        }

        [TestMethod]
        public void ZipWithEnumerable_NonEmptyEmpty()
        {
            var scheduler = new TestScheduler();

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnCompleted<int>(220)
            };

            var e = Enumerable.Empty<int>();
            var o = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o.Zip(e, (x, y) => x + y));
            results.AssertEqual(
                OnCompleted<int>(215)
            );
        }

        [TestMethod]
        public void ZipWithEnumerable_NeverNonEmpty()
        {
            var scheduler = new TestScheduler();

            var o = new[] { 2 };
            var n = Observable.Never<int>();

            var results = scheduler.Run(() => n.Zip(o, (x, y) => x + y));
            results.AssertEqual();
        }

        /*
        [TestMethod]
        public void ZipWithEnumerable_NonEmptyNever()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnCompleted<int>(220)
            };

            var o = scheduler.CreateHotObservable(msgs);
            var n = Observable.Never<int>().ToEnumerable();

            var results = scheduler.Run(() => o.Zip(n, (x, y) => x + y));
            results.AssertEqual();
        }
        */
        [TestMethod]
        public void ZipWithEnumerable_NonEmptyNonEmpty()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnCompleted<int>(230)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = new[] { 3 };

            var results = scheduler.Run(() => o1.Zip(o2, (x, y) => x + y));
            results.AssertEqual(
                OnNext(215, 2 + 3),
                OnCompleted<int>(230)
            );
        }

        [TestMethod]
        public void ZipWithEnumerable_EmptyError()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(230)
            };

            var e = scheduler.CreateHotObservable(msgs1);
            var f = ThrowEnumerable(false, ex);

            var results = scheduler.Run(() => e.Zip(f, (x, y) => x + y));
            results.AssertEqual(
                OnCompleted<int>(230)
            );
        }

        private IEnumerable<int> ThrowEnumerable(bool b, Exception ex)
        {
            if (!b)
                throw ex;
            yield break;
        }

        [TestMethod]
        public void ZipWithEnumerable_ErrorEmpty()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(220, ex),
            };

            var e = Enumerable.Empty<int>();
            var f = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => f.Zip(e, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex)
            );
        }

        [TestMethod]
        public void ZipWithEnumerable_NeverError()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var n = Observable.Never<int>();
            var f = ThrowEnumerable(false, ex);

            var results = scheduler.Run(() => n.Zip(f, (x, y) => x + y));
            results.AssertEqual(
            );
        }

        [TestMethod]
        public void ZipWithEnumerable_ErrorNever()
        {
            var evt = new ManualResetEvent(false);

            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(220, ex),
            };

            var n = EnumerableNever(evt);
            var f = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => f.Zip(n, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex)
            );
            evt.Set();
        }

        [TestMethod]
        public void ZipWithEnumerable_ErrorError()
        {
            var scheduler = new TestScheduler();

            var ex1 = new Exception();
            var ex2 = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnError<int>(230, ex1),
            };

            var f1 = scheduler.CreateHotObservable(msgs1);
            var f2 = ThrowEnumerable(false, ex2);

            var results = scheduler.Run(() => f1.Zip(f2, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(230, ex1)
            );
        }

        [TestMethod]
        public void ZipWithEnumerable_SomeError()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnCompleted<int>(230)
            };

            var o = scheduler.CreateHotObservable(msgs1);
            var e = ThrowEnumerable(false, ex);

            var results = scheduler.Run(() => o.Zip(e, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(215, ex)
            );
        }

        [TestMethod]
        public void ZipWithEnumerable_ErrorSome()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(220, ex),
            };

            var o = new[] { 2 };
            var e = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e.Zip(o, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex)
            );
        }

        [TestMethod]
        public void ZipWithEnumerable_SomeDataBothSides()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5)
            };

            var o = new[] { 5, 4, 3, 2 };
            var e = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e.Zip(o, (x, y) => x + y));
            results.AssertEqual(
                OnNext(210, 7),
                OnNext(220, 7),
                OnNext(230, 7),
                OnNext(240, 7)
            );
        }

        [TestMethod]
        public void ZipWithEnumerable_EnumeratorThrowsMoveNext()
        {
            var ex = new Exception("Bang");

            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnNext(225, 4),
                OnCompleted<int>(240)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = new MyEnumerable(false, ex);

            var results = scheduler.Run(() => o1.Zip(o2, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(215, ex)
            );
        }

        [TestMethod]
        public void ZipWithEnumerable_EnumeratorThrowsCurrent()
        {
            var ex = new Exception("Bang");

            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnNext(225, 4),
                OnCompleted<int>(240)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = new MyEnumerable(true, ex);

            var results = scheduler.Run(() => o1.Zip(o2, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(215, ex)
            );
        }

        class MyEnumerable : IEnumerable<int>
        {
            private bool _throwInCurrent;
            private Exception _ex;

            public MyEnumerable(bool throwInCurrent, Exception ex)
            {
                _throwInCurrent = throwInCurrent;
                _ex = ex;
            }

            public IEnumerator<int> GetEnumerator()
            {
                return new MyEnumerator(_throwInCurrent, _ex);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            class MyEnumerator : IEnumerator<int>
            {
                private bool _throwInCurrent;
                private Exception _ex;

                public MyEnumerator(bool throwInCurrent, Exception ex)
                {
                    _throwInCurrent = throwInCurrent;
                    _ex = ex;
                }

                public int Current
                {
                    get 
                    {
                        if (_throwInCurrent)
                            throw _ex;
                        else
                            return 1;
                    }
                }

                public void Dispose()
                {
                }

                object System.Collections.IEnumerator.Current
                {
                    get { return Current; }
                }

                public bool MoveNext()
                {
                    if (!_throwInCurrent)
                        throw _ex;
                    return true;
                }

                public void Reset()
                {
                }
            }
        }


        [TestMethod]
        public void ZipWithEnumerable_SelectorThrows()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnNext(225, 4),
                OnCompleted<int>(240)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = new[] { 3, 5 };

            var ex = new Exception("Bang");

            var results = scheduler.Run(() => o1.Zip(o2, (x, y) =>
            {
                if (y == 5)
                    throw ex;
                return x + y;
            }));
            results.AssertEqual(
                OnNext(215, 2 + 3),
                OnError<int>(225, ex)
            );
        }

        [TestMethod]
        public void CombineLatest_ArgumentChecking()
        {
            var scheduler = new TestScheduler();
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.CombineLatest<int, int, int>(someObservable, someObservable, null));
            Throws<ArgumentNullException>(() => Observable.CombineLatest<int, int, int>(null, someObservable, (_, __) => 0));
            Throws<ArgumentNullException>(() => Observable.CombineLatest<int, int, int>(someObservable, null, (_, __) => 0));
        }

        [TestMethod]
        public void CombineLatest_NeverNever()
        {
            var scheduler = new TestScheduler();

            var n1 = Observable.Never<int>();
            var n2 = Observable.Never<int>();

            var results = scheduler.Run(() => n1.CombineLatest(n2, (x, y) => x + y));
            results.AssertEqual();
        }

        [TestMethod]
        public void CombineLatest_NeverEmpty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(210)
            };

            var n = Observable.Never<int>();
            var e = scheduler.CreateHotObservable(msgs);

            var results = scheduler.Run(() => n.CombineLatest(e, (x, y) => x + y));
            results.AssertEqual();
        }

        [TestMethod]
        public void CombineLatest_EmptyNever()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(210)
            };

            var e = scheduler.CreateHotObservable(msgs);
            var n = Observable.Never<int>();

            var results = scheduler.Run(() => e.CombineLatest(n, (x, y) => x + y));
            results.AssertEqual();
        }


        [TestMethod]
        public void CombineLatest_EmptyEmpty()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(210)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(210)
            };

            var e1 = scheduler.CreateHotObservable(msgs1);
            var e2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e1.CombineLatest(e2, (x, y) => x + y));
            results.AssertEqual(
                OnCompleted<int>(210)
            );
        }

        [TestMethod]
        public void CombineLatest_EmptyReturn()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(210)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnCompleted<int>(220)
            };

            var e = scheduler.CreateHotObservable(msgs1);
            var o = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e.CombineLatest(o, (x, y) => x + y));
            results.AssertEqual(
                OnCompleted<int>(215)
            );
        }

        [TestMethod]
        public void CombineLatest_ReturnEmpty()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(210)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnCompleted<int>(220)
            };

            var e = scheduler.CreateHotObservable(msgs1);
            var o = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o.CombineLatest(e, (x, y) => x + y));
            results.AssertEqual(
                OnCompleted<int>(215)
            );
        }

        [TestMethod]
        public void CombineLatest_NeverReturn()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnCompleted<int>(220)
            };

            var o = scheduler.CreateHotObservable(msgs);
            var n = Observable.Never<int>();

            var results = scheduler.Run(() => n.CombineLatest(o, (x, y) => x + y));
            results.AssertEqual();
        }

        [TestMethod]
        public void CombineLatest_ReturnNever()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnCompleted<int>(220)
            };

            var o = scheduler.CreateHotObservable(msgs);
            var n = Observable.Never<int>();

            var results = scheduler.Run(() => o.CombineLatest(n, (x, y) => x + y));
            results.AssertEqual();
        }

        [TestMethod]
        public void CombineLatest_ReturnReturn()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(220, 3),
                OnCompleted<int>(240)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.CombineLatest(o2, (x, y) => x + y));
            results.AssertEqual(
                OnNext(220, 2 + 3),
                OnCompleted<int>(240)
            );
        }

        [TestMethod]
        public void CombineLatest_EmptyError()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(220, ex),
            };

            var e = scheduler.CreateHotObservable(msgs1);
            var f = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e.CombineLatest(f, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex)
            );
        }

        [TestMethod]
        public void CombineLatest_ErrorEmpty()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(220, ex),
            };

            var e = scheduler.CreateHotObservable(msgs1);
            var f = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => f.CombineLatest(e, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex)
            );
        }

        [TestMethod]
        public void CombineLatest_ReturnThrow()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(220, ex),
            };

            var r = scheduler.CreateHotObservable(msgs1);
            var f = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => r.CombineLatest(f, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex)
            );
        }

        [TestMethod]
        public void CombineLatest_ThrowReturn()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(220, ex),
            };

            var r = scheduler.CreateHotObservable(msgs1);
            var f = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => f.CombineLatest(r, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex)
            );
        }

        [TestMethod]
        public void CombineLatest_ThrowThrow()
        {
            var scheduler = new TestScheduler();

            var ex1 = new Exception();
            var ex2 = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnError<int>(220, ex1),
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(230, ex2),
            };

            var r = scheduler.CreateHotObservable(msgs1);
            var f = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => f.CombineLatest(r, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex1)
            );
        }

        [TestMethod]
        public void CombineLatest_ErrorThrow()
        {
            var scheduler = new TestScheduler();

            var ex1 = new Exception();
            var ex2 = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnError<int>(220, ex1),
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(230, ex2),
            };

            var t = scheduler.CreateHotObservable(msgs1);
            var f = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => f.CombineLatest(t, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex1)
            );
        }

        [TestMethod]
        public void CombineLatest_ThrowError()
        {
            var scheduler = new TestScheduler();

            var ex1 = new Exception();
            var ex2 = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnError<int>(220, ex1),
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(230, ex2),
            };

            var t = scheduler.CreateHotObservable(msgs1);
            var f = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => t.CombineLatest(f, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex1)
            );
        }

        [TestMethod]
        public void CombineLatest_NeverThrow()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(220, ex),
            };

            var n = Observable.Never<int>();
            var f = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => n.CombineLatest(f, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex)
            );
        }

        [TestMethod]
        public void CombineLatest_ThrowNever()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(220, ex),
            };

            var n = Observable.Never<int>();
            var f = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => f.CombineLatest(n, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex)
            );
        }

        [TestMethod]
        public void CombineLatest_SomeThrow()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(220, ex),
            };

            var o = scheduler.CreateHotObservable(msgs1);
            var e = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o.CombineLatest(e, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex)
            );
        }

        [TestMethod]
        public void CombineLatest_ThrowSome()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(220, ex),
            };

            var o = scheduler.CreateHotObservable(msgs1);
            var e = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e.CombineLatest(o, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex)
            );
        }

        [TestMethod]
        public void CombineLatest_ThrowAfterCompleteLeft()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnCompleted<int>(220)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(230, ex),
            };

            var o = scheduler.CreateHotObservable(msgs1);
            var e = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e.CombineLatest(o, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(230, ex)
            );
        }

        [TestMethod]
        public void CombineLatest_ThrowAfterCompleteRight()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnCompleted<int>(220)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(230, ex),
            };

            var o = scheduler.CreateHotObservable(msgs1);
            var e = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o.CombineLatest(e, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(230, ex)
            );
        }

        [TestMethod]
        public void CombineLatest_InterleavedWithTail()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnNext(225, 4),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(220, 3),
                OnNext(230, 5),
                OnNext(235, 6),
                OnNext(240, 7),
                OnCompleted<int>(250)
            };

            var o = scheduler.CreateHotObservable(msgs1);
            var e = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e.CombineLatest(o, (x, y) => x + y));
            results.AssertEqual(
                OnNext(220, 2 + 3),
                OnNext(225, 3 + 4),
                OnNext(230, 4 + 5),
                OnNext(235, 4 + 6),
                OnNext(240, 4 + 7),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void CombineLatest_Consecutive()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnNext(225, 4),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(235, 6),
                OnNext(240, 7),
                OnCompleted<int>(250)
            };

            var o = scheduler.CreateHotObservable(msgs1);
            var e = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e.CombineLatest(o, (x, y) => x + y));
            results.AssertEqual(
                OnNext(235, 4 + 6),
                OnNext(240, 4 + 7),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void CombineLatest_ConsecutiveEndWithErrorLeft()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnNext(225, 4),
                OnError<int>(230, ex)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(235, 6),
                OnNext(240, 7),
                OnCompleted<int>(250)
            };

            var o = scheduler.CreateHotObservable(msgs1);
            var e = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e.CombineLatest(o, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(230, ex)
            );
        }

        [TestMethod]
        public void CombineLatest_ConsecutiveEndWithErrorRight()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnNext(225, 4),
                OnCompleted<int>(250)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(235, 6),
                OnNext(240, 7),
                OnError<int>(245, ex)
            };

            var o = scheduler.CreateHotObservable(msgs1);
            var e = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e.CombineLatest(o, (x, y) => x + y));
            results.AssertEqual(
                OnNext(235, 4 + 6),
                OnNext(240, 4 + 7),
                OnError<int>(245, ex)
            );
        }

        [TestMethod]
        public void CombineLatest_SelectorThrows()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(220, 3),
                OnCompleted<int>(240)
            };

            var o = scheduler.CreateHotObservable(msgs1);
            var e = scheduler.CreateHotObservable(msgs2);

            var ex = new Exception();

            var results = scheduler.Run(() => e.CombineLatest<int, int, int>(o, (x, y) => { throw ex; }));
            results.AssertEqual(
                OnError<int>(220, ex)
            );
        }

        [TestMethod]
        public void ForkJoin_ArgumentChecking()
        {
            var scheduler = new TestScheduler();
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.ForkJoin(someObservable, someObservable, (Func<int, int, int>)null));
            Throws<ArgumentNullException>(() => Observable.ForkJoin(someObservable, (IObservable<int>)null, (_, __) => _ + __));
            Throws<ArgumentNullException>(() => Observable.ForkJoin((IObservable<int>)null, someObservable, (_, __) => _ + __));
            Throws<ArgumentNullException>(() => Observable.ForkJoin((IObservable<int>[])null));
            Throws<ArgumentNullException>(() => Observable.ForkJoin((IEnumerable<IObservable<int>>)null));
        }

        [TestMethod]
        public void ForkJoin_EmptyEmpty()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var o = scheduler.CreateHotObservable(msgs1);
            var e = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e.ForkJoin(o, (x, y) => x + y));
            results.AssertEqual(
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void ForkJoin_EmptyReturn()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(250)
            };

            var o = scheduler.CreateHotObservable(msgs1);
            var e = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e.ForkJoin(o, (x, y) => x + y));
            results.AssertEqual(
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void ForkJoin_ReturnEmpty()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var o = scheduler.CreateHotObservable(msgs1);
            var e = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e.ForkJoin(o, (x, y) => x + y));
            results.AssertEqual(
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void ForkJoin_ReturnReturn()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(220, 3),
                OnCompleted<int>(250)
            };

            var o = scheduler.CreateHotObservable(msgs1);
            var e = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e.ForkJoin(o, (x, y) => x + y));
            results.AssertEqual(
                OnNext(250, 2 + 3),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void ForkJoin_EmptyThrow()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(210, ex),
                OnCompleted<int>(250)
            };

            var o = scheduler.CreateHotObservable(msgs1);
            var e = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e.ForkJoin(o, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(210, ex)
            );
        }

        [TestMethod]
        public void ForkJoin_ThrowEmpty()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnError<int>(210, ex),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var o = scheduler.CreateHotObservable(msgs1);
            var e = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e.ForkJoin(o, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(210, ex)
            );
        }

        [TestMethod]
        public void ForkJoin_ReturnThrow()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(220, ex),
                OnCompleted<int>(250)
            };

            var o = scheduler.CreateHotObservable(msgs1);
            var e = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e.ForkJoin(o, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex)
            );
        }

        [TestMethod]
        public void ForkJoin_ThrowReturn()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnError<int>(220, ex),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(250)
            };

            var o = scheduler.CreateHotObservable(msgs1);
            var e = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e.ForkJoin(o, (x, y) => x + y));
            results.AssertEqual(
                OnError<int>(220, ex)
            );
        }

        [TestMethod]
        public void ForkJoin_Binary()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnNext(225, 4),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(235, 6),
                OnNext(240, 7),
                OnCompleted<int>(250)
            };

            var o = scheduler.CreateHotObservable(msgs1);
            var e = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => e.ForkJoin(o, (x, y) => x + y));
            results.AssertEqual(
                OnNext(250, 4 + 7),   // TODO: fix ForkJoin behavior
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void ForkJoin_NaryParams()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnNext(225, 4),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(235, 6),
                OnNext(240, 7),
                OnCompleted<int>(250)
            };

            var msgs3 = new[] {
                OnNext(150, 1),
                OnNext(230, 3),
                OnNext(245, 5),
                OnCompleted<int>(270)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);
            var o3 = scheduler.CreateHotObservable(msgs3);

            var results = scheduler.Run(() => Observable.ForkJoin(o1, o2, o3)).ToArray();
            Assert.IsTrue(results.Length == 2, "count");
            Assert.IsTrue(results[0].Time == 270 && results[0].Value.Value.SequenceEqual(new[] { 4, 7, 5 }), "data");    // TODO: fix ForkJoin behavior
            Assert.IsTrue(results[1].Time == 270 && results[1].Value.Kind == NotificationKind.OnCompleted, "completed");
        }

        [TestMethod]
        public void ForkJoin_Nary()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(215, 2),
                OnNext(225, 4),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(235, 6),
                OnNext(240, 7),
                OnCompleted<int>(250)
            };

            var msgs3 = new[] {
                OnNext(150, 1),
                OnNext(230, 3),
                OnNext(245, 5),
                OnCompleted<int>(270)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);
            var o3 = scheduler.CreateHotObservable(msgs3);

            var results = scheduler.Run(() => Observable.ForkJoin(new List<IObservable<int>> { o1, o2, o3 })).ToArray();
            Assert.IsTrue(results.Length == 2, "count");
            Assert.IsTrue(results[0].Time == 270 && results[0].Value.Value.SequenceEqual(new[] { 4, 7, 5 }), "data");    // TODO: fix ForkJoin behavior
            Assert.IsTrue(results[1].Time == 270 && results[1].Value.Kind == NotificationKind.OnCompleted, "completed");
        }

        [TestMethod]
        public void Concat_ArgumentChecking()
        {
            var scheduler = new TestScheduler();
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.Concat(someObservable, (IObservable<int>)null));
            Throws<ArgumentNullException>(() => Observable.Concat((IObservable<int>)null, someObservable));
            Throws<ArgumentNullException>(() => Observable.Concat((IObservable<int>[])null));
            Throws<ArgumentNullException>(() => Observable.Concat((IEnumerable<IObservable<int>>)null));
            Throws<ArgumentNullException>(() => Observable.Concat(scheduler, (IObservable<int>[])null));
            Throws<ArgumentNullException>(() => Observable.Concat((IScheduler)null, someObservable, someObservable));
            Throws<ArgumentNullException>(() => Observable.Concat((IEnumerable<IObservable<int>>)null, scheduler));
            Throws<ArgumentNullException>(() => Observable.Concat(new[] { someObservable }, null));
            Throws<ArgumentNullException>(() => Observable.Concat(someObservable, null, scheduler));
            Throws<ArgumentNullException>(() => Observable.Concat(someObservable, someObservable, (IScheduler)null));
            Throws<ArgumentNullException>(() => Observable.Concat(null, someObservable, scheduler));
        }

        [TestMethod]
        public void Concat_DefaultScheduler()
        {
            var evt = new ManualResetEvent(false);

            int sum = 0;
            Observable.Concat(Observable.Return(1), Observable.Return(2), Observable.Return(3)).Subscribe(n =>
            {
                sum += n;
            }, () => evt.Set());

            evt.WaitOne();

            Assert.AreEqual(6, sum);
        }

        [TestMethod]
        public void Concat_DefaultScheduler_IEofIO()
        {
            var evt = new ManualResetEvent(false);

            IEnumerable<IObservable<int>> sources = new[] { Observable.Return(1), Observable.Return(2), Observable.Return(3) };

            int sum = 0;
            Observable.Concat(sources).Subscribe(n =>
            {
                sum += n;
            }, () => evt.Set());

            evt.WaitOne();

            Assert.AreEqual(6, sum);
        }

        [TestMethod]
        public void Concat_EmptyEmpty()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.Concat(o2, scheduler));
            results.AssertEqual(
                OnCompleted<int>(251)
            );
        }

        [TestMethod]
        public void Concat_EmptyNever()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(230)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = Observable.Never<int>();

            var results = scheduler.Run(() => o1.Concat(o2, scheduler));
            results.AssertEqual();
        }

        [TestMethod]
        public void Concat_NeverEmpty()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(230)
            };

            var o1 = Observable.Never<int>();
            var o2 = scheduler.CreateHotObservable(msgs1);

            var results = scheduler.Run(() => o1.Concat(o2, scheduler));
            results.AssertEqual();
        }

        [TestMethod]
        public void Concat_NeverNever()
        {
            var scheduler = new TestScheduler();

            var o1 = Observable.Never<int>();
            var o2 = Observable.Never<int>();

            var results = scheduler.Run(() => o1.Concat(o2, scheduler));
            results.AssertEqual();
        }

        [TestMethod]
        public void Concat_EmptyThrow()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(250, ex)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.Concat(o2, scheduler));
            results.AssertEqual(
                OnError<int>(250, ex)
            );
        }

        [TestMethod]
        public void Concat_ThrowEmpty()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnError<int>(230, ex)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.Concat(o2, scheduler));
            results.AssertEqual(
                OnError<int>(230, ex)
            );
        }

        [TestMethod]
        public void Concat_ThrowThrow()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnError<int>(230, ex)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(250, new Exception())
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.Concat(o2, scheduler));
            results.AssertEqual(
                OnError<int>(230, ex)
            );
        }

        [TestMethod]
        public void Concat_ReturnEmpty()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.Concat(o2, scheduler));
            results.AssertEqual(
                OnNext(210, 2),
                OnCompleted<int>(251)
            );
        }

        [TestMethod]
        public void Concat_EmptyReturn()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(240, 2),
                OnCompleted<int>(250)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.Concat(o2, scheduler));
            results.AssertEqual(
                OnNext(240, 2),
                OnCompleted<int>(251)
            );
        }

        [TestMethod]
        public void Concat_ReturnNever()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(230)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = Observable.Never<int>();

            var results = scheduler.Run(() => o1.Concat(o2, scheduler));
            results.AssertEqual(
                OnNext(210, 2)
            );
        }

        [TestMethod]
        public void Concat_NeverReturn()
        {
            var scheduler = new TestScheduler();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(230)
            };

            var o1 = Observable.Never<int>();
            var o2 = scheduler.CreateHotObservable(msgs1);

            var results = scheduler.Run(() => o1.Concat(o2, scheduler));
            results.AssertEqual();
        }

        [TestMethod]
        public void Concat_ReturnReturn()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(220, 2),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(240, 3),
                OnCompleted<int>(250)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.Concat(o2, scheduler));
            results.AssertEqual(
                OnNext(220, 2),
                OnNext(240, 3),
                OnCompleted<int>(251)
            );
        }

        [TestMethod]
        public void Concat_ThrowReturn()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnError<int>(230, ex)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(240, 2),
                OnCompleted<int>(250)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.Concat(o2, scheduler));
            results.AssertEqual(
                OnError<int>(230, ex)
            );
        }

        [TestMethod]
        public void Concat_ReturnThrow()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(220, 2),
                OnCompleted<int>(230)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnError<int>(250, ex)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.Concat(o2, scheduler));
            results.AssertEqual(
                OnNext(220, 2),
                OnError<int>(250, ex)
            );
        }

        [TestMethod]
        public void Concat_SomeDataSomeData()
        {
            var scheduler = new TestScheduler();

            var ex = new Exception();

            var msgs1 = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnCompleted<int>(225)
            };

            var msgs2 = new[] {
                OnNext(150, 1),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(250)
            };

            var o1 = scheduler.CreateHotObservable(msgs1);
            var o2 = scheduler.CreateHotObservable(msgs2);

            var results = scheduler.Run(() => o1.Concat(o2, scheduler));
            results.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnNext(240, 5),
                OnCompleted<int>(251)
            );
        }

        [TestMethod]
        public void Concat_EnumerableThrows()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnCompleted<int>(225)
            };

            var o = scheduler.CreateHotObservable(msgs);

            var ex = new Exception();
            var results = scheduler.Run(() => GetObservablesForConcatThrow(o, ex).Concat());
            results.AssertEqual(
                OnNext(210, 2),
                OnNext(220, 3),
                OnError<int>(225, ex)
            );
        }

        private IEnumerable<IObservable<int>> GetObservablesForConcatThrow(IObservable<int> first, Exception ex)
        {
            yield return first;
            throw ex;
        }
    }
}
