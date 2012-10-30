using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace System.Threading
{
    /// <summary>
    /// .NET 4.0 Monitor class supports new overloads that return a boolean value
    /// representing whether the lock was successfully taken or not. The return value
    /// is meant to be accurate even in the presence of thread aborts.
    /// 
    /// Monitor2 implements these methods as simple wrappers over the .NET 3.5 methods,
    /// but without making the guarantees related to thread aborts.
    /// </summary>
    internal class Monitor2
    {
        internal static void Enter(object obj, ref bool taken)
        {
            Monitor.Enter(obj);
            taken = true;
        }

        internal static bool TryEnter(object obj)
        {
            return Monitor.TryEnter(obj);
        }

        internal static void TryEnter(object obj, ref bool taken)
        {
            taken = Monitor.TryEnter(obj);
        }

        internal static bool TryEnter(object obj, int millisecondsTimeout)
        {
            return Monitor.TryEnter(obj, millisecondsTimeout);
        }

        internal static bool TryEnter(object obj, TimeSpan timeout)
        {
            return Monitor.TryEnter(obj, timeout);
        }

        internal static void TryEnter(object obj, int millisecondsTimeout, ref bool taken)
        {
            taken = Monitor.TryEnter(obj, millisecondsTimeout);
        }
    }
}
