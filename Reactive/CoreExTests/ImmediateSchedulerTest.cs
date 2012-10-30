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
    public class ImmediateSchedulerTest
    {
        [TestMethod]
        public void Immediate_Now()
        {
            var res = Scheduler.Immediate.Now - DateTime.Now;
            Assert.IsTrue(res.Seconds < 1);
        }

        [TestMethod]
        public void Immediate_ScheduleAction()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            var ran = false;
            Scheduler.Immediate.Schedule(() => { Assert.AreEqual(id, Thread.CurrentThread.ManagedThreadId); ran = true; });
            Assert.IsTrue(ran);
        }

        [TestMethod]
        public void Immediate_ScheduleActionError()
        {
            var ex = new Exception();

            try
            {
                Scheduler.Immediate.Schedule(() => { throw ex; });
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(e, ex);
            }
        }
#if !NETCF37
        [TestMethod]
        [Ignore]
        public void Immediate_ScheduleActionDue()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            var ran = false;
            var sw = new Stopwatch();
            sw.Start();
            Scheduler.Immediate.Schedule(() => { sw.Stop(); Assert.AreEqual(id, Thread.CurrentThread.ManagedThreadId); ran = true; }, TimeSpan.FromSeconds(0.2));
            Assert.IsTrue(ran, "ran");
            Assert.IsTrue(sw.ElapsedMilliseconds > 180, "due " + sw.ElapsedMilliseconds);
        }
#endif
    }
}
