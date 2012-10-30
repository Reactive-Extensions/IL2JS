using System;

namespace Rx
{
    /// <summary>
    /// Represents an object that is both an observable sequence as well as an observer.
    /// </summary>
    [Imported]
    public class Subject : Observable, ISubject
    {
        /// <summary>
        /// Creates a subject.
        /// </summary>
        [AlternateSignature]
        public Subject(Scheduler scheduler)
        {
        }

        /// <summary>
        /// Creates a subject.
        /// </summary>
        [AlternateSignature]
        public Subject()
        {
        }

        /// <summary>
        /// Notifies all subscribed observers with the value.
        /// </summary>
        [PreserveCase]
        public void OnNext(object value)
        {            
        }

        /// <summary>
        /// Notifies all subscribed observers with the exception.
        /// </summary>
        [PreserveCase]
        public void OnError(object exception)
        {
        }

        /// <summary>
        /// Notifies all subscribed observers of the end of the sequence.
        /// </summary>
        [PreserveCase]
        public void OnCompleted()
        {
        }        
    }      
}


