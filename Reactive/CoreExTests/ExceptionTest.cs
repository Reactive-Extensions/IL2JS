using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Diagnostics;

namespace Microsoft.LiveLabs.CoreExTests
{
    [TestClass]
    public class ExceptionTest
    {
#if !SILVERLIGHT
        [TestMethod, ExpectedException(typeof(ArgumentNullException))]
        public void PrepareForRethrow_Null()
        {
            ((Exception)null).PrepareForRethrow();
        }
#endif
    }
}
