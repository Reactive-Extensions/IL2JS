//
// A proxy for a JavaScript string
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace Microsoft.LiveLabs.JavaScript
{
    [Import]
    public sealed class JSString : JSObject
    {
        [Import("String")]
        extern public JSString(string s);

        public char this[int index]
        {
            get { return (char)CharCodeAt(index); }
        }

        [Import("valueOf")]
        extern public override string ToString();

        [Import(Qualification = Qualification.Type)]
        extern public static JSString FromCharCode(params int[] charCodes);

        extern public JSString CharAt(int index);
        extern public int CharCodeAt(int index);
        extern public int Length { get; }
        extern public JSString Concat(JSString s);
        extern public int IndexOf(JSString substring);
        extern public int IndexOf(JSString substring, int start);
        extern public int LastIndexOf(JSString substring);
        extern public int LastIndexOf(JSString substring, int start);
        extern public JSMatchResults Match(JSRegExp regularExpression);
        extern public JSString Replace(JSRegExp regularExpression, JSString replacement);
        extern public JSString Replace(JSString expression, JSString replacement);
        extern public int Search(JSRegExp regularExpression);
        extern public JSString Slice(int start, int end);
        extern public JSString Split(JSString delimiter);
        extern public JSString Split(JSString delimiter, int limiter);
        extern public JSString Split(JSRegExp delimiter);
        extern public JSString Split(JSRegExp delimiter, int limiter);
        extern public JSString Substring(int from);
        extern public JSString Substring(int from, int to);
        extern public JSString ToLowerCase();
        extern public JSString ToUpperCase();
    }
}
