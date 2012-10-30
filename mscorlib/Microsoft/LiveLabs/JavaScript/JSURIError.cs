//
// A proxy for a JavaScript URI parsing exception
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript
{
    [Import]
    public sealed class JSURIError : JSError
    {
        [Import("URIError")]
        extern public JSURIError();

        [Import("URIError")]
        extern public JSURIError(string message);
    }
}
