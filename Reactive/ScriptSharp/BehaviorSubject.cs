using System;

namespace Rx
{
    /// <summary>
    /// Represents an object that is both an observable sequence as well as an observer.
    /// </summary>
    [Imported]
    public class BehaviorSubject : ReplaySubject
    {
        /// <summary>
        /// Creates a behavior subject.
        /// </summary>
        [AlternateSignature]
        public BehaviorSubject(object value, Scheduler scheduler) : base(1, -1, scheduler)
        {
        }

        /// <summary>
        /// Creates a behavior subject.
        /// </summary>
        [AlternateSignature]
        public BehaviorSubject(object value) : base(1, -1)
        {
        }      
    }      
}


