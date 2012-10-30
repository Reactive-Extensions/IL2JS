using System;

namespace Rx
{
    /// <summary>
    /// Represents a disposable whose underlying disposable can be swapped for another disposable.
    /// </summary>
    [Imported]
    public class MutableDisposable : IDisposable
    {
        /// <summary>
        /// Constructs a new MutableDisposable with no current underlying disposable.
        /// </summary>
        public MutableDisposable()
        {
        }

        /// <summary>
        /// Disposes the underlying disposable as well as all future replacements.
        /// </summary>
        [PreserveCase]
        public void Dispose()
        {            
        }

        /// <summary>
        /// Gets a value indicating whether the MutableDisposable has an underlying disposable.
        /// </summary>
        [PreserveCase]
        public IDisposable Get()
        {
            return null;
        }

        /// <summary>
        /// Swaps and disposes the current disposable with the new disposable. 
        /// </summary>
        [PreserveCase]
        public void Replace(IDisposable disposable)
        {
        }
    }
}


