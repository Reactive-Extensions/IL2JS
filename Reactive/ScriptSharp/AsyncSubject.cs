using System;

namespace Rx
{
    /// <summary>
    /// Represents the result of an asynchronous operation.
    /// </summary>
    [Imported]
    public class AsyncSubject : Observable, ISubject
    {
        /// <summary>
        /// Creates a subject that can only receive one value and that value is cached for all future observations.
        /// </summary>
        [AlternateSignature]
        public AsyncSubject(Scheduler scheduler)
        {
        }

        /// <summary>
        /// Creates a subject that can only receive one value and that value is cached for all future observations.
        /// </summary>
        [AlternateSignature]
        public AsyncSubject()
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


