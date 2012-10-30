using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Concurrency;
using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Disposables;
#if !SILVERLIGHT && !NETCF37
using System.Windows.Forms;
#endif
using System.Threading;

namespace Microsoft.LiveLabs.CoreExTests
{
    [TestClass]
    public class SchedulerTest
    {
        [TestMethod]
        public void Scheduler_ArgumentChecks()
        {
            var ms = new MyScheduler();
            AssertThrows<ArgumentNullException>(() => Scheduler.Schedule(default(IScheduler), a => { }));
            AssertThrows<ArgumentNullException>(() => Scheduler.Schedule(ms, default(Action<Action>)));
            AssertThrows<ArgumentNullException>(() => Scheduler.Schedule(default(IScheduler), () => { }, DateTimeOffset.Now));
            AssertThrows<ArgumentNullException>(() => Scheduler.Schedule(ms, default(Action), DateTimeOffset.Now));
            AssertThrows<ArgumentNullException>(() => Scheduler.Schedule(default(IScheduler), a => { }, DateTimeOffset.Now));
            AssertThrows<ArgumentNullException>(() => Scheduler.Schedule(ms, default(Action<Action<DateTimeOffset>>), DateTimeOffset.Now));
            AssertThrows<ArgumentNullException>(() => Scheduler.Schedule(default(IScheduler), a => { }, TimeSpan.Zero));
            AssertThrows<ArgumentNullException>(() => Scheduler.Schedule(ms, default(Action<Action<TimeSpan>>), TimeSpan.Zero));
        }

        [TestMethod]
        public void Schedulers_ArgumentChecks()
        {
            AssertThrows<ArgumentNullException>(() => Scheduler.CurrentThread.EnsureTrampoline(null));
            AssertThrows<ArgumentNullException>(() => Scheduler.CurrentThread.Schedule(null));
            AssertThrows<ArgumentNullException>(() => Scheduler.CurrentThread.Schedule(null, TimeSpan.Zero));
            AssertThrows<ArgumentNullException>(() => Scheduler.Dispatcher.Schedule(null));
            AssertThrows<ArgumentNullException>(() => Scheduler.Dispatcher.Schedule(null, TimeSpan.Zero));
            AssertThrows<ArgumentNullException>(() => Scheduler.Immediate.Schedule(null));
            AssertThrows<ArgumentNullException>(() => Scheduler.Immediate.Schedule(null, TimeSpan.Zero));
            AssertThrows<ArgumentNullException>(() => Scheduler.NewThread.Schedule(null));
            AssertThrows<ArgumentNullException>(() => Scheduler.NewThread.Schedule(null, TimeSpan.Zero));
#if !SILVERLIGHT && !NETCF37
            AssertThrows<ArgumentNullException>(() => Scheduler.TaskPool.Schedule(null));
            AssertThrows<ArgumentNullException>(() => Scheduler.TaskPool.Schedule(null, TimeSpan.Zero));
#endif
            AssertThrows<ArgumentNullException>(() => Scheduler.ThreadPool.Schedule(null));
            AssertThrows<ArgumentNullException>(() => Scheduler.ThreadPool.Schedule(null, TimeSpan.Zero));
#if !SILVERLIGHT && !NETCF37
            var lbl = new Label();
            AssertThrows<ArgumentNullException>(() => new ControlScheduler(lbl).Schedule(null));
            AssertThrows<ArgumentNullException>(() => new ControlScheduler(lbl).Schedule(null, TimeSpan.Zero));
#endif
            var ctx = new SynchronizationContext();
            AssertThrows<ArgumentNullException>(() => new SynchronizationContextScheduler(ctx).Schedule(null));
            AssertThrows<ArgumentNullException>(() => new SynchronizationContextScheduler(ctx).Schedule(null, TimeSpan.Zero));
        }

        [TestMethod]
        public void Scheduler_ScheduleNonRecursive()
        {
            var ms = new MyScheduler();
            var res = false;
            Scheduler.Schedule(ms, a => { res = true; });
            Assert.IsTrue(res);
        }

        [TestMethod]
        public void Scheduler_ScheduleRecursive()
        {
            var ms = new MyScheduler();
            var i = 0;
            Scheduler.Schedule(ms, a => { if (++i < 10) a(); });
            Assert.AreEqual(10, i);
        }

        [TestMethod]
        public void Scheduler_ScheduleWithTimeNonRecursive()
        {
            var now = DateTimeOffset.Now;
            var ms = new MyScheduler(now) { Check = (a, t) => { Assert.IsTrue(t == TimeSpan.Zero); } };
            var res = false;
            Scheduler.Schedule(ms, a => { res = true; }, now);
            Assert.IsTrue(res);
            Assert.IsTrue(ms.WaitCycles == 0);
        }

        [TestMethod]
        public void Scheduler_ScheduleWithTimeRecursive()
        {
            var now = DateTimeOffset.Now;
            var i = 0;
            var ms = new MyScheduler(now) { Check = (a, t) => { Assert.IsTrue(t == TimeSpan.Zero); } };
            Scheduler.Schedule(ms, a => { if (++i < 10) a(now); }, now);
            Assert.IsTrue(ms.WaitCycles == 0);
            Assert.AreEqual(10, i);
        }

        [TestMethod]
        public void Scheduler_ScheduleWithTimeSpanNonRecursive()
        {
            var now = DateTimeOffset.Now;
            var ms = new MyScheduler(now) { Check = (a, t) => { Assert.IsTrue(t == TimeSpan.Zero); } };
            var res = false;
            Scheduler.Schedule(ms, a => { res = true; }, TimeSpan.Zero);
            Assert.IsTrue(res);
            Assert.IsTrue(ms.WaitCycles == 0);
        }

        [TestMethod]
        public void Scheduler_ScheduleWithTimeSpanRecursive()
        {
            var now = DateTimeOffset.Now;
            var ms = new MyScheduler(now) { Check = (a, t) => { Assert.IsTrue(t < TimeSpan.FromTicks(10)); } };
            var i = 0;
            Scheduler.Schedule(ms, a => { if (++i < 10) a(TimeSpan.FromTicks(i)); }, TimeSpan.Zero);
            Assert.IsTrue(ms.WaitCycles == Enumerable.Range(1, 9).Sum());
            Assert.AreEqual(10, i);
        }

        class MyScheduler : IScheduler
        {
            public MyScheduler()
                : this(DateTimeOffset.Now)
            {
            }

            public MyScheduler(DateTimeOffset now)
            {
                Now = now;
            }

            public DateTimeOffset Now
            {
                get;
                private set;
            }

            public IDisposable Schedule(Action action)
            {
                action();
                return Disposable.Empty;
            }

            public Action<Action, TimeSpan> Check { get; set; }
            public long WaitCycles { get; set; }

            public IDisposable Schedule(Action action, TimeSpan dueTime)
            {
                Check(action, dueTime);
                WaitCycles += dueTime.Ticks;
                action();
                return Disposable.Empty;
            }
        }

        private void AssertThrows<E>(Action a) where E : Exception
        {
            try
            {
                a();
                Assert.Fail();
            }
            catch (E)
            {
                return;
            }
            Assert.Fail();
        }
    }
}
