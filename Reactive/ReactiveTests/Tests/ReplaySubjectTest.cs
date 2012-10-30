using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Collections.Generic;
using ReactiveTests.Mocks;
using ReactiveTests.Dummies;

namespace ReactiveTests.Tests
{
    [TestClass]
    public partial class ReplaySubjectTest : Test
    {
        [TestMethod]
        public void Subscribe_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => new ReplaySubject<int>().Subscribe(null));
        }

        [TestMethod]
        public void OnError_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => new ReplaySubject<int>(DummyScheduler.Instance).OnError(null));
        }

        [TestMethod]
        public void Constructor_ArgumentChecking()
        {
            Throws<ArgumentOutOfRangeException>(() => new ReplaySubject<int>(-1));
            Throws<ArgumentOutOfRangeException>(() => new ReplaySubject<int>(-1, DummyScheduler.Instance));
            Throws<ArgumentOutOfRangeException>(() => new ReplaySubject<int>(-1, TimeSpan.Zero));
            Throws<ArgumentOutOfRangeException>(() => new ReplaySubject<int>(-1, TimeSpan.Zero, DummyScheduler.Instance));

            Throws<ArgumentOutOfRangeException>(() => new ReplaySubject<int>(TimeSpan.FromTicks(-1)));
            Throws<ArgumentOutOfRangeException>(() => new ReplaySubject<int>(TimeSpan.FromTicks(-1), DummyScheduler.Instance));
            Throws<ArgumentOutOfRangeException>(() => new ReplaySubject<int>(0, TimeSpan.FromTicks(-1)));
            Throws<ArgumentOutOfRangeException>(() => new ReplaySubject<int>(0, TimeSpan.FromTicks(-1), DummyScheduler.Instance));

            Throws<ArgumentNullException>(() => new ReplaySubject<int>(null));
            Throws<ArgumentNullException>(() => new ReplaySubject<int>(0, null));
            Throws<ArgumentNullException>(() => new ReplaySubject<int>(TimeSpan.Zero, null));
            Throws<ArgumentNullException>(() => new ReplaySubject<int>(0, TimeSpan.Zero, null));

            // zero allowed
            new ReplaySubject<int>(0);
            new ReplaySubject<int>(TimeSpan.Zero);
            new ReplaySubject<int>(0, TimeSpan.Zero);
            new ReplaySubject<int>(0, DummyScheduler.Instance);
            new ReplaySubject<int>(TimeSpan.Zero, DummyScheduler.Instance);
            new ReplaySubject<int>(0, TimeSpan.Zero, DummyScheduler.Instance);
        }

        [TestMethod]
        public void Infinite()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 1),
                OnNext(110, 2),
                OnNext(220, 3),
                OnNext(270, 4),
                OnNext(340, 5),
                OnNext(410, 6),
                OnNext(520, 7),
                OnNext(630, 8),
                OnNext(710, 9),
                OnNext(870, 10),
                OnNext(940, 11),
                OnNext(1020, 12)
                );

            var subject = default(ReplaySubject<int>);
            var subscription = default(IDisposable);

            var results1 = new MockObserver<int>(scheduler);
            var subscription1 = default(IDisposable);

            var results2 = new MockObserver<int>(scheduler);
            var subscription2 = default(IDisposable);

            var results3 = new MockObserver<int>(scheduler);
            var subscription3 = default(IDisposable);

            scheduler.Schedule(() => subject = new ReplaySubject<int>(3, TimeSpan.FromTicks(100), scheduler), 100);
            scheduler.Schedule(() => subscription = xs.Subscribe(subject), 200);
            scheduler.Schedule(() => subscription.Dispose(), 1000);

            scheduler.Schedule(() => subscription1 = subject.Subscribe(results1), 300);
            scheduler.Schedule(() => subscription2 = subject.Subscribe(results2), 400);
            scheduler.Schedule(() => subscription3 = subject.Subscribe(results3), 900);

            scheduler.Schedule(() => subscription1.Dispose(), 600);
            scheduler.Schedule(() => subscription2.Dispose(), 700);
            scheduler.Schedule(() => subscription1.Dispose(), 800);
            scheduler.Schedule(() => subscription3.Dispose(), 950);

            scheduler.Run();

            results1.AssertEqual(
                OnNext(301, 3),
                OnNext(302, 4),
                OnNext(340, 5),
                OnNext(410, 6),
                OnNext(520, 7)
                );

            results2.AssertEqual(
                OnNext(401, 5),
                OnNext(410, 6),
                OnNext(520, 7),
                OnNext(630, 8)
                );

            results3.AssertEqual(
                OnNext(901, 10),
                OnNext(940, 11)
                );
        }

        [TestMethod]
        public void Infinite2()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 1),
                OnNext(110, 2),
                OnNext(220, 3),
                OnNext(270, 4),
                OnNext(280, -1),
                OnNext(290, -2),
                OnNext(340, 5),
                OnNext(410, 6),
                OnNext(520, 7),
                OnNext(630, 8),
                OnNext(710, 9),
                OnNext(870, 10),
                OnNext(940, 11),
                OnNext(1020, 12)
                );

            var subject = default(ReplaySubject<int>);
            var subscription = default(IDisposable);

            var results1 = new MockObserver<int>(scheduler);
            var subscription1 = default(IDisposable);

            var results2 = new MockObserver<int>(scheduler);
            var subscription2 = default(IDisposable);

            var results3 = new MockObserver<int>(scheduler);
            var subscription3 = default(IDisposable);

            scheduler.Schedule(() => subject = new ReplaySubject<int>(3, TimeSpan.FromTicks(100), scheduler), 100);
            scheduler.Schedule(() => subscription = xs.Subscribe(subject), 200);
            scheduler.Schedule(() => subscription.Dispose(), 1000);

            scheduler.Schedule(() => subscription1 = subject.Subscribe(results1), 300);
            scheduler.Schedule(() => subscription2 = subject.Subscribe(results2), 400);
            scheduler.Schedule(() => subscription3 = subject.Subscribe(results3), 900);

            scheduler.Schedule(() => subscription1.Dispose(), 600);
            scheduler.Schedule(() => subscription2.Dispose(), 700);
            scheduler.Schedule(() => subscription1.Dispose(), 800);
            scheduler.Schedule(() => subscription3.Dispose(), 950);

            scheduler.Run();

            results1.AssertEqual(
                OnNext(301, 4),
                OnNext(302, -1),
                OnNext(303, -2),
                OnNext(340, 5),
                OnNext(410, 6),
                OnNext(520, 7)
                );

            results2.AssertEqual(
                OnNext(401, 5),
                OnNext(410, 6),
                OnNext(520, 7),
                OnNext(630, 8)
                );

            results3.AssertEqual(
                OnNext(901, 10),
                OnNext(940, 11)
                );
        }

        [TestMethod]
        public void Finite()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 1),
                OnNext(110, 2),
                OnNext(220, 3),
                OnNext(270, 4),
                OnNext(340, 5),
                OnNext(410, 6),
                OnNext(520, 7),
                OnCompleted<int>(630),
                OnNext(640, 9),
                OnCompleted<int>(650),
                OnError<int>(660, new MockException(1))
                );

            var subject = default(ReplaySubject<int>);
            var subscription = default(IDisposable);

            var results1 = new MockObserver<int>(scheduler);
            var subscription1 = default(IDisposable);

            var results2 = new MockObserver<int>(scheduler);
            var subscription2 = default(IDisposable);

            var results3 = new MockObserver<int>(scheduler);
            var subscription3 = default(IDisposable);

            scheduler.Schedule(() => subject = new ReplaySubject<int>(3, TimeSpan.FromTicks(100), scheduler), 100);
            scheduler.Schedule(() => subscription = xs.Subscribe(subject), 200);
            scheduler.Schedule(() => subscription.Dispose(), 1000);

            scheduler.Schedule(() => subscription1 = subject.Subscribe(results1), 300);
            scheduler.Schedule(() => subscription2 = subject.Subscribe(results2), 400);
            scheduler.Schedule(() => subscription3 = subject.Subscribe(results3), 900);

            scheduler.Schedule(() => subscription1.Dispose(), 600);
            scheduler.Schedule(() => subscription2.Dispose(), 700);
            scheduler.Schedule(() => subscription1.Dispose(), 800);
            scheduler.Schedule(() => subscription3.Dispose(), 950);

            scheduler.Run();

            results1.AssertEqual(
                OnNext(301, 3),
                OnNext(302, 4),
                OnNext(340, 5),
                OnNext(410, 6),
                OnNext(520, 7)
                );

            results2.AssertEqual(
                OnNext(401, 5),
                OnNext(410, 6),
                OnNext(520, 7),
                OnCompleted<int>(630)
                );

            results3.AssertEqual(
                );
        }

        [TestMethod]
        public void Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(70, 1),
                OnNext(110, 2),
                OnNext(220, 3),
                OnNext(270, 4),
                OnNext(340, 5),
                OnNext(410, 6),
                OnNext(520, 7),
                OnError<int>(630, new MockException(30)),
                OnNext(640, 9),
                OnCompleted<int>(650),
                OnError<int>(660, new MockException(1))
                );

            var subject = default(ReplaySubject<int>);
            var subscription = default(IDisposable);

            var results1 = new MockObserver<int>(scheduler);
            var subscription1 = default(IDisposable);

            var results2 = new MockObserver<int>(scheduler);
            var subscription2 = default(IDisposable);

            var results3 = new MockObserver<int>(scheduler);
            var subscription3 = default(IDisposable);

            scheduler.Schedule(() => subject = new ReplaySubject<int>(3, TimeSpan.FromTicks(100), scheduler), 100);
            scheduler.Schedule(() => subscription = xs.Subscribe(subject), 200);
            scheduler.Schedule(() => subscription.Dispose(), 1000);

            scheduler.Schedule(() => subscription1 = subject.Subscribe(results1), 300);
            scheduler.Schedule(() => subscription2 = subject.Subscribe(results2), 400);
            scheduler.Schedule(() => subscription3 = subject.Subscribe(results3), 900);

            scheduler.Schedule(() => subscription1.Dispose(), 600);
            scheduler.Schedule(() => subscription2.Dispose(), 700);
            scheduler.Schedule(() => subscription1.Dispose(), 800);
            scheduler.Schedule(() => subscription3.Dispose(), 950);

            scheduler.Run();

            results1.AssertEqual(
                OnNext(301, 3),
                OnNext(302, 4),
                OnNext(340, 5),
                OnNext(410, 6),
                OnNext(520, 7)
                );

            results2.AssertEqual(
                OnNext(401, 5),
                OnNext(410, 6),
                OnNext(520, 7),
                OnError<int>(630, new MockException(30))
                );

            results3.AssertEqual(
                );
        }

        [TestMethod]
        public void Canceled()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnCompleted<int>(630),
                OnNext(640, 9),
                OnCompleted<int>(650),
                OnError<int>(660, new MockException(1))
                );

            var subject = default(ReplaySubject<int>);
            var subscription = default(IDisposable);

            var results1 = new MockObserver<int>(scheduler);
            var subscription1 = default(IDisposable);

            var results2 = new MockObserver<int>(scheduler);
            var subscription2 = default(IDisposable);

            var results3 = new MockObserver<int>(scheduler);
            var subscription3 = default(IDisposable);

            scheduler.Schedule(() => subject = new ReplaySubject<int>(3, TimeSpan.FromTicks(100), scheduler), 100);
            scheduler.Schedule(() => subscription = xs.Subscribe(subject), 200);
            scheduler.Schedule(() => subscription.Dispose(), 1000);

            scheduler.Schedule(() => subscription1 = subject.Subscribe(results1), 300);
            scheduler.Schedule(() => subscription2 = subject.Subscribe(results2), 400);
            scheduler.Schedule(() => subscription3 = subject.Subscribe(results3), 900);

            scheduler.Schedule(() => subscription1.Dispose(), 600);
            scheduler.Schedule(() => subscription2.Dispose(), 700);
            scheduler.Schedule(() => subscription1.Dispose(), 800);
            scheduler.Schedule(() => subscription3.Dispose(), 950);

            scheduler.Run();

            results1.AssertEqual(
                );

            results2.AssertEqual(
                OnCompleted<int>(630)
                );

            results3.AssertEqual(
                );
        }
    }
}
