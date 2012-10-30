//
// A proxy for a JavaScript function
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript
{
    [Import]
    public class JSFunction : JSObject
    {
        [Import("function (parameterNamesAndBody) { return Function.apply(null, parameterNamesAndBody); }")]
        extern public JSFunction(string[] parameterNamesAndBody);

        extern public JSObject Apply(JSObject thisObject, JSObject[] arguments);
        extern public JSObject[] Arguments { get; }
        extern public JSObject Call(JSObject thisObject, params JSObject[] arguments);
        extern public JSFunction Caller { get; }
        extern public int Length { get; }
        extern public JSObject Prototype { get; }
    }
}
