// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// Platform.cs
//
// <OWNER>igoro</OWNER>
//
// Some common helpers used across the code-base.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Security.Permissions;
using System.Security;
using System.Runtime.InteropServices;
using System.Diagnostics.Contracts;

namespace System.Threading
{

    /// <summary>
    /// A convenience class for common platform-related logic.
    /// </summary>
    internal static class Platform
    {
        [DllImport("kernel32.dll")]
        private static extern int SwitchToThread();
        /// <summary>
        /// Gets the number of available processors available to this process on the current machine.
        /// </summary>
        internal static int ProcessorCount
        {
            get { return Environment.ProcessorCount; }
        }

        internal static bool IsSingleProcessor
        {
            get { return ProcessorCount == 1; }
        }

        internal static void Yield()
        {
            SwitchToThread();
        }
    }

}
