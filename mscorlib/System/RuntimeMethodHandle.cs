//
// A reimplementation of runtime method handles for the JavaScript runtime system.
// Underlying representation is a MethodBase object or null.
//

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    [Runtime(true)]
    public struct RuntimeMethodHandle
    {
        internal static RuntimeMethodHandle EmptyHandle
        {
            [Import("function() { return null; }")]
            get { throw new InvalidOperationException(); }
        }
    }
}
