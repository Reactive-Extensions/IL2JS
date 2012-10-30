using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Concurrency;

using System.Threading;
using System.Diagnostics;

namespace Microsoft.LiveLabs.CoreExTests
{
    [TestClass]
    public class ThreadPoolSchedulerTest
    {
        [TestMethod]
        public void ThreadPool_Now()
        {
            var res = Scheduler.ThreadPool.Now - DateTime.Now;
            Assert.IsTrue(res.Seconds < 1);
        }

        [TestMethod]
        public void ThreadPool_ScheduleAction()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            var nt = Scheduler.ThreadPool;
            var evt = new ManualResetEvent(false);
            nt.Schedule(() => { Assert.AreNotEqual(id, Thread.CurrentThread.ManagedThreadId); evt.Set(); });
            evt.WaitOne();
        }
#if !NETCF37
        [TestMethod]
        [Ignore]
        public void ThreadPool_ScheduleActionDue()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            var nt = Scheduler.ThreadPool;
            var evt = new ManualResetEvent(false);
            var sw = new Stopwatch();
            sw.Start();
            nt.Schedule(() => { sw.Stop(); Assert.AreNotEqual(id, Thread.CurrentThread.ManagedThreadId); evt.Set(); }, TimeSpan.FromSeconds(0.2));
            evt.WaitOne();
            Assert.IsTrue(sw.ElapsedMilliseconds > 180, "due " + sw.ElapsedMilliseconds);
        }
#endif

        [TestMethod]
        public void ThreadPool_ScheduleActionCancel()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            var nt = Scheduler.ThreadPool;
            var set = false;
            var d = nt.Schedule(() => { Assert.Fail(); set = true; }, TimeSpan.FromSeconds(0.2));
            d.Dispose();
            Thread.Sleep(400);
            Assert.IsFalse(set);
        }
    }
}
