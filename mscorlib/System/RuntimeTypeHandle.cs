//
// A reimplementation of runtime type handles for the JavaScript runtime system.
// Underlying representation is the runtime <type> structure, or null.
//

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    [Runtime(true)]
    public struct RuntimeTypeHandle
    {
        internal static RuntimeTypeHandle EmptyHandle
        {
            [Import("function() { return null; }")]
            get { throw new NotSupportedException(); }
        }
    }
}
