using System;

namespace Rx
{
    /// <summary>
    /// Represents a push-style collection.
    /// </summary>
    [Imported]
    public interface IObservable
    {
        /// <summary>
        /// Subscribes an observer to the observable sequence.
        /// </summary>
        [PreserveCase]
        IDisposable Subscribe(IObserver observer);
    }
}


