using System.Runtime.InteropServices;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.WSH.Host
{
    [ComImport(), Guid("3127CA40-446E-11CE-8135-00AA004BB851"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    public interface IErrorLog
    {
        void AddError(
               [In, MarshalAs(UnmanagedType.LPWStr)] 
                             string pszPropName_p0,
               [In, MarshalAs(UnmanagedType.Struct)] 
                              ref System.Runtime.InteropServices.ComTypes.EXCEPINFO pExcepInfo_p1);

    }
}
