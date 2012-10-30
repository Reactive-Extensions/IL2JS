using System;
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

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Collections.Generic
{
    /// <summary>
    /// Represents an observable that can be connected and disconnected.
    /// </summary>
    public interface IConnectableObservable<T> : IObservable<T>
    {
        /// <summary>
        /// Connects the observable.
        /// </summary>
        IDisposable Connect();
    }
}
