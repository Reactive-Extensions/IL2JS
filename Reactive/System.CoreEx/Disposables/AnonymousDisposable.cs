using System;

namespace
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Disposables
{
    /// <summary>
    /// Represents an Action-based disposable.
    /// </summary>
    internal sealed class AnonymousDisposable : IDisposable
    {
        readonly Action dispose;
        bool isDisposed;

        /// <summary>
        /// Constructs a new disposable with the given action used for disposal.
        /// </summary>
        /// <param name="dispose">Disposal action.</param>
        public AnonymousDisposable(Action dispose)
        {
            this.dispose = dispose;
        }

        /// <summary>
        /// Calls the disposal action.
        /// </summary>
        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                dispose();
            }
        }
    }
}
