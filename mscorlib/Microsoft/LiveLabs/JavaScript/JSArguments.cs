//
// A proxy for the 'arguments' object bound within any function body
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript
{
    [Import]
    public sealed class JSArguments
    {
        // Constructed by JavaScript runtime only
        public JSArguments(JSContext ctxt)
        {
        }

        extern public JSFunction Callee { get; }
        extern public JSObject this[int index] { get; }
        extern public int Length { get; }
    }
}
