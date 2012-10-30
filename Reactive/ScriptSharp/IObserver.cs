using System;

namespace Rx
{
    /// <summary>
    /// Supports push-style iteration over an observable sequence.
    /// </summary>
    [Imported]
    public interface IObserver
    {
        /// <summary>
        /// Notifies the observer of a new value in the sequence.
        /// </summary>
        [PreserveCase]
        void OnNext(object value);

        /// <summary>
        /// Notifies the observer that an exception has occurred.
        /// </summary>
        [PreserveCase]
        void OnError(object exception);

        /// <summary>
        /// Notifies the observer of the end of the sequence.
        /// </summary>
        [PreserveCase]
        void OnCompleted();
    }
}


