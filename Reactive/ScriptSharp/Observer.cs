using System;

namespace Rx
{
    /// <summary>
    /// Supports push-style iteration over an observable sequence.
    /// </summary>
    [Imported]
    public class Observer : IObserver
    {
        /// <summary>
        /// Creates an observer from the specified OnNext action.
        /// </summary>
        [AlternateSignature]
        public Observer(ActionObject onNext)
        {
        }

        /// <summary>
        /// Creates an observer from the specified OnNext and OnError actions.
        /// </summary>
        [AlternateSignature]
        public Observer(ActionObject onNext, ActionObject onError)
        {
        }

        /// <summary>
        /// Creates an observer from the specified OnNext, OnError, and OnCompleted actions.
        /// </summary>
        [AlternateSignature]
        public Observer(ActionObject onNext, ActionObject onError, Action onCompleted)
        {
        }

        /// <summary>
        /// Notifies the observer of a new value in the sequence.
        /// </summary>
        [PreserveCase]
        public void OnNext(object value)
        {
        }

        /// <summary>
        /// Notifies the observer that an exception has occurred.
        /// </summary>
        [PreserveCase]
        public void OnError(object value)
        {
        }

        /// <summary>
        /// Notifies the observer of the end of the sequence.
        /// </summary>
        [PreserveCase]
        public void OnCompleted()
        {
        }

        /// <summary>
        /// Hides the identity of an observer.
        /// </summary>
        [PreserveCase]
        public Observer AsObserver()
        {
            return null;
        }

        /// <summary>
        /// Creates an observer from the specified OnNext action.
        /// </summary>
        [AlternateSignature]
        public static Observer Create(ActionObject onNext)
        {
            return null;
        }

        /// <summary>
        /// Creates an observer from the specified OnNext and OnError actions.
        /// </summary>
        [AlternateSignature]
        public static Observer Create(ActionObject onNext, ActionObject onError)
        {
            return null;
        }

        /// <summary>
        /// Creates an observer from the specified OnNext, OnError, and OnCompleted actions.
        /// </summary>
        [AlternateSignature]
        public static Observer Create(ActionObject onNext, ActionObject onError, Action onCompleted)
        {
            return null;
        }
    }
}