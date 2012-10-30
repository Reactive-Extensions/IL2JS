//
// Reimplement some of the BCL/CLR helpers for the JavaScript runtime
//

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;
using System.Reflection;

namespace System.Runtime.CompilerServices
{
    public static class RuntimeHelpers
    {
        public static new bool Equals(object o1, object o2)
        {
            if (o1 == null && o2 == null) return true;
            if (o1 == null || o2 == null) return false;
            return o1.Equals(o2);
        }

        public static int GetHashCode(object o)
        {
            if (o == null) return 0;
            return o.GetHashCode();
        }

        // We suppress array copying
        [Import("function(root, array, initialization) { root.InitializeArray(array, initialization); }", PassRootAsArgument = true)]
        extern private static void PrimInitializeArray(Array array, [NoInterop(true)]int[] initialization);

        public static void InitializeArray(Array array, RuntimeFieldHandle fldHandle)
        {
            var field = FieldInfo.GetFieldFromHandle(fldHandle);
            PrimInitializeArray(array, field.Initialization);
        }
    }
}
