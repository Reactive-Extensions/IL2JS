//
// Helpers for converting JavaScript literals and identifiers to and from their .Net representation, 
// generating identifiers, and encoding filenames.
// NOTE: Shared with CLR JST and JSTypes assemblies.
//

using System;
using System.Text;
using System.Globalization;
#if SILVERLIGHT || WSH
using System.Collections.Generic;
#else
using Microsoft.LiveLabs.Extras;
#endif

namespace Microsoft.LiveLabs.JavaScript.JST
{
    public static class Lexemes
    {
        // ----------------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------------

        private static string WithStringBuilder(Action<StringBuilder> f)
        {
            var sb = new StringBuilder();
            f(sb);
            return sb.ToString();
        }

        private static char Base16(uint d)
        {
            return "0123456789abcdef"[(int)(d % 16)];
        }

        private static char Base26(uint d)
        {
            return "abcdefghijklmnopqrstuvwxyz"[(int)(d % 26)];
        }

        private static char Base36(uint d)
        {
            return "0123456789abcdefghijklmnopqrstuvwxyz"[(int)(d % 36)];
        }

        private static char Base52(uint d)
        {
            return "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"[(int)(d % 52)];
        }

        private static char Base62(uint d)
        {
            return "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"[(int)(d % 62)];
        }

        private static void AppendBase16(StringBuilder sb, uint n, uint mask)
        {
            var rsb = new StringBuilder();
            while (n > 0 || mask > 0)
            {
                rsb.Append(Base16(n));
                n /= 16;
                mask /= 16;
            }
            var str = rsb.ToString();
            for (var i = str.Length - 1; i >= 0; i--)
                sb.Append(str[i]);
        }

        private static void AppendBase36(StringBuilder sb, uint n, uint mask)
        {
            var rsb = new StringBuilder();
            while (n > 0 || mask > 0)
            {
                rsb.Append(Base36(n));
                n /= 36;
                mask /= 36;
            }
            var str = rsb.ToString();
            for (var i = str.Length - 1; i >= 0; i--)
                sb.Append(str[i]);
        }

        private static uint ScanBase16Fixed(string str, ref int i, uint mask)
        {
            uint n = 0;
            while (mask > 0)
            {
                if (i >= str.Length)
                    throw new SyntaxException(null, "hexadecimal number", "too few digits");
                else
                {
                    var c = str[i++];
                    if (c >= '0' && c <= '9')
                        n = n * 16 + (uint)(c - '0');
                    else if (c >= 'a' && c <= 'f')
                        n = n * 16 + 10 + (uint)(c - 'a');
                    else if (c >= 'A' && c <= 'F')
                        n = n * 16 + 10 + (uint)(c - 'A');
                    else
                        throw new SyntaxException(null, "hexadecimal number", "invalid hex digit");
                    mask /= 16;
                }
            }
            return n;
        }

        private static double ScanBase8Variable(string str, ref int i)
        {
            double n = 0;
            bool any = false;
            while (i < str.Length)
            {
                var c = str[i];
                var d = default(int);
                if (c >= '0' && c <= '7')
                    d = (int)(c - '0');
                else
                    break;
                n *= 8;
                n += d;
                i++;
                any = true;
            }
            if (!any)
                throw new SyntaxException(null, "octal number", "no digits");
            return n;
        }

        private static double ScanBase10Variable(string str, ref int i)
        {
            double n = 0;
            bool any = false;
            while (i < str.Length)
            {
                var c = str[i];
                var d = default(int);
                if (c >= '0' && c <= '9')
                    d = (int)(c - '0');
                else
                    break;
                n *= 10;
                n += d;
                i++;
                any = true;
            }
            if (!any)
                throw new SyntaxException(null, "decimal number", "no digits");
            return n;
        }

        private static double ScanBase16Variable(string str, ref int i)
        {
            double n = 0;
            bool any = false;
            while (i < str.Length)
            {
                var c = str[i];
                var d = default(int);
                if (c >= '0' && c <= '9')
                    d = (int)(c - '0');
                else if (c >= 'a' && c <= 'f')
                    d = (int)(c - 'a' + 10);
                else if (c >= 'A' && c <= 'F')
                    d = (int)(c - 'A' + 10);
                else
                    break;
                n *= 16;
                n += d;
                i++;
                any = true;
            }
            if (!any)
                throw new SyntaxException(null, "hexadecimal number", "no digits");
            return n;
        }

