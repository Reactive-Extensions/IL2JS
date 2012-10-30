using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Threading;
using System.Threading;
#if !NETCF37
using Reactive.Windows.Forms;
#endif
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Windows.Threading;
using ReactiveTests.Mocks;
using ReactiveTests.Dummies;

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

#if !NETCF37 && !SILVERLIGHT
using System.Windows.Forms;
#endif

namespace ReactiveTests.Tests
{
    [TestClass]
    public partial class ObservableConcurrencyTest : Test
    {
        [TestMethod]
        public void ObserveOn_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

#if DESKTOPCLR20 || DESKTOPCLR40
            Throws<ArgumentNullException>(() => Observable.ObserveOn<int>(default(IObservable<int>), new ControlScheduler(new Label())));
            Throws<ArgumentNullException>(() => Observable.ObserveOn<int>(someObservable, default(ControlScheduler)));

            Throws<ArgumentNullException>(() => ControlObservableExtensions.ObserveOn<int>(default(IObservable<int>), new Label()));
            Throws<ArgumentNullException>(() => ControlObservableExtensions.ObserveOn<int>(someObservable, default(Label)));
#endif

#if SILVERLIGHT || NETCF37
            Throws<ArgumentNullException>(() => Observable.ObserveOn<int>(default(IObservable<int>), new DispatcherScheduler(System.Windows.Deployment.Current.Dispatcher)));
#else
            Throws<ArgumentNullException>(() => Observable.ObserveOn<int>(default(IObservable<int>), new DispatcherScheduler(Dispatcher.CurrentDispatcher)));
#endif
            Throws<ArgumentNullException>(() => Observable.ObserveOn<int>(someObservable, default(DispatcherScheduler)));

#if SILVERLIGHT || NETCF37
            Throws<ArgumentNullException>(() => DispatcherObservableExtensions.ObserveOn<int>(default(IObservable<int>), System.Windows.Deployment.Current.Dispatcher));
#else
            Throws<ArgumentNullException>(() => DispatcherObservableExtensions.ObserveOn<int>(default(IObservable<int>), Dispatcher.CurrentDispatcher));
#endif
            Throws<ArgumentNullException>(() => DispatcherObservableExtensions.ObserveOn<int>(someObservable, default(Dispatcher)));

            Throws<ArgumentNullException>(() => Observable.ObserveOn<int>(default(IObservable<int>), new SynchronizationContext()));
            Throws<ArgumentNullException>(() => Observable.ObserveOn<int>(someObservable, default(SynchronizationContext)));

            Throws<ArgumentNullException>(() => Observable.ObserveOnDispatcher<int>(default(IObservable<int>)));
        }

#if DESKTOPCLR20 || DESKTOPCLR40
        [TestMethod]
        public void ObserveOn_Control()
        {
            var lbl = CreateLabel();

            var evt = new ManualResetEvent(false);
            bool okay = true;
            Observable.Range(0, 10, Scheduler.NewThread).ObserveOn(lbl).Subscribe(x =>
            {
                lbl.Text = x.ToString();
                okay &= (SynchronizationContext.Current is System.Windows.Forms.WindowsFormsSynchronizationContext);
            }, () => evt.Set());

            evt.WaitOne();
            Application.Exit();
            Assert.IsTrue(okay);
        }

