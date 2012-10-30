using System;
using System.Runtime.InteropServices;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.WSH.Host
{
    [ComImport(), Guid("0000010A-0000-0000-C000-000000000046"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    public interface IPersistStorage
    {
        [PreserveSig]
        int GetClassID(
               [Out] 
                  out Guid pClassID);

        [PreserveSig]
        int IsDirty();

        [PreserveSig]
        int InitNew(IStorage pstg);

        [PreserveSig]
        int Load(IStorage pstg);

        [PreserveSig]
        int Save(IStorage pStgSave, int fSameAsLoad);

        [PreserveSig]
        int SaveCompleted(IStorage pStgNew);

        [PreserveSig]
        int HandsOffStorage();
    }
}
