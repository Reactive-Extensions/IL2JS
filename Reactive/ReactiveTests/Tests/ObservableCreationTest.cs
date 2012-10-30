#if !SILVERLIGHT && !NETCF37
using System.Threading.Tasks;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Threading.Tasks;
using ReactiveTests.Mocks;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using ReactiveTests.Dummies;
using System.ComponentModel;
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


namespace ReactiveTests
{
    [TestClass]
    public partial class ObservableTest : Test
    {
        [TestMethod]
        public void Return_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Return(0, null));
            Throws<ArgumentNullException>(() => Observable.Return(0, DummyScheduler.Instance).Subscribe(null));
        }

        [TestMethod]
        public void Return_Basic()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Return(42, scheduler));

            results.AssertEqual(
                OnNext(201, 42),
                OnCompleted<int>(201)
                );
        }

        [TestMethod]
        public void Return_Disposed()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Return(42, scheduler), 200);

            results.AssertEqual(
                );
        }

        [TestMethod]
        public void Return_DisposedAfterNext()
        {
            var scheduler = new TestScheduler();

            var d = new MutableDisposable();

            var xs = Observable.Return(42, scheduler);

            var results = new MockObserver<int>(scheduler);

            scheduler.Schedule(() => d.Disposable = xs.Subscribe(x =>
                {
                    d.Dispose();
                    results.OnNext(x);
                }, results.OnError, results.OnCompleted), 100);

            scheduler.Run();

            results.AssertEqual(
                OnNext(101, 42)
                );
        }

        [TestMethod]
        public void Return_ObserverThrows()
        {
            var scheduler1 = new TestScheduler();

            var xs = Observable.Return(1, scheduler1);

            xs.Subscribe(x => { throw new InvalidOperationException(); });

            Throws<InvalidOperationException>(() => scheduler1.Run());

            var scheduler2 = new TestScheduler();

            var ys = Observable.Return(1, scheduler2);

            ys.Subscribe(x => { }, ex => { }, () => { throw new InvalidOperationException(); });

            Throws<InvalidOperationException>(() => scheduler2.Run());
        }

        [TestMethod]
        public void Never_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Never<int>().Subscribe(null));
        }

        [TestMethod]
        public void Never_Basic()
        {
            var scheduler = new TestScheduler();

            var xs = Observable.Never<int>();

            var results = new MockObserver<int>(scheduler);

            xs.Subscribe(results);

            scheduler.Run();

            results.AssertEqual();
        }

        [TestMethod]
        public void Throw_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Throw<int>(new Exception(), null));
            Throws<ArgumentNullException>(() => Observable.Throw<int>(null, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.Throw<int>(new Exception(), DummyScheduler.Instance).Subscribe(null));
            Throws<ArgumentNullException>(() => Observable.Throw<int>(null));
        }

        [TestMethod]
        public void Throw_Basic()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Throw<int>(new MockException(42), scheduler));

            results.AssertEqual(
                OnError<int>(201, new MockException(42))
                );
        }

        [TestMethod]
        public void Throw_Disposed()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Throw<int>(new MockException(42), scheduler), 200);

            results.AssertEqual(
                );
        }

        [TestMethod]
        public void Throw_ObserverThrows()
        {
            var scheduler1 = new TestScheduler();

            var xs = Observable.Throw<int>(new MockException(1), scheduler1);

            xs.Subscribe(x => { }, ex => { throw new InvalidOperationException(); }, () => { });

            Throws<InvalidOperationException>(() => scheduler1.Run());
        }

        [TestMethod]
        public void Empty_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Empty<int>(null));
            Throws<ArgumentNullException>(() => Observable.Empty<int>(DummyScheduler.Instance).Subscribe(null));
        }

        [TestMethod]
        public void Empty_Basic()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Empty<int>(scheduler));

            results.AssertEqual(
                OnCompleted<int>(201)
                );
        }

        [TestMethod]
        public void Empty_Disposed()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Empty<int>(scheduler), 200);

            results.AssertEqual(
                );
        }

        [TestMethod]
        public void Empty_ObserverThrows()
        {
            var scheduler1 = new TestScheduler();

            var xs = Observable.Empty<int>(scheduler1);

            xs.Subscribe(x => { }, exception => { }, () => { throw new InvalidOperationException(); });

            Throws<InvalidOperationException>(() => scheduler1.Run());
        }

        [TestMethod]
        public void Return_DefaultScheduler()
        {
            Observable.Return(42).AssertEqual(Observable.Return(42, Scheduler.ThreadPool));
        }

        [TestMethod]
        public void Empty_DefaultScheduler()
        {
            Observable.Empty<int>().AssertEqual(Observable.Empty<int>(Scheduler.ThreadPool));
        }

        [TestMethod]
        public void Throw_DefaultScheduler()
        {
            Observable.Throw<int>(new MockException(42)).AssertEqual(Observable.Throw<int>(new MockException(42), Scheduler.ThreadPool));
        }

        IEnumerable<int> Enumerable_Finite()
        {
            yield return 1;
            yield return 2;
            yield return 3;
            yield return 4;
            yield return 5;
            yield break;
        }

        IEnumerable<int> Enumerable_Infinite()
        {
            while (true)
                yield return 1;
        }

        IEnumerable<int> Enumerable_Error()
        {
            yield return 1;
            yield return 2;
            yield return 3;
            throw new MockException(4);
        }

        [TestMethod]
        public void SubscribeToEnumerable_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Subscribe<int>((IEnumerable<int>)null, DummyObserver<int>.Instance));
            Throws<ArgumentNullException>(() => Observable.Subscribe<int>(DummyEnumerable<int>.Instance, (IObserver<int>)null));

            Throws<ArgumentNullException>(() => Observable.Subscribe<int>((IEnumerable<int>)null, DummyObserver<int>.Instance, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.Subscribe<int>(DummyEnumerable<int>.Instance, DummyObserver<int>.Instance, null));
            Throws<ArgumentNullException>(() => Observable.Subscribe<int>(DummyEnumerable<int>.Instance, (IObserver<int>)null, DummyScheduler.Instance));
            Throws<NullReferenceException>(() => NullEnumeratorEnumerable<int>.Instance.Subscribe(Observer.Create<int>(x => { }), Scheduler.CurrentThread));
        }

        [TestMethod]
        public void SubscribeToEnumerable_Finite()
        {
            var scheduler = new TestScheduler();

            var results = new MockObserver<int>(scheduler);
            var d = default(IDisposable);
            var xs = default(MockEnumerable<int>);

            scheduler.Schedule(() => xs = new MockEnumerable<int>(scheduler, Enumerable_Finite()), Created);
            scheduler.Schedule(() => d = xs.Subscribe(results, scheduler), Subscribed);
            scheduler.Schedule(() => d.Dispose(), Disposed);

            scheduler.Run();

            results.AssertEqual(
                OnNext(201, 1),
                OnNext(202, 2),
                OnNext(203, 3),
                OnNext(204, 4),
                OnNext(205, 5),
                OnCompleted<int>(206)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 206)
                );
        }

        [TestMethod]
        public void SubscribeToEnumerable_Infinite()
        {
            var scheduler = new TestScheduler();

            var results = new MockObserver<int>(scheduler);
            var d = default(IDisposable);
            var xs = default(MockEnumerable<int>);

            scheduler.Schedule(() => xs = new MockEnumerable<int>(scheduler, Enumerable_Infinite()), Created);
            scheduler.Schedule(() => d = xs.Subscribe(results, scheduler), Subscribed);
            scheduler.Schedule(() => d.Dispose(), 210);

            scheduler.Run();

            results.AssertEqual(
                OnNext(201, 1),
                OnNext(202, 1),
                OnNext(203, 1),
                OnNext(204, 1),
                OnNext(205, 1),
                OnNext(206, 1),
                OnNext(207, 1),
                OnNext(208, 1),
                OnNext(209, 1)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 210)
                );
        }

        [TestMethod]
        public void SubscribeToEnumerable_Error()
        {
            var scheduler = new TestScheduler();

            var results = new MockObserver<int>(scheduler);
            var d = default(IDisposable);
            var xs = default(MockEnumerable<int>);

            scheduler.Schedule(() => xs = new MockEnumerable<int>(scheduler, Enumerable_Error()), Created);
            scheduler.Schedule(() => d = xs.Subscribe(results, scheduler), Subscribed);
            scheduler.Schedule(() => d.Dispose(), Disposed);

            scheduler.Run();

            results.AssertEqual(
                OnNext(201, 1),
                OnNext(202, 2),
                OnNext(203, 3),
                OnError<int>(204, new MockException(4))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 204)
                );
        }

#if !NETCF37
        [TestMethod]
        public void SubscribeToEnumerable_DefaultScheduler()
        {
            for (int i = 0; i < 100; i++)
            {
                var scheduler = new TestScheduler();

                var results1 = new List<int>();
                var results2 = new List<int>();

                var s1 = new Semaphore(0, 1);
                var s2 = new Semaphore(0, 1);

                Observable.Subscribe(Enumerable_Finite(),
                    Observer.Create<int>(x => results1.Add(x), ex => { throw ex; }, () => s1.Release()));
                Observable.Subscribe(Enumerable_Finite(),
                    Observer.Create<int>(x => results2.Add(x), ex => { throw ex; }, () => s2.Release()),
                    Scheduler.ThreadPool);

                s1.WaitOne();
                s2.WaitOne();

                results1.AssertEqual(results2);
            }
        }
