using System;
using System.Runtime.InteropServices;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.WSH.Host
{

    [Guid("BB1A2AE1-A4F9-11cf-8F20-00805F2CD064")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IActiveScript
    {
        [PreserveSig]
        int SetScriptSite(IActiveScriptSite site);

        [PreserveSig]
        int GetScriptSite(ref Guid riid, out object obj);

        [PreserveSig]
        int SetScriptState(ScriptState state);

        [PreserveSig]
        int GetScriptState(out ScriptState state);

        [PreserveSig]
        int Close();

        [PreserveSig]
        int AddNamedItem(string name, ScriptItem flags);

        [PreserveSig]
        int AddTypeLib(ref Guid typeLib, int major, int minor, int flags);

        [PreserveSig]
        int GetScriptDispatch(string itemName, out object dispatch);

        [PreserveSig]
        int GetCurrentScriptThreadID(out int thread);

        [PreserveSig]
        int GetScriptThreadID(int win32ThreadId,out int thread);

        [PreserveSig]
        int GetScriptThreadState(int thread, out ScriptThreadState state);

        [PreserveSig]
        int InterruptScriptThread(int thread, ref System.Runtime.InteropServices.ComTypes.EXCEPINFO exceptionInfo,int flags);

        [PreserveSig]
        int Clone(out IActiveScript script);
    }
}
