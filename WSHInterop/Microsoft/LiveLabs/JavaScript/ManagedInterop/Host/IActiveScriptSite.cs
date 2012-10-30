using System;
using System.Runtime.InteropServices;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.WSH.Host
{
    [Guid("DB01A1E3-A42B-11cf-8F20-00805F2CD064")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IActiveScriptSite
    {
        [PreserveSig]
        int GetLCID(out int lcid);

        [PreserveSig]
        int GetItemInfo(string name, ScriptInfo returnMask, [Out, MarshalAs(UnmanagedType.IUnknown)] out object item, IntPtr typeInfo);

        [PreserveSig]
        int GetDocVersionString(out string version);

        [PreserveSig]
        int OnScriptTerminate(ref object result, ref System.Runtime.InteropServices.ComTypes.EXCEPINFO exceptionInfo);

        [PreserveSig]
        int OnStateChange(ScriptState state);

        [PreserveSig]
        int OnScriptError(IActiveScriptError error);

        [PreserveSig]
        int OnEnterScript();

        [PreserveSig]
        int OnLeaveScript();
    }
}
