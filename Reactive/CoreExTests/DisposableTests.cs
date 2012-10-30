using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Disposables;

using System.Collections;
using System.Threading;
using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Concurrency;


namespace Microsoft.LiveLabs.CoreExTests
{
    [TestClass]
    public class DisposableTests
    {
        [TestMethod]
        public void AnonymousDisposable_Create()
        {
            var d = Disposable.Create(() => { });
            Assert.IsNotNull(d);
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void AnonymousDisposable_CreateNull()
        {
            Disposable.Create(null);
        }

        [TestMethod]
        public void AnonymousDisposable_Dispose()
        {
            var disposed = false;
            var d = Disposable.Create(() => { disposed = true; });
            Assert.IsFalse(disposed);
            d.Dispose();
            Assert.IsTrue(disposed);
        }

        [TestMethod]
        public void EmptyDisposable()
        {
            var d = Disposable.Empty;
            Assert.IsNotNull(d);
            d.Dispose();
        }

        [TestMethod]
        public void BooleanDisposable()
        {
            var d = new BooleanDisposable();
            Assert.IsFalse(d.IsDisposed);
            d.Dispose();
            Assert.IsTrue(d.IsDisposed);
            d.Dispose();
            Assert.IsTrue(d.IsDisposed);
        }

        [TestMethod]
        public void FutureDisposable_SetNull()
        {
            var d = new MutableDisposable();
            d.Disposable = null;
        }

        [TestMethod]
        public void FutureDisposable_DisposeAfterSet()
        {
            var disposed = false;

            var d = new MutableDisposable();
            var dd = Disposable.Create(() => { disposed = true; });
            d.Disposable = dd;

            Assert.AreSame(dd, d.Disposable);

            Assert.IsFalse(disposed);
            d.Dispose();
            Assert.IsTrue(disposed);
            d.Dispose();
            Assert.IsTrue(disposed);
        }

        [TestMethod]
        public void FutureDisposable_DisposeBeforeSet()
        {
            var disposed = false;

            var d = new MutableDisposable();
            var dd = Disposable.Create(() => { disposed = true; });

            Assert.IsFalse(disposed);
            d.Dispose();
            Assert.IsFalse(disposed);

            d.Disposable = dd;
            Assert.IsNull(d.Disposable); // CHECK!
            Assert.IsTrue(disposed);

            d.Dispose();
            Assert.IsTrue(disposed);
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void GroupDisposable_Ctor_Null()
        {
            new CompositeDisposable(null);
        }

        [TestMethod, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GroupDisposable_Ctor_HasNull()
        {
            new CompositeDisposable(Disposable.Empty, null, Disposable.Empty);
        }

        [TestMethod]
        public void GroupDisposable_Contains()
        {
            var d1 = Disposable.Create(() => {} );
            var d2 = Disposable.Create(() => { });
            var g = new CompositeDisposable(d1, d2);
            Assert.AreEqual(2, g.Count);
            Assert.IsTrue(g.Contains(d1));
            Assert.IsTrue(g.Contains(d2));
        }

        [TestMethod]
        public void GroupDisposable_IsReadOnly()
        {
            Assert.IsFalse(new CompositeDisposable().IsReadOnly);
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void GroupDisposable_CopyTo_Null()
        {
            new CompositeDisposable().CopyTo(null, 0);
        }

        [TestMethod, ExpectedException(typeof(IndexOutOfRangeException))]
        public void GroupDisposable_CopyTo_Negative()
        {
            new CompositeDisposable().CopyTo(new IDisposable[2], -1);
        }

        [TestMethod, ExpectedException(typeof(IndexOutOfRangeException))]
        public void GroupDisposable_CopyTo_BeyondEnd()
        {
            new CompositeDisposable().CopyTo(new IDisposable[2], 2);
        }

        [TestMethod]
        public void GroupDisposable_CopyTo()
        {
            var d1 = Disposable.Create(() => { });
            var d2 = Disposable.Create(() => { });
            var g = new CompositeDisposable(d1, d2);

            var d = new IDisposable[3];
            g.CopyTo(d, 1);
            Assert.AreSame(d1, d[1]);
            Assert.AreSame(d2, d[2]);
        }

        [TestMethod]
        public void GroupDisposable_ToArray()
        {
            var d1 = Disposable.Create(() => { });
            var d2 = Disposable.Create(() => { });
            var g = new CompositeDisposable(d1, d2);
            Assert.AreEqual(2, g.Count);
            var x = Enumerable.ToArray(g);
            Assert.IsTrue(g.ToArray().SequenceEqual(new[] { d1, d2 }));
        }

        [TestMethod]
        public void GroupDisposable_GetEnumerator()
        {
            var d1 = Disposable.Create(() => { });
            var d2 = Disposable.Create(() => { });
            var g = new CompositeDisposable(d1, d2);
            var lst = new List<IDisposable>();
            foreach (var x in g)
                lst.Add(x);
            Assert.IsTrue(lst.SequenceEqual(new[] { d1, d2 }));
        }

        [TestMethod]
        public void GroupDisposable_GetEnumeratorNonGeneric()
        {
            var d1 = Disposable.Create(() => { });
            var d2 = Disposable.Create(() => { });
            var g = new CompositeDisposable(d1, d2);
            var lst = new List<IDisposable>();
            foreach (IDisposable x in (IEnumerable)g)
                lst.Add(x);
            Assert.IsTrue(lst.SequenceEqual(new[] { d1, d2 }));
        }

        [TestMethod]
        public void GroupDisposable_CollectionInitializer()
        {
            var d1 = Disposable.Create(() => { });
            var d2 = Disposable.Create(() => { });
            var g = new CompositeDisposable { d1, d2 };
            Assert.AreEqual(2, g.Count);
            Assert.IsTrue(g.Contains(d1));
            Assert.IsTrue(g.Contains(d2));
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void GroupDisposable_AddNull()
        {
            new CompositeDisposable().Add(null);
        }

        [TestMethod]
        public void GroupDisposable_Add()
        {
            var d1 = Disposable.Create(() => { });
            var d2 = Disposable.Create(() => { });
            var g = new CompositeDisposable(d1);
            Assert.AreEqual(1, g.Count);
            Assert.IsTrue(g.Contains(d1));
            g.Add(d2);
            Assert.AreEqual(2, g.Count);
            Assert.IsTrue(g.Contains(d2));
        }

        [TestMethod]
        public void GroupDisposable_AddAfterDispose()
        {
            var disp1 = false;
            var disp2 = false;

            var d1 = Disposable.Create(() => { disp1 = true; });
            var d2 = Disposable.Create(() => { disp2 = true; });
            var g = new CompositeDisposable(d1);
            Assert.AreEqual(1, g.Count);

            g.Dispose();
            Assert.IsTrue(disp1);
            Assert.AreEqual(0, g.Count); // CHECK

            g.Add(d2);
            Assert.IsTrue(disp2);
            Assert.AreEqual(0, g.Count); // CHECK
        }

        [TestMethod]
        public void GroupDisposable_Remove()
        {
            var disp1 = false;
            var disp2 = false;

            var d1 = Disposable.Create(() => { disp1 = true; });
            var d2 = Disposable.Create(() => { disp2 = true; });
            var g = new CompositeDisposable(d1, d2);

            Assert.AreEqual(2, g.Count);
            Assert.IsTrue(g.Contains(d1));
            Assert.IsTrue(g.Contains(d2));

            Assert.IsTrue(g.Remove(d1));
            Assert.AreEqual(1, g.Count);
            Assert.IsFalse(g.Contains(d1));
            Assert.IsTrue(g.Contains(d2));
            Assert.IsTrue(disp1);

            Assert.IsTrue(g.Remove(d2));
            Assert.IsFalse(g.Contains(d1));
            Assert.IsFalse(g.Contains(d2));
            Assert.IsTrue(disp2);

            var disp3 = false;
            var d3 = Disposable.Create(() => { disp3 = true; });
            Assert.IsFalse(g.Remove(d3));
            Assert.IsFalse(disp3);
        }

        [TestMethod]
        public void GroupDisposable_Clear()
        {
            var disp1 = false;
            var disp2 = false;

            var d1 = Disposable.Create(() => { disp1 = true; });
            var d2 = Disposable.Create(() => { disp2 = true; });
            var g = new CompositeDisposable(d1, d2);
            Assert.AreEqual(2, g.Count);

            g.Clear();
            Assert.IsTrue(disp1);
            Assert.IsTrue(disp2);
            Assert.AreEqual(0, g.Count);

            var disp3 = false;
            var d3 = Disposable.Create(() => { disp3 = true; });
            g.Add(d3);
            Assert.IsFalse(disp3);
            Assert.AreEqual(1, g.Count);
        }


        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void GroupDisposable_RemoveNull()
        {
            new CompositeDisposable().Remove(null);
        }

#if !SILVERLIGHT && !NETCF37
        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void CancellationDisposable_Ctor_Null()
        {
            new CancellationDisposable(null);
        }

        [TestMethod]
        public void CancellationDisposable_DefaultCtor()
        {
            var c = new CancellationDisposable();
            Assert.IsNotNull(c.Token);
            Assert.IsFalse(c.Token.IsCancellationRequested);
            Assert.IsTrue(c.Token.CanBeCanceled);
            c.Dispose();
            Assert.IsTrue(c.Token.IsCancellationRequested);
        }

        [TestMethod]
        public void CancellationDisposable_TokenCtor()
        {
            var t = new CancellationTokenSource();
            var c = new CancellationDisposable(t);
            Assert.IsTrue(t.Token == c.Token);
            Assert.IsFalse(c.Token.IsCancellationRequested);
            Assert.IsTrue(c.Token.CanBeCanceled);
            c.Dispose();
            Assert.IsTrue(c.Token.IsCancellationRequested);
        }
#endif
        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void ContextDisposable_CreateNullContext()
        {
            new ContextDisposable(null, Disposable.Empty);
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void ContextDisposable_CreateNullDisposable()
        {
            new ContextDisposable(new SynchronizationContext(), null);
        }

        [TestMethod]
        public void ContextDisposable()
        {
            var disp = false;
            var m = new MySync();
            var c = new ContextDisposable(m, Disposable.Create(() => { disp = true; }));
            Assert.IsFalse(m._disposed);
            Assert.IsFalse(disp);
            c.Dispose();
            Assert.IsTrue(m._disposed);
            Assert.IsTrue(disp);
        }

        class MySync : SynchronizationContext
        {
            internal bool _disposed = false;

            public override void Post(SendOrPostCallback d, object state)
            {
                d(state);
                _disposed = true;
            }
        }

        [TestMethod]
        public void MutableDisposable_Ctor_Prop()
        {
            var m = new MutableDisposable();
            Assert.IsNull(m.Disposable);
        }

        [TestMethod]
        public void MutableDisposable_ReplaceBeforeDispose()
        {
            var disp1 = false;
            var disp2 = false;

            var m = new MutableDisposable();
            var d1 = Disposable.Create(() => { disp1 = true; });
            m.Disposable = d1;
            Assert.AreSame(d1, m.Disposable);
            Assert.IsFalse(disp1);

            var d2 = Disposable.Create(() => { disp2 = true; });
            m.Disposable = d2;
            Assert.AreSame(d2, m.Disposable);
            Assert.IsTrue(disp1);
            Assert.IsFalse(disp2);
        }

        [TestMethod]
        public void MutableDisposable_ReplaceAfterDispose()
        {
            var disp1 = false;
            var disp2 = false;

            var m = new MutableDisposable();
            m.Dispose();

            var d1 = Disposable.Create(() => { disp1 = true; });
            m.Disposable = d1;
            Assert.IsNull(m.Disposable); // CHECK
            Assert.IsTrue(disp1);

            var d2 = Disposable.Create(() => { disp2 = true; });
            m.Disposable = d2;
            Assert.IsNull(m.Disposable); // CHECK
            Assert.IsTrue(disp2);
        }

        [TestMethod]
        public void MutableDisposable_Dispose()
        {
            var disp = false;

            var m = new MutableDisposable();
            var d = Disposable.Create(() => { disp = true; });
            m.Disposable = d;
            Assert.AreSame(d, m.Disposable);
            Assert.IsFalse(disp);

            m.Dispose();
            Assert.IsTrue(disp);
            Assert.IsNull(m.Disposable);
        }

        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void RefCountDisposable_Ctor_Null()
        {
            new RefCountDisposable(null);
        }

        [TestMethod]
        public void RefCountDisposable_SingleReference()
        {
            var d = new BooleanDisposable();
            var r = new RefCountDisposable(d);
            Assert.IsFalse(d.IsDisposed);
            r.Dispose();
            Assert.IsTrue(d.IsDisposed);
            r.Dispose();
            Assert.IsTrue(d.IsDisposed);
        }

        [TestMethod]
        public void RefCountDisposable_RefCounting()
        {
            var d = new BooleanDisposable();
            var r = new RefCountDisposable(d);
            Assert.IsFalse(d.IsDisposed);

            var d1 = r.GetDisposable();
            var d2 = r.GetDisposable();
            Assert.IsFalse(d.IsDisposed);

            d1.Dispose();
            Assert.IsFalse(d.IsDisposed);

            d2.Dispose();
            Assert.IsFalse(d.IsDisposed); // CHECK

            r.Dispose();
            Assert.IsTrue(d.IsDisposed);

            var d3 = r.GetDisposable(); // CHECK
            d3.Dispose();
        }

        [TestMethod]
        public void RefCountDisposable_PrimaryDisposesFirst()
        {
            var d = new BooleanDisposable();
            var r = new RefCountDisposable(d);
            Assert.IsFalse(d.IsDisposed);

            var d1 = r.GetDisposable();
            var d2 = r.GetDisposable();
            Assert.IsFalse(d.IsDisposed);

            d1.Dispose();
            Assert.IsFalse(d.IsDisposed);

            r.Dispose();
            Assert.IsFalse(d.IsDisposed);

            d2.Dispose();
            Assert.IsTrue(d.IsDisposed);
        }

        [TestMethod]
        public void ScheduledDisposable()
        {
            var d = new BooleanDisposable();
            var s = new ScheduledDisposable(Scheduler.Immediate, d);

            Assert.IsFalse(d.IsDisposed);

            Assert.AreSame(Scheduler.Immediate, s.Scheduler);
            Assert.AreSame(d, s.Disposable);

            s.Dispose();

            Assert.IsTrue(d.IsDisposed);

            Assert.AreSame(Scheduler.Immediate, s.Scheduler);
            Assert.AreSame(d, s.Disposable);
        }
    }
}
