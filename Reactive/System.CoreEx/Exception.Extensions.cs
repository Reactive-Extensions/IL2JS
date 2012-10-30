using System;
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;

namespace
#if WM7
Microsoft.Windows.Phone.
#endif
Reactive.Diagnostics
{
    /// <summary>
    /// Provides a set of static methods for exceptions.
    /// </summary>
    public static class ExceptionExtensions
    {
#if !SILVERLIGHT && !NETCF37
        private const string ExceptionPrepForRemotingMethodName = "PrepForRemoting";

        private static readonly MethodInfo prepForRemoting = typeof(Exception).GetMethod(
                ExceptionPrepForRemotingMethodName,
                BindingFlags.Instance | BindingFlags.NonPublic);

#endif
        /// <summary>
        /// Preserve callstack when rethrowing.
        /// </summary>
        public static Exception PrepareForRethrow(this Exception exception)
        {
#if !SILVERLIGHT && !NETCF37
            if (exception == null)
                throw new ArgumentNullException("exception");

            prepForRemoting.Invoke(exception, new object[] { });
            
#endif
            return exception;
        }
    }
}
