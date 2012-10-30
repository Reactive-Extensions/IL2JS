using System;
using System.Runtime.InteropServices;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.WSH.Host
{
    [ComImport(), Guid("7FD52380-4E07-101B-AE2D-08002B2EC713"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    public interface IPersistStreamInit
    {
        [PreserveSig]
        int GetClassID(
               [Out] 
                  out Guid pClassID);


        [PreserveSig]
        int IsDirty();


        [PreserveSig]
        int Load(
               [In, MarshalAs(UnmanagedType.Interface)] 
                  IStream pstm);


        [PreserveSig]
        int Save(
               [In, MarshalAs(UnmanagedType.Interface)] 
                      IStream pstm,
               [In, MarshalAs(UnmanagedType.Bool)] 
                     bool fClearDirty);

        [PreserveSig]
        int GetSizeMax(
               [Out, MarshalAs(UnmanagedType.LPArray)] 
                 long pcbSize);

        [PreserveSig]

        int InitNew();
    }
}
