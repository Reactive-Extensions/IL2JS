//
// A proxy for a JavaScript undefined variable exception
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript
{
    [Import]
    public sealed class JSReferenceError : JSError
    {
        [Import("ReferenceError")]
        extern public JSReferenceError();

        [Import("ReferenceError")]
        extern public JSReferenceError(string message);
    }
}
