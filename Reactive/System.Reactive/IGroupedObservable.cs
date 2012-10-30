using System;
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
Reactive.Linq
{
    /// <summary>
    /// Represents an observable sequence of values that have a common key.
    /// </summary>
    public interface IGroupedObservable<TKey, TElement> : IObservable<TElement>
    {
        /// <summary>
        /// Gets the common key.
        /// </summary>
        TKey Key { get; }
    }
}
