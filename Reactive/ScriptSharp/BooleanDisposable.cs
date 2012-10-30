using System;

namespace Rx
{
    /// <summary>
    /// Represents an IDisposable that can be checked for status.
    /// </summary>
    [Imported]
    public class BooleanDisposable : IDisposable
    {
        /// <summary>
        /// Constructs a new undisposed BooleanDisposable. 
        /// </summary>
        public BooleanDisposable()
        {
        }

        /// <summary>
        /// Sets the status to Disposed.
        /// </summary>
        [PreserveCase]
        public void Dispose()
        {            
        }

        /// <summary>
        /// Gets a value indicating whether the object is disposed.
        /// </summary>
        [PreserveCase]
        public bool GetIsDisposed()
        {
            return false;
        }
    }
}


