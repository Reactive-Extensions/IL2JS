using System;

namespace
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Disposables
{
    /// <summary>
    /// Represents a disposable that does nothing on disposal.
    /// </summary>
    internal sealed class DefaultDisposable : IDisposable
    {
        /// <summary>
        /// Singleton default disposable.
        /// </summary>
        public static readonly DefaultDisposable Instance = new DefaultDisposable();

        private DefaultDisposable()
        {
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        public void Dispose()
        {
            // no op
        }
    }
}
