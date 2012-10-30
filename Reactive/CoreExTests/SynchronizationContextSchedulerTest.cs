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
    public class SynchronizationContextSchedulerTest
    {
        [TestMethod]
        public void SynchronizationContext_Now()
        {
            var ms = new MySync();
            var s = new SynchronizationContextScheduler(ms);

            var res = s.Now - DateTime.Now;
            Assert.IsTrue(res.Seconds < 1);
        }

        [TestMethod]
        public void SynchronizationContext_ScheduleAction()
        {
            var ms = new MySync();
            var s = new SynchronizationContextScheduler(ms);

            var ran = false;
            s.Schedule(() => { ran = true; });
            Assert.IsTrue(ms.Count == 1);
            Assert.IsTrue(ran);
        }

        [TestMethod]
        public void SynchronizationContext_ScheduleActionError()
        {
            var ms = new MySync();
            var s = new SynchronizationContextScheduler(ms);

            var ex = new Exception();

            try
            {
                s.Schedule(() => { throw ex; });
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(e, ex);
            }

            Assert.IsTrue(ms.Count == 1);
        }

#if !NETCF37
        [TestMethod]
        [Ignore]
        public void SynchronizationContext_ScheduleActionDue()
        {
            var ms = new MySync();
            var s = new SynchronizationContextScheduler(ms);

            var evt = new ManualResetEvent(false);
            var sw = new Stopwatch();
            sw.Start();
            s.Schedule(() => { sw.Stop(); evt.Set(); }, TimeSpan.FromSeconds(0.2));
            evt.WaitOne();
            Assert.IsTrue(sw.ElapsedMilliseconds > 180, "due " + sw.ElapsedMilliseconds);
            Assert.IsTrue(ms.Count == 1);
        }
#endif
        class MySync : SynchronizationContext
        {
            public int Count { get; private set; }

            public override void Post(SendOrPostCallback d, object state)
            {
                Count++;
                d(state);
            }

            public override void Send(SendOrPostCallback d, object state)
            {
                throw new NotImplementedException();
            }
        }
    }
}
