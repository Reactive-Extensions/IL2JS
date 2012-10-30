using System;
using System.Runtime.InteropServices;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.WSH.Host
{
    [ComImport(), Guid("37D84F60-42CB-11CE-8135-00AA004BB851"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]

    public interface IPersistPropertyBag
    {
        [PreserveSig]
        int GetClassID(
            [Out]
                out Guid pClassID);


        [PreserveSig]
        int InitNew();


        [PreserveSig]
        int Load(
            [In, MarshalAs(UnmanagedType.Interface)]
                IPropertyBag pPropBag,
            [In, MarshalAs(UnmanagedType.Interface)]
                IErrorLog pErrorLog);


        [PreserveSig]
        int Save(
            [In, MarshalAs(UnmanagedType.Interface)]
                IPropertyBag pPropBag,
            [In, MarshalAs(UnmanagedType.Bool)]
                bool fClearDirty,
            [In, MarshalAs(UnmanagedType.Bool)]
                bool fSaveAllProperties);
    }
}
