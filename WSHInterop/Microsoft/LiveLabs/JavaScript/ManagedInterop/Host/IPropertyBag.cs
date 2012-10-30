using System.Runtime.InteropServices;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.WSH.Host
{
    [
    ComImport(),
    Guid("55272A00-42CB-11CE-8135-00AA004BB851"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)
    ]
    [ComVisible(true)]
    public interface IPropertyBag
    {
        [PreserveSig]
        int Read(
            [In, MarshalAs(UnmanagedType.LPWStr)]
                string pszPropName,
            [In, Out]
                ref object pVar,
            [In]
                IErrorLog pErrorLog);


        [PreserveSig]
        int Write(
            [In, MarshalAs(UnmanagedType.LPWStr)]
                string pszPropName,
            [In]
                ref object pVar);
    }
}
