using System.Runtime.InteropServices;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.WSH
{
    public static class HResult
    {
        public const int E_FAIL = unchecked((int)0x80004005);
        public const int E_INVALIDARG = unchecked((int)0x80070057);
        public const int E_NOTIMPL = unchecked((int)0x80004001);
        public const int E_POINTER = (int)-2147467261;
        public const int S_FALSE = 1;
        public const int S_OK = 0;
        public const int TYPE_E_ELEMENTNOTFOUND = unchecked((int)0x8002802B);


        public static bool Failed(int hResult)
        {
            return hResult < 0;
        }

        public static bool Succeeded(int hResult)
        {
            return hResult >= 0;
        }

        public static void ThrowOnFailure(int hResult)
        {
            if (Failed(hResult)) throw Marshal.GetExceptionForHR(hResult);
        }

        public static void IngoreHr(int hResult)
        {
            // Use this to indicate you explicity want to ignore the result.
        }
    }
}
