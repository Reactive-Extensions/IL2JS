//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.ServiceBus.Common.Interop
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Security;
    using Microsoft.Win32.SafeHandles;

    [Fx.Tag.SecurityNote(Critical = "Usage of SafeHandleZeroOrMinusOneIsInvalid, which is protected by a LinkDemand and InheritanceDemand")]
    [SecurityCritical]
    sealed class SafeEventLogWriteHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Note: RegisterEventSource returns 0 on failure
        [Fx.Tag.SecurityNote(Critical = "Usage of SafeHandleZeroOrMinusOneIsInvalid, which is protected by a LinkDemand and InheritanceDemand")]
        [SecurityCritical]
        SafeEventLogWriteHandle() : base(true) { }

        [ResourceConsumption(ResourceScope.Machine)]
        [Fx.Tag.SecurityNote(Critical = "Usage of SafeHandleZeroOrMinusOneIsInvalid, which is protected by a LinkDemand and InheritanceDemand")]
        [SecurityCritical]
        public static SafeEventLogWriteHandle RegisterEventSource(string uncServerName, string sourceName)
        {
            SafeEventLogWriteHandle retval = UnsafeNativeMethods.RegisterEventSource(uncServerName, sourceName);
            int error = Marshal.GetLastWin32Error();
            if (retval.IsInvalid)
            {
                Debug.Print("SafeEventLogWriteHandle::RegisterEventSource[" + uncServerName + ", " + sourceName + "] Failed. Last Error: " +
                    error.ToString(CultureInfo.InvariantCulture));
            }

            return retval;
        }

        [SuppressMessage(FxCop.Category.Interoperability, FxCop.Rule.MarkBooleanPInvokeArgumentsWithMarshalAs, Justification = "Opened as CSDMain #183080.", Scope = "Member", Target = "Microsoft.ServiceBus.Common.Interop.SafeEventLogWriteHandle.#DeregisterEventSource(System.IntPtr)")]
        [SuppressMessage(FxCop.Category.Design, FxCop.Rule.MovePInvokesToNativeMethodsClass, Justification = "Opened as CSDMain #183080.", Scope = "Member", Target = "Microsoft.ServiceBus.Common.Interop.SafeEventLogWriteHandle.#DeregisterEventSource(System.IntPtr)")]
        [SuppressMessage(FxCop.Category.Usage, FxCop.Rule.UseManagedEquivalentsOfWin32Api, Justification = "Opened as CSDMain #183080.", Scope = "Member", Target = "Microsoft.ServiceBus.Common.Interop.SafeEventLogWriteHandle.#DeregisterEventSource(System.IntPtr)")]
        [DllImport("advapi32", SetLastError = true)]
        [ResourceExposure(ResourceScope.None)]
        static extern bool DeregisterEventSource(IntPtr hEventLog);

        [Fx.Tag.SecurityNote(Critical = "Usage of SafeHandleZeroOrMinusOneIsInvalid, which is protected by a LinkDemand and InheritanceDemand")]
        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            return DeregisterEventSource(this.handle);
        }
    }
}