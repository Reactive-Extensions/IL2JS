//
// A proxy for a JavaScript evaluation exception
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript
{
    [Import]
    public sealed class JSEvalError : JSError
    {
        [Import("EvalError")]
        extern public JSEvalError();

        [Import("EvalError")]
        extern public JSEvalError(string message);
    }
}
