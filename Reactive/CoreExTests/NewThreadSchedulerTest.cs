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
    public class NewThreadSchedulerTest
    {
        [TestMethod]
        public void NewThread_Now()
        {
            var res = Scheduler.NewThread.Now - DateTime.Now;
            Assert.IsTrue(res.Seconds < 1);
        }

        [TestMethod]
        public void NewThread_ScheduleAction()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            var nt = Scheduler.NewThread;
            var evt = new ManualResetEvent(false);
            nt.Schedule(() => { Assert.AreNotEqual(id, Thread.CurrentThread.ManagedThreadId); evt.Set(); });
            evt.WaitOne();
        }
#if !NETCF37
        [TestMethod]
        [Ignore]
        public void NewThread_ScheduleActionDue()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            var nt = Scheduler.NewThread;
            var evt = new ManualResetEvent(false);
            var sw = new Stopwatch();
            sw.Start();
            nt.Schedule(() => { sw.Stop(); Assert.AreNotEqual(id, Thread.CurrentThread.ManagedThreadId); evt.Set(); }, TimeSpan.FromSeconds(0.2));
            evt.WaitOne();
            Assert.IsTrue(sw.ElapsedMilliseconds > 180, "due " + sw.ElapsedMilliseconds);
        }
#endif
    }
}
