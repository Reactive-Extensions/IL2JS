using System;
using System.Runtime.InteropServices;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.WSH.Host
{
    [ComImport(), Guid("0000000C-0000-0000-C000-000000000046"), InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    public interface IStream
    {

        int Read(

                IntPtr buf,

                int len);


        int Write(

                IntPtr buf,

                int len);

        [return: MarshalAs(UnmanagedType.I8)]
        long Seek(
               [In, MarshalAs(UnmanagedType.I8)] 
                 long dlibMove,

                int dwOrigin);

        void SetSize(
               [In, MarshalAs(UnmanagedType.I8)] 
                 long libNewSize);

        [return: MarshalAs(UnmanagedType.I8)]
        long CopyTo(
               [In, MarshalAs(UnmanagedType.Interface)] 
                  IStream pstm,
               [In, MarshalAs(UnmanagedType.I8)] 
                 long cb,
               [Out, MarshalAs(UnmanagedType.LPArray)] 
                 long[] pcbRead);


        void Commit(

                int grfCommitFlags);


        void Revert();

        void LockRegion(
               [In, MarshalAs(UnmanagedType.I8)] 
                 long libOffset,
               [In, MarshalAs(UnmanagedType.I8)] 
                 long cb,

                int dwLockType);


        void UnlockRegion(
               [In, MarshalAs(UnmanagedType.I8)] 
                 long libOffset,
               [In, MarshalAs(UnmanagedType.I8)] 
                 long cb,

                int dwLockType);

        void Stat(
                [Out] 
                 System.Runtime.InteropServices.ComTypes.STATSTG pStatstg,
                int grfStatFlag);

        [return: MarshalAs(UnmanagedType.Interface)]
        IStream Clone();
    }

}