#endif
        public class FromEvent
        {
            [DebuggerDisplay("{Id}")]
            public class TestEventArgs : EventArgs, IEquatable<TestEventArgs>
            {
                public int Id { get; set; }

                public override string ToString()
                {
                    return Id.ToString();
                }

                public bool Equals(TestEventArgs other)
                {
                    if (other == this)
                        return true;
                    if (other == null)
                        return false;
                    return other.Id == Id;
                }

                public override bool Equals(object obj)
                {
                    return Equals(obj as TestEventArgs);
                }

                public override int GetHashCode()
                {
                    return Id;
                }
            }

            public delegate void TestEventHandler(object sender, TestEventArgs eventArgs);

            public event TestEventHandler E1;

            public void M1(int i)
            {
                var e = E1;
                if (e != null)
                    e(this, new TestEventArgs { Id = i });
            }

            public event EventHandler<TestEventArgs> E2;

            public void M2(int i)
            {
                var e = E2;
                if (e != null)
                    e(this, new TestEventArgs { Id = i });
            }

            public event Action<object, TestEventArgs> E3;

            public void M3(int i)
            {
                var e = E3;
                if (e != null)
                    e(this, new TestEventArgs { Id = i });
            }

            public event Action<int> E4;

            public void M4(int i)
            {
                var e = E4;
                if (e != null)
                    e(i);
            }

            public event TestEventHandler AddThrows
            {
                add { throw new InvalidOperationException(); }
                remove { }
            }

            public event TestEventHandler RemoveThrows
            {
                add { }
                remove { throw new InvalidOperationException(); }
            }
        }

        class FromEvent_ArgCheck
        {
#pragma warning disable 67
            public event Action E1;
            public event Action<int, int> E2;
            public event Action<object, object> E3;
            public event Action<object, int> E4;
            public event Func<object, EventArgs, int> E5;
            public event Action<EventArgs> E6;
            public event EventHandler<CancelEventArgs> E7;
#pragma warning restore 67
        }

        [TestMethod]
        public void FromEvent_Reflection_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.FromEvent<EventArgs>(null, "foo"));
            Throws<ArgumentNullException>(() => Observable.FromEvent<EventArgs>(new FromEvent_ArgCheck(), null));
            Throws<InvalidOperationException>(() => Observable.FromEvent<EventArgs>(new FromEvent_ArgCheck(), "foo"));
            Throws<InvalidOperationException>(() => Observable.FromEvent<EventArgs>(new FromEvent_ArgCheck(), "E1"));
            Throws<InvalidOperationException>(() => Observable.FromEvent<EventArgs>(new FromEvent_ArgCheck(), "E2"));
            Throws<InvalidOperationException>(() => Observable.FromEvent<EventArgs>(new FromEvent_ArgCheck(), "E3"));
            Throws<InvalidOperationException>(() => Observable.FromEvent<EventArgs>(new FromEvent_ArgCheck(), "E4"));
            Throws<InvalidOperationException>(() => Observable.FromEvent<EventArgs>(new FromEvent_ArgCheck(), "E5"));
            Throws<InvalidOperationException>(() => Observable.FromEvent<EventArgs>(new FromEvent_ArgCheck(), "E6"));
            Throws<InvalidOperationException>(() => Observable.FromEvent<EventArgs>(new FromEvent_ArgCheck(), "E7"));
        }

        [TestMethod]
        public void FromEvent_Reflection_Throws()
        {
            var xs = Observable.FromEvent<FromEvent.TestEventArgs>(new FromEvent(), "AddThrows");
            Throws<TargetInvocationException>(() => xs.Subscribe());
            var ys = Observable.FromEvent<FromEvent.TestEventArgs>(new FromEvent(), "RemoveThrows");
            var d = ys.Subscribe();
            Throws<TargetInvocationException>(() => d.Dispose());
        }

        [TestMethod]
        public void FromEvent_Reflection_E1()
        {
            var scheduler = new TestScheduler();

            var fe = new FromEvent();

            scheduler.Schedule(() => fe.M1(1), 50);
            scheduler.Schedule(() => fe.M1(2), 150);
            scheduler.Schedule(() => fe.M1(3), 250);
            scheduler.Schedule(() => fe.M1(4), 350);
            scheduler.Schedule(() => fe.M1(5), 450);
            scheduler.Schedule(() => fe.M1(6), 1050);

            var results = scheduler.Run(() => Observable.FromEvent<FromEvent.TestEventArgs>(fe, "E1").Select(evt => new { evt.Sender, evt.EventArgs }));

            results.AssertEqual(
                OnNext(250, new { Sender = (object)fe, EventArgs = new FromEvent.TestEventArgs { Id = 3 } }),
                OnNext(350, new { Sender = (object)fe, EventArgs = new FromEvent.TestEventArgs { Id = 4 } }),
                OnNext(450, new { Sender = (object)fe, EventArgs = new FromEvent.TestEventArgs { Id = 5 } })
                );
        }

        [TestMethod]
        public void FromEvent_Reflection_E2()
        {
            var scheduler = new TestScheduler();

            var fe = new FromEvent();

            scheduler.Schedule(() => fe.M2(1), 50);
            scheduler.Schedule(() => fe.M2(2), 150);
            scheduler.Schedule(() => fe.M2(3), 250);
            scheduler.Schedule(() => fe.M2(4), 350);
            scheduler.Schedule(() => fe.M2(5), 450);
            scheduler.Schedule(() => fe.M2(6), 1050);

            var results = scheduler.Run(() => Observable.FromEvent<FromEvent.TestEventArgs>(fe, "E2").Select(evt => new { evt.Sender, evt.EventArgs }));

            results.AssertEqual(
                OnNext(250, new { Sender = (object)fe, EventArgs = new FromEvent.TestEventArgs { Id = 3 } }),
                OnNext(350, new { Sender = (object)fe, EventArgs = new FromEvent.TestEventArgs { Id = 4 } }),
                OnNext(450, new { Sender = (object)fe, EventArgs = new FromEvent.TestEventArgs { Id = 5 } })
                );
        }

        [TestMethod]
        public void FromEvent_Reflection_E3()
        {
            var scheduler = new TestScheduler();

            var fe = new FromEvent();

            scheduler.Schedule(() => fe.M3(1), 50);
            scheduler.Schedule(() => fe.M3(2), 150);
            scheduler.Schedule(() => fe.M3(3), 250);
            scheduler.Schedule(() => fe.M3(4), 350);
            scheduler.Schedule(() => fe.M3(5), 450);
            scheduler.Schedule(() => fe.M3(6), 1050);

            var results = scheduler.Run(() => Observable.FromEvent<FromEvent.TestEventArgs>(fe, "E3").Select(evt => new { evt.Sender, evt.EventArgs }));

            results.AssertEqual(
                OnNext(250, new { Sender = (object)fe, EventArgs = new FromEvent.TestEventArgs { Id = 3 } }),
                OnNext(350, new { Sender = (object)fe, EventArgs = new FromEvent.TestEventArgs { Id = 4 } }),
                OnNext(450, new { Sender = (object)fe, EventArgs = new FromEvent.TestEventArgs { Id = 5 } })
                );
        }

#if DESKTOPCLR20 || DESKTOPCLR40
        [TestMethod]
        public void FromEvent_Reflection_MissingAccessors()
        {
            var asm = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("EventsTest"), System.Reflection.Emit.AssemblyBuilderAccess.RunAndSave);
            var mod = asm.DefineDynamicModule("Events");
            var tpe = mod.DefineType("FromEvent");

            var ev1 = tpe.DefineEvent("Bar", (EventAttributes)MethodAttributes.Public, typeof(Action));
            var add = tpe.DefineMethod("add_Bar", MethodAttributes.Public, CallingConventions.Standard, typeof(void), new Type[0]);
            var ge1 = add.GetILGenerator();
            ge1.Emit(System.Reflection.Emit.OpCodes.Ret);
            ev1.SetAddOnMethod(add);

            var ev2 = tpe.DefineEvent("Foo", (EventAttributes)MethodAttributes.Public, typeof(Action));
            var rem = tpe.DefineMethod("remove_Foo", MethodAttributes.Public, CallingConventions.Standard, typeof(void), new Type[0]);
            var ge2 = rem.GetILGenerator();
            ge2.Emit(System.Reflection.Emit.OpCodes.Ret);
            ev2.SetRemoveOnMethod(rem);

            var evt = tpe.DefineEvent("Evt", (EventAttributes)MethodAttributes.Public, typeof(Action));
            evt.SetAddOnMethod(add);
            evt.SetRemoveOnMethod(rem);

            var res = tpe.CreateType();
            var obj = Activator.CreateInstance(res);

            Throws<InvalidOperationException>(() => Observable.FromEvent<EventArgs>(obj, "Bar"));
            Throws<InvalidOperationException>(() => Observable.FromEvent<EventArgs>(obj, "Foo"));
            Throws<InvalidOperationException>(() => Observable.FromEvent<EventArgs>(obj, "Evt"));
        }
