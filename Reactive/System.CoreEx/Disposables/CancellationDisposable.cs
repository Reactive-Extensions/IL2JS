#if !SILVERLIGHT && !NETCF37
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Disposables
{
    /// <summary>
    /// Represents an IDisposable that can be checked for cancellation status.
    /// </summary>
    public sealed class CancellationDisposable : IDisposable
    {
        CancellationTokenSource cts;

        /// <summary>
        /// Constructs a new CancellationDisposable that uses an existing CancellationTokenSource.
        /// </summary>
        public CancellationDisposable(CancellationTokenSource cts)
        {
            if (cts == null)
                throw new ArgumentNullException("cts");

            this.cts = cts;
        }

        /// <summary>
        /// Constructs a new CancellationDisposable that uses a new CancellationTokenSource.
        /// </summary>
        public CancellationDisposable()
            : this(new CancellationTokenSource())
        {
        }

        /// <summary>
        /// Gets the CancellationToken used by this CancellationDisposable.
        /// </summary>
        public CancellationToken Token { get { return cts.Token; } }

        /// <summary>
        /// Cancels the CancellationTokenSource.
        /// </summary>
        public void Dispose()
        {
            cts.Cancel();
        }
    }
}
#endif