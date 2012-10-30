using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

namespace ReactiveTests.Tests
{
    [TestClass]
    public partial class AsyncSubjectTest : Test
    {
        [TestMethod]
        public void Constructor_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => new AsyncSubject<int>(null));
        }

        [TestMethod]
        public void Subscribe_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => new AsyncSubject<int>().Subscribe(null));
        }

        [TestMethod]
        public void OnError_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => new AsyncSubject<int>().OnError(null));
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

            var subject = default(AsyncSubject<int>);
            var subscription = default(IDisposable);

            var results1 = new MockObserver<int>(scheduler);
            var subscription1 = default(IDisposable);

            var results2 = new MockObserver<int>(scheduler);
            var subscription2 = default(IDisposable);

            var results3 = new MockObserver<int>(scheduler);
            var subscription3 = default(IDisposable);

            scheduler.Schedule(() => subject = new AsyncSubject<int>(scheduler), 100);
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
                );

            results3.AssertEqual(
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

            var subject = default(AsyncSubject<int>);
            var subscription = default(IDisposable);

            var results1 = new MockObserver<int>(scheduler);
            var subscription1 = default(IDisposable);

            var results2 = new MockObserver<int>(scheduler);
            var subscription2 = default(IDisposable);

            var results3 = new MockObserver<int>(scheduler);
            var subscription3 = default(IDisposable);

            scheduler.Schedule(() => subject = new AsyncSubject<int>(scheduler), 100);
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
                OnNext(630, 7),
                OnCompleted<int>(630)
                );

            results3.AssertEqual(
                OnNext(901, 7),
                OnCompleted<int>(901)
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

            var subject = default(AsyncSubject<int>);
            var subscription = default(IDisposable);

            var results1 = new MockObserver<int>(scheduler);
            var subscription1 = default(IDisposable);

            var results2 = new MockObserver<int>(scheduler);
            var subscription2 = default(IDisposable);

            var results3 = new MockObserver<int>(scheduler);
            var subscription3 = default(IDisposable);

            scheduler.Schedule(() => subject = new AsyncSubject<int>(scheduler), 100);
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
                OnError<int>(630, new MockException(30))
                );

            results3.AssertEqual(
                OnError<int>(901, new MockException(30))
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

            var subject = default(AsyncSubject<int>);
            var subscription = default(IDisposable);

            var results1 = new MockObserver<int>(scheduler);
            var subscription1 = default(IDisposable);

            var results2 = new MockObserver<int>(scheduler);
            var subscription2 = default(IDisposable);

            var results3 = new MockObserver<int>(scheduler);
            var subscription3 = default(IDisposable);

            scheduler.Schedule(() => subject = new AsyncSubject<int>(scheduler), 100);
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
                OnCompleted<int>(901)
                );
        }
    }
}