#endif

        [TestMethod]
        public void FromEvent_Conversion_ArgumentChecking()
        {
#if !DESKTOPCLR40
#pragma warning disable 1911
#endif
            Throws<ArgumentNullException>(() => Observable.FromEvent<EventHandler, EventArgs>(null, h => { }, h => { }));
            Throws<ArgumentNullException>(() => Observable.FromEvent<EventHandler, EventArgs>(h => new EventHandler(h), null, h => { }));
            Throws<ArgumentNullException>(() => Observable.FromEvent<EventHandler, EventArgs>(h => new EventHandler(h), h => { }, null));
#if !DESKTOPCLR40
#pragma warning restore 1911
#endif
        }

        [TestMethod]
        public void FromEvent_Conversion_E4()
        {
            var scheduler = new TestScheduler();

            var fe = new FromEvent();

            scheduler.Schedule(() => fe.M4(1), 50);
            scheduler.Schedule(() => fe.M4(2), 150);
            scheduler.Schedule(() => fe.M4(3), 250);
            scheduler.Schedule(() => fe.M4(4), 350);
            scheduler.Schedule(() => fe.M4(5), 450);
            scheduler.Schedule(() => fe.M4(6), 1050);

            var results = scheduler.Run(() =>
                Observable.FromEvent<Action<int>, FromEvent.TestEventArgs>(
                    h => new Action<int>(x => h(fe, new FromEvent.TestEventArgs { Id = x })),
                    h => fe.E4 += h,
                    h => fe.E4 -= h)
                .Select(evt => new { evt.Sender, evt.EventArgs }));

            results.AssertEqual(
                OnNext(250, new { Sender = (object)fe, EventArgs = new FromEvent.TestEventArgs { Id = 3 } }),
                OnNext(350, new { Sender = (object)fe, EventArgs = new FromEvent.TestEventArgs { Id = 4 } }),
                OnNext(450, new { Sender = (object)fe, EventArgs = new FromEvent.TestEventArgs { Id = 5 } })
                );
        }

        [TestMethod]
        public void FromEvent_AddRemove_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.FromEvent<EventArgs>(null, h => { }));
            Throws<ArgumentNullException>(() => Observable.FromEvent<EventArgs>(h => { }, null));
        }

        [TestMethod]
        public void FromEvent_AddRemove_E4()
        {
            var scheduler = new TestScheduler();

            var fe = new FromEvent();

            scheduler.Schedule(() => fe.M2(1), 50);
            scheduler.Schedule(() => fe.M2(2), 150);
            scheduler.Schedule(() => fe.M2(3), 250);
            scheduler.Schedule(() => fe.M2(4), 350);
            scheduler.Schedule(() => fe.M2(5), 450);
            scheduler.Schedule(() => fe.M2(6), 1050);

            var results = scheduler.Run(() =>
                Observable.FromEvent<FromEvent.TestEventArgs>(
                    h => fe.E2 += h,
                    h => fe.E2 -= h)
                .Select(evt => new { evt.Sender, evt.EventArgs }));

            results.AssertEqual(
                OnNext(250, new { Sender = (object)fe, EventArgs = new FromEvent.TestEventArgs { Id = 3 } }),
                OnNext(350, new { Sender = (object)fe, EventArgs = new FromEvent.TestEventArgs { Id = 4 } }),
                OnNext(450, new { Sender = (object)fe, EventArgs = new FromEvent.TestEventArgs { Id = 5 } })
                );
        }

        [TestMethod]
        public void Generate_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Generate(0, DummyFunc<int, bool>.Instance, DummyFunc<int, int>.Instance, DummyFunc<int, int>.Instance, (IScheduler)null));
            Throws<ArgumentNullException>(() => Observable.Generate(0, (Func<int, bool>)null, DummyFunc<int, int>.Instance, DummyFunc<int, int>.Instance, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.Generate(0, DummyFunc<int, bool>.Instance, (Func<int, int>)null, DummyFunc<int, int>.Instance, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.Generate(0, DummyFunc<int, bool>.Instance, DummyFunc<int, int>.Instance, (Func<int, int>)null, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.Generate(0, DummyFunc<int, bool>.Instance, DummyFunc<int, int>.Instance, DummyFunc<int, int>.Instance, DummyScheduler.Instance).Subscribe(null));
        }

        [TestMethod]
        public void Generate_Finite()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Generate(0, x => x <= 3, x => x, x => x + 1, scheduler));

            results.AssertEqual(
                OnNext(201, 0),
                OnNext(202, 1),
                OnNext(203, 2),
                OnNext(204, 3),
                OnCompleted<int>(205)
                );
        }

        [TestMethod]
        public void Generate_Throw_Condition()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Generate(0, new Func<int, bool>(x => { throw new MockException(x); }),
                x => x,
                x => x + 1, scheduler));

            results.AssertEqual(
                OnError<int>(201, new MockException(0))
                );
        }

        [TestMethod]
        public void Generate_Throw_ResultSelector()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Generate(0, x => true,
                new Func<int, int>(x => { throw new MockException(x); }),
                x => x + 1, scheduler));

            results.AssertEqual(
                OnError<int>(201, new MockException(0))
                );
        }

        [TestMethod]
        public void Generate_Throw_Iterate()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Generate(0, x => true,
                x => x,
                new Func<int, int>(x => { throw new MockException(x); }), scheduler));

            results.AssertEqual(
                OnNext(201, 0),
                OnError<int>(202, new MockException(0))
                );
        }

        [TestMethod]
        public void Generate_Dispose()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Generate(0, x => true, x => x, x => x + 1, scheduler), 203);

            results.AssertEqual(
                OnNext(201, 0),
                OnNext(202, 1)
                );
        }

        [TestMethod]
        public void Generate_DefaultScheduler_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Generate(0, (Func<int, bool>)null, DummyFunc<int, int>.Instance, DummyFunc<int, int>.Instance));
            Throws<ArgumentNullException>(() => Observable.Generate(0, DummyFunc<int, bool>.Instance, (Func<int, int>)null, DummyFunc<int, int>.Instance));
            Throws<ArgumentNullException>(() => Observable.Generate(0, DummyFunc<int, bool>.Instance, DummyFunc<int, int>.Instance, (Func<int, int>)null));
            Throws<ArgumentNullException>(() => Observable.Generate(0, DummyFunc<int, bool>.Instance, DummyFunc<int, int>.Instance, DummyFunc<int, int>.Instance).Subscribe(null));
        }

        [TestMethod]
        public void Generate_DefaultScheduler()
        {
            Observable.Generate(0, x => x < 10, x => x, x => x + 1).AssertEqual(Observable.Generate(0, x => x < 10, x => x, x => x + 1, Scheduler.ThreadPool));
        }