        // -----------------------------------------------------------------------
        // Filenames (nothing to do with JavaScript, but convenient to deal with here)
        // ----------------------------------------------------------------------

        public static bool IsFileNameChar(char c)
        {
            return c >= 'a' && c <= 'z' || c >= '0' && c <= '9' ||
                   c == '.' || c == ' ' || c == ',' || c == '=' || c == '_';
        }

        public static bool IsFileName(string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;
            for (var i = 0; i < str.Length; i++)
            {
                if (!IsFileNameChar(str[i]))
                    return false;
            }
            return true;
        }

        public static void AppendStringToFileName(StringBuilder sb, string str)
        {
            str = str.ToLower(CultureInfo.InvariantCulture).Replace(" ", "");
            if (IsFileName(str))
                sb.Append(str);
            else
            {
                for (var i = 0; i < str.Length; i++)
                {
                    var c = str[i];
                    if (IsFileNameChar(c))
                        sb.Append(c);
                    else
                    {
                        sb.Append('$');
                        AppendBase36(sb, (uint)c, 0x1);
                        sb.Append('$');
                    }
                }
            }
        }

        public static string StringToFileName(string str)
        {
            return WithStringBuilder(sb => AppendStringToFileName(sb, str));
        }

        public static void AppendUIntToFileName(StringBuilder sb, uint n, uint mask)
        {
            sb.Append(Base36(n));
            n /= 36;
            mask /= 36;
            while (n > 36 || mask > 0)
            {
                sb.Append(Base36(n));
                n /= 36;
                mask /= 36;
            }
            if (n > 0)
                sb.Append(Base36(n - 1));
        }

        public static string UIntToFileName(uint n, uint mask)
        {
            return WithStringBuilder(sb => AppendUIntToFileName(sb, n, mask));
        }

        // ----------------------------------------------------------------------
        // JavaScript identifiers
        // ----------------------------------------------------------------------

#if SILVERLIGHT || WSH
        private static readonly List<string> javaScriptReservedNames = new List<string>
        
#else
        private static readonly Set<string> javaScriptReservedNames = new Set<string>
#endif
                                                                          {
                                                                              "break",
                                                                              "else",
                                                                              "new",
                                                                              "var",
                                                                              "case",
                                                                              "finally",
                                                                              "return",
                                                                              "void",
                                                                              "catch",
                                                                              "for",
                                                                              "switch",
                                                                              "while",
                                                                              "continue",
                                                                              "function",
                                                                              "this",
                                                                              "with",
                                                                              "default",
                                                                              "if",
                                                                              "throw",
                                                                              "in",
                                                                              "try",
                                                                              "do",
                                                                              "instanceof",
                                                                              "typeof",
                                                                              "abstract",
                                                                              "enum",
                                                                              "int",
                                                                              "short",
                                                                              "boolean",
                                                                              "export",
                                                                              "interface",
                                                                              "static",
                                                                              "byte",
                                                                              "extends",
                                                                              "long",
                                                                              "super",
                                                                              "char",
                                                                              "final",
                                                                              "native",
                                                                              "synchronized",
                                                                              "class",
                                                                              "float",
                                                                              "package",
                                                                              "throws",
                                                                              "const",
                                                                              "goto",
                                                                              "private",
                                                                              "transient",
                                                                              "debugger",
                                                                              "implements",
                                                                              "protected",
                                                                              "volatile",
                                                                              "double",
                                                                              "import",
                                                                              "public",
                                                                              "true",
                                                                              "false",
                                                                              "null"
                                                                          };

        public static bool IsJavaScriptReservedName(string str)
        {
            return javaScriptReservedNames.Contains(str);
        }

        public static bool IsLineTerminator(char c)
        {
            return c == '\u000a' || c == '\u000d' || c == '\u2028' || c == '\u2029';
        }

        public static bool IsFirstIdentifierChar(char c)
        {
            if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c == '$' || c == '_')
                return true;
            else
            {
                var cat = Char.GetUnicodeCategory(c);
                return cat == UnicodeCategory.UppercaseLetter ||
                       cat == UnicodeCategory.LowercaseLetter ||
                       cat == UnicodeCategory.TitlecaseLetter ||
                       cat == UnicodeCategory.ModifierLetter ||
                       cat == UnicodeCategory.OtherLetter ||
                       cat == UnicodeCategory.LetterNumber;
            }
        }

