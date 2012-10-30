using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Pex.Framework;

namespace Microsoft.LiveLabs.ReactiveTests
{
    [TestClass]
    public partial class DummyTest
    {
        [PexMethod]
        public void Dummy(int x)
        {         
            Observable.Return(x);
        }
    }
}