#if !SILVERLIGHT && !NETCF37
        [TestMethod]
        public void TaskToObservable_NonVoid_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => TaskObservableExtensions.ToObservable((System.Threading.Tasks.Task<int>)null));
            var tcs = new System.Threading.Tasks.TaskCompletionSource<int>();
            var task = tcs.Task;
            Throws<ArgumentNullException>(() => task.ToObservable().Subscribe(null));
        }

        [TestMethod]
        public void TaskToObservable_NonVoid_Complete_BeforeCreate()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<int>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetResult(42), 10);

                results = scheduler.Run(() => taskSource.Task.ToObservable());
            });

            results.AssertEqual(
                OnNext(200, 42),
                OnCompleted<int>(200)
                );
        }

        [TestMethod]
        public void TaskToObservable_NonVoid_Complete_BeforeSubscribe()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<int>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetResult(42), 110);

                results = scheduler.Run(() => taskSource.Task.ToObservable());
            });

            results.AssertEqual(
                OnNext(200, 42),
                OnCompleted<int>(200)
                );
        }

        [TestMethod]
        public void TaskToObservable_NonVoid_Complete_BeforeDispose()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<int>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetResult(42), 300);

                results = scheduler.Run(() => taskSource.Task.ToObservable());
            });

            results.AssertEqual(
                OnNext(300, 42),
                OnCompleted<int>(300)
                );
        }

        [TestMethod]
        public void TaskToObservable_NonVoid_Complete_AfterDispose()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<int>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetResult(42), 1100);

                results = scheduler.Run(() => taskSource.Task.ToObservable());
            });

            results.AssertEqual(
                );
        }

        [TestMethod]
        public void TaskToObservable_NonVoid_Exception_BeforeCreate()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<int>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetException(new MockException(42)), 10);

                results = scheduler.Run(() => taskSource.Task.ToObservable());
            });

            var message = results.Single();
            Assert.AreEqual(200, message.Time);
            Assert.AreEqual(NotificationKind.OnError, message.Value.Kind);
            AggregateException ex = ((Notification<int>.OnError)message.Value).Exception as AggregateException;
            Assert.IsNotNull(ex);
            Assert.AreEqual(1, ex.InnerExceptions.Count);
            Assert.AreEqual(new MockException(42), ex.InnerException);
        }

        [TestMethod]
        public void TaskToObservable_NonVoid_Exception_BeforeSubscribe()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<int>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetException(new MockException(42)), 110);

                results = scheduler.Run(() => taskSource.Task.ToObservable());
            });

            var message = results.Single();
            Assert.AreEqual(200, message.Time);
            Assert.AreEqual(NotificationKind.OnError, message.Value.Kind);
            AggregateException ex = ((Notification<int>.OnError)message.Value).Exception as AggregateException;
            Assert.IsNotNull(ex);
            Assert.AreEqual(1, ex.InnerExceptions.Count);
            Assert.AreEqual(new MockException(42), ex.InnerException);
        }

        [TestMethod]
        public void TaskToObservable_NonVoid_Exception_BeforeDispose()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<int>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetException(new MockException(42)), 300);

                results = scheduler.Run(() => taskSource.Task.ToObservable());
            });

            var message = results.Single();
            Assert.AreEqual(300, message.Time);
            Assert.AreEqual(NotificationKind.OnError, message.Value.Kind);
            AggregateException ex = ((Notification<int>.OnError)message.Value).Exception as AggregateException;
            Assert.IsNotNull(ex);
            Assert.AreEqual(1, ex.InnerExceptions.Count);
            Assert.AreEqual(new MockException(42), ex.InnerException);
        }

        [TestMethod]
        public void TaskToObservable_NonVoid_Exception_AfterDispose()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<int>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetException(new MockException(42)), 1100);

                results = scheduler.Run(() => taskSource.Task.ToObservable());
            });

            results.AssertEqual(
                );
        }

        [TestMethod]
        public void TaskToObservable_NonVoid_Canceled_BeforeCreate()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<int>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetCanceled(), 10);

                results = scheduler.Run(() => taskSource.Task.ToObservable());
            });

            results.AssertEqual(
                OnCompleted<int>(200)
                );
        }

        [TestMethod]
        public void TaskToObservable_NonVoid_Canceled_BeforeSubscribe()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<int>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetCanceled(), 110);

                results = scheduler.Run(() => taskSource.Task.ToObservable());
            });

            results.AssertEqual(
                OnCompleted<int>(200)
                );
        }

        [TestMethod]
        public void TaskToObservable_NonVoid_Canceled_BeforeDispose()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<int>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetCanceled(), 300);

                results = scheduler.Run(() => taskSource.Task.ToObservable());
            });

            results.AssertEqual(
                OnCompleted<int>(300)
                );
        }

        [TestMethod]
        public void TaskToObservable_NonVoid_Canceled_AfterDispose()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<int>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetCanceled(), 1100);

                results = scheduler.Run(() => taskSource.Task.ToObservable());
            });

            results.AssertEqual(
                );
        }

        [TestMethod]
        public void TaskToObservable_Void_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => TaskObservableExtensions.ToObservable((System.Threading.Tasks.Task)null));
            var tcs = new System.Threading.Tasks.TaskCompletionSource<int>();
            System.Threading.Tasks.Task task = tcs.Task;
            Throws<ArgumentNullException>(() => task.ToObservable().Subscribe(null));
        }

        [TestMethod]
        public void TaskToObservable_Void_Complete_BeforeCreate()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<Unit>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetResult(42), 10);

                results = scheduler.Run(() => ((Task)taskSource.Task).ToObservable());
            });

            results.AssertEqual(
                OnNext(200, new Unit()),
                OnCompleted<Unit>(200)
                );
        }

        [TestMethod]
        public void TaskToObservable_Void_Complete_BeforeSubscribe()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<Unit>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetResult(42), 110);

                results = scheduler.Run(() => ((Task)taskSource.Task).ToObservable());
            });

            results.AssertEqual(
                OnNext(200, new Unit()),
                OnCompleted<Unit>(200)
                );
        }

        [TestMethod]
        public void TaskToObservable_Void_Complete_BeforeDispose()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<Unit>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetResult(42), 300);

                results = scheduler.Run(() => ((Task)taskSource.Task).ToObservable());
            });

            results.AssertEqual(
                OnNext(300, new Unit()),
                OnCompleted<Unit>(300)
                );
        }

        [TestMethod]
        public void TaskToObservable_Void_Complete_AfterDispose()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<Unit>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetResult(42), 1100);

                results = scheduler.Run(() => ((Task)taskSource.Task).ToObservable());
            });

            results.AssertEqual(
                );
        }

        [TestMethod]
        public void TaskToObservable_Void_Exception_BeforeCreate()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<Unit>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetException(new MockException(42)), 10);

                results = scheduler.Run(() => ((Task)taskSource.Task).ToObservable());
            });

            var message = results.Single();
            Assert.AreEqual(200, message.Time);
            Assert.AreEqual(NotificationKind.OnError, message.Value.Kind);
            AggregateException ex = ((Notification<Unit>.OnError)message.Value).Exception as AggregateException;
            Assert.IsNotNull(ex);
            Assert.AreEqual(1, ex.InnerExceptions.Count);
            Assert.AreEqual(new MockException(42), ex.InnerException);
        }

        [TestMethod]
        public void TaskToObservable_Void_Exception_BeforeSubscribe()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<Unit>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetException(new MockException(42)), 110);

                results = scheduler.Run(() => ((Task)taskSource.Task).ToObservable());
            });

            var message = results.Single();
            Assert.AreEqual(200, message.Time);
            Assert.AreEqual(NotificationKind.OnError, message.Value.Kind);
            AggregateException ex = ((Notification<Unit>.OnError)message.Value).Exception as AggregateException;
            Assert.IsNotNull(ex);
            Assert.AreEqual(1, ex.InnerExceptions.Count);
            Assert.AreEqual(new MockException(42), ex.InnerException);
        }

        [TestMethod]
        public void TaskToObservable_Void_Exception_BeforeDispose()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<Unit>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetException(new MockException(42)), 300);

                results = scheduler.Run(() => ((Task)taskSource.Task).ToObservable());
            });

            var message = results.Single();
            Assert.AreEqual(300, message.Time);
            Assert.AreEqual(NotificationKind.OnError, message.Value.Kind);
            AggregateException ex = ((Notification<Unit>.OnError)message.Value).Exception as AggregateException;
            Assert.IsNotNull(ex);
            Assert.AreEqual(1, ex.InnerExceptions.Count);
            Assert.AreEqual(new MockException(42), ex.InnerException);
        }

        [TestMethod]
        public void TaskToObservable_Void_Exception_AfterDispose()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<Unit>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetException(new MockException(42)), 1100);

                results = scheduler.Run(() => ((Task)taskSource.Task).ToObservable());
            });

            results.AssertEqual(
                );
        }

        [TestMethod]
        public void TaskToObservable_Void_Canceled_BeforeCreate()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<Unit>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetCanceled(), 10);

                results = scheduler.Run(() => ((Task)taskSource.Task).ToObservable());
            });

            results.AssertEqual(
                OnCompleted<Unit>(200)
                );
        }

        [TestMethod]
        public void TaskToObservable_Void_Canceled_BeforeSubscribe()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<Unit>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetCanceled(), 110);

                results = scheduler.Run(() => ((Task)taskSource.Task).ToObservable());
            });

            results.AssertEqual(
                OnCompleted<Unit>(200)
                );
        }

        [TestMethod]
        public void TaskToObservable_Void_Canceled_BeforeDispose()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<Unit>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetCanceled(), 300);

                results = scheduler.Run(() => ((Task)taskSource.Task).ToObservable());
            });

            results.AssertEqual(
                OnCompleted<Unit>(300)
                );
        }

        [TestMethod]
        public void TaskToObservable_Void_Canceled_AfterDispose()
        {
            var taskScheduler = new TestTaskScheduler();
            var taskFactory = new TaskFactory(taskScheduler);
            var results = default(IEnumerable<Recorded<Notification<Unit>>>);

            taskFactory.StartNew(() =>
            {
                var scheduler = new TestScheduler();

                var taskSource = new TaskCompletionSource<int>();
                taskSource.Task.ContinueWith(t => { var e = t.Exception; });

                scheduler.Schedule(() => taskSource.SetCanceled(), 1100);

                results = scheduler.Run(() => ((Task)taskSource.Task).ToObservable());
            });

            results.AssertEqual(
                );
        }
