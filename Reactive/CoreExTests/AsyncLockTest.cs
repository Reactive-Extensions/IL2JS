using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using
#if WM7
Microsoft.Windows.Phone.
#endif
 Reactive.Concurrency;


namespace Microsoft.LiveLabs.CoreExTests
{
    [TestClass]
    public class AsyncLockTest
    {
        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void Wait_ArgumentChecking()
        {
            new AsyncLock().Wait(null);
        }

        [TestMethod]
        public void Wait_Graceful()
        {
            var ok = false;
            new AsyncLock().Wait(() => { ok = true; });
            Assert.IsTrue(ok);
        }

        [TestMethod]
        public void Wait_Fail()
        {
            var l = new AsyncLock();

            var ex = new Exception();
            try
            {
                l.Wait(() => { throw ex; });
                Assert.Fail();
            }
            catch (Exception e)
            {
                Assert.AreSame(ex, e);
            }

            // has faulted; should not run
            l.Wait(() => { Assert.Fail(); });
        }

        [TestMethod]
        public void Wait_QueuesWork()
        {
            var l = new AsyncLock();

            var l1 = false;
            var l2 = false;
            l.Wait(() => { l.Wait(() => { Assert.IsTrue(l1); l2 = true; }); l1 = true; });
            Assert.IsTrue(l2);
        }
    }
}