        public static bool IsIdentifierChar(char c)
        {
            if (c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '$' || c == '_')
                return true;
            else
            {
                var cat = Char.GetUnicodeCategory(c);
                return cat == UnicodeCategory.UppercaseLetter ||
                       cat == UnicodeCategory.LowercaseLetter ||
                       cat == UnicodeCategory.TitlecaseLetter ||
                       cat == UnicodeCategory.ModifierLetter ||
                       cat == UnicodeCategory.OtherLetter ||
                       cat == UnicodeCategory.LetterNumber ||
                       cat == UnicodeCategory.NonSpacingMark ||
                       cat == UnicodeCategory.SpacingCombiningMark ||
                       cat == UnicodeCategory.DecimalDigitNumber ||
                       cat == UnicodeCategory.ConnectorPunctuation;
            }
        }

        public static bool IsIdentifier(string str)
        {
            if (string.IsNullOrEmpty(str))
                return false;
            if (!IsFirstIdentifierChar(str[0]))
                return false;
            for (var i = 1; i < str.Length; i++)
            {
                if (!IsIdentifierChar(str[i]))
                    return false;
            }
            if (IsJavaScriptReservedName(str))
                return false;
            return true;
        }

        public static void AppendJavaScriptToIdentifier(StringBuilder sb, string str)
        {
            if (string.IsNullOrEmpty(str))
                throw new SyntaxException(null, "identifier", "empty identifier");
            if (IsIdentifier(str))
                sb.Append(str);
            else
            {
                var i = 0;
                while (i < str.Length)
                {
                    var c = default(char);
                    if (str[i] == '\\' && i + 5 < str.Length &&
                        str[i + 1] == 'u')
                    {
                        i += 2;
                        c = (char)ScanBase16Fixed(str, ref i, 0xffff);
                    }
                    else
                        c = str[i++];
                    if ((i == 0 && IsFirstIdentifierChar(c)) || (i > 0 && IsIdentifierChar(c)))
                        sb.Append(c);
                    else
                        throw new SyntaxException(null, "identifier", "illegal identifier character");
                }
            }
        }

        public static string JavaScriptToIdentifier(string str)
        {
            if (IsIdentifier(str))
                return str;
            else
                return WithStringBuilder(sb => AppendJavaScriptToIdentifier(sb, str));
        }

        public static void AppendIdentifierToJavaScript(StringBuilder sb, string id)
        {
            sb.Append(id);
        }

        public static string IdentifierToJavaScript(string id)
        {
            return id;
        }

        public static void AppendStringToIdentifier(StringBuilder sb, string str)
        {
            if (IsIdentifier(str) &&
                str.IndexOf('$') < 0)
                sb.Append(str);
            else if (str == null)
                sb.Append("$N$");
            else if (str.Length == 0)
                sb.Append("$E$");
            else if (IsJavaScriptReservedName(str))
            {
                sb.Append('$');
                sb.Append(str.ToUpper(CultureInfo.InvariantCulture));
                sb.Append('$');
            }
            else
            {
                for (var i = 0; i < str.Length; i++)
                {
                    var c = str[i];
                    if (c != '$' &&
                        ((i == 0 && IsFirstIdentifierChar(c)) || (i > 0 && IsIdentifierChar(c))))
                        sb.Append(c);
                    else
                    {
                        sb.Append('$');
                        AppendBase36(sb, (uint)c, 0x1u);
                        sb.Append('$');
                    }
                }
            }
        }

        public static string StringToIdentifier(string str)
        {
            if (IsIdentifier(str) && str.IndexOf('$') < 0)
                return str;
            else
                return WithStringBuilder(sb => AppendStringToIdentifier(sb, str));
        }

        public static void AppendStringToFriendlyIdentifier(StringBuilder sb, string str, int maxlen)
        {
            var n = 0;
            for (var i = 0; i < str.Length && n < maxlen; i++)
            {
                var c = str[i];
                if (IsIdentifierChar(c))
                {
                    n++;
                    sb.Append(c);
                }
            }
        }

