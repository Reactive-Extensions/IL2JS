using System;

namespace
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Disposables
{
    /// <summary>
    /// Provides a set of static methods for creating Disposables.
    /// </summary>
    public static class Disposable
    {
        /// <summary>
        /// Represents the disposable that does nothing when disposed.
        /// </summary>
        public static IDisposable Empty
        {
            get { return DefaultDisposable.Instance; }
        }

        /// <summary>
        /// Creates the disposable that invokes dispose when disposed.
        /// </summary>
        public static IDisposable Create(Action dispose)
        {
            if (dispose == null)
                throw new ArgumentNullException("dispose");

            return new AnonymousDisposable(dispose);
        }
    }
}
