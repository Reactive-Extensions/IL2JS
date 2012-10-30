#if !SILVERLIGHT && !NETCF37
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
using System.Windows.Forms;

namespace Microsoft.LiveLabs.CoreExTests
{
    [TestClass]
    public class ControlSchedulerTest
    {
        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void Control_ArgumentChecking()
        {
            new ControlScheduler(null);
        }

        [TestMethod]
        public void Control_Property()
        {
            var lbl = new Label();
            Assert.AreSame(lbl, new ControlScheduler(lbl).Control);
        }

        [TestMethod]
        public void Control_Now()
        {
            var res = new ControlScheduler(new Label()).Now - DateTime.Now;
            Assert.IsTrue(res.Seconds < 1);
        }

        [TestMethod]
        public void Control_ScheduleAction()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            var lbl = CreateLabel();
            var sch = new ControlScheduler(lbl);
            var evt = new ManualResetEvent(false);
            sch.Schedule(() => { lbl.Text = "Okay"; Assert.AreNotEqual(id, Thread.CurrentThread.ManagedThreadId); });
            sch.Schedule(() => { Assert.AreEqual("Okay", lbl.Text); Assert.AreNotEqual(id, Thread.CurrentThread.ManagedThreadId); evt.Set(); });
            evt.WaitOne();
            Application.Exit();
        }

        [TestMethod]
        public void Control_ScheduleActionError()
        {
            var ex = new Exception();

            var evt = new ManualResetEvent(false);
            var id = Thread.CurrentThread.ManagedThreadId;
            var lbl = CreateLabelWithHandler(e => {
                Assert.AreSame(ex, e);
                evt.Set();
            });
            var sch = new ControlScheduler(lbl);
            sch.Schedule(() => { throw ex; });
            evt.WaitOne();
            Application.Exit();
        }

        [TestMethod]
        [Ignore]
        public void Control_ScheduleActionDue()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            var lbl = CreateLabel();
            var sch = new ControlScheduler(lbl);
            var evt = new ManualResetEvent(false);
            var sw = new Stopwatch();
            sw.Start();
            sch.Schedule(() => {
                sw.Stop();
                lbl.Text = "Okay";
                Assert.AreNotEqual(id, Thread.CurrentThread.ManagedThreadId);
                sw.Start();
                sch.Schedule(() => {
                    sw.Stop();
                    Assert.AreEqual("Okay", lbl.Text);
                    Assert.AreNotEqual(id, Thread.CurrentThread.ManagedThreadId);
                    evt.Set();
                }, TimeSpan.FromSeconds(0.2));
            }, TimeSpan.FromSeconds(0.2));
            evt.WaitOne();
            Assert.IsTrue(sw.ElapsedMilliseconds > 380, "due " + sw.ElapsedMilliseconds);
            Application.Exit();
        }

        [TestMethod]
        [Ignore]
        public void Control_ScheduleActionDueNow()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            var lbl = CreateLabel();
            var sch = new ControlScheduler(lbl);
            var evt = new ManualResetEvent(false);
            var sw = new Stopwatch();
            sw.Start();
            sch.Schedule(() =>
            {
                sw.Stop();
                lbl.Text = "Okay";
                Assert.AreNotEqual(id, Thread.CurrentThread.ManagedThreadId);
                sw.Start();
                sch.Schedule(() =>
                {
                    sw.Stop();
                    Assert.AreEqual("Okay", lbl.Text);
                    Assert.AreNotEqual(id, Thread.CurrentThread.ManagedThreadId);
                    evt.Set();
                }, TimeSpan.Zero);
            }, TimeSpan.Zero);
            evt.WaitOne();
            Application.Exit();
        }

        [TestMethod]
        [Ignore]
        public void Control_ScheduleActionDueCancel()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            var lbl = CreateLabel();
            var sch = new ControlScheduler(lbl);
            var evt = new ManualResetEvent(false);
            var sw = new Stopwatch();
            sw.Start();
            sch.Schedule(() =>
            {
                sw.Stop();
                lbl.Text = "Okay";
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
                    Assert.AreEqual("Okay", lbl.Text);
                    Assert.AreNotEqual(id, Thread.CurrentThread.ManagedThreadId);
                    evt.Set();
                }, TimeSpan.FromSeconds(0.2));
            }, TimeSpan.FromSeconds(0.2));
            evt.WaitOne();
            Assert.IsTrue(sw.ElapsedMilliseconds > 380, "due " + sw.ElapsedMilliseconds);
            Application.Exit();
        }

        private Label CreateLabel()
        {
            var loaded = new ManualResetEvent(false);
            var lbl = default(Label);

            var t = new Thread(() =>
            {
                lbl = new Label();
                var frm = new Form { Controls = { lbl }, Width = 0, Height = 0, FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false };
                frm.Load += (_, __) =>
                {
                    loaded.Set();
                };
                Application.Run(frm);
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();

            loaded.WaitOne();
            return lbl;
        }

        private Label CreateLabelWithHandler(Action<Exception> handler)
        {
            var loaded = new ManualResetEvent(false);
            var lbl = default(Label);

            var t = new Thread(() =>
            {
                lbl = new Label();
                var frm = new Form { Controls = { lbl }, Width = 0, Height = 0, FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false };
                frm.Load += (_, __) =>
                {
                    loaded.Set();
                };
                Application.ThreadException += (o, e) =>
                {
                    handler(e.Exception);
                };
                Application.Run(frm);
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();

            loaded.WaitOne();
            return lbl;
        }
    }
}
#endif