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
#if !IL2JS
    /// <summary>
    /// Represents a thread-affine IDisposable.
    /// </summary>
    public sealed class ContextDisposable : IDisposable
    {
        /// <summary>
        /// Gets the provided SynchronizationContext.
        /// </summary>
        public SynchronizationContext Context
        {
            get;
            private set;
        }

        IDisposable disposable;
        int disposed = 0;

        /// <summary>
        /// Constructs a ContextDisposable that uses a SynchronziationContext on which to dipose the disposable.
        /// </summary>
        public ContextDisposable(SynchronizationContext context, IDisposable disposable)
        {
            if (context == null)
                throw new ArgumentNullException("context");
            if (disposable == null)
                throw new ArgumentNullException("disposable");

            this.Context = context;
            this.disposable = disposable;
        }

        /// <summary>
        /// Disposes the wrapped disposable on the provided SynchronizationContext.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
                Context.Post(_ => disposable.Dispose(), null);
        }
    }
#endif
}
