//
// Access to some important global JavaScript values and functions
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript
{
    [Import]
    public static class JSGlobals
    {
        extern public static string DecodeURI(string uri);
        extern public static string DecodeURIComponent(string uriComponent);
        extern public static string EncodeURI(string uri);
        extern public static string EncodeURIComponent(string uriComponent);
        extern public static string Escape(string value);

        [Import(@"function(code) { eval(""var temp = "" + code); return temp; }")]
        extern public static JSObject Eval(string code);

        [Import(@"function(code) { eval(""var temp = "" + code); return temp; }")]
        extern public static T Eval<T>(string code);

        [Import(MemberNameCasing = Casing.Exact)]
        extern public static JSNumber Infinity { get; }

        extern public static bool IsFinite(JSNumber number);
        extern public static bool IsNaN(JSNumber number);

        [Import(MemberNameCasing = Casing.Exact)]
        extern public static JSNumber NaN { get; }

        extern public static JSNumber ParseFloat(string value);
        extern public static JSNumber ParseInt(string value);
        extern public static JSNumber ParseInt(string value, int radix);

        [Import(@"function(item) { return typeof item; }")]
        public static extern string TypeOf(JSObject item);

        [Import(@"function(item, typeConstructor) { return item instanceof typeConstructor; }")]
        public static extern bool InstanceOf(JSObject item, JSFunction typeConstructor);

        extern public static JSObject Undefined { get; }
        extern public static string Unescape(string value);
        extern public static JSArguments Arguments { get; }

        [Import(@"function (name) { return window[name]; }")]
        extern public static JSObject GetGlobal(string name);

        [Import(@"function (name, value) { window[name] = value; }")]
        extern public static JSObject SetGlobal(string name, JSObject value);
    }
}
