using System;

namespace Rx
{
    /// <summary>
    /// Represents an observable that can be connected and disconnected from its source.
    /// </summary>
    [Imported]
    public class ConnectableObservable : Observable
    {
        /// <summary>
        /// Creates an observable that can be connected and disconnected from its source.
        /// </summary>
        [AlternateSignature]
        public ConnectableObservable(Observable source, ISubject subject)
        {
        }

        /// <summary>
        /// Creates an observable that can be connected and disconnected from its source.
        /// </summary>
        [AlternateSignature]
        public ConnectableObservable(Observable source)
        {
        }

        /// <summary>
        /// Connects the observable to its source.
        /// </summary>
        [PreserveCase]
        public IDisposable Connect()
        {
            return null;
        }

        /// <summary>
        /// Returns an observable sequence that stays connected to the source as long as there is at least one subscription to the observable sequence.
        /// </summary>
        [PreserveCase]
        public Observable RefCount()
        {
            return null;
        }
    }      
}


