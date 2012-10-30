//
// A proxy for a JavaScript type error exception
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript
{
    [Import]
    public sealed class JSTypeError : JSError
    {
        [Import("TypeError")]
        extern public JSTypeError();

        [Import("TypeError")]
        extern public JSTypeError(string message);
    }
}
