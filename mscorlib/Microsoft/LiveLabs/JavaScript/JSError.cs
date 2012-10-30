//
// A proxy for a JavaScript exception. Derived types:
//  - JSEvalError
//  - JSRangeError
//  - JSReferenceError
//  - JSSyntaxError
//  - JSTypeError
//  - JSUriError
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript
{
    [Import]
    public class JSError : JSObject
    {
        [Import("Error")]
        extern public JSError();

        [Import("Error")]
        extern public JSError(string message);

        extern public string Message { get; }
        extern public string Name { get; }
    }
}
