//
// A proxy for a JavaScript parse exception
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript
{
    [Import]
    public sealed class JSSyntaxError : JSError
    {
        [Import("SyntaxError")]
        extern public JSSyntaxError();

        [Import("SyntaxError")]
        extern public JSSyntaxError(string message);
    }
}
