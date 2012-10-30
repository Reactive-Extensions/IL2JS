using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Collections.Generic;

namespace Microsoft.LiveLabs.CoreExTests
{
    [TestClass]
    public class EventTest
    {
        [TestMethod]
        public void Event_Create()
        {
            var s = new object();
            var e = new EventArgs();
            var evt = Event.Create<EventArgs>(s, e);
            Assert.AreSame(s, evt.Sender);
            Assert.AreSame(e, evt.EventArgs);
        }
    }
}