        [TestMethod]
        public void ObserveOn_ControlScheduler()
        {
            var lbl = CreateLabel();

            var evt = new ManualResetEvent(false);
            bool okay = true;
            Observable.Range(0, 10, Scheduler.NewThread).ObserveOn(new ControlScheduler(lbl)).Subscribe(x =>
            {
                lbl.Text = x.ToString();
                okay &= (SynchronizationContext.Current is System.Windows.Forms.WindowsFormsSynchronizationContext);
            }, () => evt.Set());

            evt.WaitOne();
            Application.Exit();
            Assert.IsTrue(okay);
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
#endif

        [TestMethod]
        public void ObserveOn_Dispatcher()
        {
            var dispatcher = EnsureDispatcher();

            var evt = new ManualResetEvent(false);
            bool okay = true;
            Observable.Range(0, 10, Scheduler.NewThread).ObserveOn(dispatcher).Subscribe(x =>
            {
                okay &= (SynchronizationContext.Current is System.Windows.Threading.DispatcherSynchronizationContext);
            }, () => evt.Set());

            evt.WaitOne();

#if DESKTOPCLR20 || DESKTOPCLR40
            dispatcher.InvokeShutdown();
#endif
            Assert.IsTrue(okay);
        }

        [TestMethod]
        public void ObserveOn_DispatcherScheduler()
        {
            var dispatcher = EnsureDispatcher();

            var evt = new ManualResetEvent(false);
            bool okay = true;
            Observable.Range(0, 10, Scheduler.NewThread).ObserveOn(new DispatcherScheduler(dispatcher)).Subscribe(x =>
            {
                okay &= (SynchronizationContext.Current is System.Windows.Threading.DispatcherSynchronizationContext);
            }, () => evt.Set());

            evt.WaitOne();

#if DESKTOPCLR20 || DESKTOPCLR40
            dispatcher.InvokeShutdown();
#endif
            Assert.IsTrue(okay);
        }

        [TestMethod]
        public void ObserveOn_CurrentDispatcher()
        {
            var dispatcher = EnsureDispatcher();

            var evt = new ManualResetEvent(false);
            bool okay = true;

            dispatcher.BeginInvoke(new Action(() =>
            {
                Observable.Range(0, 10, Scheduler.NewThread).ObserveOnDispatcher().Subscribe(x =>
                {
                    okay &= (SynchronizationContext.Current is System.Windows.Threading.DispatcherSynchronizationContext);
                }, () => evt.Set());
            }));

            evt.WaitOne();

#if DESKTOPCLR20 || DESKTOPCLR40
            dispatcher.InvokeShutdown();
#endif
            Assert.IsTrue(okay);
        }

        [TestMethod]
        public void ObserveOn_Error()
        {
            var dispatcher = EnsureDispatcher();

            var ex = new Exception();

            var evt = new ManualResetEvent(false);
            bool okay = true;

            var _e = default(Exception);

            dispatcher.BeginInvoke(new Action(() =>
            {
                Observable.Throw<int>(ex).ObserveOnDispatcher().Subscribe(x =>
                {
                    okay &= (SynchronizationContext.Current is System.Windows.Threading.DispatcherSynchronizationContext);
                },
                e => { _e = e; evt.Set(); },
                () => { Assert.Fail(); evt.Set(); });
            }));

            evt.WaitOne();
            Assert.AreSame(ex, _e);

#if DESKTOPCLR20 || DESKTOPCLR40
            dispatcher.InvokeShutdown();
#endif
            Assert.IsTrue(okay);
        }

        private Dispatcher EnsureDispatcher()
        {
#if DESKTOPCLR20 || DESKTOPCLR40
            var dispatcher = new Thread(Dispatcher.Run);
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

        [TestMethod]
        public void SubscribeOn_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

#if DESKTOPCLR20 || DESKTOPCLR40
            Throws<ArgumentNullException>(() => Observable.SubscribeOn<int>(default(IObservable<int>), new ControlScheduler(new Label())));
            Throws<ArgumentNullException>(() => Observable.SubscribeOn<int>(someObservable, default(ControlScheduler)));

            Throws<ArgumentNullException>(() => ControlObservableExtensions.SubscribeOn<int>(default(IObservable<int>), new Label()));
            Throws<ArgumentNullException>(() => ControlObservableExtensions.SubscribeOn<int>(someObservable, default(Label)));
#endif

#if SILVERLIGHT || NETCF37
            Throws<ArgumentNullException>(() => Observable.SubscribeOn<int>(default(IObservable<int>), new DispatcherScheduler(System.Windows.Deployment.Current.Dispatcher)));
#else
            Throws<ArgumentNullException>(() => Observable.SubscribeOn<int>(default(IObservable<int>), new DispatcherScheduler(Dispatcher.CurrentDispatcher)));
#endif
            Throws<ArgumentNullException>(() => Observable.SubscribeOn<int>(someObservable, default(DispatcherScheduler)));

#if SILVERLIGHT || NETCF37
            Throws<ArgumentNullException>(() => DispatcherObservableExtensions.SubscribeOn<int>(default(IObservable<int>), System.Windows.Deployment.Current.Dispatcher));
#else
            Throws<ArgumentNullException>(() => DispatcherObservableExtensions.SubscribeOn<int>(default(IObservable<int>), Dispatcher.CurrentDispatcher));
#endif
            Throws<ArgumentNullException>(() => DispatcherObservableExtensions.SubscribeOn<int>(someObservable, default(Dispatcher)));

            Throws<ArgumentNullException>(() => Observable.SubscribeOn<int>(default(IObservable<int>), new SynchronizationContext()));
            Throws<ArgumentNullException>(() => Observable.SubscribeOn<int>(someObservable, default(SynchronizationContext)));

            Throws<ArgumentNullException>(() => Observable.SubscribeOnDispatcher<int>(default(IObservable<int>)));
        }

#if DESKTOPCLR20 || DESKTOPCLR40
        [TestMethod]
        public void SubscribeOn_Control()
        {
            var lbl = CreateLabel();

            var evt = new ManualResetEvent(false);
            bool okay = true;
            Observable.Create<int>(obs =>
            {
                lbl.Text = "Subscribe";
                okay &= (SynchronizationContext.Current is System.Windows.Forms.WindowsFormsSynchronizationContext);

                return () =>
                {
                    lbl.Text = "Unsubscribe";
                    okay &= (SynchronizationContext.Current is System.Windows.Forms.WindowsFormsSynchronizationContext);
                    evt.Set();
                };
            })
            .SubscribeOn(lbl)
            .Subscribe(_ => {}).Dispose();

            evt.WaitOne();
            Application.Exit();
            Assert.IsTrue(okay);
        }

        [TestMethod]
        public void SubscribeOn_ControlScheduler()
        {
            var lbl = CreateLabel();

            var evt = new ManualResetEvent(false);
            bool okay = true;
            Observable.Create<int>(obs =>
            {
                lbl.Text = "Subscribe";
                okay &= (SynchronizationContext.Current is System.Windows.Forms.WindowsFormsSynchronizationContext);

                return () =>
                {
                    lbl.Text = "Unsubscribe";
                    okay &= (SynchronizationContext.Current is System.Windows.Forms.WindowsFormsSynchronizationContext);
                    evt.Set();
                };
            })
            .SubscribeOn(new ControlScheduler(lbl))
            .Subscribe(_ => { }).Dispose();

            evt.WaitOne();
            Application.Exit();
            Assert.IsTrue(okay);
        }
#endif

        [TestMethod]
        public void SubscribeOn_Dispatcher()
        {
            var dispatcher = EnsureDispatcher();

            var evt = new ManualResetEvent(false);
            bool okay = true;
            Observable.Create<int>(obs =>
            {
                okay &= (SynchronizationContext.Current is System.Windows.Threading.DispatcherSynchronizationContext);

                return () =>
                {
                    okay &= (SynchronizationContext.Current is System.Windows.Threading.DispatcherSynchronizationContext);
                    evt.Set();
                };
            })
            .SubscribeOn(dispatcher)
            .Subscribe(_ => { }).Dispose();

            evt.WaitOne();

#if DESKTOPCLR20 || DESKTOPCLR40
            dispatcher.InvokeShutdown();
#endif
            Assert.IsTrue(okay);
        }

        [TestMethod]
        public void SubscribeOn_DispatcherScheduler()
        {
            var dispatcher = EnsureDispatcher();

            var evt = new ManualResetEvent(false);
            bool okay = true;
            Observable.Create<int>(obs =>
            {
                okay &= (SynchronizationContext.Current is System.Windows.Threading.DispatcherSynchronizationContext);

                return () =>
                {
                    okay &= (SynchronizationContext.Current is System.Windows.Threading.DispatcherSynchronizationContext);
                    evt.Set();
                };
            })
            .SubscribeOn(new DispatcherScheduler(dispatcher))
            .Subscribe(_ => { }).Dispose();

            evt.WaitOne();

#if DESKTOPCLR20 || DESKTOPCLR40
            dispatcher.InvokeShutdown();
#endif
            Assert.IsTrue(okay);
        }

        [TestMethod]
        public void SubscribeOn_CurrentDispatcher()
        {
            var dispatcher = EnsureDispatcher();

            var evt = new ManualResetEvent(false);
            bool okay = true;

            dispatcher.BeginInvoke(new Action(() =>
            {
                Observable.Create<int>(obs =>
                {
                    okay &= (SynchronizationContext.Current is System.Windows.Threading.DispatcherSynchronizationContext);

                    return () =>
                    {
                        okay &= (SynchronizationContext.Current is System.Windows.Threading.DispatcherSynchronizationContext);
                        evt.Set();
                    };
                })
                .SubscribeOnDispatcher()
                .Subscribe(_ => { }).Dispose();
            }));

            evt.WaitOne();

#if DESKTOPCLR20 || DESKTOPCLR40
            dispatcher.InvokeShutdown();
#endif
            Assert.IsTrue(okay);
        }

        [TestMethod]
        public void Synchronize_ArgumentChecking()
        {
            var someObservable = Observable.Empty<int>();

            Throws<ArgumentNullException>(() => Observable.Synchronize<int>(default(IObservable<int>)));

            Throws<ArgumentNullException>(() => Observable.Synchronize<int>(default(IObservable<int>), new object()));
            Throws<ArgumentNullException>(() => Observable.Synchronize<int>(someObservable, null));
        }

        [TestMethod]
        public void Synchronize_Range()
        {
            int i = 0;
            bool outsideLock = true;

            var gate = new object();
            lock (gate)
            {
                outsideLock = false;
                Observable.Range(0, 100, Scheduler.NewThread).Synchronize(gate).Subscribe(x => i++, () => { Assert.IsTrue(outsideLock); });
                Thread.Sleep(100);
                Assert.AreEqual(0, i);
                outsideLock = true;
            }

            while (i < 100)
            {
                Thread.Sleep(10);
                lock (gate)
                {
                    int start = i;
                    Thread.Sleep(100);
                    Assert.AreEqual(start, i);
                }
            }
        }

        [TestMethod]
        public void Synchronize_Throw()
        {
            var ex = new Exception();
            var e = default(Exception);
            bool outsideLock = true;

            var gate = new object();
            lock (gate)
            {
                outsideLock = false;
                Observable.Throw<int>(ex, Scheduler.NewThread).Synchronize(gate).Subscribe(x => { Assert.Fail(); }, err => { e = err; }, () => { Assert.IsTrue(outsideLock); });
                Thread.Sleep(100);
                Assert.IsNull(e);
                outsideLock = true;
            }

            while (e == null)
                ;

            Assert.AreSame(ex, e);
        }

        [TestMethod]
        public void Synchronize_BadObservable()
        {
            var o = Observable.Create<int>(obs =>
            {
                var t1 = new Thread(() =>
                {
                    for (int i = 0; i < 100; i++)
                    {
                        obs.OnNext(i);
                    }
                });

                new Thread(() =>
                {
                    t1.Start();

                    for (int i = 100; i < 200; i++)
                    {
                        obs.OnNext(i);
                    }

                    t1.Join();
                    obs.OnCompleted();
                }).Start();

                return () => { };
            });

            var evt = new ManualResetEvent(false);

            int sum = 0;
            o.Synchronize().Subscribe(x => sum += x, () => { evt.Set(); });

            evt.WaitOne();

            Assert.AreEqual(Enumerable.Range(0, 200).Sum(), sum);
        }

        [TestMethod]
        public void Synchronize_NullError()
        {
            Throws<ArgumentNullException>(() => NullErrorObservable<int>.Instance.Synchronize().Subscribe());
        }

        [TestMethod]
        public void ObserveOn_Scheduler_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.ObserveOn(default(IObservable<int>), DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.ObserveOn(DummyObservable<int>.Instance, default(IScheduler)));
        }

