//
// An alias for a JavaScript regular expression match result
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript
{
    [Import]
    public sealed class JSMatchResults
    {
        // Created by JavaScript runtime only
        public JSMatchResults(JSContext ctxt)
        {
        }

        extern public int Index { get; }
        extern public JSString Input { get; }
        extern public int Length { get; }
        extern public JSString this[int index] { get; }
    }
}