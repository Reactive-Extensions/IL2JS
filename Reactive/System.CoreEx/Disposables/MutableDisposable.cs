using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Disposables
{
    /// <summary>
    /// Represents a disposable whose underlying disposable can be swapped for another disposable.
    /// </summary>
    public sealed class MutableDisposable : IDisposable
    {
        object gate = new object();
        IDisposable current;
        bool disposed;

        /// <summary>
        /// Constructs a new MutableDisposable with no current underlying disposable.
        /// </summary>
        public MutableDisposable()
        {
        }

        /// <summary>
        /// Gets a value indicating whether the MutableDisposable has an underlying disposable.
        /// </summary>
        public IDisposable Disposable
        {
            get
            {
                return current;
            }
            set
            {
                var shouldDispose = false;
                lock (gate)
                {
                    shouldDispose = disposed;
                    if (!shouldDispose)
                    {
                        if (current != null)
                            current.Dispose();
                        current = value;
                    }
                }
                if (shouldDispose && value != null)
                    value.Dispose();
            }
        }

        /// <summary>
        /// Disposes the underlying disposable as well as all future replacements.
        /// </summary>
        public void Dispose()
        {
            lock (gate)
            {
                if (!disposed)
                {
                    disposed = true;
                    if (current != null)
                    {
                        current.Dispose();
                        current = null;
                    }
                }
            }
        }
    }
}