        [TestMethod]
        public void ObserveOn_Scheduler_Completed()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext( 90, 1),
                OnNext(120, 2),
                OnNext(230, 3),
                OnNext(240, 4),
                OnNext(310, 5),
                OnNext(470, 6),
                OnCompleted<int>(530)
                );

            var results = scheduler.Run(() => xs.ObserveOn(scheduler));

            results.AssertEqual(
                OnNext(231, 3),
                OnNext(241, 4),
                OnNext(311, 5),
                OnNext(471, 6),
                OnCompleted<int>(531)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 530)
                );
        }

        [TestMethod]
        public void ObserveOn_Scheduler_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, 1),
                OnNext(120, 2),
                OnNext(230, 3),
                OnNext(240, 4),
                OnNext(310, 5),
                OnNext(470, 6),
                OnError<int>(530, new MockException(1))
                );

            var results = scheduler.Run(() => xs.ObserveOn(scheduler));

            results.AssertEqual(
                OnNext(231, 3),
                OnNext(241, 4),
                OnNext(311, 5),
                OnNext(471, 6),
                OnError<int>(531, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 530)
                );
        }

        [TestMethod]
        public void ObserveOn_Scheduler_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(90, 1),
                OnNext(120, 2),
                OnNext(230, 3),
                OnNext(240, 4),
                OnNext(310, 5),
                OnNext(470, 6)
                );

            var results = scheduler.Run(() => xs.ObserveOn(scheduler));

            results.AssertEqual(
                OnNext(231, 3),
                OnNext(241, 4),
                OnNext(311, 5),
                OnNext(471, 6)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 1000)
                );
        }

        [TestMethod]
        public void ObserveOn_Scheduler_SameTime()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnNext(210, 1),
                OnNext(210, 2)
                );

            var results = scheduler.Run(() => xs.ObserveOn(scheduler));

            results.AssertEqual(
                OnNext(211, 1),
                OnNext(212, 2)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 1000)
                );
        }

        [TestMethod]
        public void SubscribeOn_Scheduler_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.SubscribeOn(default(IObservable<int>), DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.SubscribeOn(DummyObservable<int>.Instance, default(IScheduler)));
        }

        [TestMethod]
        public void SubscribeOn_Scheduler_Sleep()
        {
            var scheduler = new TestScheduler();

            var s = 0;
            var d = 0;

            var xs = Observable.Create<int>(observer =>
                {
                    s = scheduler.Ticks;
                    return () => d = scheduler.Ticks;
                });

            var results = scheduler.Run(() => xs.SubscribeOn(scheduler));

            results.AssertEqual(
                );

            Assert.AreEqual(201, s);
            Assert.AreEqual(1001, d);
        }

        [TestMethod]
        public void SubscribeOn_Scheduler_Completed()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnCompleted<int>(300)
                );

            var results = scheduler.Run(() => xs.SubscribeOn(scheduler));

            results.AssertEqual(
                OnCompleted<int>(300)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 301)
                );
        }

        [TestMethod]
        public void SubscribeOn_Scheduler_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable(
                OnError<int>(300, new MockException(1))
                );

            var results = scheduler.Run(() => xs.SubscribeOn(scheduler));

            results.AssertEqual(
                OnError<int>(300, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 301)
                );
        }

        [TestMethod]
        public void SubscribeOn_Scheduler_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateHotObservable<int>(
                );

            var results = scheduler.Run(() => xs.SubscribeOn(scheduler));

            results.AssertEqual(
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 1001)
                );
        }
    }
}
