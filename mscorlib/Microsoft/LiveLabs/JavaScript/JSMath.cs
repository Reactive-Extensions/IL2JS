//
// Access to the Math.* family of JavaScript helper functions
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript
{
    [Import]
    public static class JSMath
    {
        [Import("Math.E")]
        extern public static JSNumber E { get; }

        [Import("Math.LN10")]
        extern public static JSNumber LN10 { get; }

        [Import("Math.LN2")]
        extern public static JSNumber LN2 { get; }

        [Import("Math.LOG10E")]
        extern public static JSNumber LOG10E { get; }

        [Import("Math.LOG2E")]
        extern public static JSNumber LOG2E { get; }

        [Import("Math.PI")]
        extern public static JSNumber PI { get; }

        [Import("Math.SQRT1_2")]
        extern public static JSNumber SQRT1_2 { get; }

        [Import("Math.SQRT2")]
        extern public static JSNumber SQRT2 { get; }

        [Import("Math.abs")]
        extern public static JSNumber Abs(JSNumber value);

        [Import("Math.acos")]
        extern public static JSNumber Acos(JSNumber value);

        [Import("Math.asin")]
        extern public static JSNumber Asin(JSNumber value);

        [Import("Math.atan")]
        extern public static JSNumber Atan(JSNumber value);

        [Import("Math.atan2")]
        extern public static JSNumber Atan2(JSNumber y, JSNumber x);

        [Import("Math.ceil")]
        extern public static JSNumber Ceil(JSNumber value);

        [Import("Math.cos")]
        extern public static JSNumber Cos(JSNumber value);

        [Import("Math.exp")]
        extern public static JSNumber Exp(JSNumber value);

        [Import("Math.floor")]
        extern public static JSNumber Floor(JSNumber value);

        [Import("Math.log")]
        extern public static JSNumber Log(JSNumber value);

        [Import("Math.max")]
        extern public static JSNumber Max(JSNumber first, JSNumber second);

        [Import("Math.min")]
        extern public static JSNumber Min(JSNumber first, JSNumber second);

        [Import("Math.pow")]
        extern public static JSNumber Pow(JSNumber x, JSNumber y);

        [Import("Math.random")]
        extern public static JSNumber Random();

        [Import("Math.round")]
        extern public static JSNumber Round(JSNumber value);

        [Import("Math.sin")]
        extern public static JSNumber Sin(JSNumber value);

        [Import("Math.sqrt")]
        extern public static JSNumber Sqrt(JSNumber value);

        [Import("Math.tan")]
        extern public static JSNumber Tan(JSNumber value);
    }
}
