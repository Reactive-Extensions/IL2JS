using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive
{
    /// <summary>
    /// Supports push-style iteration over an observable sequence.
    /// </summary>
    public interface IObserver<TValue, TResult>
    {
        /// <summary>
        /// Notifies the observer of a new value in the sequence.
        /// </summary>
        TResult OnNext(TValue value);

        /// <summary>
        /// Notifies the observer that an exception has occurred.
        /// </summary>
        TResult OnError(Exception exception);

        /// <summary>
        /// Notifies the observer of the end of the sequence.
        /// </summary>
        TResult OnCompleted();
    }
}
