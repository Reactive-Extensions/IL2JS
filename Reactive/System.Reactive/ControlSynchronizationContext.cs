#if !NETCF37 && !SILVERLIGHT
using System;
using System.Threading;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Collections.Generic;
using
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Linq;
using System.Text;
using System.Windows.Forms;

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Threading
{
    class ControlSynchronizationContext : SynchronizationContext
    {
        Control control;

        public ControlSynchronizationContext(Control control)
        {
            this.control = control;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            control.BeginInvoke(d, state);
        }
    }
}
#endif