        public static void AppendUIntToIdentifier(StringBuilder sb, uint n, uint mask)
        {
            sb.Append(Base52(n));
            n /= 52;
            mask /= 52;
            while (n > 62 || mask > 0)
            {
                sb.Append(Base62(n));
                n /= 62;
                mask /= 62;
            }
            if (n > 0)
                sb.Append(Base62(n - 1));
        }

        public static string UIntToIdentifier(uint n, uint mask)
        {
            return WithStringBuilder(sb => AppendUIntToIdentifier(sb, n, mask));
        }

        public static void AppendUIntToLowercaseIdentifier(StringBuilder sb, uint n, uint mask)
        {
            sb.Append(Base26(n));
            n /= 26;
            mask /= 26;
            while (n > 36 || mask > 0)
            {
                sb.Append(Base36(n));
                n /= 36;
                mask /= 36;
            }
            if (n > 0)
                sb.Append(Base36(n - 1));
        }

        public static string UIntToLowercaseIdentifier(uint n, uint mask)
        {
            return WithStringBuilder(sb => AppendUIntToLowercaseIdentifier(sb, n, mask));
        }

        private static uint RotL1(uint v) { return v << 1 | v >> 31; }
        private static uint RotL5(uint v) { return v << 5 | v >> 27; }
        private static uint RotL30(uint v) { return v << 30 | v >> 2; }

        public static void AppendHashToIdentifier(StringBuilder sb, string str)
        {
            var start = 0;
            var w = new uint[80];
            var h = new uint[5];
            h[0] = 0x67452301u;
            h[1] = 0xEFCDAB89u;
            h[2] = 0x98BADCFEu;
            h[3] = 0x10325476u;
            h[4] = 0xC3D2E1F0u;
            while (start < str.Length)
            {
                for (var i = 0; i < 16; i++)
                {
                    w[i] = 0u;
                    for (var j = 0; j < 4; j++)
                        w[i] = (w[i] << 8) | (start < str.Length ? (uint)str[start++] : 0u);
                }
                for (var i = 16; i < 80; i++)
                    w[i] = RotL1(w[i - 3] ^ w[i - 8] ^ w[i - 14] ^ w[i - 16]);

                var a = h[0];
                var b = h[1];
                var c = h[2];
                var d = h[3];
                var e = h[4];

                for (var i = 0; i < 20; i++)
                {
                    var t = RotL5(a) + ((b & c) | ((~b) & d)) + e + 0x5A827999u + w[i];
                    e = d;
                    d = c;
                    c = RotL30(b);
                    b = a;
                    a = t;
                }
                for (var i = 20; i < 40; i++)
                {
                    var t = RotL5(a) + (b ^ c ^ d) + e + 0x6ED9EBA1u + w[i];
                    e = d;
                    d = c;
                    c = RotL30(b);
                    b = a;
                    a = t;
                }
                for (var i = 40; i < 60; i++)
                {
                    var t = RotL5(a) + ((b & c) | (b & d) | (c & d)) + e + 0x8F1BBCDCu + w[i];
                    e = d;
                    d = c;
                    c = RotL30(b);
                    b = a;
                    a = t;
                }
                for (var i = 60; i < 80; i++)
                {
                    var t = RotL5(a) + (b ^ c ^ d) + e + 0xCA62C1D6u + w[i];
                    e = d;
                    d = c;
                    c = RotL30(b);
                    b = a;
                    a = t;
                }

                h[0] += a;
                h[1] += b;
                h[2] += c;
                h[3] += d;
                h[4] += e;
            }

            const ulong k32 = 1ul << 32;
            var mask = 0ul;
            var acc = 0ul;
            var word = 0;
            var first = true;
            while (true)
            {
                if (word < h.Length && mask < k32)
                {
                    acc = acc << 32 | h[word++];
                    mask = mask << 32 | k32;
                }
                if (mask == 0ul)
                    break;
                if (first)
                {
                    sb.Append(Base52((uint)(acc % 52)));
                    acc /= 52;
                    mask /= 52;
                    first = false;
                }
                else
                {
                    sb.Append(Base62((uint)(acc % 62)));
                    acc /= 62;
                    mask /= 62;
                }
            }
        }

        public static string HashToIdentifier(string str)
        {
            return WithStringBuilder(sb => AppendHashToIdentifier(sb, str));
        }

