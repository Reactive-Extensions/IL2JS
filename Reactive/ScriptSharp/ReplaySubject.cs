using System;

namespace Rx
{
    /// <summary>
    /// Represents an object that is both an observable sequence as well as an observer.
    /// </summary>
    [Imported]
    public class ReplaySubject : Observable, ISubject
    {
        /// <summary>
        /// Creates a replayable subject.
        /// </summary>
        [AlternateSignature]
        public ReplaySubject(int bufferSize, int window, Scheduler scheduler)
        {
        }

        /// <summary>
        /// Creates a replayable subject.
        /// </summary>
        [AlternateSignature]
        public ReplaySubject(int bufferSize, int window)
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
        public void OnError(object exception)
        {
        }

        /// <summary>
        /// Notifies the observer of the end of the sequence.
        /// </summary>
        [PreserveCase]
        public void OnCompleted()
        {
        }        
    }      
}


