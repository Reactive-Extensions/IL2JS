using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Collections.Generic;

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
    public partial class ObservableAggregateTest : Test
    {
        [TestMethod]
        public void Aggregate_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.Aggregate<int, int>(default(IObservable<int>), 1, (x, y) => x + y));
            Throws<ArgumentNullException>(() => Observable.Aggregate<int, int>(someObservable, 1, default(Func<int, int, int>)));

            Throws<ArgumentNullException>(() => Observable.Aggregate<int>(default(IObservable<int>), (x, y) => x + y));
            Throws<ArgumentNullException>(() => Observable.Aggregate<int>(someObservable, default(Func<int, int, int>)));
        }

        [TestMethod]
        public void AggregateWithSeed_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Aggregate(42, (acc, x) => acc + x)).ToArray();
            res.AssertEqual(
                OnNext(250, 42),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void AggregateWithSeed_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 24),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Aggregate(42, (acc, x) => acc + x)).ToArray();
            res.AssertEqual(
                OnNext<int>(250, 42 + 24),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void AggregateWithSeed_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnError<int>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Aggregate(42, (acc, x) => acc + x)).ToArray();
            res.AssertEqual(
                OnError<int>(210, ex)
            );
        }

        [TestMethod]
        public void AggregateWithSeed_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Aggregate(42, (acc, x) => acc + x)).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void AggregateWithSeed_Range()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 0),
                OnNext(220, 1),
                OnNext(230, 2),
                OnNext(240, 3),
                OnNext(250, 4),
                OnCompleted<int>(260)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Aggregate(42, (acc, x) => acc + x)).ToArray();
            res.AssertEqual(
                OnNext(260, 42 + Enumerable.Range(0, 5).Sum()),
                OnCompleted<int>(260)
            );
        }

        [TestMethod]
        public void AggregateWithoutSeed_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Aggregate((acc, x) => acc + x)).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<int>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.AreEqual(250, res[0].Time);
        }

        [TestMethod]
        public void AggregateWithoutSeed_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 24),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Aggregate((acc, x) => acc + x)).ToArray();
            res.AssertEqual(
                OnNext<int>(250, 24),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void AggregateWithoutSeed_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnError<int>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Aggregate((acc, x) => acc + x)).ToArray();
            res.AssertEqual(
                OnError<int>(210, ex)
            );
        }

        [TestMethod]
        public void AggregateWithoutSeed_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Aggregate((acc, x) => acc + x)).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void AggregateWithoutSeed_Range()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 0),
                OnNext(220, 1),
                OnNext(230, 2),
                OnNext(240, 3),
                OnNext(250, 4),
                OnCompleted<int>(260)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Aggregate((acc, x) => acc + x)).ToArray();
            res.AssertEqual(
                OnNext(260, Enumerable.Range(0, 5).Sum()),
                OnCompleted<int>(260)
            );
        }

        [TestMethod]
        public void IsEmpty_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.IsEmpty(default(IObservable<int>)));
        }

        [TestMethod]
        public void IsEmpty_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.IsEmpty()).ToArray();
            res.AssertEqual(
                OnNext(250, true),
                OnCompleted<bool>(250)
            );
        }

        [TestMethod]
        public void IsEmpty_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.IsEmpty()).ToArray();
            res.AssertEqual(
                OnNext(210, false),
                OnCompleted<bool>(210)
            );
        }

        [TestMethod]
        public void IsEmpty_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnError<int>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.IsEmpty()).ToArray();
            res.AssertEqual(
                OnError<bool>(210, ex)
            );
        }

        [TestMethod]
        public void IsEmpty_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.IsEmpty()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Any_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.Any(default(IObservable<int>)));
            Throws<ArgumentNullException>(() => Observable.Any(someObservable, default(Func<int, bool>)));
            Throws<ArgumentNullException>(() => Observable.Any(default(IObservable<int>), x => true));
        }

        [TestMethod]
        public void Any_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Any()).ToArray();
            res.AssertEqual(
                OnNext(250, false),
                OnCompleted<bool>(250)
            );
        }

        [TestMethod]
        public void Any_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Any()).ToArray();
            res.AssertEqual(
                OnNext(210, true),
                OnCompleted<bool>(210)
            );
        }

        [TestMethod]
        public void Any_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnError<int>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Any()).ToArray();
            res.AssertEqual(
                OnError<bool>(210, ex)
            );
        }

        [TestMethod]
        public void Any_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Any()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Any_Predicate_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Any(x => x > 0));
            res.AssertEqual(
                OnNext(250, false),
                OnCompleted<bool>(250)
            );
        }

        [TestMethod]
        public void Any_Predicate_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Any(x => x > 0)).ToArray();
            res.AssertEqual(
                OnNext(210, true),
                OnCompleted<bool>(210)
            );
        }

        [TestMethod]
        public void Any_Predicate_ReturnNotMatch()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, -2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Any(x => x > 0)).ToArray();
            res.AssertEqual(
                OnNext(250, false),
                OnCompleted<bool>(250)
            );
        }

        [TestMethod]
        public void Any_Predicate_SomeNoneMatch()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, -2),
                OnNext(220, -3),
                OnNext(230, -4),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Any(x => x > 0)).ToArray();
            res.AssertEqual(
                OnNext(250, false),
                OnCompleted<bool>(250)
            );
        }

        [TestMethod]
        public void Any_Predicate_SomeMatch()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, -2),
                OnNext(220, 3),
                OnNext(230, -4),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Any(x => x > 0)).ToArray();
            res.AssertEqual(
                OnNext(220, true),
                OnCompleted<bool>(220)
            );
        }

        [TestMethod]
        public void Any_Predicate_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnError<int>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Any(x => x > 0)).ToArray();
            res.AssertEqual(
                OnError<bool>(210, ex)
            );
        }

        [TestMethod]
        public void Any_Predicate_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Any(x => x > 0)).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void All_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.All(someObservable, default(Func<int, bool>)));
            Throws<ArgumentNullException>(() => Observable.All(default(IObservable<int>), x => true));
        }

        [TestMethod]
        public void All_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.All(x => x > 0));
            res.AssertEqual(
                OnNext(250, true),
                OnCompleted<bool>(250)
            );
        }

        [TestMethod]
        public void All_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.All(x => x > 0)).ToArray();
            res.AssertEqual(
                OnNext(250, true),
                OnCompleted<bool>(250)
            );
        }

        [TestMethod]
        public void All_ReturnNotMatch()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, -2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.All(x => x > 0)).ToArray();
            res.AssertEqual(
                OnNext(210, false),
                OnCompleted<bool>(210)
            );
        }

        [TestMethod]
        public void All_SomeNoneMatch()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, -2),
                OnNext(220, -3),
                OnNext(230, -4),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.All(x => x > 0)).ToArray();
            res.AssertEqual(
                OnNext(210, false),
                OnCompleted<bool>(210)
            );
        }

        [TestMethod]
        public void All_SomeMatch()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, -2),
                OnNext(220, 3),
                OnNext(230, -4),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.All(x => x > 0)).ToArray();
            res.AssertEqual(
                OnNext(210, false),
                OnCompleted<bool>(210)
            );
        }

        [TestMethod]
        public void All_SomeAllMatch()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.All(x => x > 0)).ToArray();
            res.AssertEqual(
                OnNext(250, true),
                OnCompleted<bool>(250)
            );
        }

        [TestMethod]
        public void All_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnError<int>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.All(x => x > 0)).ToArray();
            res.AssertEqual(
                OnError<bool>(210, ex)
            );
        }

        [TestMethod]
        public void All_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.All(x => x > 0)).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Contains_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.Contains(default(IObservable<int>), 42));
            Throws<ArgumentNullException>(() => Observable.Contains(default(IObservable<int>), 42, EqualityComparer<int>.Default));
            Throws<ArgumentNullException>(() => Observable.Contains(someObservable, 42, null));
        }

        [TestMethod]
        public void Contains_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Contains(42)).ToArray();
            res.AssertEqual(
                OnNext(250, false),
                OnCompleted<bool>(250)
            );
        }

        [TestMethod]
        public void Contains_ReturnPositive()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Contains(2)).ToArray();
            res.AssertEqual(
                OnNext(210, true),
                OnCompleted<bool>(210)
            );
        }

        [TestMethod]
        public void Contains_ReturnNegative()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Contains(-2)).ToArray();
            res.AssertEqual(
                OnNext(250, false),
                OnCompleted<bool>(250)
            );
        }

        [TestMethod]
        public void Contains_SomePositive()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Contains(3)).ToArray();
            res.AssertEqual(
                OnNext(220, true),
                OnCompleted<bool>(220)
            );
        }

        [TestMethod]
        public void Contains_SomeNegative()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Contains(-3)).ToArray();
            res.AssertEqual(
                OnNext(250, false),
                OnCompleted<bool>(250)
            );
        }

        [TestMethod]
        public void Contains_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnError<int>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Contains(42)).ToArray();
            res.AssertEqual(
                OnError<bool>(210, ex)
            );
        }

        [TestMethod]
        public void Contains_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Contains(42)).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Contains_ComparerThrows()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Contains(42, new ContainsComparerThrows())).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<bool>.OnError)res[0].Value).Exception is NotImplementedException);
            Assert.AreEqual(210, res[0].Time);
        }

        class ContainsComparerThrows : IEqualityComparer<int>
        {
            public bool Equals(int x, int y)
            {
                throw new NotImplementedException();
            }

            public int GetHashCode(int obj)
            {
                throw new NotImplementedException();
            }
        }

        [TestMethod]
        public void Contains_ComparerContainsValue()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 3),
                OnNext(220, 4),
                OnNext(230, 8),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Contains(42, new ContainsComparerMod2())).ToArray();
            res.AssertEqual(
                OnNext(220, true),
                OnCompleted<bool>(220)
            );
        }

        [TestMethod]
        public void Contains_ComparerDoesNotContainValue()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 4),
                OnNext(230, 8),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Contains(21, new ContainsComparerMod2())).ToArray();
            res.AssertEqual(
                OnNext(250, false),
                OnCompleted<bool>(250)
            );
        }

        class ContainsComparerMod2 : IEqualityComparer<int>
        {
            public bool Equals(int x, int y)
            {
                return x % 2 == y % 2;
            }

            public int GetHashCode(int obj)
            {
                return obj.GetHashCode();
            }
        }

        [TestMethod]
        public void Count_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Count(default(IObservable<int>)));
        }

        [TestMethod]
        public void Count_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Count()).ToArray();
            res.AssertEqual(
                OnNext(250, 0),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void Count_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Count()).ToArray();
            res.AssertEqual(
                OnNext(250, 1),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void Count_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Count()).ToArray();
            res.AssertEqual(
                OnNext(250, 3),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void Count_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnError<int>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Count()).ToArray();
            res.AssertEqual(
                OnError<int>(210, ex)
            );
        }

        [TestMethod]
        public void Count_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Count()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void LongCount_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.LongCount(default(IObservable<int>)));
        }

        [TestMethod]
        public void LongCount_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.LongCount()).ToArray();
            res.AssertEqual(
                OnNext(250, 0L),
                OnCompleted<long>(250)
            );
        }

        [TestMethod]
        public void LongCount_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.LongCount()).ToArray();
            res.AssertEqual(
                OnNext(250, 1L),
                OnCompleted<long>(250)
            );
        }

        [TestMethod]
        public void LongCount_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.LongCount()).ToArray();
            res.AssertEqual(
                OnNext(250, 3L),
                OnCompleted<long>(250)
            );
        }

        [TestMethod]
        public void LongCount_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnError<int>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.LongCount()).ToArray();
            res.AssertEqual(
                OnError<long>(210, ex)
            );
        }

        [TestMethod]
        public void LongCount_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.LongCount()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Sum_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Sum(default(IObservable<int>)));
            Throws<ArgumentNullException>(() => Observable.Sum(default(IObservable<double>)));
            Throws<ArgumentNullException>(() => Observable.Sum(default(IObservable<float>)));
            Throws<ArgumentNullException>(() => Observable.Sum(default(IObservable<decimal>)));
            Throws<ArgumentNullException>(() => Observable.Sum(default(IObservable<long>)));
            Throws<ArgumentNullException>(() => Observable.Sum(default(IObservable<int?>)));
            Throws<ArgumentNullException>(() => Observable.Sum(default(IObservable<double?>)));
            Throws<ArgumentNullException>(() => Observable.Sum(default(IObservable<float?>)));
            Throws<ArgumentNullException>(() => Observable.Sum(default(IObservable<decimal?>)));
            Throws<ArgumentNullException>(() => Observable.Sum(default(IObservable<long?>)));
        }

        [TestMethod]
        public void Sum_Int32_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, 0),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void Sum_Int32_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, 2),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void Sum_Int32_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnNext(220, 3),
                OnNext(230, 4),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, 2 + 3 + 4),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void Sum_Int32_Overflow()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, int.MaxValue),
                OnNext(220, 1),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<int>.OnError)res[0].Value).Exception is OverflowException);
            Assert.IsTrue(res[0].Time == 220);
        }

        [TestMethod]
        public void Sum_Int32_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnError<int>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnError<int>(210, ex)
            );
        }

        [TestMethod]
        public void Sum_Int32_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Sum_Int64_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1L),
                OnCompleted<long>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, 0L),
                OnCompleted<long>(250)
            );
        }

        [TestMethod]
        public void Sum_Int64_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1L),
                OnNext(210, 2L),
                OnCompleted<long>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, 2L),
                OnCompleted<long>(250)
            );
        }

        [TestMethod]
        public void Sum_Int64_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1L),
                OnNext(210, 2L),
                OnNext(220, 3L),
                OnNext(230, 4L),
                OnCompleted<long>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, 2L + 3L + 4L),
                OnCompleted<long>(250)
            );
        }

        [TestMethod]
        public void Sum_Int64_Overflow()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1L),
                OnNext(210, long.MaxValue),
                OnNext(220, 1L),
                OnCompleted<long>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<long>.OnError)res[0].Value).Exception is OverflowException);
            Assert.IsTrue(res[0].Time == 220);
        }

        [TestMethod]
        public void Sum_Int64_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1L),
                OnError<long>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnError<long>(210, ex)
            );
        }

        [TestMethod]
        public void Sum_Int64_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1L)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Sum_Float_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1f),
                OnCompleted<float>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, 0f),
                OnCompleted<float>(250)
            );
        }

        [TestMethod]
        public void Sum_Float_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1f),
                OnNext(210, 2f),
                OnCompleted<float>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, 2f),
                OnCompleted<float>(250)
            );
        }

        [TestMethod]
        public void Sum_Float_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1f),
                OnNext(210, 2f),
                OnNext(220, 3f),
                OnNext(230, 4f),
                OnCompleted<float>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, 2f + 3f + 4f),
                OnCompleted<float>(250)
            );
        }

        [TestMethod]
        public void Sum_Float_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1f),
                OnError<float>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnError<float>(210, ex)
            );
        }

        [TestMethod]
        public void Sum_Float_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1f)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Sum_Double_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1.0),
                OnCompleted<double>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, 0.0),
                OnCompleted<double>(250)
            );
        }

        [TestMethod]
        public void Sum_Double_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1.0),
                OnNext(210, 2.0),
                OnCompleted<double>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, 2.0),
                OnCompleted<double>(250)
            );
        }

        [TestMethod]
        public void Sum_Double_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1.0),
                OnNext(210, 2.0),
                OnNext(220, 3.0),
                OnNext(230, 4.0),
                OnCompleted<double>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, 2.0 + 3.0 + 4.0),
                OnCompleted<double>(250)
            );
        }

        [TestMethod]
        public void Sum_Double_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1.0),
                OnError<double>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnError<double>(210, ex)
            );
        }

        [TestMethod]
        public void Sum_Double_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1.0)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Sum_Decimal_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1m),
                OnCompleted<decimal>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, 0m),
                OnCompleted<decimal>(250)
            );
        }

        [TestMethod]
        public void Sum_Decimal_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1m),
                OnNext(210, 2m),
                OnCompleted<decimal>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, 2m),
                OnCompleted<decimal>(250)
            );
        }

        [TestMethod]
        public void Sum_Decimal_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1m),
                OnNext(210, 2m),
                OnNext(220, 3m),
                OnNext(230, 4m),
                OnCompleted<decimal>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, 2m + 3m + 4m),
                OnCompleted<decimal>(250)
            );
        }

        [TestMethod]
        public void Sum_Decimal_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1m),
                OnError<decimal>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnError<decimal>(210, ex)
            );
        }

        [TestMethod]
        public void Sum_Decimal_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1m)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Sum_Nullable_Int32_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnCompleted<int?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, (int?)0),
                OnCompleted<int?>(250)
            );
        }

        [TestMethod]
        public void Sum_Nullable_Int32_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnNext(210, (int?)2),
                OnCompleted<int?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, (int?)2),
                OnCompleted<int?>(250)
            );
        }

        [TestMethod]
        public void Sum_Nullable_Int32_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnNext(210, (int?)2),
                OnNext(220, (int?)null),
                OnNext(230, (int?)4),
                OnCompleted<int?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, (int?)(2 + 4)),
                OnCompleted<int?>(250)
            );
        }

        [TestMethod]
        public void Sum_Nullable_Int32_Overflow()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnNext(210, (int?)int.MaxValue),
                OnNext(220, (int?)1),
                OnCompleted<int?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<int?>.OnError)res[0].Value).Exception is OverflowException);
            Assert.IsTrue(res[0].Time == 220);
        }

        [TestMethod]
        public void Sum_Nullable_Int32_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnError<int?>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnError<int?>(210, ex)
            );
        }

        [TestMethod]
        public void Sum_Nullable_Int32_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Sum_Nullable_Int64_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (long?)1L),
                OnCompleted<long?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, (long?)0L),
                OnCompleted<long?>(250)
            );
        }

        [TestMethod]
        public void Sum_Nullable_Int64_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (long?)1L),
                OnNext(210, (long?)2L),
                OnCompleted<long?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, (long?)2L),
                OnCompleted<long?>(250)
            );
        }

        [TestMethod]
        public void Sum_Nullable_Int64_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (long?)1L),
                OnNext(210, (long?)2L),
                OnNext(220, (long?)null),
                OnNext(230, (long?)4L),
                OnCompleted<long?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, (long?)(2L + 4L)),
                OnCompleted<long?>(250)
            );
        }

        [TestMethod]
        public void Sum_Nullable_Int64_Overflow()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (long?)1L),
                OnNext(210, (long?)long.MaxValue),
                OnNext(220, (long?)1L),
                OnCompleted<long?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<long?>.OnError)res[0].Value).Exception is OverflowException);
            Assert.IsTrue(res[0].Time == 220);
        }

        [TestMethod]
        public void Sum_Nullable_Int64_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (long?)1L),
                OnError<long?>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnError<long?>(210, ex)
            );
        }

        [TestMethod]
        public void Sum_Nullable_Int64_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (long?)1L)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Sum_Nullable_Float_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (float?)1f),
                OnCompleted<float?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, (float?)0f),
                OnCompleted<float?>(250)
            );
        }

        [TestMethod]
        public void Sum_Nullable_Float_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (float?)1f),
                OnNext(210, (float?)2f),
                OnCompleted<float?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, (float?)2f),
                OnCompleted<float?>(250)
            );
        }

        [TestMethod]
        public void Sum_Nullable_Float_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (float?)1f),
                OnNext(210, (float?)2f),
                OnNext(220, (float?)null),
                OnNext(230, (float?)4f),
                OnCompleted<float?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, (float?)(2f + 4f)),
                OnCompleted<float?>(250)
            );
        }

        [TestMethod]
        public void Sum_Nullable_Float_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (float?)1f),
                OnError<float?>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnError<float?>(210, ex)
            );
        }

        [TestMethod]
        public void Sum_Nullable_Float_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (float?)1f)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Sum_Nullable_Double_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (double?)1.0),
                OnCompleted<double?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, (double?)0.0),
                OnCompleted<double?>(250)
            );
        }

        [TestMethod]
        public void Sum_Nullable_Double_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (double?)1.0),
                OnNext(210, (double?)2.0),
                OnCompleted<double?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, (double?)2.0),
                OnCompleted<double?>(250)
            );
        }

        [TestMethod]
        public void Sum_Nullable_Double_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (double?)1.0),
                OnNext(210, (double?)2.0),
                OnNext(220, (double?)null),
                OnNext(230, (double?)4.0),
                OnCompleted<double?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, (double?)(2.0 + 4.0)),
                OnCompleted<double?>(250)
            );
        }

        [TestMethod]
        public void Sum_Nullable_Double_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (double?)1.0),
                OnError<double?>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnError<double?>(210, ex)
            );
        }

        [TestMethod]
        public void Sum_Nullable_Double_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (double?)1.0)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Sum_Nullable_Decimal_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (decimal?)1m),
                OnCompleted<decimal?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, (decimal?)0m),
                OnCompleted<decimal?>(250)
            );
        }

        [TestMethod]
        public void Sum_Nullable_Decimal_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (decimal?)1m),
                OnNext(210, (decimal?)2m),
                OnCompleted<decimal?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, (decimal?)2m),
                OnCompleted<decimal?>(250)
            );
        }

        [TestMethod]
        public void Sum_Nullable_Decimal_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (decimal?)1m),
                OnNext(210, (decimal?)2m),
                OnNext(220, (decimal?)null),
                OnNext(230, (decimal?)4m),
                OnCompleted<decimal?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnNext(250, (decimal?)(2m + 4m)),
                OnCompleted<decimal?>(250)
            );
        }

        [TestMethod]
        public void Sum_Nullable_Decimal_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (decimal?)1m),
                OnError<decimal?>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
                OnError<decimal?>(210, ex)
            );
        }

        [TestMethod]
        public void Sum_Nullable_Decimal_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (decimal?)1m)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Sum()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Min_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Min(default(IObservable<int>)));
            Throws<ArgumentNullException>(() => Observable.Min(default(IObservable<double>)));
            Throws<ArgumentNullException>(() => Observable.Min(default(IObservable<float>)));
            Throws<ArgumentNullException>(() => Observable.Min(default(IObservable<decimal>)));
            Throws<ArgumentNullException>(() => Observable.Min(default(IObservable<long>)));
            Throws<ArgumentNullException>(() => Observable.Min(default(IObservable<int?>)));
            Throws<ArgumentNullException>(() => Observable.Min(default(IObservable<double?>)));
            Throws<ArgumentNullException>(() => Observable.Min(default(IObservable<float?>)));
            Throws<ArgumentNullException>(() => Observable.Min(default(IObservable<decimal?>)));
            Throws<ArgumentNullException>(() => Observable.Min(default(IObservable<long?>)));
            
            Throws<ArgumentNullException>(() => Observable.Min(default(IObservable<string>)));
            Throws<ArgumentNullException>(() => Observable.Min(Observable.Return("1"), default(IComparer<string>)));
            Throws<ArgumentNullException>(() => Observable.Min(default(IObservable<string>), Comparer<string>.Default));
        }

        [TestMethod]
        public void Min_Int32_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<int>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void Min_Int32_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, 2),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void Min_Int32_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 3),
                OnNext(220, 2),
                OnNext(230, 4),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, 2),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void Min_Int32_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnError<int>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnError<int>(210, ex)
            );
        }

        [TestMethod]
        public void Min_Int32_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Min_Int64_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1L),
                OnCompleted<long>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<long>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void Min_Int64_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1L),
                OnNext(210, 2L),
                OnCompleted<long>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, 2L),
                OnCompleted<long>(250)
            );
        }

        [TestMethod]
        public void Min_Int64_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1L),
                OnNext(210, 3L),
                OnNext(220, 2L),
                OnNext(230, 4L),
                OnCompleted<long>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, 2L),
                OnCompleted<long>(250)
            );
        }

        [TestMethod]
        public void Min_Int64_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1L),
                OnError<long>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnError<long>(210, ex)
            );
        }

        [TestMethod]
        public void Min_Int64_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1L)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Min_Float_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1f),
                OnCompleted<float>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<float>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void Min_Float_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1f),
                OnNext(210, 2f),
                OnCompleted<float>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, 2f),
                OnCompleted<float>(250)
            );
        }

        [TestMethod]
        public void Min_Float_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1f),
                OnNext(210, 3f),
                OnNext(220, 2f),
                OnNext(230, 4f),
                OnCompleted<float>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, 2f),
                OnCompleted<float>(250)
            );
        }

        [TestMethod]
        public void Min_Float_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1f),
                OnError<float>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnError<float>(210, ex)
            );
        }

        [TestMethod]
        public void Min_Float_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1f)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Min_Double_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1.0),
                OnCompleted<double>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<double>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void Min_Double_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1.0),
                OnNext(210, 2.0),
                OnCompleted<double>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, 2.0),
                OnCompleted<double>(250)
            );
        }

        [TestMethod]
        public void Min_Double_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1.0),
                OnNext(210, 3.0),
                OnNext(220, 2.0),
                OnNext(230, 4.0),
                OnCompleted<double>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, 2.0),
                OnCompleted<double>(250)
            );
        }

        [TestMethod]
        public void Min_Double_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1.0),
                OnError<double>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnError<double>(210, ex)
            );
        }

        [TestMethod]
        public void Min_Double_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1.0)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Min_Decimal_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1m),
                OnCompleted<decimal>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<decimal>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void Min_Decimal_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1m),
                OnNext(210, 2m),
                OnCompleted<decimal>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, 2m),
                OnCompleted<decimal>(250)
            );
        }

        [TestMethod]
        public void Min_Decimal_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1m),
                OnNext(210, 3m),
                OnNext(220, 2m),
                OnNext(230, 4m),
                OnCompleted<decimal>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, 2m),
                OnCompleted<decimal>(250)
            );
        }

        [TestMethod]
        public void Min_Decimal_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1m),
                OnError<decimal>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnError<decimal>(210, ex)
            );
        }

        [TestMethod]
        public void Min_Decimal_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1m)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Min_Nullable_Int32_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnCompleted<int?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext<int?>(250, null),
                OnCompleted<int?>(250)
            );
        }

        [TestMethod]
        public void Min_Nullable_Int32_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnNext(210, (int?)2),
                OnCompleted<int?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, (int?)2),
                OnCompleted<int?>(250)
            );
        }

        [TestMethod]
        public void Min_Nullable_Int32_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnNext(210, (int?)null),
                OnNext(220, (int?)2),
                OnNext(230, (int?)4),
                OnCompleted<int?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, (int?)2),
                OnCompleted<int?>(250)
            );
        }

        [TestMethod]
        public void Min_Nullable_GeneralNullableMinTest_LhsIsNull()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnNext(210, (int?)null),
                OnNext(220, (int?)2),
                OnCompleted<int?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, (int?)2),
                OnCompleted<int?>(250)
            );
        }

        [TestMethod]
        public void Min_Nullable_GeneralNullableMinTest_RhsIsNull()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnNext(210, (int?)2),
                OnNext(220, (int?)null),
                OnCompleted<int?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, (int?)2),
                OnCompleted<int?>(250)
            );
        }

        [TestMethod]
        public void Min_Nullable_GeneralNullableMinTest_Less()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnNext(210, (int?)2),
                OnNext(220, (int?)3),
                OnCompleted<int?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, (int?)2),
                OnCompleted<int?>(250)
            );
        }

        [TestMethod]
        public void Min_Nullable_GeneralNullableMinTest_Greater()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnNext(210, (int?)3),
                OnNext(220, (int?)2),
                OnCompleted<int?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, (int?)2),
                OnCompleted<int?>(250)
            );
        }

        [TestMethod]
        public void Min_Nullable_Int32_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnError<int?>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnError<int?>(210, ex)
            );
        }

        [TestMethod]
        public void Min_Nullable_Int32_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Min_Nullable_Int64_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (long?)1L),
                OnCompleted<long?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext<long?>(250, null),
                OnCompleted<long?>(250)
            );
        }

        [TestMethod]
        public void Min_Nullable_Int64_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (long?)1L),
                OnNext(210, (long?)2L),
                OnCompleted<long?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, (long?)2L),
                OnCompleted<long?>(250)
            );
        }

        [TestMethod]
        public void Min_Nullable_Int64_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (long?)1L),
                OnNext(210, (long?)null),
                OnNext(220, (long?)2L),
                OnNext(230, (long?)4L),
                OnCompleted<long?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, (long?)2L),
                OnCompleted<long?>(250)
            );
        }

        [TestMethod]
        public void Min_Nullable_Int64_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (long?)1L),
                OnError<long?>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnError<long?>(210, ex)
            );
        }

        [TestMethod]
        public void Min_Nullable_Int64_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (long?)1L)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Min_Nullable_Float_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (float?)1f),
                OnCompleted<float?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext<float?>(250, null),
                OnCompleted<float?>(250)
            );
        }

        [TestMethod]
        public void Min_Nullable_Float_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (float?)1f),
                OnNext(210, (float?)2f),
                OnCompleted<float?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, (float?)2f),
                OnCompleted<float?>(250)
            );
        }

        [TestMethod]
        public void Min_Nullable_Float_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (float?)1f),
                OnNext(210, (float?)null),
                OnNext(220, (float?)2f),
                OnNext(230, (float?)4f),
                OnCompleted<float?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, (float?)2f),
                OnCompleted<float?>(250)
            );
        }

        [TestMethod]
        public void Min_Nullable_Float_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (float?)1f),
                OnError<float?>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnError<float?>(210, ex)
            );
        }

        [TestMethod]
        public void Min_Nullable_Float_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (float?)1f)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Min_Nullable_Double_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (double?)1.0),
                OnCompleted<double?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext<double?>(250, null),
                OnCompleted<double?>(250)
            );
        }

        [TestMethod]
        public void Min_Nullable_Double_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (double?)1.0),
                OnNext(210, (double?)2.0),
                OnCompleted<double?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, (double?)2.0),
                OnCompleted<double?>(250)
            );
        }

        [TestMethod]
        public void Min_Nullable_Double_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (double?)1.0),
                OnNext(210, (double?)null),
                OnNext(220, (double?)2.0),
                OnNext(230, (double?)4.0),
                OnCompleted<double?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, (double?)2.0),
                OnCompleted<double?>(250)
            );
        }

        [TestMethod]
        public void Min_Nullable_Double_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (double?)1.0),
                OnError<double?>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnError<double?>(210, ex)
            );
        }

        [TestMethod]
        public void Min_Nullable_Double_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (double?)1.0)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Min_Nullable_Decimal_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (decimal?)1m),
                OnCompleted<decimal?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext<decimal?>(250, null),
                OnCompleted<decimal?>(250)
            );
        }

        [TestMethod]
        public void Min_Nullable_Decimal_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (decimal?)1m),
                OnNext(210, (decimal?)2m),
                OnCompleted<decimal?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, (decimal?)2m),
                OnCompleted<decimal?>(250)
            );
        }

        [TestMethod]
        public void Min_Nullable_Decimal_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (decimal?)1m),
                OnNext(210, (decimal?)null),
                OnNext(220, (decimal?)2m),
                OnNext(230, (decimal?)4m),
                OnCompleted<decimal?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, (decimal?)2m),
                OnCompleted<decimal?>(250)
            );
        }

        [TestMethod]
        public void Min_Nullable_Decimal_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (decimal?)1m),
                OnError<decimal?>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnError<decimal?>(210, ex)
            );
        }

        [TestMethod]
        public void Min_Nullable_Decimal_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (decimal?)1m)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void MinOfT_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, "z"),
                OnCompleted<string>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<string>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void MinOfT_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, "z"),
                OnNext(210, "a"),
                OnCompleted<string>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, "a"),
                OnCompleted<string>(250)
            );
        }

        [TestMethod]
        public void MinOfT_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, "z"),
                OnNext(210, "b"),
                OnNext(220, "c"),
                OnNext(230, "a"),
                OnCompleted<string>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnNext(250, "a"),
                OnCompleted<string>(250)
            );
        }

        [TestMethod]
        public void MinOfT_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, "z"),
                OnError<string>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
                OnError<string>(210, ex)
            );
        }

        [TestMethod]
        public void MinOfT_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, "z")
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void MinOfT_Comparer_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, "z"),
                OnCompleted<string>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min(new ReverseComparer<string>(Comparer<string>.Default))).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<string>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void MinOfT_Comparer_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, "z"),
                OnNext(210, "a"),
                OnCompleted<string>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min(new ReverseComparer<string>(Comparer<string>.Default))).ToArray();
            res.AssertEqual(
                OnNext(250, "a"),
                OnCompleted<string>(250)
            );
        }

        [TestMethod]
        public void MinOfT_Comparer_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, "z"),
                OnNext(210, "b"),
                OnNext(220, "c"),
                OnNext(230, "a"),
                OnCompleted<string>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min(new ReverseComparer<string>(Comparer<string>.Default))).ToArray();
            res.AssertEqual(
                OnNext(250, "c"),
                OnCompleted<string>(250)
            );
        }

        [TestMethod]
        public void MinOfT_Comparer_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, "z"),
                OnError<string>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min(new ReverseComparer<string>(Comparer<string>.Default))).ToArray();
            res.AssertEqual(
                OnError<string>(210, ex)
            );
        }

        [TestMethod]
        public void MinOfT_Comparer_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, "z")
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min(new ReverseComparer<string>(Comparer<string>.Default))).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void MinOfT_ComparerThrows()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, "z"),
                OnNext(210, "b"),
                OnNext(220, "c"),
                OnNext(230, "a"),
                OnCompleted<string>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Min(new ThrowingComparer<string>(ex))).ToArray();
            res.AssertEqual(
                OnError<string>(220, ex)
            );
        }

        [TestMethod]
        public void MinBy_ArgumentChecking()
        {
            var someObservable = Observable.Range(0, 10);

            Throws<ArgumentNullException>(() => Observable.MinBy(default(IObservable<int>), x => x));
            Throws<ArgumentNullException>(() => Observable.MinBy(someObservable, default(Func<int, int>)));
            Throws<ArgumentNullException>(() => Observable.MinBy(default(IObservable<int>), x => x, Comparer<int>.Default));
            Throws<ArgumentNullException>(() => Observable.MinBy(someObservable, default(Func<int, int>), Comparer<int>.Default));
            Throws<ArgumentNullException>(() => Observable.MinBy(someObservable, x => x, null));
        }

        [TestMethod]
        public void MinBy_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z")),
                OnCompleted<KeyValuePair<int, string>>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MinBy(x => x.Key)).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<KeyValuePair<int, string>>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void MinBy_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z")),
                OnNext(210, new KeyValuePair<int, string>(2, "a")),
                OnCompleted<KeyValuePair<int, string>>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MinBy(x => x.Key)).ToArray();
            res.AssertEqual(
                OnNext(250, new KeyValuePair<int, string>(2, "a")),
                OnCompleted<KeyValuePair<int, string>>(250)
            );
        }

        [TestMethod]
        public void MinBy_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z")),
                OnNext(210, new KeyValuePair<int, string>(3, "b")),
                OnNext(220, new KeyValuePair<int, string>(2, "c")),
                OnNext(230, new KeyValuePair<int, string>(4, "a")),
                OnCompleted<KeyValuePair<int, string>>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MinBy(x => x.Key)).ToArray();
            res.AssertEqual(
                OnNext(250, new KeyValuePair<int, string>(2, "c")),
                OnCompleted<KeyValuePair<int, string>>(250)
            );
        }

        [TestMethod]
        public void MinBy_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z")),
                OnError<KeyValuePair<int, string>>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MinBy(x => x.Key)).ToArray();
            res.AssertEqual(
                OnError<KeyValuePair<int, string>>(210, ex)
            );
        }

        [TestMethod]
        public void MinBy_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z"))
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MinBy(x => x.Key)).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void MinBy_Comparer_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z")),
                OnCompleted<KeyValuePair<int, string>>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MinBy(x => x.Key, new ReverseComparer<int>(Comparer<int>.Default))).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<KeyValuePair<int, string>>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void MinBy_Comparer_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z")),
                OnNext(210, new KeyValuePair<int, string>(2, "a")),
                OnCompleted<KeyValuePair<int, string>>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MinBy(x => x.Key, new ReverseComparer<int>(Comparer<int>.Default))).ToArray();
            res.AssertEqual(
                OnNext(250, new KeyValuePair<int, string>(2, "a")),
                OnCompleted<KeyValuePair<int, string>>(250)
            );
        }

        [TestMethod]
        public void MinBy_Comparer_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z")),
                OnNext(210, new KeyValuePair<int, string>(3, "b")),
                OnNext(220, new KeyValuePair<int, string>(20, "c")),
                OnNext(230, new KeyValuePair<int, string>(4, "a")),
                OnCompleted<KeyValuePair<int, string>>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MinBy(x => x.Key, new ReverseComparer<int>(Comparer<int>.Default))).ToArray();
            res.AssertEqual(
                OnNext(250, new KeyValuePair<int, string>(20, "c")),
                OnCompleted<KeyValuePair<int, string>>(250)
            );
        }

        [TestMethod]
        public void MinBy_Comparer_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z")),
                OnError<KeyValuePair<int, string>>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MinBy(x => x.Key, new ReverseComparer<int>(Comparer<int>.Default))).ToArray();
            res.AssertEqual(
                OnError<KeyValuePair<int, string>>(210, ex)
            );
        }

        [TestMethod]
        public void MinBy_Comparer_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z"))
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MinBy(x => x.Key, new ReverseComparer<int>(Comparer<int>.Default))).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void MinBy_SelectorThrows()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z")),
                OnNext(210, new KeyValuePair<int, string>(3, "b")),
                OnNext(220, new KeyValuePair<int, string>(2, "c")),
                OnNext(230, new KeyValuePair<int, string>(4, "a")),
                OnCompleted<KeyValuePair<int, string>>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MinBy<KeyValuePair<int, string>, int>(x => { throw ex; })).ToArray();
            res.AssertEqual(
                OnError<KeyValuePair<int, string>>(210, ex)
            );
        }

        [TestMethod]
        public void MinBy_ComparerThrows()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z")),
                OnNext(210, new KeyValuePair<int, string>(3, "b")),
                OnNext(220, new KeyValuePair<int, string>(2, "c")),
                OnNext(230, new KeyValuePair<int, string>(4, "a")),
                OnCompleted<KeyValuePair<int, string>>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MinBy(x => x.Key, new ThrowingComparer<int>(ex))).ToArray();
            res.AssertEqual(
                OnError<KeyValuePair<int, string>>(220, ex)
            );
        }

        class ReverseComparer<T> : IComparer<T>
        {
            private IComparer<T> _comparer;

            public ReverseComparer(IComparer<T> comparer)
            {
                _comparer = comparer;
            }

            public int Compare(T x, T y)
            {
                return -_comparer.Compare(x, y);
            }
        }

        class ThrowingComparer<T> : IComparer<T>
        {
            private Exception _ex;

            public ThrowingComparer(Exception ex)
	        {
                _ex = ex;
	        }

            public int Compare(T x, T y)
            {
                throw _ex;
            }
        }

        [TestMethod]
        public void Max_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Max(default(IObservable<int>)));
            Throws<ArgumentNullException>(() => Observable.Max(default(IObservable<double>)));
            Throws<ArgumentNullException>(() => Observable.Max(default(IObservable<float>)));
            Throws<ArgumentNullException>(() => Observable.Max(default(IObservable<decimal>)));
            Throws<ArgumentNullException>(() => Observable.Max(default(IObservable<long>)));
            Throws<ArgumentNullException>(() => Observable.Max(default(IObservable<int?>)));
            Throws<ArgumentNullException>(() => Observable.Max(default(IObservable<double?>)));
            Throws<ArgumentNullException>(() => Observable.Max(default(IObservable<float?>)));
            Throws<ArgumentNullException>(() => Observable.Max(default(IObservable<decimal?>)));
            Throws<ArgumentNullException>(() => Observable.Max(default(IObservable<long?>)));

            Throws<ArgumentNullException>(() => Observable.Max(default(IObservable<string>)));
            Throws<ArgumentNullException>(() => Observable.Max(Observable.Return("1"), default(IComparer<string>)));
            Throws<ArgumentNullException>(() => Observable.Max(default(IObservable<string>), Comparer<string>.Default));
        }

        [TestMethod]
        public void Max_Int32_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<int>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void Max_Int32_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, 2),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void Max_Int32_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 3),
                OnNext(220, 4),
                OnNext(230, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, 4),
                OnCompleted<int>(250)
            );
        }

        [TestMethod]
        public void Max_Int32_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnError<int>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnError<int>(210, ex)
            );
        }

        [TestMethod]
        public void Max_Int32_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Max_Int64_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1L),
                OnCompleted<long>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<long>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void Max_Int64_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1L),
                OnNext(210, 2L),
                OnCompleted<long>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, 2L),
                OnCompleted<long>(250)
            );
        }

        [TestMethod]
        public void Max_Int64_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1L),
                OnNext(210, 3L),
                OnNext(220, 4L),
                OnNext(230, 2L),
                OnCompleted<long>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, 4L),
                OnCompleted<long>(250)
            );
        }

        [TestMethod]
        public void Max_Int64_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1L),
                OnError<long>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnError<long>(210, ex)
            );
        }

        [TestMethod]
        public void Max_Int64_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1L)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Max_Float_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1f),
                OnCompleted<float>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<float>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void Max_Float_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1f),
                OnNext(210, 2f),
                OnCompleted<float>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, 2f),
                OnCompleted<float>(250)
            );
        }

        [TestMethod]
        public void Max_Float_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1f),
                OnNext(210, 3f),
                OnNext(220, 4f),
                OnNext(230, 2f),
                OnCompleted<float>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, 4f),
                OnCompleted<float>(250)
            );
        }

        [TestMethod]
        public void Max_Float_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1f),
                OnError<float>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnError<float>(210, ex)
            );
        }

        [TestMethod]
        public void Max_Float_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1f)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Max_Double_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1.0),
                OnCompleted<double>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<double>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void Max_Double_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1.0),
                OnNext(210, 2.0),
                OnCompleted<double>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, 2.0),
                OnCompleted<double>(250)
            );
        }

        [TestMethod]
        public void Max_Double_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1.0),
                OnNext(210, 3.0),
                OnNext(220, 4.0),
                OnNext(230, 2.0),
                OnCompleted<double>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, 4.0),
                OnCompleted<double>(250)
            );
        }

        [TestMethod]
        public void Max_Double_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1.0),
                OnError<double>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnError<double>(210, ex)
            );
        }

        [TestMethod]
        public void Max_Double_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1.0)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Max_Decimal_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1m),
                OnCompleted<decimal>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<decimal>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void Max_Decimal_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1m),
                OnNext(210, 2m),
                OnCompleted<decimal>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, 2m),
                OnCompleted<decimal>(250)
            );
        }

        [TestMethod]
        public void Max_Decimal_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1m),
                OnNext(210, 3m),
                OnNext(220, 4m),
                OnNext(230, 2m),
                OnCompleted<decimal>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, 4m),
                OnCompleted<decimal>(250)
            );
        }

        [TestMethod]
        public void Max_Decimal_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1m),
                OnError<decimal>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnError<decimal>(210, ex)
            );
        }

        [TestMethod]
        public void Max_Decimal_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1m)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Max_Nullable_Int32_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnCompleted<int?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext<int?>(250, null),
                OnCompleted<int?>(250)
            );
        }

        [TestMethod]
        public void Max_Nullable_Int32_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnNext(210, (int?)2),
                OnCompleted<int?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, (int?)2),
                OnCompleted<int?>(250)
            );
        }

        [TestMethod]
        public void Max_Nullable_Int32_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnNext(210, (int?)null),
                OnNext(220, (int?)4),
                OnNext(230, (int?)2),
                OnCompleted<int?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, (int?)4),
                OnCompleted<int?>(250)
            );
        }

        [TestMethod]
        public void Max_Nullable_GeneralNullableMaxTest_LhsIsNull()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnNext(210, (int?)null),
                OnNext(220, (int?)2),
                OnCompleted<int?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, (int?)2),
                OnCompleted<int?>(250)
            );
        }

        [TestMethod]
        public void Max_Nullable_GeneralNullableMaxTest_RhsIsNull()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnNext(210, (int?)2),
                OnNext(220, (int?)null),
                OnCompleted<int?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, (int?)2),
                OnCompleted<int?>(250)
            );
        }

        [TestMethod]
        public void Max_Nullable_GeneralNullableMaxTest_Less()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnNext(210, (int?)2),
                OnNext(220, (int?)3),
                OnCompleted<int?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, (int?)3),
                OnCompleted<int?>(250)
            );
        }

        [TestMethod]
        public void Max_Nullable_GeneralNullableMaxTest_Greater()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnNext(210, (int?)3),
                OnNext(220, (int?)2),
                OnCompleted<int?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, (int?)3),
                OnCompleted<int?>(250)
            );
        }

        [TestMethod]
        public void Max_Nullable_Int32_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnError<int?>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnError<int?>(210, ex)
            );
        }

        [TestMethod]
        public void Max_Nullable_Int32_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Max_Nullable_Int64_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (long?)1L),
                OnCompleted<long?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext<long?>(250, null),
                OnCompleted<long?>(250)
            );
        }

        [TestMethod]
        public void Max_Nullable_Int64_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (long?)1L),
                OnNext(210, (long?)2L),
                OnCompleted<long?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, (long?)2L),
                OnCompleted<long?>(250)
            );
        }

        [TestMethod]
        public void Max_Nullable_Int64_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (long?)1L),
                OnNext(210, (long?)null),
                OnNext(220, (long?)4L),
                OnNext(230, (long?)2L),
                OnCompleted<long?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, (long?)4L),
                OnCompleted<long?>(250)
            );
        }

        [TestMethod]
        public void Max_Nullable_Int64_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (long?)1L),
                OnError<long?>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnError<long?>(210, ex)
            );
        }

        [TestMethod]
        public void Max_Nullable_Int64_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (long?)1L)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Max_Nullable_Float_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (float?)1f),
                OnCompleted<float?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext<float?>(250, null),
                OnCompleted<float?>(250)
            );
        }

        [TestMethod]
        public void Max_Nullable_Float_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (float?)1f),
                OnNext(210, (float?)2f),
                OnCompleted<float?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, (float?)2f),
                OnCompleted<float?>(250)
            );
        }

        [TestMethod]
        public void Max_Nullable_Float_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (float?)1f),
                OnNext(210, (float?)null),
                OnNext(220, (float?)4f),
                OnNext(230, (float?)2f),
                OnCompleted<float?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, (float?)4f),
                OnCompleted<float?>(250)
            );
        }

        [TestMethod]
        public void Max_Nullable_Float_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (float?)1f),
                OnError<float?>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnError<float?>(210, ex)
            );
        }

        [TestMethod]
        public void Max_Nullable_Float_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (float?)1f)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Max_Nullable_Double_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (double?)1.0),
                OnCompleted<double?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext<double?>(250, null),
                OnCompleted<double?>(250)
            );
        }

        [TestMethod]
        public void Max_Nullable_Double_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (double?)1.0),
                OnNext(210, (double?)2.0),
                OnCompleted<double?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, (double?)2.0),
                OnCompleted<double?>(250)
            );
        }

        [TestMethod]
        public void Max_Nullable_Double_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (double?)1.0),
                OnNext(210, (double?)null),
                OnNext(220, (double?)4.0),
                OnNext(230, (double?)2.0),
                OnCompleted<double?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, (double?)4.0),
                OnCompleted<double?>(250)
            );
        }

        [TestMethod]
        public void Max_Nullable_Double_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (double?)1.0),
                OnError<double?>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnError<double?>(210, ex)
            );
        }

        [TestMethod]
        public void Max_Nullable_Double_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (double?)1.0)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Max_Nullable_Decimal_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (decimal?)1m),
                OnCompleted<decimal?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext<decimal?>(250, null),
                OnCompleted<decimal?>(250)
            );
        }

        [TestMethod]
        public void Max_Nullable_Decimal_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (decimal?)1m),
                OnNext(210, (decimal?)2m),
                OnCompleted<decimal?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, (decimal?)2m),
                OnCompleted<decimal?>(250)
            );
        }

        [TestMethod]
        public void Max_Nullable_Decimal_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (decimal?)1m),
                OnNext(210, (decimal?)null),
                OnNext(220, (decimal?)4m),
                OnNext(230, (decimal?)2m),
                OnCompleted<decimal?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, (decimal?)4m),
                OnCompleted<decimal?>(250)
            );
        }

        [TestMethod]
        public void Max_Nullable_Decimal_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (decimal?)1m),
                OnError<decimal?>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnError<decimal?>(210, ex)
            );
        }

        [TestMethod]
        public void Max_Nullable_Decimal_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (decimal?)1m)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void MaxOfT_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, "z"),
                OnCompleted<string>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<string>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void MaxOfT_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, "z"),
                OnNext(210, "a"),
                OnCompleted<string>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, "a"),
                OnCompleted<string>(250)
            );
        }

        [TestMethod]
        public void MaxOfT_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, "z"),
                OnNext(210, "b"),
                OnNext(220, "c"),
                OnNext(230, "a"),
                OnCompleted<string>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnNext(250, "c"),
                OnCompleted<string>(250)
            );
        }

        [TestMethod]
        public void MaxOfT_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, "z"),
                OnError<string>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
                OnError<string>(210, ex)
            );
        }

        [TestMethod]
        public void MaxOfT_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, "z")
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void MaxOfT_Comparer_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, "z"),
                OnCompleted<string>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max(new ReverseComparer<string>(Comparer<string>.Default))).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<string>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void MaxOfT_Comparer_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, "z"),
                OnNext(210, "a"),
                OnCompleted<string>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max(new ReverseComparer<string>(Comparer<string>.Default))).ToArray();
            res.AssertEqual(
                OnNext(250, "a"),
                OnCompleted<string>(250)
            );
        }

        [TestMethod]
        public void MaxOfT_Comparer_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, "z"),
                OnNext(210, "b"),
                OnNext(220, "c"),
                OnNext(230, "a"),
                OnCompleted<string>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max(new ReverseComparer<string>(Comparer<string>.Default))).ToArray();
            res.AssertEqual(
                OnNext(250, "a"),
                OnCompleted<string>(250)
            );
        }

        [TestMethod]
        public void MaxOfT_Comparer_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, "z"),
                OnError<string>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max(new ReverseComparer<string>(Comparer<string>.Default))).ToArray();
            res.AssertEqual(
                OnError<string>(210, ex)
            );
        }

        [TestMethod]
        public void MaxOfT_Comparer_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, "z")
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max(new ReverseComparer<string>(Comparer<string>.Default))).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void MaxOfT_ComparerThrows()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, "z"),
                OnNext(210, "b"),
                OnNext(220, "c"),
                OnNext(230, "a"),
                OnCompleted<string>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Max(new ThrowingComparer<string>(ex))).ToArray();
            res.AssertEqual(
                OnError<string>(220, ex)
            );
        }

        [TestMethod]
        public void MaxBy_ArgumentChecking()
        {
            var someObservable = Observable.Range(0, 10);

            Throws<ArgumentNullException>(() => Observable.MaxBy(default(IObservable<int>), x => x));
            Throws<ArgumentNullException>(() => Observable.MaxBy(someObservable, default(Func<int, int>)));
            Throws<ArgumentNullException>(() => Observable.MaxBy(default(IObservable<int>), x => x, Comparer<int>.Default));
            Throws<ArgumentNullException>(() => Observable.MaxBy(someObservable, default(Func<int, int>), Comparer<int>.Default));
            Throws<ArgumentNullException>(() => Observable.MaxBy(someObservable, x => x, null));
        }

        [TestMethod]
        public void MaxBy_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z")),
                OnCompleted<KeyValuePair<int, string>>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MaxBy(x => x.Key)).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<KeyValuePair<int, string>>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void MaxBy_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z")),
                OnNext(210, new KeyValuePair<int, string>(2, "a")),
                OnCompleted<KeyValuePair<int, string>>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MaxBy(x => x.Key)).ToArray();
            res.AssertEqual(
                OnNext(250, new KeyValuePair<int, string>(2, "a")),
                OnCompleted<KeyValuePair<int, string>>(250)
            );
        }

        [TestMethod]
        public void MaxBy_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z")),
                OnNext(210, new KeyValuePair<int, string>(3, "b")),
                OnNext(220, new KeyValuePair<int, string>(4, "c")),
                OnNext(230, new KeyValuePair<int, string>(2, "a")),
                OnCompleted<KeyValuePair<int, string>>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MaxBy(x => x.Key)).ToArray();
            res.AssertEqual(
                OnNext(250, new KeyValuePair<int, string>(4, "c")),
                OnCompleted<KeyValuePair<int, string>>(250)
            );
        }

        [TestMethod]
        public void MaxBy_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z")),
                OnError<KeyValuePair<int, string>>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MaxBy(x => x.Key)).ToArray();
            res.AssertEqual(
                OnError<KeyValuePair<int, string>>(210, ex)
            );
        }

        [TestMethod]
        public void MaxBy_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z"))
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MaxBy(x => x.Key)).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void MaxBy_Comparer_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z")),
                OnCompleted<KeyValuePair<int, string>>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MaxBy(x => x.Key, new ReverseComparer<int>(Comparer<int>.Default))).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<KeyValuePair<int, string>>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void MaxBy_Comparer_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z")),
                OnNext(210, new KeyValuePair<int, string>(2, "a")),
                OnCompleted<KeyValuePair<int, string>>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MaxBy(x => x.Key, new ReverseComparer<int>(Comparer<int>.Default))).ToArray();
            res.AssertEqual(
                OnNext(250, new KeyValuePair<int, string>(2, "a")),
                OnCompleted<KeyValuePair<int, string>>(250)
            );
        }

        [TestMethod]
        public void MaxBy_Comparer_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z")),
                OnNext(210, new KeyValuePair<int, string>(3, "b")),
                OnNext(220, new KeyValuePair<int, string>(4, "c")),
                OnNext(230, new KeyValuePair<int, string>(2, "a")),
                OnCompleted<KeyValuePair<int, string>>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MaxBy(x => x.Key, new ReverseComparer<int>(Comparer<int>.Default))).ToArray();
            res.AssertEqual(
                OnNext(250, new KeyValuePair<int, string>(2, "a")),
                OnCompleted<KeyValuePair<int, string>>(250)
            );
        }

        [TestMethod]
        public void MaxBy_Comparer_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z")),
                OnError<KeyValuePair<int, string>>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MaxBy(x => x.Key, new ReverseComparer<int>(Comparer<int>.Default))).ToArray();
            res.AssertEqual(
                OnError<KeyValuePair<int, string>>(210, ex)
            );
        }

        [TestMethod]
        public void MaxBy_Comparer_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z"))
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MaxBy(x => x.Key, new ReverseComparer<int>(Comparer<int>.Default))).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void MaxBy_SelectorThrows()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z")),
                OnNext(210, new KeyValuePair<int, string>(3, "b")),
                OnNext(220, new KeyValuePair<int, string>(2, "c")),
                OnNext(230, new KeyValuePair<int, string>(4, "a")),
                OnCompleted<KeyValuePair<int, string>>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MaxBy<KeyValuePair<int, string>, int>(x => { throw ex; })).ToArray();
            res.AssertEqual(
                OnError<KeyValuePair<int, string>>(210, ex)
            );
        }

        [TestMethod]
        public void MaxBy_ComparerThrows()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, new KeyValuePair<int, string>(1, "z")),
                OnNext(210, new KeyValuePair<int, string>(3, "b")),
                OnNext(220, new KeyValuePair<int, string>(2, "c")),
                OnNext(230, new KeyValuePair<int, string>(4, "a")),
                OnCompleted<KeyValuePair<int, string>>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.MaxBy(x => x.Key, new ThrowingComparer<int>(ex))).ToArray();
            res.AssertEqual(
                OnError<KeyValuePair<int, string>>(220, ex)
            );
        }

        [TestMethod]
        public void Average_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Average(default(IObservable<int>)));
            Throws<ArgumentNullException>(() => Observable.Average(default(IObservable<double>)));
            Throws<ArgumentNullException>(() => Observable.Average(default(IObservable<float>)));
            Throws<ArgumentNullException>(() => Observable.Average(default(IObservable<decimal>)));
            Throws<ArgumentNullException>(() => Observable.Average(default(IObservable<long>)));
            Throws<ArgumentNullException>(() => Observable.Average(default(IObservable<int?>)));
            Throws<ArgumentNullException>(() => Observable.Average(default(IObservable<double?>)));
            Throws<ArgumentNullException>(() => Observable.Average(default(IObservable<float?>)));
            Throws<ArgumentNullException>(() => Observable.Average(default(IObservable<decimal?>)));
            Throws<ArgumentNullException>(() => Observable.Average(default(IObservable<long?>)));
        }

        [TestMethod]
        public void Average_Int32_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<double>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void Average_Int32_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, 2.0),
                OnCompleted<double>(250)
            );
        }

        [TestMethod]
        public void Average_Int32_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnNext(210, 3),
                OnNext(220, 4),
                OnNext(230, 2),
                OnCompleted<int>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, 3.0),
                OnCompleted<double>(250)
            );
        }

        [TestMethod]
        public void Average_Int32_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1),
                OnError<int>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnError<double>(210, ex)
            );
        }

        [TestMethod]
        public void Average_Int32_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Average_Int64_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1L),
                OnCompleted<long>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<double>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void Average_Int64_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1L),
                OnNext(210, 2L),
                OnCompleted<long>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, 2.0),
                OnCompleted<double>(250)
            );
        }

        [TestMethod]
        public void Average_Int64_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1L),
                OnNext(210, 3L),
                OnNext(220, 4L),
                OnNext(230, 2L),
                OnCompleted<long>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, 3.0),
                OnCompleted<double>(250)
            );
        }

        [TestMethod]
        public void Average_Int64_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1L),
                OnError<long>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnError<double>(210, ex)
            );
        }

        [TestMethod]
        public void Average_Int64_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1L)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Average_Double_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1.0),
                OnCompleted<double>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<double>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void Average_Double_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1.0),
                OnNext(210, 2.0),
                OnCompleted<double>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, 2.0),
                OnCompleted<double>(250)
            );
        }

        [TestMethod]
        public void Average_Double_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1.0),
                OnNext(210, 3.0),
                OnNext(220, 4.0),
                OnNext(230, 2.0),
                OnCompleted<double>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, 3.0),
                OnCompleted<double>(250)
            );
        }

        [TestMethod]
        public void Average_Double_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1.0),
                OnError<double>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnError<double>(210, ex)
            );
        }

        [TestMethod]
        public void Average_Double_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1.0)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Average_Float_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1f),
                OnCompleted<float>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<float>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void Average_Float_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1f),
                OnNext(210, 2f),
                OnCompleted<float>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, 2f),
                OnCompleted<float>(250)
            );
        }

        [TestMethod]
        public void Average_Float_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1f),
                OnNext(210, 3f),
                OnNext(220, 4f),
                OnNext(230, 2f),
                OnCompleted<float>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, 3f),
                OnCompleted<float>(250)
            );
        }

        [TestMethod]
        public void Average_Float_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1f),
                OnError<float>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnError<float>(210, ex)
            );
        }

        [TestMethod]
        public void Average_Float_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1f)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Average_Decimal_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1m),
                OnCompleted<decimal>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            Assert.AreEqual(1, res.Length);
            Assert.IsTrue(res[0].Value.Kind == NotificationKind.OnError && ((Notification<decimal>.OnError)res[0].Value).Exception is InvalidOperationException);
            Assert.IsTrue(res[0].Time == 250);
        }

        [TestMethod]
        public void Average_Decimal_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1m),
                OnNext(210, 2m),
                OnCompleted<decimal>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, 2m),
                OnCompleted<decimal>(250)
            );
        }

        [TestMethod]
        public void Average_Decimal_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1m),
                OnNext(210, 3m),
                OnNext(220, 4m),
                OnNext(230, 2m),
                OnCompleted<decimal>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, 3m),
                OnCompleted<decimal>(250)
            );
        }

        [TestMethod]
        public void Average_Decimal_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1m),
                OnError<decimal>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnError<decimal>(210, ex)
            );
        }

        [TestMethod]
        public void Average_Decimal_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, 1m)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Average_Nullable_Int32_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnCompleted<int?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, (double?)null),
                OnCompleted<double?>(250)
            );
        }

        [TestMethod]
        public void Average_Nullable_Int32_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnNext(210, (int?)2),
                OnCompleted<int?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, (double?)2.0),
                OnCompleted<double?>(250)
            );
        }

        [TestMethod]
        public void Average_Nullable_Int32_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnNext(210, (int?)3),
                OnNext(220, (int?)null),
                OnNext(230, (int?)2),
                OnCompleted<int?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, (double?)2.5),
                OnCompleted<double?>(250)
            );
        }

        [TestMethod]
        public void Average_Nullable_Int32_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1),
                OnError<int?>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnError<double?>(210, ex)
            );
        }

        [TestMethod]
        public void Average_Nullable_Int32_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (int?)1)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Average_Nullable_Int64_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (long?)1L),
                OnCompleted<long?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, (double?)null),
                OnCompleted<double?>(250)
            );
        }

        [TestMethod]
        public void Average_Nullable_Int64_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (long?)1L),
                OnNext(210, (long?)2L),
                OnCompleted<long?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, (double?)2.0),
                OnCompleted<double?>(250)
            );
        }

        [TestMethod]
        public void Average_Nullable_Int64_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (long?)1L),
                OnNext(210, (long?)3L),
                OnNext(220, (long?)null),
                OnNext(230, (long?)2L),
                OnCompleted<long?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, (double?)2.5),
                OnCompleted<double?>(250)
            );
        }

        [TestMethod]
        public void Average_Nullable_Int64_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (long?)1L),
                OnError<long?>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnError<double?>(210, ex)
            );
        }

        [TestMethod]
        public void Average_Nullable_Int64_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (long?)1L)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Average_Nullable_Double_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (double?)1.0),
                OnCompleted<double?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, (double?)null),
                OnCompleted<double?>(250)
            );
        }

        [TestMethod]
        public void Average_Nullable_Double_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (double?)1.0),
                OnNext(210, (double?)2.0),
                OnCompleted<double?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, (double?)2.0),
                OnCompleted<double?>(250)
            );
        }

        [TestMethod]
        public void Average_Nullable_Double_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (double?)1.0),
                OnNext(210, (double?)3.0),
                OnNext(220, (double?)null),
                OnNext(230, (double?)2.0),
                OnCompleted<double?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, (double?)2.5),
                OnCompleted<double?>(250)
            );
        }

        [TestMethod]
        public void Average_Nullable_Double_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (double?)1.0),
                OnError<double?>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnError<double?>(210, ex)
            );
        }

        [TestMethod]
        public void Average_Nullable_Double_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (double?)1.0)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Average_Nullable_Float_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (float?)1f),
                OnCompleted<float?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, (float?)null),
                OnCompleted<float?>(250)
            );
        }

        [TestMethod]
        public void Average_Nullable_Float_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (float?)1f),
                OnNext(210, (float?)2f),
                OnCompleted<float?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, (float?)2f),
                OnCompleted<float?>(250)
            );
        }

        [TestMethod]
        public void Average_Nullable_Float_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (float?)1f),
                OnNext(210, (float?)3f),
                OnNext(220, (float?)null),
                OnNext(230, (float?)2f),
                OnCompleted<float?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, (float?)2.5f),
                OnCompleted<float?>(250)
            );
        }

        [TestMethod]
        public void Average_Nullable_Float_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (float?)1f),
                OnError<float?>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnError<float?>(210, ex)
            );
        }

        [TestMethod]
        public void Average_Nullable_Float_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (float?)1f)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
            );
        }

        [TestMethod]
        public void Average_Nullable_Decimal_Empty()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (decimal?)1m),
                OnCompleted<decimal?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, (decimal?)null),
                OnCompleted<decimal?>(250)
            );
        }

        [TestMethod]
        public void Average_Nullable_Decimal_Return()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (decimal?)1m),
                OnNext(210, (decimal?)2m),
                OnCompleted<decimal?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, (decimal?)2m),
                OnCompleted<decimal?>(250)
            );
        }

        [TestMethod]
        public void Average_Nullable_Decimal_Some()
        {
            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (decimal?)1m),
                OnNext(210, (decimal?)3m),
                OnNext(220, (decimal?)null),
                OnNext(230, (decimal?)2m),
                OnCompleted<decimal?>(250)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnNext(250, (decimal?)2.5m),
                OnCompleted<decimal?>(250)
            );
        }

        [TestMethod]
        public void Average_Nullable_Decimal_Throw()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (decimal?)1m),
                OnError<decimal?>(210, ex)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
                OnError<decimal?>(210, ex)
            );
        }

        [TestMethod]
        public void Average_Nullable_Decimal_Never()
        {
            var ex = new Exception();

            var scheduler = new TestScheduler();

            var msgs = new[] {
                OnNext(150, (decimal?)1m)
            };

            var xs = scheduler.CreateHotObservable(msgs);

            var res = scheduler.Run(() => xs.Average()).ToArray();
            res.AssertEqual(
            );
        }
    }
}
