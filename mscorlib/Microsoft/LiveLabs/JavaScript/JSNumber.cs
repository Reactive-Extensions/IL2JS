//
// A proxy for a JavaScript number
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript
{
    [Import]
    public sealed class JSNumber : JSObject
    {
        [Import("Number.MAX_VALUE")]
        extern public static double MaxValue { get; }

        [Import("Number.MIN_VALUE")]
        extern public static double MinValue { get; }

        [Import("Number.NaN")]
        extern public static double NaN { get; }

        [Import("Number.NEGATIVE_INFINITY")]
        extern public static double NegativeInfinity { get; }

        [Import("Number.POSITIVE_INFINITY")]
        extern public static double PositiveInfinity { get; }

        [Import("Number")]
        extern public JSNumber(double value);

        [Import("Number")]
        extern public JSNumber(int value);

        [Import("Number")]
        extern public JSNumber(string value);

        extern public JSNumber ToExponential(int digits);
        extern public JSNumber ToFixed(int digits);
        extern public JSNumber ToPrecision(int precission);

        [Import(@"function(inst) { return inst; }", PassInstanceAsArgument = true)]
        extern public double ToDouble();

        [Import(@"function(inst) { return inst.toFixed(0); }", PassInstanceAsArgument = true)]
        extern public int ToInt();
    }
}
