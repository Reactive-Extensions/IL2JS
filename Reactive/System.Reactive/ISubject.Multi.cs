using System;

namespace
#if WM7
Microsoft.Windows.Phone
#endif
Reactive.Collections.Generic
{
    /// <summary>
    /// Represents an object that is both an observable sequence as well as an observer.
    /// </summary>
    public interface ISubject<T1, T2> : IObserver<T1>, IObservable<T2>
    {
    }
}
