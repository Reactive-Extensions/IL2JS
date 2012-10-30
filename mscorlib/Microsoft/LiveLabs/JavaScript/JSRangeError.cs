//
// A proxy for a JavaScript argument range exception
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript
{
    [Import]
    public sealed class JSRangeError : JSError
    {
        [Import("RangeError")]
        extern public JSRangeError();

        [Import("RangeError")]
        extern public JSRangeError(string message);
    }
}
