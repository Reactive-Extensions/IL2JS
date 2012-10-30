using System;

namespace Rx
{
    /// <summary>
    /// Represents an observable sequence of values that have a common key.
    /// </summary>
    [Imported]
    public class GroupedObservable : Observable
    {
        private GroupedObservable()
        {
        }

        /// <summary>
        /// Gets the common key.
        /// </summary>
        [IntrinsicProperty]
        [PreserveCase]
        public object Key { get { return null; } }
    }
}


