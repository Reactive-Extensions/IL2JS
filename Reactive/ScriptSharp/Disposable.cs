using System;

namespace Rx
{
    /// <summary>
    /// Provides a set of static methods for creating Disposables.
    /// </summary>
    [Imported]    
    public static class Disposable
    {
        /// <summary>
        /// Represents the disposable that does nothing when disposed.
        /// </summary>
        [PreserveCase]
        [IntrinsicProperty]
        public static IDisposable Empty { get { return null; } }

        /// <summary>
        /// Creates the disposable that invokes dispose when disposed.
        /// </summary>
        [PreserveCase]
        public static IDisposable Create(Action action)
        {
            return null;
        }
    }
}


