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
using System.Windows.Threading;

namespace Microsoft.LiveLabs.CoreExTests
{
    [TestClass]
    public class DispatcherSchedulerTest
    {
        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void Dispatcher_ArgumentChecking()
        {
            new DispatcherScheduler(null);
        }

        [TestMethod]
        public void Dispatcher_Property()
        {
            var disp = EnsureDispatcher();
            Assert.AreSame(disp, new DispatcherScheduler(disp).Dispatcher);
        }

        [TestMethod]
        public void Dispatcher_Now()
        {
            var disp = EnsureDispatcher();
            var res = new DispatcherScheduler(disp).Now - DateTime.Now;
            Assert.IsTrue(res.Seconds < 1);
        }

        [TestMethod]
        public void Dispatcher_ScheduleAction()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            var disp = EnsureDispatcher();
            var sch = new DispatcherScheduler(disp);
            var evt = new ManualResetEvent(false);
            sch.Schedule(() => { Assert.AreNotEqual(id, Thread.CurrentThread.ManagedThreadId); evt.Set(); });
            evt.WaitOne();
            disp.InvokeShutdown();
        }
#if !SILVERLIGHT && !NETCF37
        [TestMethod]
        public void Dispatcher_ScheduleActionError()
        {
            var ex = new Exception();

            var id = Thread.CurrentThread.ManagedThreadId;
            var disp = EnsureDispatcher();
            var evt = new ManualResetEvent(false);
            disp.UnhandledException += (o, e) =>
            {
                Assert.AreSame(ex, e.Exception.InnerException); // CHECK
                evt.Set();
                e.Handled = true;
            };
            var sch = new DispatcherScheduler(disp);
            sch.Schedule(() => { throw ex; });
            evt.WaitOne();
            disp.InvokeShutdown();
        }
#endif
#if !NETCF37
        [TestMethod]
        [Ignore]
        public void Dispatcher_ScheduleActionDue()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            var disp = EnsureDispatcher();
            var sch = new DispatcherScheduler(disp);
            var evt = new ManualResetEvent(false);
            var sw = new Stopwatch();
            sw.Start();
            sch.Schedule(() =>
            {
                sw.Stop();
                Assert.AreNotEqual(id, Thread.CurrentThread.ManagedThreadId);
                sw.Start();
                sch.Schedule(() =>
                {
                    sw.Stop();
                    Assert.AreNotEqual(id, Thread.CurrentThread.ManagedThreadId);
                    evt.Set();
                }, TimeSpan.FromSeconds(0.2));
            }, TimeSpan.FromSeconds(0.2));
            evt.WaitOne();
            Assert.IsTrue(sw.ElapsedMilliseconds > 380, "due " + sw.ElapsedMilliseconds);
            disp.InvokeShutdown();
        }

        [TestMethod]
        [Ignore]
        public void Dispatcher_ScheduleActionDueNow()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            var disp = EnsureDispatcher();
            var sch = new DispatcherScheduler(disp);
            var evt = new ManualResetEvent(false);
            var sw = new Stopwatch();
            sw.Start();
            sch.Schedule(() =>
            {
                sw.Stop();
                Assert.AreNotEqual(id, Thread.CurrentThread.ManagedThreadId);
                sw.Start();
                sch.Schedule(() =>
                {
                    sw.Stop();
                    Assert.AreNotEqual(id, Thread.CurrentThread.ManagedThreadId);
                    evt.Set();
                }, TimeSpan.Zero);
            }, TimeSpan.Zero);
            evt.WaitOne();
            disp.InvokeShutdown();
        }

        [TestMethod]
        [Ignore]
        public void Dispatcher_ScheduleActionDueCancel()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            var disp = EnsureDispatcher();
            var sch = new DispatcherScheduler(disp);
            var evt = new ManualResetEvent(false);
            var sw = new Stopwatch();
            sw.Start();
            sch.Schedule(() =>
            {
                sw.Stop();
                Assert.AreNotEqual(id, Thread.CurrentThread.ManagedThreadId);
                sw.Start();
                var d = sch.Schedule(() =>
                {
                    Assert.Fail();
                    evt.Set();
                }, TimeSpan.FromSeconds(0.2));
                d.Dispose();
                sch.Schedule(() =>
                {
                    sw.Stop();
                    Assert.AreNotEqual(id, Thread.CurrentThread.ManagedThreadId);
                    evt.Set();
                }, TimeSpan.FromSeconds(0.2));
            }, TimeSpan.FromSeconds(0.2));
            evt.WaitOne();
            Assert.IsTrue(sw.ElapsedMilliseconds > 380, "due " + sw.ElapsedMilliseconds);
            disp.InvokeShutdown();        
        }
#endif
        private Dispatcher EnsureDispatcher()
        {
#if DESKTOPCLR20 || DESKTOPCLR40
            var dispatcher = new Thread(Dispatcher.Run);
            dispatcher.IsBackground = true;
            dispatcher.Start();

            while (Dispatcher.FromThread(dispatcher) == null)
                Thread.Sleep(10);

            var d = Dispatcher.FromThread(dispatcher);

            while (d.BeginInvoke(new Action(() => { })).Status == DispatcherOperationStatus.Aborted) ;

            return d;
#else
            return System.Windows.Deployment.Current.Dispatcher;
#endif
        }
    }

#if SILVERLIGHT || NETCF37
    internal static class FakeDispatcherHelpers
    {
        public static void InvokeShutdown(this Dispatcher dispatcher)
        {
        }
    }
#endif
}