#endif

        [TestMethod]
        public void Defer_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Defer<int>(null));
            Throws<ArgumentNullException>(() => Observable.Defer(() => DummyObservable<int>.Instance).Subscribe(null));
            Throws<NullReferenceException>(() => Observable.Defer<int>(() => null).Subscribe());
            Throws<ArgumentNullException>(() => Observable.Defer(() => NullErrorObservable<int>.Instance).Subscribe());
        }

        [TestMethod]
        public void Defer_Complete()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;
            var xs = default(ColdObservable<int>);

            var results = scheduler.Run(() => Observable.Defer(() =>
            {
                invoked++;
                xs = scheduler.CreateColdObservable(
                    OnNext<int>(100, scheduler.Ticks),
                    OnCompleted<int>(200));
                return xs;
            }));

            results.AssertEqual(
                OnNext(300, 200),
                OnCompleted<int>(400)
                );

            Assert.AreEqual(1, invoked);

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 400)
                );

        }

        [TestMethod]
        public void Defer_Error()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;
            var xs = default(ColdObservable<int>);

            var results = scheduler.Run(() => Observable.Defer(() =>
            {
                invoked++;
                xs = scheduler.CreateColdObservable(
                    OnNext<int>(100, scheduler.Ticks),
                    OnError<int>(200, new MockException(scheduler.Ticks)));
                return xs;
            }));

            results.AssertEqual(
                OnNext(300, 200),
                OnError<int>(400, new MockException(200))
                );

            Assert.AreEqual(1, invoked);

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 400)
                );

        }

        [TestMethod]
        public void Defer_Dispose()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;
            var xs = default(ColdObservable<int>);

            var results = scheduler.Run(() => Observable.Defer(() =>
            {
                invoked++;
                xs = scheduler.CreateColdObservable(
                    OnNext<int>(100, scheduler.Ticks),
                    OnNext<int>(200, invoked),
                    OnNext<int>(1100, 1000));
                return xs;
            }));

            results.AssertEqual(
                OnNext(300, 200),
                OnNext(400, 1)
                );

            Assert.AreEqual(1, invoked);

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 1000)
                );

        }

        [TestMethod]
        public void Defer_Throw()
        {
            var scheduler = new TestScheduler();

            var invoked = 0;

            var results = scheduler.Run(() => Observable.Defer<int>(() =>
            {
                invoked++;
                throw new MockException(scheduler.Ticks);
            }));

            results.AssertEqual(
                OnError<int>(200, new MockException(200))
                );

            Assert.AreEqual(1, invoked);
        }

        [TestMethod]
        public void Using_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Using((Func<IDisposable>)null, DummyFunc<IDisposable, IObservable<int>>.Instance));
            Throws<ArgumentNullException>(() => Observable.Using(DummyFunc<IDisposable>.Instance, (Func<IDisposable, IObservable<int>>)null));
            Throws<NullReferenceException>(() => Observable.Using(() => DummyDisposable.Instance, d => default(IObservable<int>)).Subscribe());
            Throws<ArgumentNullException>(() => Observable.Using(() => DummyDisposable.Instance, d => DummyObservable<int>.Instance).Subscribe(null));
            Throws<ArgumentNullException>(() => Observable.Using(() => DummyDisposable.Instance, d => NullErrorObservable<int>.Instance).Subscribe());
        }

        [TestMethod]
        public void Using_Null()
        {
            var scheduler = new TestScheduler();

            var disposeInvoked = 0;
            var createInvoked = 0;
            var xs = default(ColdObservable<int>);
            var disposable = default(MockDisposable);
            var _d = default(MockDisposable);

            var results = scheduler.Run(() => Observable.Using(() =>
            {
                disposeInvoked++;
                disposable = default(MockDisposable);
                return disposable;
            },
            d =>
            {
                _d = d;
                createInvoked++;
                xs = scheduler.CreateColdObservable(
                    OnNext<int>(100, scheduler.Ticks),
                    OnCompleted<int>(200));
                return xs;
            }));

            Assert.AreSame(disposable, _d);

            results.AssertEqual(
                OnNext(300, 200),
                OnCompleted<int>(400)
                );

            Assert.AreEqual(1, createInvoked);
            Assert.AreEqual(1, disposeInvoked);

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 400)
                );

            Assert.IsNull(disposable);
        }

        [TestMethod]
        public void Using_Complete()
        {
            var scheduler = new TestScheduler();

            var disposeInvoked = 0;
            var createInvoked = 0;
            var xs = default(ColdObservable<int>);
            var disposable = default(MockDisposable);
            var _d = default(MockDisposable);

            var results = scheduler.Run(() => Observable.Using(() =>
                {
                    disposeInvoked++;
                    disposable = new MockDisposable(scheduler);
                    return disposable;
                },
            d =>
            {
                _d = d;
                createInvoked++;
                xs = scheduler.CreateColdObservable(
                    OnNext<int>(100, scheduler.Ticks),
                    OnCompleted<int>(200));
                return xs;
            }));

            Assert.AreSame(disposable, _d);

            results.AssertEqual(
                OnNext(300, 200),
                OnCompleted<int>(400)
                );

            Assert.AreEqual(1, createInvoked);
            Assert.AreEqual(1, disposeInvoked);

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 400)
                );

            disposable.AssertEqual(
                200,
                400
                );
        }

        [TestMethod]
        public void Using_Error()
        {
            var scheduler = new TestScheduler();

            var disposeInvoked = 0;
            var createInvoked = 0;
            var xs = default(ColdObservable<int>);
            var disposable = default(MockDisposable);
            var _d = default(MockDisposable);

            var results = scheduler.Run(() => Observable.Using(() =>
            {
                disposeInvoked++;
                disposable = new MockDisposable(scheduler);
                return disposable;
            },
            d =>
            {
                _d = d;
                createInvoked++;
                xs = scheduler.CreateColdObservable(
                    OnNext<int>(100, scheduler.Ticks),
                    OnError<int>(200, new MockException(scheduler.Ticks)));
                return xs;
            }));

            Assert.AreSame(disposable, _d);

            results.AssertEqual(
                OnNext(300, 200),
                OnError<int>(400, new MockException(200))
                );

            Assert.AreEqual(1, createInvoked);
            Assert.AreEqual(1, disposeInvoked);

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 400)
                );

            disposable.AssertEqual(
                200,
                400
                );
        }

        [TestMethod]
        public void Using_Dispose()
        {
            var scheduler = new TestScheduler();

            var disposeInvoked = 0;
            var createInvoked = 0;
            var xs = default(ColdObservable<int>);
            var disposable = default(MockDisposable);
            var _d = default(MockDisposable);

            var results = scheduler.Run(() => Observable.Using(() =>
            {
                disposeInvoked++;
                disposable = new MockDisposable(scheduler);
                return disposable;
            },
            d =>
            {
                _d = d;
                createInvoked++;
                xs = scheduler.CreateColdObservable(
                    OnNext<int>(100, scheduler.Ticks),
                    OnNext<int>(1000, scheduler.Ticks + 1));
                return xs;
            }));

            Assert.AreSame(disposable, _d);

            results.AssertEqual(
                OnNext(300, 200)
                );

            Assert.AreEqual(1, createInvoked);
            Assert.AreEqual(1, disposeInvoked);

            xs.Subscriptions.AssertEqual(
                Subscribe(200, 1000)
                );

            disposable.AssertEqual(
                200,
                1000
                );
        }

        [TestMethod]
        public void Using_ThrowResourceSelector()
        {
            var scheduler = new TestScheduler();

            var disposeInvoked = 0;
            var createInvoked = 0;

            var results = scheduler.Run(() => Observable.Using<int, IDisposable>(() =>
            {
                disposeInvoked++;
                throw new MockException(scheduler.Ticks);
            },
            d =>
            {
                createInvoked++;
                return Observable.Never<int>();
            }));

            results.AssertEqual(
                OnError<int>(200, new MockException(200))
                );

            Assert.AreEqual(0, createInvoked);
            Assert.AreEqual(1, disposeInvoked);
        }

        [TestMethod]
        public void Using_ThrowResourceUsage()
        {
            var scheduler = new TestScheduler();

            var disposeInvoked = 0;
            var createInvoked = 0;
            var disposable = default(MockDisposable);

            var results = scheduler.Run(() => Observable.Using<int, IDisposable>(() =>
            {
                disposeInvoked++;
                disposable = new MockDisposable(scheduler);
                return disposable;
            },
            d =>
            {
                createInvoked++;
                throw new MockException(scheduler.Ticks);
            }));

            results.AssertEqual(
                OnError<int>(200, new MockException(200))
                );

            Assert.AreEqual(1, createInvoked);
            Assert.AreEqual(1, disposeInvoked);

            disposable.AssertEqual(
                200,
                200
                );
        }

        [TestMethod]
        public void EnumerableToObservable_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.ToObservable((IEnumerable<int>)null, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.ToObservable(DummyEnumerable<int>.Instance, (IScheduler)null));
            Throws<ArgumentNullException>(() => Observable.ToObservable(DummyEnumerable<int>.Instance, DummyScheduler.Instance).Subscribe(null));
            Throws<NullReferenceException>(() => Observable.ToObservable(NullEnumeratorEnumerable<int>.Instance, Scheduler.CurrentThread).Subscribe());
        }

        [TestMethod]
        public void EnumerableToObservable_Complete()
        {
            var scheduler = new TestScheduler();

            var e = new MockEnumerable<int>(scheduler, new[] {3, 1, 2, 4});

            var results = scheduler.Run(() => e.ToObservable(scheduler));

            results.AssertEqual(
                OnNext(201, 3),
                OnNext(202, 1),
                OnNext(203, 2),
                OnNext(204, 4),
                OnCompleted<int>(205)
                );

            e.Subscriptions.AssertEqual(Subscribe(200, 205));
        }

        [TestMethod]
        public void EnumerableToObservable_Dispose()
        {
            var scheduler = new TestScheduler();

            var e = new MockEnumerable<int>(scheduler, new[] { 3, 1, 2, 4 });

            var results = scheduler.Run(() => e.ToObservable(scheduler), 203);

            results.AssertEqual(
                OnNext(201, 3),
                OnNext(202, 1)
                );

            e.Subscriptions.AssertEqual(Subscribe(200, 203));
        }

        static IEnumerable<int> EnumerableToObservable_Error_Core()
        {
            yield return 1;
            yield return 2;
            throw new MockException(3);
        }

        [TestMethod]
        public void EnumerableToObservable_Error()
        {
            var scheduler = new TestScheduler();

            var e = new MockEnumerable<int>(scheduler, EnumerableToObservable_Error_Core());

            var results = scheduler.Run(() => e.ToObservable(scheduler));

            results.AssertEqual(
                OnNext(201, 1),
                OnNext(202, 2),
                OnError<int>(203, new MockException(3))
                );

            e.Subscriptions.AssertEqual(Subscribe(200, 203));
        }

        [TestMethod]
        public void EnumerableToObservable_Default_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.ToObservable((IEnumerable<int>)null));
            Throws<ArgumentNullException>(() => Observable.ToObservable(DummyEnumerable<int>.Instance).Subscribe(null));
        }

        [TestMethod]
        public void EnumerableToObservable_Default()
        {
            var xs = new[] { 4, 3, 1, 5, 9, 2 };

            xs.ToObservable().AssertEqual(xs.ToObservable(Scheduler.ThreadPool));
        }

        [TestMethod]
        public void Create_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Create<int>(null));
            Throws<ArgumentNullException>(() => Observable.Create<int>(o => null).Subscribe(DummyObserver<int>.Instance));
            Throws<ArgumentNullException>(() => Observable.Create<int>(o => () => { }).Subscribe(null));
            Throws<ArgumentNullException>(() => Observable.Create<int>(o =>
            {
                o.OnError(null);
                return () => { };
            }).Subscribe(null));
        }

        [TestMethod]
        public void Create_Next()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Create<int>(o =>
            {
                o.OnNext(1);
                o.OnNext(2);
                return () => { };
            }));

            results.AssertEqual(
                OnNext(200, 1),
                OnNext(200, 2)
                );
        }

        [TestMethod]
        public void Create_Completed()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Create<int>(o =>
            {
                o.OnCompleted();
                o.OnNext(100);
                o.OnError(new MockException(100));
                o.OnCompleted();
                return () => { };
            }));

            results.AssertEqual(
                OnCompleted<int>(200)
                );
        }

        [TestMethod]
        public void Create_Error()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Create<int>(o =>
            {
                o.OnError(new MockException(1));
                o.OnNext(100);
                o.OnError(new MockException(100));
                o.OnCompleted();
                return () => { };
            }));

            results.AssertEqual(
                OnError<int>(200, new MockException(1))
                );
        }

        [TestMethod]
        public void Create_Exception()
        {
            Throws<InvalidOperationException>(() =>
                Observable.Create<int>(o => { throw new InvalidOperationException(); }).Subscribe());
        }

        [TestMethod]
        public void Create_Dispose()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Create<int>(o =>
            {
                var stopped = false;

                o.OnNext(1);
                o.OnNext(2);
                scheduler.Schedule(() =>
                {
                    if (!stopped)
                        o.OnNext(3);
                }, TimeSpan.FromTicks(600));
                scheduler.Schedule(() =>
                {
                    if (!stopped)
                        o.OnNext(4);
                }, TimeSpan.FromTicks(700));
                scheduler.Schedule(() =>
                {
                    if (!stopped)
                        o.OnNext(5);
                }, TimeSpan.FromTicks(900));
                scheduler.Schedule(() =>
                {
                    if (!stopped)
                        o.OnNext(6);
                }, TimeSpan.FromTicks(1100));

                return () => { stopped = true; };
            }));

            results.AssertEqual(
                OnNext(200, 1),
                OnNext(200, 2),
                OnNext(800, 3),
                OnNext(900, 4)
                );
        }

        [TestMethod]
        public void Create_ObserverThrows()
        {
            Throws<InvalidOperationException>(() =>
                Observable.Create<int>(o =>
                {
                    o.OnNext(1);
                    return () => { };
                }).Subscribe(x => { throw new InvalidOperationException(); }));
            Throws<InvalidOperationException>(() =>
                Observable.Create<int>(o =>
                {
                    o.OnError(new MockException(1));
                    return () => { };
                }).Subscribe(x => { }, ex => { throw new InvalidOperationException(); }));
            Throws<InvalidOperationException>(() =>
                Observable.Create<int>(o =>
                {
                    o.OnCompleted();
                    return () => { };
                }).Subscribe(x => { }, ex => { }, () => { throw new InvalidOperationException(); }));
        }

        [TestMethod]
        public void CreateWithDisposable_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.CreateWithDisposable<int>(null));
            Throws<ArgumentNullException>(() => Observable.CreateWithDisposable<int>(o => null).Subscribe(DummyObserver<int>.Instance));
            Throws<ArgumentNullException>(() => Observable.CreateWithDisposable<int>(o => DummyDisposable.Instance).Subscribe(null));
            Throws<ArgumentNullException>(() => Observable.CreateWithDisposable<int>(o =>
            {
                o.OnError(null);
                return DummyDisposable.Instance;
            }).Subscribe(null));
        }

        [TestMethod]
        public void CreateWithDisposable_Next()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.CreateWithDisposable<int>(o =>
            {
                o.OnNext(1);
                o.OnNext(2);
                return Disposable.Empty;
            }));

            results.AssertEqual(
                OnNext(200, 1),
                OnNext(200, 2)
                );
        }

        [TestMethod]
        public void CreateWithDisposable_Completed()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.CreateWithDisposable<int>(o =>
            {
                o.OnCompleted();
                o.OnNext(100);
                o.OnError(new MockException(100));
                o.OnCompleted();
                return Disposable.Empty;
            }));

            results.AssertEqual(
                OnCompleted<int>(200)
                );
        }

        [TestMethod]
        public void CreateWithDisposable_Error()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.CreateWithDisposable<int>(o =>
            {
                o.OnError(new MockException(1));
                o.OnNext(100);
                o.OnError(new MockException(100));
                o.OnCompleted();
                return Disposable.Empty;
            }));

            results.AssertEqual(
                OnError<int>(200, new MockException(1))
                );
        }

        [TestMethod]
        public void CreateWithDisposable_Exception()
        {
            Throws<InvalidOperationException>(() =>
                Observable.CreateWithDisposable<int>(o => { throw new InvalidOperationException(); }).Subscribe());
        }

        [TestMethod]
        public void CreateWithDisposable_Dispose()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.CreateWithDisposable<int>(o =>
            {
                var d = new BooleanDisposable();

                o.OnNext(1);
                o.OnNext(2);
                scheduler.Schedule(() =>
                {
                    if (!d.IsDisposed)
                        o.OnNext(3);
                }, TimeSpan.FromTicks(600));
                scheduler.Schedule(() =>
                {
                    if (!d.IsDisposed)
                        o.OnNext(4);
                }, TimeSpan.FromTicks(700));
                scheduler.Schedule(() =>
                {
                    if (!d.IsDisposed)
                        o.OnNext(5);
                }, TimeSpan.FromTicks(900));
                scheduler.Schedule(() =>
                {
                    if (!d.IsDisposed)
                        o.OnNext(6);
                }, TimeSpan.FromTicks(1100));

                return d;
            }));

            results.AssertEqual(
                OnNext(200, 1),
                OnNext(200, 2),
                OnNext(800, 3),
                OnNext(900, 4)
                );
        }

        [TestMethod]
        public void CreateWithDisposable_ObserverThrows()
        {
            Throws<InvalidOperationException>(() =>
                Observable.CreateWithDisposable<int>(o =>
                {
                    o.OnNext(1);
                    return Disposable.Empty;
                }).Subscribe(x => { throw new InvalidOperationException(); }));
            Throws<InvalidOperationException>(() =>
                Observable.CreateWithDisposable<int>(o =>
                {
                    o.OnError(new MockException(1));
                    return Disposable.Empty;
                }).Subscribe(x => { }, ex => { throw new InvalidOperationException(); }));
            Throws<InvalidOperationException>(() =>
                Observable.CreateWithDisposable<int>(o =>
                {
                    o.OnCompleted();
                    return Disposable.Empty;
                }).Subscribe(x => { }, ex => { }, () => { throw new InvalidOperationException(); }));
        }

        [TestMethod]
        public void Range_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Range(0, 0, null));
            Throws<ArgumentOutOfRangeException>(() => Observable.Range(0, -1, DummyScheduler.Instance));
            Throws<ArgumentOutOfRangeException>(() => Observable.Range(int.MaxValue, 2, DummyScheduler.Instance));
        }

        [TestMethod]
        public void Range_Zero()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Range(0, 0, scheduler));

            results.AssertEqual(
                OnCompleted<int>(201)
                );
        }

        [TestMethod]
        public void Range_One()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Range(0, 1, scheduler));

            results.AssertEqual(
                OnNext(201, 0),
                OnCompleted<int>(202)
                );
        }

        [TestMethod]
        public void Range_Five()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Range(10, 5, scheduler));

            results.AssertEqual(
                OnNext(201, 10),
                OnNext(202, 11),
                OnNext(203, 12),
                OnNext(204, 13),
                OnNext(205, 14),
                OnCompleted<int>(206)
                );
        }

        [TestMethod]
        public void Range_Dispose()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Range(-10, 5, scheduler), 204);

            results.AssertEqual(
                OnNext(201, -10),
                OnNext(202, -9),
                OnNext(203, -8)
                );
        }

        [TestMethod]
        public void Range_Default_ArgumentChecking()
        {
            Throws<ArgumentOutOfRangeException>(() => Observable.Range(0, -1));
            Throws<ArgumentOutOfRangeException>(() => Observable.Range(int.MaxValue, 2));
        }

        [TestMethod]
        public void Range_Default()
        {
            for (int i = 0; i < 100; i++)
                Observable.Range(100, 100).AssertEqual(Observable.Range(100, 100, Scheduler.ThreadPool));
        }

        [TestMethod]
        public void Repeat_Observable_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Repeat<int>(null, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.Repeat(null));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.Repeat(DummyScheduler.Instance).Subscribe(null));
            Throws<ArgumentNullException>(() => NullErrorObservable<int>.Instance.Repeat(Scheduler.CurrentThread).Subscribe(DummyObserver<int>.Instance));
        }

        [TestMethod]
        public void Repeat_Observable_Basic()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(100, 1),
                OnNext(150, 2),
                OnNext(200, 3),
                OnCompleted<int>(250)
                );

            var results = scheduler.Run(() => xs.Repeat(scheduler));

            results.AssertEqual(
                OnNext(301, 1),
                OnNext(351, 2),
                OnNext(401, 3),
                OnNext(552, 1),
                OnNext(602, 2),
                OnNext(652, 3),
                OnNext(803, 1),
                OnNext(853, 2),
                OnNext(903, 3)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 452),
                Subscribe(452, 703),
                Subscribe(703, 954),
                Subscribe(954, 1000)
                );
        }

        [TestMethod]
        public void Repeat_Observable_Infinite()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(100, 1),
                OnNext(150, 2),
                OnNext(200, 3)
                );

            var results = scheduler.Run(() => xs.Repeat(scheduler));

            results.AssertEqual(
                OnNext(301, 1),
                OnNext(351, 2),
                OnNext(401, 3)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 1000)
                );
        }

        [TestMethod]
        public void Repeat_Observable_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(100, 1),
                OnNext(150, 2),
                OnNext(200, 3),
                OnError<int>(250, new MockException(1))
                );

            var results = scheduler.Run(() => xs.Repeat(scheduler));

            results.AssertEqual(
                OnNext(301, 1),
                OnNext(351, 2),
                OnNext(401, 3),
                OnError<int>(451, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 451)
                );
        }

        [TestMethod]
        public void Repeat_Observable_Throws()
        {
            var scheduler1 = new TestScheduler();

            var xs = Observable.Return(1, scheduler1).Repeat(scheduler1);

            xs.Subscribe(x => { throw new InvalidOperationException(); });

            Throws<InvalidOperationException>(() => scheduler1.Run());

            var scheduler2 = new TestScheduler();

            var ys = Observable.Throw<int>(new MockException(1), scheduler2).Repeat(scheduler2);

            ys.Subscribe(x => { }, ex => { throw new InvalidOperationException(); });

            Throws<InvalidOperationException>(() => scheduler2.Run());

            var scheduler3 = new TestScheduler();

            var zs = Observable.Return(1, scheduler3).Repeat(scheduler3);

            var d = zs.Subscribe(x => { }, ex => { }, () => { throw new InvalidOperationException(); });

            scheduler3.Schedule(() => d.Dispose(), 210);

            scheduler3.Run();

            var scheduler4 = new TestScheduler();

            var xss = new SubscribeThrowsObservable<int>().Repeat(scheduler4);

            xss.Subscribe();

            Throws<InvalidOperationException>(() => scheduler4.Run());
        }

        [TestMethod]
        public void Repeat_Observable_Default_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Repeat<int>((IObservable<int>)null));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.Repeat().Subscribe(null));
        }

        [TestMethod]
        public void Repeat_Observable_Default()
        {
            Observable.Range(1, 3).Repeat().Take(10).AssertEqual(Observable.Range(1, 3).Repeat(Scheduler.ThreadPool).Take(10));
        }

        [TestMethod]
        public void Repeat_Observable_RepeatCount_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Repeat<int>(null, 0, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.Repeat(0, null));
            Throws<ArgumentOutOfRangeException>(() => DummyObservable<int>.Instance.Repeat(-1, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.Repeat(0, DummyScheduler.Instance).Subscribe(null));
            Throws<ArgumentNullException>(() => NullErrorObservable<int>.Instance.Repeat(1, Scheduler.CurrentThread).Subscribe());
        }

        [TestMethod]
        public void Repeat_Observable_RepeatCount_Basic()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(5, 1),
                OnNext(10, 2),
                OnNext(15, 3),
                OnCompleted<int>(20)
                );

            var results = scheduler.Run(() => xs.Repeat(3, scheduler));

            results.AssertEqual(
                OnNext(206, 1),
                OnNext(211, 2),
                OnNext(216, 3),
                OnNext(227, 1),
                OnNext(232, 2),
                OnNext(237, 3),
                OnNext(248, 1),
                OnNext(253, 2),
                OnNext(258, 3),
                OnCompleted<int>(264)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 222),
                Subscribe(222, 243),
                Subscribe(243, 264)
                );
        }

        [TestMethod]
        public void Repeat_Observable_RepeatCount_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(5, 1),
                OnNext(10, 2),
                OnNext(15, 3),
                OnCompleted<int>(20)
                );

            var results = scheduler.Run(() => xs.Repeat(3, scheduler), 231);

            results.AssertEqual(
                OnNext(206, 1),
                OnNext(211, 2),
                OnNext(216, 3),
                OnNext(227, 1)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 222),
                Subscribe(222, 231)
                );
        }

        [TestMethod]
        public void Repeat_Observable_RepeatCount_Infinite()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(100, 1),
                OnNext(150, 2),
                OnNext(200, 3)
                );

            var results = scheduler.Run(() => xs.Repeat(3, scheduler));

            results.AssertEqual(
                OnNext(301, 1),
                OnNext(351, 2),
                OnNext(401, 3)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 1000)
                );
        }

        [TestMethod]
        public void Repeat_Observable_RepeatCount_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(100, 1),
                OnNext(150, 2),
                OnNext(200, 3),
                OnError<int>(250, new MockException(1))
                );

            var results = scheduler.Run(() => xs.Repeat(3, scheduler));

            results.AssertEqual(
                OnNext(301, 1),
                OnNext(351, 2),
                OnNext(401, 3),
                OnError<int>(451, new MockException(1))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 451)
                );
        }

        [TestMethod]
        public void Repeat_Observable_RepeatCount_Throws()
        {
            var scheduler1 = new TestScheduler();

            var xs = Observable.Return(1, scheduler1).Repeat(3, scheduler1);

            xs.Subscribe(x => { throw new InvalidOperationException(); });

            Throws<InvalidOperationException>(() => scheduler1.Run());

            var scheduler2 = new TestScheduler();

            var ys = Observable.Throw<int>(new MockException(1), scheduler2).Repeat(3, scheduler2);

            ys.Subscribe(x => { }, ex => { throw new InvalidOperationException(); });

            Throws<InvalidOperationException>(() => scheduler2.Run());

            var scheduler3 = new TestScheduler();

            var zs = Observable.Return(1, scheduler3).Repeat(100, scheduler3);

            var d = zs.Subscribe(x => { }, ex => { }, () => { throw new InvalidOperationException(); });

            scheduler3.Schedule(() => d.Dispose(), 10);

            scheduler3.Run();

            var scheduler4 = new TestScheduler();

            var xss = new SubscribeThrowsObservable<int>().Repeat(3, scheduler4);

            xss.Subscribe();

            Throws<InvalidOperationException>(() => scheduler4.Run());
        }

        [TestMethod]
        public void Repeat_Observable_RepeatCount_Default_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Repeat<int>(default(IObservable<int>), 0));
            Throws<ArgumentOutOfRangeException>(() => DummyObservable<int>.Instance.Repeat(-1));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.Repeat(0).Subscribe(null));
        }

        [TestMethod]
        public void Repeat_Observable_RepeatCount_Default()
        {
            Observable.Range(1, 3).Repeat(3).AssertEqual(Observable.Range(1, 3).Repeat(3, Scheduler.ThreadPool));
        }

        [TestMethod]
        public void Retry_Observable_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Retry<int>(null, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.Retry(null));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.Retry(DummyScheduler.Instance).Subscribe(null));
            Throws<ArgumentNullException>(() => NullErrorObservable<int>.Instance.Retry(Scheduler.CurrentThread).Subscribe(DummyObserver<int>.Instance));
        }

        [TestMethod]
        public void Retry_Observable_Basic()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(100, 1),
                OnNext(150, 2),
                OnNext(200, 3),
                OnCompleted<int>(250)
                );

            var results = scheduler.Run(() => xs.Retry(scheduler));

            results.AssertEqual(
                OnNext(301, 1),
                OnNext(351, 2),
                OnNext(401, 3),
                OnCompleted<int>(451)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 451)
                );
        }

        [TestMethod]
        public void Retry_Observable_Infinite()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(100, 1),
                OnNext(150, 2),
                OnNext(200, 3)
                );

            var results = scheduler.Run(() => xs.Retry(scheduler));

            results.AssertEqual(
                OnNext(301, 1),
                OnNext(351, 2),
                OnNext(401, 3)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 1000)
                );
        }

        [TestMethod]
        public void Retry_Observable_Error()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(100, 1),
                OnNext(150, 2),
                OnNext(200, 3),
                OnError<int>(250, new MockException(1))
                );

            var results = scheduler.Run(() => xs.Retry(scheduler), 1100);

            results.AssertEqual(
                OnNext(301, 1),
                OnNext(351, 2),
                OnNext(401, 3),
                OnNext(552, 1),
                OnNext(602, 2),
                OnNext(652, 3),
                OnNext(803, 1),
                OnNext(853, 2),
                OnNext(903, 3),
                OnNext(1054, 1)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 452),
                Subscribe(452, 703),
                Subscribe(703, 954),
                Subscribe(954, 1100)
                );
        }

        [TestMethod]
        public void Retry_Observable_Throws()
        {
            var scheduler1 = new TestScheduler();

            var xs = Observable.Return(1, scheduler1).Retry(scheduler1);

            xs.Subscribe(x => { throw new InvalidOperationException(); });

            Throws<InvalidOperationException>(() => scheduler1.Run());

            var scheduler2 = new TestScheduler();

            var ys = Observable.Throw<int>(new MockException(1), scheduler2).Retry(scheduler2);

            var d = ys.Subscribe(x => { }, ex => { throw new InvalidOperationException(); });

            scheduler2.Schedule(() => d.Dispose(), 210);

            scheduler2.Run();

            var scheduler3 = new TestScheduler();

            var zs = Observable.Return(1, scheduler3).Retry(scheduler3);

            zs.Subscribe(x => { }, ex => { }, () => { throw new InvalidOperationException(); });

            Throws<InvalidOperationException>(() => scheduler3.Run());

            var scheduler4 = new TestScheduler();

            var xss = new SubscribeThrowsObservable<int>().Retry(scheduler4);

            xss.Subscribe();

            Throws<InvalidOperationException>(() => scheduler4.Run());
        }

        [TestMethod]
        public void Retry_Observable_Default_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Retry<int>((IObservable<int>)null));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.Retry().Subscribe(null));
        }

        [TestMethod]
        public void Retry_Observable_Default()
        {
            Observable.Range(1, 3).Concat(Observable.Throw<int>(new Exception())).Retry().Take(10)
                .AssertEqual(Observable.Range(1, 3).Concat(Observable.Throw<int>(new Exception())).Retry(Scheduler.ThreadPool).Take(10));
        }

        [TestMethod]
        public void Retry_Observable_RetryCount_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Retry<int>(null, 0, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.Retry(0, null));
            Throws<ArgumentOutOfRangeException>(() => DummyObservable<int>.Instance.Retry(-1, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.Retry(0, DummyScheduler.Instance).Subscribe(null));
            Throws<ArgumentNullException>(() => NullErrorObservable<int>.Instance.Retry(1, Scheduler.CurrentThread).Subscribe());
        }

        [TestMethod]
        public void Retry_Observable_RetryCount_Basic()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(5, 1),
                OnNext(10, 2),
                OnNext(15, 3),
                OnError<int>(20, new MockException(32))
                );

            var results = scheduler.Run(() => xs.Retry(3, scheduler));

            results.AssertEqual(
                OnNext(206, 1),
                OnNext(211, 2),
                OnNext(216, 3),
                OnNext(227, 1),
                OnNext(232, 2),
                OnNext(237, 3),
                OnNext(248, 1),
                OnNext(253, 2),
                OnNext(258, 3),
                OnError<int>(264, new MockException(32))
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 222),
                Subscribe(222, 243),
                Subscribe(243, 264)
                );
        }

        [TestMethod]
        public void Retry_Observable_RetryCount_Dispose()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(5, 1),
                OnNext(10, 2),
                OnNext(15, 3),
                OnError<int>(20, new MockException(32))
                );

            var results = scheduler.Run(() => xs.Retry(3, scheduler), 231);

            results.AssertEqual(
                OnNext(206, 1),
                OnNext(211, 2),
                OnNext(216, 3),
                OnNext(227, 1)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 222),
                Subscribe(222, 231)
                );
        }

        [TestMethod]
        public void Retry_Observable_RetryCount_Infinite()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(100, 1),
                OnNext(150, 2),
                OnNext(200, 3)
                );

            var results = scheduler.Run(() => xs.Retry(3, scheduler));

            results.AssertEqual(
                OnNext(301, 1),
                OnNext(351, 2),
                OnNext(401, 3)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 1000)
                );
        }

        [TestMethod]
        public void Retry_Observable_RetryCount_Completed()
        {
            var scheduler = new TestScheduler();

            var xs = scheduler.CreateColdObservable(
                OnNext(100, 1),
                OnNext(150, 2),
                OnNext(200, 3),
                OnCompleted<int>(250)
                );

            var results = scheduler.Run(() => xs.Retry(3, scheduler));

            results.AssertEqual(
                OnNext(301, 1),
                OnNext(351, 2),
                OnNext(401, 3),
                OnCompleted<int>(451)
                );

            xs.Subscriptions.AssertEqual(
                Subscribe(201, 451)
                );
        }

        [TestMethod]
        public void Retry_Observable_RetryCount_Throws()
        {
            var scheduler1 = new TestScheduler();

            var xs = Observable.Return(1, scheduler1).Retry(3, scheduler1);

            xs.Subscribe(x => { throw new InvalidOperationException(); });

            Throws<InvalidOperationException>(() => scheduler1.Run());

            var scheduler2 = new TestScheduler();

            var ys = Observable.Throw<int>(new MockException(1), scheduler2).Retry(100, scheduler2);

            var d = ys.Subscribe(x => { }, ex => { throw new InvalidOperationException(); });

            scheduler2.Schedule(() => d.Dispose(), 10);

            scheduler2.Run();

            var scheduler3 = new TestScheduler();

            var zs = Observable.Return(1, scheduler3).Retry(100, scheduler3);

            zs.Subscribe(x => { }, ex => { }, () => { throw new InvalidOperationException(); });

            Throws<InvalidOperationException>(() => scheduler3.Run());

            var scheduler4 = new TestScheduler();

            var xss = new SubscribeThrowsObservable<int>().Retry(3, scheduler4);

            xss.Subscribe();

            Throws<InvalidOperationException>(() => scheduler4.Run());
        }

        [TestMethod]
        public void Retry_Observable_RetryCount_Default_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Retry<int>(default(IObservable<int>), 0));
            Throws<ArgumentOutOfRangeException>(() => DummyObservable<int>.Instance.Retry(-1));
            Throws<ArgumentNullException>(() => DummyObservable<int>.Instance.Retry(0).Subscribe(null));
        }

        [TestMethod]
        public void Retry_Observable_RetryCount_Default()
        {
            Observable.Range(1, 3).Retry(3).AssertEqual(Observable.Range(1, 3).Retry(3, Scheduler.ThreadPool));
        }

        [TestMethod]
        public void Repeat_Value_Count_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Repeat(1, 0, default(IScheduler)));
            Throws<ArgumentOutOfRangeException>(() => Observable.Repeat(1, -1, DummyScheduler.Instance));
            Throws<ArgumentNullException>(() => Observable.Repeat(1, 1, DummyScheduler.Instance).Subscribe(null));
        }

        [TestMethod]
        public void Repeat_Value_Count_Zero()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Repeat(42, 0, scheduler));

            results.AssertEqual(
                OnCompleted<int>(201)
                );
        }

        [TestMethod]
        public void Repeat_Value_Count_One()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Repeat(42, 1, scheduler));

            results.AssertEqual(
                OnNext(201, 42),
                OnCompleted<int>(202)
                );
        }

        [TestMethod]
        public void Repeat_Value_Count_Ten()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Repeat(42, 10, scheduler));

            results.AssertEqual(
                OnNext(201, 42),
                OnNext(202, 42),
                OnNext(203, 42),
                OnNext(204, 42),
                OnNext(205, 42),
                OnNext(206, 42),
                OnNext(207, 42),
                OnNext(208, 42),
                OnNext(209, 42),
                OnNext(210, 42),
                OnCompleted<int>(211)
                );
        }

        [TestMethod]
        public void Repeat_Value_Count_Dispose()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Repeat(42, 10, scheduler), 207);

            results.AssertEqual(
                OnNext(201, 42),
                OnNext(202, 42),
                OnNext(203, 42),
                OnNext(204, 42),
                OnNext(205, 42),
                OnNext(206, 42)
                );
        }

        [TestMethod]
        public void Repeat_Value_Count_Default_ArgumentChecking()
        {
            Throws<ArgumentOutOfRangeException>(() => Observable.Repeat(1, -1));
            Throws<ArgumentNullException>(() => Observable.Repeat(1, 1).Subscribe(null));
        }

        [TestMethod]
        public void Repeat_Value_Count_Default()
        {
            Observable.Repeat(42, 10).AssertEqual(Observable.Repeat(42, 10, Scheduler.ThreadPool));
        }

        [TestMethod]
        public void Repeat_Value_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Repeat(1, (IScheduler)null));
            Throws<ArgumentNullException>(() => Observable.Repeat(DummyScheduler.Instance, 1).Subscribe(null));
        }

        [TestMethod]
        public void Repeat_Value()
        {
            var scheduler = new TestScheduler();

            var results = scheduler.Run(() => Observable.Repeat(42, scheduler), 207);

            results.AssertEqual(
                OnNext(201, 42),
                OnNext(202, 42),
                OnNext(203, 42),
                OnNext(204, 42),
                OnNext(205, 42),
                OnNext(206, 42)
                );
        }

        [TestMethod]
        public void Repeat_Value_Default_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Repeat(1).Subscribe(null));
        }

        [TestMethod]
        public void Repeat_Value_Default()
        {
            Observable.Repeat(42).Take(100).AssertEqual(Observable.Repeat(42, Scheduler.ThreadPool).Take(100));
        }

    }
}
