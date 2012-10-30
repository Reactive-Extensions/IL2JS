using System;

namespace Rx
{
    /// <summary>
    /// Represents a disposable that can be disposed before the underlying resource has been created.
    /// </summary>
    [Imported]
    public class FutureDisposable : IDisposable
    {
        /// <summary>
        /// Constructs a FutureDisposable.
        /// </summary>
        public FutureDisposable()
        {
        }

        /// <summary>
        /// Disposes the underlying disposable as soon as it is available.
        /// </summary>
        [PreserveCase]
        public void Dispose()
        {            
        }

        /// <summary>
        /// Gets the underlying Disposable
        /// </summary>
        [PreserveCase]
        public IDisposable Get()
        {
            return null;
        }

        /// <summary>
        /// Sets the underlying disposable. If the FutureDisposable is already disposed, the underlying disposable is immediately disposed.
        /// </summary>
        [PreserveCase]
        public void Set(IDisposable disposable)
        {
        }
    }
}


