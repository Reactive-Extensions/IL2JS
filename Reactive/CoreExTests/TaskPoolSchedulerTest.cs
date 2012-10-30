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
using System.Reflection;

namespace Microsoft.LiveLabs.CoreExTests
{
#if !NETCF37 && !SILVERLIGHT
    [TestClass]
    public class TaskPoolSchedulerTest
    {
        [TestMethod]
        public void TaskPool_Now()
        {
            var res = Scheduler.TaskPool.Now - DateTime.Now;
            Assert.IsTrue(res.Seconds < 1);
        }

        [TestMethod]
        public void TaskPool_ScheduleAction()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            var nt = Scheduler.TaskPool;
            var evt = new ManualResetEvent(false);
            nt.Schedule(() => { Assert.AreNotEqual(id, Thread.CurrentThread.ManagedThreadId); evt.Set(); });
            evt.WaitOne();
        }

        [TestMethod]
        [Ignore]
        public void TaskPool_ScheduleActionDue()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            var nt = Scheduler.TaskPool;
            var evt = new ManualResetEvent(false);
            var sw = new Stopwatch();
            sw.Start();
            nt.Schedule(() => { sw.Stop(); Assert.AreNotEqual(id, Thread.CurrentThread.ManagedThreadId); evt.Set(); }, TimeSpan.FromSeconds(0.2));
            evt.WaitOne();
            Assert.IsTrue(sw.ElapsedMilliseconds > 180, "due " + sw.ElapsedMilliseconds);
        }

        [TestMethod]
        public void TaskPool_ScheduleActionCancel()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            var nt = Scheduler.TaskPool;
            var set = false;
            var d = nt.Schedule(() => { Assert.Fail(); set = true; }, TimeSpan.FromSeconds(0.2));
            d.Dispose();
            Thread.Sleep(400);
            Assert.IsFalse(set);
        }

        [Ignore]
        [TestMethod]
        public void TaskPool_ExceptionComesOut()
        {
            var ex = default(Exception);
            TestCrashTaskPool.Do(out ex);
            Assert.AreEqual("Oops!", ex.Message);
        }
    }

    [Serializable]
    public class TestCrashTaskPool
    {
        private static ManualResetEvent evt = new ManualResetEvent(false);
        private static Exception s_ex;

        public static void Do(out Exception ex)
        {
            var ads = new AppDomainSetup { ApplicationBase = AppDomain.CurrentDomain.BaseDirectory };
            var ad = AppDomain.CreateDomain("TaskPool exception test", null, ads);
            ad.UnhandledException += new UnhandledExceptionEventHandler(ad_UnhandledException);
            var test = (CrashTaskPool)ad.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, "Microsoft.LiveLabs.CoreExTests.CrashTaskPool");
            test.Do();
            evt.WaitOne();
            ex = s_ex;
        }

        public static void ad_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            s_ex = (Exception)e.ExceptionObject;
            evt.Set();
        }
    }

    public class CrashTaskPool : MarshalByRefObject
    {
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public void Do()
        {
            //var res = (ThreadAbortException)typeof(ThreadAbortException).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { }, null).Invoke(new object[] { });
            var res = new Exception("Oops!");
            Scheduler.TaskPool.Schedule(() => { throw res; });
        }
    }
#endif
}
