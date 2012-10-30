//
// A proxy for a JavaScript regular expression (which is also a function)
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript
{
    [Import]
    public sealed class JSRegExp : JSFunction
    {
        [Import("RegExp")]
        extern public JSRegExp(string pattern);

        [Import("RegExp")]
        extern public JSRegExp(string pattern, string attributes);

        extern public JSMatchResults Exec(string value);
        extern public bool Global { get; }
        extern public bool IgnoreCase { get; }
        extern public int LastIndex { get; }
        extern public bool Multiline { get; }
        extern public string Source { get; set; }
        extern public bool Test(JSString value);
    }
}
