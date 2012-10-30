using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Disposables
{
    /// <summary>
    /// Represents an IDisposable that can be checked for status.
    /// </summary>
    public sealed class BooleanDisposable : IDisposable
    {
        /// <summary>
        /// Constructs a new undisposed BooleanDisposable. 
        /// </summary>
        public BooleanDisposable()
        {
        }

        /// <summary>
        /// Gets a value indicating whether the object is disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Sets the status to Disposed.
        /// </summary>
        public void Dispose()
        {
            IsDisposed = true;
        }
    }
}
