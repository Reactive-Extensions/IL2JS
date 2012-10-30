using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Diagnostics.Contracts
{
    /// <summary>
    /// A stub version of .NET 4.0 contracts.
    /// </summary>
    internal static class Contract
    {
        [Conditional("DEBUG")]
        internal static void Assert(bool condition)
        {
            Debug.Assert(condition);
        }

        [Conditional("DEBUG")]
        internal static void Assert(bool condition, string message)
        {
            Debug.Assert(condition, message);
        }

        [Conditional("DEBUG")]
        internal static void Ensures(bool condition)
        {
            // The post-condition checker cannot be easily implemented, so we just make Ensures a no-op.
        }

        [Conditional("DEBUG")]
        internal static void EndContractBlock()
        {
        }
    }
}