        // ----------------------------------------------------------------------
        // JavaScript boolean literals
        // ----------------------------------------------------------------------

        public static bool JavaScriptToBoolean(string str)
        {
            if (str == "true")
                return true;
            else if (str == "false")
                return false;
            else
                throw new SyntaxException(null, "boolean literal", "unrecognised literal");
        }

        public static string BooleanToJavaScript(bool b)
        {
            return b ? "true" : "false";
        }

        // ----------------------------------------------------------------------
        // JavaScript string literals (without the surrounding single or double quotes)
        // ----------------------------------------------------------------------

        public static void AppendStringToJavaScript(StringBuilder sb, string str)
        {
            var s = 0;
            var i = 0;
            while (i < str.Length)
            {
                var c = str[i];
                if (c >= ' ' && c <= '~' && c != '"' && c != '\'' && c != '\\')
                    i++;
                else
                {
                    sb.Append(str.Substring(s, i - s));
                    sb.Append('\\');
                    switch (c)
                    {
                        // It's safe to use the '\0' escape sequence only if the following character is not
                        // a decimal digit. Rather than dealing with that subtlety, we just always
                        // escape 0 as \x00
                        case '\'':
                        case '\"':
                        case '\\':
                            sb.Append(c);
                            break;
                        case '\b':
                            sb.Append('b');
                            break;
                        case '\f':
                            sb.Append('f');
                            break;
                        case '\n':
                            sb.Append('n');
                            break;
                        case '\r':
                            sb.Append('r');
                            break;
                        case '\t':
                            sb.Append('t');
                            break;
                        case '\v':
                            sb.Append('v');
                            break;
                        default:
                            if ((uint)c <= 0xff)
                            {
                                sb.Append('x');
                                AppendBase16(sb, (uint)c, 0xff);
                            }
                            else
                            {
                                sb.Append('u');
                                AppendBase16(sb, (uint)c, 0xffff);
                            }
                            break;
                    }
                    i++;
                    s = i;
                }
            }
            sb.Append(str.Substring(s, i - s));
        }

        public static string StringToJavaScript(string str)
        {
            var sb = new StringBuilder();
            AppendStringToJavaScript(sb, str);
            return sb.ToString();
        }

        public static void AppendJavaScriptToString(StringBuilder sb, string str, bool strict)
        {
            var s = 0;
            var i = 0;
            while (i < str.Length)
            {
                if (str[i] == '\\')
                {
                    sb.Append(str.Substring(s, i - s));
                    i++;
                    if (i >= str.Length)
                        throw new SyntaxException(null, "string literal", "unrecognized escape sequence");
                    else
                    {
                        switch (str[i])
                        {
                            case '0':
                                i++;
                                sb.Append('\0');
                                break;
                            case 'x':
                                i++;
                                sb.Append((char)ScanBase16Fixed(str, ref i, 0xff));
                                break;
                            case 'u':
                                i++;
                                sb.Append((char)ScanBase16Fixed(str, ref i, 0xffff));
                                break;
                            case 'b':
                                i++;
                                sb.Append('\b');
                                break;
                            case 'f':
                                i++;
                                sb.Append('\f');
                                break;
                            case 'n':
                                i++;
                                sb.Append('\n');
                                break;
                            case 'r':
                                i++;
                                sb.Append('\r');
                                break;
                            case 't':
                                i++;
                                sb.Append('\t');
                                break;
                            case 'v':
                                i++;
                                sb.Append('\v');
                                break;
                            default:
                                if (IsLineTerminator(str[i]))
                                {
                                    if (strict)
                                        throw new SyntaxException(null, "string literal", "line terminator in string");
                                    else
                                    {
                                        if (str[i++] == '\r' && i < str.Length && str[i] == '\n')
                                            i++;
                                    }
                                }
                                else
                                    sb.Append(str[i++]);
                                break;
                        }
                    }
                    s = i;
                }
                else
                    i++;
            }
            sb.Append(str.Substring(s, str.Length - s));
        }

        public static string JavaScriptToString(string str, bool strict)
        {
            var sb = new StringBuilder();
            AppendJavaScriptToString(sb, str, strict);
            return sb.ToString();
        }

        // ----------------------------------------------------------------------
        // JavaScript "escape" and "unescape"
        // ----------------------------------------------------------------------

