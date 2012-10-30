using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Threading;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Linq;
using Reactive;

namespace ReactiveTests.Tests
{
#if DESKTOPCLR20 || DESKTOPCLR40
    [TestClass]
    public partial class ObservableRemotingTest : Test
    {
        [TestMethod]
        public void Remotable_ArgumentChecking()
        {
            Throws<ArgumentNullException>(() => Observable.Remotable<int>(null));
        }

        [TestMethod]
        public void Remotable_Empty()
        {
            var evt = new ManualResetEvent(false);

            var e = GetRemoteObservable(t => t.Empty());
            using (e.Subscribe(_ => { Assert.Fail(); }, _ => { Assert.Fail(); }, () => { evt.Set(); }))
            {
                evt.WaitOne();
            }
        }

        [TestMethod]
        public void Remotable_Return()
        {
            var evt = new ManualResetEvent(false);

            bool next = false;
            var e = GetRemoteObservable(t => t.Return(42));
            using (e.Subscribe(value => { next = true; Assert.AreEqual(42, value); }, _ => { Assert.Fail(); }, () => { evt.Set(); }))
            {
                evt.WaitOne();
                Assert.IsTrue(next);
            }
        }

        [TestMethod]
        public void Remotable_Throw()
        {
            var ex = new InvalidOperationException("Oops!");

            var evt = new ManualResetEvent(false);

            bool error = false;
            var e = GetRemoteObservable(t => t.Throw(ex));
            using (e.Subscribe(value => { Assert.Fail(); }, err => { error = true; Assert.IsTrue(err is InvalidOperationException && err.Message == ex.Message); evt.Set(); }, () => { Assert.Fail(); }))
            {
                evt.WaitOne();
                Assert.IsTrue(error);
            }
        }

        [TestMethod]
        public void Remotable_Disposal()
        {
            var test = GetRemoteTestObject();
            test.Disposal().Subscribe().Dispose();
            Assert.IsTrue(test.Disposed);
        }

        private IObservable<int> GetRemoteObservable(Func<RemotingTest, IObservable<int>> f)
        {
            var test = GetRemoteTestObject();
            return f(test);
        }

        private RemotingTest GetRemoteTestObject()
        {
            var ads = new AppDomainSetup { ApplicationBase = AppDomain.CurrentDomain.BaseDirectory };
            var ad = AppDomain.CreateDomain("test", null, ads);
            var test = (RemotingTest)ad.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, "ReactiveTests.Tests.RemotingTest");
            return test;
        }
    }

    public class RemotingTest : MarshalByRefObject
    {
        public override object InitializeLifetimeService()
        {
            return null;
        }

        public IObservable<int> Empty()
        {
            return Observable.Empty<int>().Remotable();
        }

        public IObservable<int> Return(int value)
        {
            return Observable.Return<int>(value).Remotable();
        }

        public IObservable<int> Throw(Exception ex)
        {
            return Observable.Throw<int>(ex).Remotable();
        }

        public IObservable<int> Disposal()
        {
            return Observable.Create<int>(obs =>
            {
                return () => { Disposed = true; };
            }).Remotable();
        }

        public bool Disposed { get; set; }
    }
#endif
}
