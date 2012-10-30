using System;
using System.Runtime.InteropServices;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.WSH.Host
{
    [ComImport(), Guid("0000000B-0000-0000-C000-000000000046"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]

    public interface IStorage
    {

        [return: MarshalAs(UnmanagedType.Interface)]
        IStream CreateStream(
              [In, MarshalAs(UnmanagedType.BStr)] 
                 string pwcsName,
              [In, MarshalAs(UnmanagedType.U4)] 
                 int grfMode,
              [In, MarshalAs(UnmanagedType.U4)] 
                 int reserved1,
              [In, MarshalAs(UnmanagedType.U4)] 
                 int reserved2);

        [return: MarshalAs(UnmanagedType.Interface)]
        IStream OpenStream(
              [In, MarshalAs(UnmanagedType.BStr)] 
                 string pwcsName,

               IntPtr reserved1,
              [In, MarshalAs(UnmanagedType.U4)] 
                 int grfMode,
              [In, MarshalAs(UnmanagedType.U4)] 
                 int reserved2);

        [return: MarshalAs(UnmanagedType.Interface)]
        IStorage CreateStorage(
              [In, MarshalAs(UnmanagedType.BStr)] 
                 string pwcsName,
              [In, MarshalAs(UnmanagedType.U4)] 
                 int grfMode,
              [In, MarshalAs(UnmanagedType.U4)] 
                 int reserved1,
              [In, MarshalAs(UnmanagedType.U4)] 
                 int reserved2);

        [return: MarshalAs(UnmanagedType.Interface)]
        IStorage OpenStorage(
              [In, MarshalAs(UnmanagedType.BStr)] 
                 string pwcsName,

               IntPtr pstgPriority,   // must be null
              [In, MarshalAs(UnmanagedType.U4)] 
                 int grfMode,

               IntPtr snbExclude,
              [In, MarshalAs(UnmanagedType.U4)] 
                 int reserved);


        void CopyTo(

                int ciidExclude,
               [In, MarshalAs(UnmanagedType.LPArray)] 
                 Guid[] pIIDExclude,

                IntPtr snbExclude,
               [In, MarshalAs(UnmanagedType.Interface)] 
                 IStorage stgDest);


        void MoveElementTo(
               [In, MarshalAs(UnmanagedType.BStr)] 
                 string pwcsName,
               [In, MarshalAs(UnmanagedType.Interface)] 
                 IStorage stgDest,
               [In, MarshalAs(UnmanagedType.BStr)] 
                 string pwcsNewName,
               [In, MarshalAs(UnmanagedType.U4)] 
                 int grfFlags);


        void Commit(

                int grfCommitFlags);


        void Revert();


        void EnumElements(
               [In, MarshalAs(UnmanagedType.U4)] 
                 int reserved1,
            // void *
                IntPtr reserved2,
               [In, MarshalAs(UnmanagedType.U4)] 
                 int reserved3,
               [Out, MarshalAs(UnmanagedType.Interface)]
                 out object ppVal);                     // IEnumSTATSTG


        void DestroyElement(
               [In, MarshalAs(UnmanagedType.BStr)] 
                 string pwcsName);


        void RenameElement(
               [In, MarshalAs(UnmanagedType.BStr)] 
                 string pwcsOldName,
               [In, MarshalAs(UnmanagedType.BStr)] 
                 string pwcsNewName);


        void SetElementTimes(
               [In, MarshalAs(UnmanagedType.BStr)] 
                 string pwcsName,
               [In] 
                 System.Runtime.InteropServices.ComTypes.FILETIME pctime,
               [In] 
                 System.Runtime.InteropServices.ComTypes.FILETIME patime,
               [In] 
                 System.Runtime.InteropServices.ComTypes.FILETIME pmtime);

        void SetClass(
               [In] 
                 ref Guid clsid);


        void SetStateBits(

                int grfStateBits,

                int grfMask);

        void Stat(
               [Out] 
                 System.Runtime.InteropServices.ComTypes.STATSTG pStatStg,
                int grfStatFlag);
    }

}