        public static void AppendJavaScriptEscape(StringBuilder sb, string str)
        {
            for (var i = 0; i < str.Length; i++)
            {
                if (str[i] >= ' ' && str[i] <= '~')
                    sb.Append(str[i]);
                else if (str[i] <= '\xff')
                {
                    sb.Append('%');
                    AppendBase16(sb, str[i], 0xff);
                }
                else
                {
                    sb.Append("%u");
                    AppendBase16(sb, str[i], 0xffff);
                }
            }
        }

        public static string JavaScriptEscape(string str)
        {
            if (str == null)
                return null;
            var sb = new StringBuilder();
            AppendJavaScriptEscape(sb, str);
            return sb.ToString();
        }

        public static void AppendJavaScriptUnescape(StringBuilder sb, string str)
        {
            var i = 0;
            while (i < str.Length)
            {
                if (str[i] == '%')
                {
                    i++;
                    if (i >= str.Length)
                        throw new SyntaxException(null, "escaped string", "invalid escape sequence");
                    if (str[i] == 'u' || str[i] == 'U')
                    {
                        i++;
                        var n = ScanBase16Fixed(str, ref i, 0xffff);
                        sb.Append((char)n);
                    }
                    else
                    {
                        var n = ScanBase16Fixed(str, ref i, 0xff);
                        sb.Append((char)n);
                    }
                }
                else
                    sb.Append(str[i++]);
            }
        }

        public static string JavaScriptUnescape(string str)
        {
            if (str == null)
                return null;
            var sb = new StringBuilder();
            AppendJavaScriptUnescape(sb, str);
            return sb.ToString();
        }

        // ----------------------------------------------------------------------
        // Numbers
        // ----------------------------------------------------------------------

        public static bool IsNumber(string str)
        {
            var dummy = default(double);
            return !String.IsNullOrEmpty(str) && Double.TryParse(str, out dummy);
        }

        public static double JavaScriptToNumber(string str)
        {
            if (String.IsNullOrEmpty(str))
                throw new SyntaxException(null, "number literal", "empty number");
            else
            {
                var d = default(double);
                if (Double.TryParse(str, out d))
                    return d;
                else
                {
                    if (str.Length == 1 && str[0] == '0')
                        return 0.0;
                    else
                    {
                        var n = default(double);
                        var i = default(int);
                        if (str[0] == '0' && (str[1] == 'x' || str[1] == 'X'))
                        {
                            i = 2;
                            n = ScanBase16Variable(str, ref i);
                        }
                        else if (str[0] == '0')
                        {
                            i = 1;
                            n = ScanBase8Variable(str, ref i);
                        }
                        else
                        {
                            i = 0;
                            n = ScanBase10Variable(str, ref i);
                        }
                        if (i == str.Length || i == str.Length - 1 && (str[i] == 'l' || str[i] == 'L'))
                            return (double)n;
                        else
                            throw new SyntaxException(null, "number literal", "invalid digit");
                    }
                }
            }
        }

        public static string NumberToJavaScript(double value)
        {
            return value.ToString();
        }

        // ----------------------------------------------------------------------
        // JavaScript regular expressions (with surrounding/delimiting slashes)
        // ----------------------------------------------------------------------

        public static void JavaScriptToRegexp(string str, out string pattern, out string attributes)
        {
            if (String.IsNullOrEmpty(str))
                throw new SyntaxException(null, "regexp literal", "empty regexp");
            else
            {
                var i = 0;
                if (str[i++] != '/')
                    throw new SyntaxException(null, "regexp literal", "expecting opening '/'");
                var sb = new StringBuilder();
                while (true)
                {
                    if (i >= str.Length)
                        throw new SyntaxException(null, "regexp literal", "expecting closing '/'");
                    else if (str[i] == '/')
                    {
                        i++;
                        pattern = sb.ToString();
                        break;
                    }
                    else if (str[i] == '\\' && i + 1 < str.Length)
                    {
                        sb.Append(str[i++]);
                        sb.Append(str[i++]);
                    }
                    else
                    {
                        sb.Append(str[i]);
                        i++;
                    }
                }
                if (i < str.Length)
                    attributes = str.Substring(i);
                else
                    attributes = null;
            }
        }
    }
}