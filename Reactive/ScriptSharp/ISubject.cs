using System;

namespace Rx
{
    /// <summary>
    /// Represents an object that is both an observable sequence as well as an observer.
    /// </summary>
    [Imported]
    public interface ISubject : IObservable, IObserver
    {
    }
}


