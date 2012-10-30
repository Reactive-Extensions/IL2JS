using System.Runtime.InteropServices;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.WSH.Host
{
    [Guid("EAE1BA61-A4ED-11cf-8F20-00805F2CD064")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IActiveScriptError
    {
        [PreserveSig]
        int GetExceptionInfo(out System.Runtime.InteropServices.ComTypes.EXCEPINFO info);

        [PreserveSig]
        int RemoteGetExceptionInfo(out System.Runtime.InteropServices.ComTypes.EXCEPINFO info);

        [PreserveSig]
        int GetSourcePosition(out int sourceContext, out uint lineNumber, out int characterPosition);

        [PreserveSig]
        int GetSourceLineText(out string sourceLine);
    }
}
