using System;
using System.Runtime.InteropServices;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.WSH.Host
{
    [Guid("BB1A2AE2-A4F9-11cf-8F20-00805F2CD064")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IActiveScriptParse
    {
        [PreserveSig]
        int InitNew();

        [PreserveSig]
        int AddScriptlet(string defaultName, string code, string itemName, string subItemName, string eventName, string delimiter, uint sourceContextCookie, uint startingLineNumber, uint flags, out string name, out System.Runtime.InteropServices.ComTypes.EXCEPINFO exceptionInfo);

        [PreserveSig]
        int ParseScriptText(string code, string itemName, IntPtr context, string delimiter, uint sourceContextCookie, uint startingLineNumber, uint flags,IntPtr result, out System.Runtime.InteropServices.ComTypes.EXCEPINFO exceptionInfo);

    }
}
