#if DESKTOPCLR20 || DESKTOPCLR40
using System;
using System.Windows.Forms;
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
using System.Threading;
using

#if WM7

Microsoft.Windows.Phone.

#endif
Reactive.Threading;

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Windows.Forms
{
    /// <summary>
    /// Provides a set of static methods for subscribing to IObservables using Windows Forms controls.
    /// </summary>
    public static class ControlObservableExtensions
    {

        /// <summary>
        /// Asynchronously subscribes and unsubscribes observers using the Windows Forms control.
        /// </summary>
        public static IObservable<TSource> SubscribeOn<TSource>(this IObservable<TSource> source, Control control)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (control == null)
                throw new ArgumentNullException("control");

            return source.SubscribeOn(new ControlSynchronizationContext(control));
        }

        /// <summary>
        /// Asynchronously notify observers using the Windows Forms control.
        /// </summary>
        public static IObservable<TSource> ObserveOn<TSource>(this IObservable<TSource> source, Control control)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (control == null)
                throw new ArgumentNullException("control");

            return source.ObserveOn(new ControlSynchronizationContext(control));
        }

    }
}
#endif