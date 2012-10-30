using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive;

namespace Microsoft.LiveLabs.CoreExTests
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void Unit()
        {
            var u1 = new Unit();
            var u2 = new Unit();
            Assert.IsTrue(u1.Equals(u2));
            Assert.IsFalse(u1.Equals(""));
            Assert.IsFalse(u1.Equals(null));
            Assert.IsTrue(u1 == u2);
            Assert.IsFalse(u1 != u2);
            u1.GetHashCode(); // CHECK
        }
    }
}
