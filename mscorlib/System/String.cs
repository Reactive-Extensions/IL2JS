//
// A reimplementation of String for the JavaScript runtime.
// Underlying representation is a JavaScript string who's prototype has type field bound to type structure
// of this type.
//

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

using System.Collections.Generic;
using System.Collections;
using System.Text;

namespace System
{
    [UsedType(true)]
    [Runtime(true)]
    public sealed class String : IComparable, ICloneable, IComparable<string>, IEnumerable<char>, IEquatable<string>
    {
        private const int TrimHead = 0;
        private const int TrimTail = 1;
        private const int TrimBoth = 2;
        private const int charPtrAlignConst = 1;
        private const int alignConst = 3;
    
        public static string Empty;

        internal static char[] WhitespaceChars;

        static String()
        {
            Empty = "";
            // Don't use standard initilizer to avoid depending on InitializeArray
            // WhitespaceChars = new char[] { '\t', '\n', '\v', '\f', '\r', ' ' };
            WhitespaceChars = new char[6];
            WhitespaceChars[0] = '\t';
            WhitespaceChars[1] = '\n';
            WhitespaceChars[2] = '\v';
            WhitespaceChars[3] = '\f';
            WhitespaceChars[4] = '\r';
            WhitespaceChars[5] = ' ';
        }
    
        [Import(@"function(root, value) {
                      if (value == null)
                          throw root.NullReferenceException();
                      return value.join("""");
                  }", PassRootAsArgument = true)]
        extern public String(char[] value);
    
        [Import(@"function (c, count) {
                      var s = [];
                      for (var i = 0; i < count; i++)
                          s.push(String.fromCharCode(c));
                      return s.join("""");
                  }")]
        extern public String(char c, int count);
    
        [Import(@"function(root, value, startIndex, length) { 
                      if (value == null)
                          throw root.NullReferenceException();
                      if (startIndex == 0 && length == value.length) 
                          return value.join(""""); 
                      var s = []; 
                      var endIndex = startIndex + length;
                      for (var i = startIndex; i < endIndex; i++) 
                          s.push(value[i]);
                      return s.join("""");
                  }", PassRootAsArgument = true)]
        extern public String(char[] value, int startIndex, int length);
    
        public object Clone()
        {
            return this;
        }

        [Import(@"function(strA, strB) {
                      if (strA == strB)
                          return 0;
                      if (strA == null)
                          return -1;
                      if (strB == null)
                          return 1;
                      if (strA < strB)
                          return -1;
                      return 1;
                  }")]
        public extern static int Compare(string strA, string strB);
    
        public static int Compare(string strA, string strB, bool ignoreCase)
        {
            if (ignoreCase)
                return Compare(strA == null ? null : strA.ToUpper(), strB == null ? null : strB.ToUpper());
            else
                return Compare(strA, strB);
        }

        public static int Compare(string strA, string strB, StringComparison comparisonType)
        {
            return Compare
                (strA,
                 strB,
                 comparisonType == StringComparison.OrdinalIgnoreCase);
        }
    
        public static int Compare(string strA, int indexA, string strB, int indexB, int length)
        {
            return Compare(strA.Substring(indexA, length), strB.Substring(indexB, length));
        }

        public static int Compare(string strA, int indexA, string strB, int indexB, int length, bool ignoreCase)
        {
            return Compare(strA.Substring(indexA, length), strB.Substring(indexB, length), ignoreCase);
        }

        public static int Compare(string strA, int indexA, string strB, int indexB, int length, StringComparison comparisonType)
        {
            return Compare(strA.Substring(indexA, length), strB.Substring(indexB, length), comparisonType);
        }
    
        public static int CompareOrdinal(string strA, string strB)
        {
            return Compare(strA, strB);
        }

        public static int CompareOrdinal(string strA, int indexA, string strB, int indexB, int length)
        {
            return Compare(strA.Substring(indexA, length), strB.Substring(indexB, length));
        }
    
        public int CompareTo(object value)
        {
            if (value == null)
                return 1;
            var str = value as string;
            if (str == null)
                throw new ArgumentException();
            return Compare(this, str);
        }

        public int CompareTo(string strB)
        {
            return Compare(this, strB);
        }
    
        public static string Concat(params object[] args)
        {
            if (args == null)
                throw new ArgumentNullException("args");
            var strs = new string[args.Length];
            for (var i = 0; i < args.Length; i++)
                strs[i] = args[i] == null ? Empty : args[i].ToString();
            return Concat(strs);
        }

        [Import(@"function(root) {
                      var values = [];
                      for (var i = 1; i < arguments.length; i++) {
                          if (arguments[i] != null)
                              values.push(arguments[i]);
                      }
                      return values.join("""");
                  }", PassRootAsArgument = true, InlineParamsArray = true)]
        extern public static string Concat(params string[] values);

        // We need the following since the C# compiler wishes to bind to them directly
        public static string Concat(object arg0)
        {
            if (arg0 == null)
                return Empty;
            return arg0.ToString();
        }

        public static string Concat(object arg0, object arg1)
        {
            if (arg0 == null)
                arg0 = Empty;
            if (arg1 == null)
                arg1 = Empty;
            return (arg0.ToString() + arg1.ToString());
        }

        public static string Concat(string str0, string str1)
        {
            if (IsNullOrEmpty(str0))
            {
                if (IsNullOrEmpty(str1))
                    return Empty;
                return str1;
            }
            if (IsNullOrEmpty(str1))
                return str0;
            return Concat2(str0, str1);
        }

        [Import(@"function(str0, str1) { return [str0, str1].join(""""); }")]
        extern private static string Concat2(string str0, string str1);

        public static string Concat(object arg0, object arg1, object arg2)
        {
            if (arg0 == null)
                arg0 = Empty;
            if (arg1 == null)
                arg1 = Empty;
            if (arg2 == null)
                arg2 = Empty;
            return (arg0.ToString() + arg1.ToString() + arg2.ToString());
        }

        public static string Concat(string str0, string str1, string str2)
        {
            if (((str0 == null) && (str1 == null)) && (str2 == null))
                return Empty;
            if (str0 == null)
                str0 = Empty;
            if (str1 == null)
                str1 = Empty;
            if (str2 == null)
                str2 = Empty;
            return Concat3(str0, str1, str2);
        }

        [Import(@"function(str0, str1, str2) { return [str0, str1, str2].join(""""); }")]
        extern private static string Concat3(string str0, string str1, string str2);

        public static string Concat(string str0, string str1, string str2, string str3)
        {
            if (((str0 == null) && (str1 == null)) && ((str2 == null) && (str3 == null)))
                return Empty;
            if (str0 == null)
                str0 = Empty;
            if (str1 == null)
                str1 = Empty;
            if (str2 == null)
                str2 = Empty;
            if (str3 == null)
                str3 = Empty;
            return Concat4(str0, str1, str2, str3);
        }

        [Import(@"function(str0, str1, str2, str3) { return [str0, str1, str2, str3].join(""""); }")]
        extern private static string Concat4(string str0, string str1, string str2, string str3);

        [Import(@"function(root, inst, value) {
                      if (value == null)
                          throw root.NullReferenceException();
                      return value == """" || inst.indexOf(value) >= 0;
                  }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern public bool Contains(string value);

        internal bool EndsWith(char value)
        {
            var length = Length;
            return length != 0 && this[length - 1] == value;
        }

        public bool EndsWith(string value)
        {
            return EndsWith(value, StringComparison.Ordinal);
        }
    
        public bool EndsWith(string value, StringComparison comparisonType)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (value.Length == 0)
                return true;
            if (value.Length > Length)
                return false;
            var substr = Substring(Length - value.Length, value.Length);
            return Compare(substr, value, comparisonType) == 0;
        }

        public override bool Equals(object obj)
        {
            var strB = obj as string;
            if (strB == null)
                return false;
            return EqualsHelper(this, strB);
        }
    
        public bool Equals(string value)
        {
            if (value == null)
                return false;
            return EqualsHelper(this, value);
        }

        public static bool Equals(string a, string b)
        {
            if (a == null && b == null)
                return true;
            if (a == null || b == null)
                return false;
            return EqualsHelper(a, b);
        }

        public bool Equals(string value, StringComparison comparisonType)
        {
            return Equals(this, value, comparisonType);
        }

        public static bool Equals(string a, string b, StringComparison comparisonType)
        {
            if (comparisonType == StringComparison.OrdinalIgnoreCase)
                return Equals(a.ToLower(), b.ToLower());
            else
                return Equals(a, b);
        }
    
        [Import("function(strA, strB) { return strA == strB; }")]
        private extern static bool EqualsHelper(string strA, string strB);

        public static string Format(IFormatProvider provider, string format, params object[] args)
        {
            return Format(format, args);
        }

        public static string Format(string format, params object[] args)
        {
            if (format == null)
                throw new ArgumentNullException("format");
            if (args == null)
                throw new ArgumentNullException("args");
            var sb = new StringBuilder();
            var i = 0;
            while (i < format.Length)
            {
                if (format[i] == '{')
                {
                    i++;
                    if (i < format.Length)
                    {
                        if (format[i] == '{')
                            sb.Append(format[i++]);
                        else if (format[i] >= '0' && format[i] <= '9')
                        {
                            var n = 0;
                            do
                            {
                                n = n * 10 + (format[i++] - '0');
                                if (i >= format.Length)
                                    throw new FormatException();
                            }
                            while (format[i] >= '0' && format[i] <= '9');
                            if (format[i] == '}')
                            {
                                i++;
                                if (n >= args.Length)
                                {
                                    throw new FormatException();
                                }
                                if (args[n] != null)
                                {
                                    sb.Append(args[n].ToString());
                                }
                                else
                                {
                                    sb.Append("NULL");
                                }
                            }
                            else
                                throw new FormatException();
                        }
                        else
                            throw new FormatException();
                    }
                    else
                        throw new FormatException();
                }
                else
                    sb.Append(format[i++]);
            }
            return sb.ToString();
        }
    
        public static string Format(string format, object arg0)
        {
            return Format(format, new object[] { arg0 });
        }

        public static string Format(string format, object arg0, object arg1)
        {
            return Format(format, new object[] { arg0, arg1 });
        }

        public static string Format(string format, object arg0, object arg1, object arg2)
        {
            return Format(format, new object[] { arg0, arg1, arg2 });
        }
    
        public IEnumerator GetEnumerator()
        {
            return new CharEnumerator(this);
        }

        [Import(@"function(inst) {
                      var res = 0x15051505;
                      for (var i = 0; i < inst.length; i++)
                          res = ((res << 5) | (res >>> 27)) ^ inst.charCodeAt(i);
                      return res;
                  }", PassInstanceAsArgument = true)]
        extern public override int GetHashCode();

        public TypeCode GetTypeCode()
        {
            return TypeCode.String;
        }
    
        public int IndexOf(char value)
        {
            return PrimIndexOf(value, 0);
        }

        public int IndexOf(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            return PrimIndexOf(value, 0);
        }

        public int IndexOf(char value, int startIndex)
        {
            return PrimIndexOf(value, startIndex);
        }

        public int IndexOf(string value, int startIndex)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            return PrimIndexOf(value, startIndex);
        }
    
        public int IndexOf(string value, StringComparison comparisonType)
        {
            return IndexOf(value, 0, comparisonType);
        }

        public int IndexOf(string value, int startIndex, StringComparison comparisonType)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (comparisonType == StringComparison.OrdinalIgnoreCase)
                return ToLower().PrimIndexOf(value.ToLower(), 0);
            else
                return PrimIndexOf(value, startIndex);
        }
    
        [Import("function(inst, value, startIndex) { return inst.indexOf(String.fromCharCode(value), startIndex); }", PassInstanceAsArgument = true)]
        private extern int PrimIndexOf(char value, int startIndex);

        [Import("function(inst, value, startIndex) { return inst.indexOf(value, startIndex); }", PassInstanceAsArgument = true)]
        private extern int PrimIndexOf(string value, int startIndex);

        public int IndexOfAny(char[] anyOf)
        {
            return IndexOfAny(anyOf, 0);
        }

        public int IndexOfAny(char[] anyOf, int startIndex)
        {
            if (anyOf == null)
                return -1;
            for (var i = 0; i < anyOf.Length; i++)
            {
                var j = IndexOf(anyOf[i], startIndex);
                if (j >= 0)
                    return j;
            }
            return -1;
        }
    
        public static string Intern(string str)
        {
            return str;
        }

        public static string IsInterned(string str)
        {
            if (str == null)
                throw new ArgumentNullException("str");
            return str;
        }
    
        public bool IsNormalized()
        {
            return true;
        }

        public static bool IsNullOrEmpty(string value)
        {
            return value == null || value.Length == 0;
        }

        [Import(@"function(root, seperator, value) {
                      if (seperator == null)
                          separator = """";
                      if (value == null)
                          throw root.NullReferenceException();
                      return value.join(seperator);
                  }", PassRootAsArgument = true)]
        extern public static string Join(string separator, string[] value);
    
        public static string Join(string separator, string[] value, int startIndex, int count)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException("startIndex");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            if (startIndex + count > value.Length)
                throw new ArgumentOutOfRangeException();
            if (count == 0)
                return Empty;
            if (startIndex == 0 && count == value.Length)
                return Join(separator, value);
            var subValue = new string[count];
            for (var i = 0; i < count; i++)
                subValue[i] = value[startIndex + i];
            return Join(separator, subValue);
        }

        public int LastIndexOf(char value)
        {
            return LastIndexOf(value, Length - 1);
        }

        public int LastIndexOf(string value)
        {
            return LastIndexOf(value, Length - 1);
        }
    
        public int LastIndexOf(char value, int startIndex)
        {
            if (startIndex < 0 || startIndex > Length)
                throw new ArgumentOutOfRangeException("startIndex");
            if (startIndex == Length)
                startIndex--;
            return PrimLastIndexOf(value, startIndex);
        }

        public int LastIndexOf(string value, int startIndex)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (Length == 0 && (startIndex == -1 || startIndex == 0))
                return value.Length == 0 ? 0 : -1;
            if (startIndex < 0 || startIndex > Length)
                throw new ArgumentOutOfRangeException("startIndex");
            if (startIndex == Length)
                startIndex--;
            if (value.Length == 0)
                return startIndex;
            return PrimLastIndexOf(value, startIndex);
        }

        public int LastIndexOf(string value, StringComparison comparisonType)
        {
            return LastIndexOf(value, Length - 1, comparisonType);
        }
    
        public int LastIndexOf(string value, int startIndex, StringComparison comparisonType)
        {
            if (value != null && comparisonType == StringComparison.OrdinalIgnoreCase)
                return ToLower().LastIndexOf(value.ToLower(), startIndex);
            else
                return LastIndexOf(value, startIndex);
        }
    
        public int LastIndexOfAny(char[] anyOf)
        {
            return LastIndexOfAny(anyOf, Length - 1);
        }

        public int LastIndexOfAny(char[] anyOf, int startIndex)
        {
            if (anyOf == null)
                throw new ArgumentNullException("anyOf");
            if (startIndex < 0 || startIndex >= Length)
                throw new ArgumentOutOfRangeException("startIndex");
            for (var i = 0; i < anyOf.Length; i++)
            {
                var j = LastIndexOf(anyOf[i], startIndex);
                if (j >= 0)
                    return j;
            }
            return -1;
        }

        [Import(@"function (inst, value, startIndex) {
                      return inst.lastIndexOf(String.fromCharCode(value), startIndex);
                  }", PassInstanceAsArgument = true)]
        private extern int PrimLastIndexOf(char value, int startIndex);

        [Import("lastIndexOf")]
        private extern int PrimLastIndexOf(string value, int startIndex);

        public string Normalize()
        {
            return this;
        }

        public string Remove(int startIndex)
        {
            if (startIndex < 0 || startIndex >= this.Length)
                throw new ArgumentOutOfRangeException("startIndex");
            return Substring(0, startIndex);
        }

        [Import(@"function(inst, oldChar, newChar) {
                      var oldValue = String.fromCharCode(oldChar);
                      var newValue = String.fromCharCode(newChar);
                      var res = inst;
                      while (res.indexOf(oldValue) != -1)
                          res = res.replace(oldValue, newValue);
                      return res;
                  }", PassInstanceAsArgument = true)]
        extern public string Replace(char oldChar, char newChar);

        [Import(@"function(inst, oldValue, newValue) {
                      var res = inst;
                      while (res.indexOf(oldValue) != -1)
                          res = res.replace(oldValue, newValue);
                      return res;
                  }", PassInstanceAsArgument = true)]
        extern public string Replace(string oldValue, string newValue);

        public string[] Split(params char[] separator)
        {
            return Split(separator, int.MaxValue, StringSplitOptions.None);
        }

        public string[] Split(char[] separator, int count)
        {
            return Split(separator, count, StringSplitOptions.None);
        }

        public string[] Split(char[] separator, StringSplitOptions options)
        {
            return Split(separator, int.MaxValue, options);
        }

        public string[] Split(char[] separator, int count, StringSplitOptions options)
        {
            if (separator == null)
                throw new ArgumentNullException("separator");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            if (count == 0 || options == StringSplitOptions.RemoveEmptyEntries && Length == 0)
                return new string[0];
            var tokens = new List<string>();
            var begin = 0;
            for (var i = 0; i < Length && tokens.Count < count; i++)
            {
                var c = this[i];
                for (var j = 0; j < separator.Length; j++)
                {
                    if (separator[j] == c)
                    {
                        if (i - begin > 0 || options != StringSplitOptions.RemoveEmptyEntries)
                            tokens.Add(Substring(begin, i - begin));
                        begin = i + 1;
                    }
                }
            }
            if (Length - begin > 0 || options != StringSplitOptions.RemoveEmptyEntries)
            {
                if (Length > 0)
                    tokens.Add(Substring(begin));
            }
            return tokens.ToArray();
        }

        public bool StartsWith(string value)
        {
            return StartsWith(value, StringComparison.Ordinal);
        }

        public bool StartsWith(string value, StringComparison comparisonType)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (value.Equals(""))
                return true;
            if (value.Length > Length)
                return false;
            return Equals(Substring(0, value.Length), value, comparisonType);
        }

        public string Substring(int startIndex)
        {
            if (startIndex < 0 || startIndex >= Length)
                throw new ArgumentOutOfRangeException("startIndex");
            return PrimSubstring(startIndex);
        }

        public string Substring(int startIndex, int length)
        {
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException("startIndex");
            if (length < 0)
                throw new ArgumentOutOfRangeException("startIndex");
            if (startIndex + length > Length)
                throw new ArgumentOutOfRangeException();
            return PrimSubstring(startIndex, length);
        }

        [Import("substr")]
        extern public string PrimSubstring(int startIndex);

        [Import("substr")]
        extern public string PrimSubstring(int startIndex, int length);

        IEnumerator<char> IEnumerable<char>.GetEnumerator()
        {
            return new CharEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new CharEnumerator(this);
        }

        public char[] ToCharArray()
        {
            return ToCharArray(0, Length);
        }

        public char[] ToCharArray(int startIndex, int length)
        {
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException("startIndex");
            if (length < 0)
                throw new ArgumentOutOfRangeException("length");
            if (startIndex + length > Length)
                throw new ArgumentOutOfRangeException();
            var chars = new char[length];
            for (var i = 0; i < length; i++)
                chars[i] = this[startIndex + i];
            return chars;
        }
    
        [Import("toLowerCase")]
        extern public string ToLower();

        public string ToLowerInvariant()
        {
            return ToLower();
        }

        public override string ToString()
        {
            return this;
        }

        [Import("toUpperCase")]
        extern public string ToUpper();

        public string ToUpperInvariant()
        {
            return ToUpper();
        }

        [Import(@"function(inst) {
                      var m = inst.match(/\s*(\S+(\s*\S+)*)\s*/);
                      return m == null ? """" : m[1];
                  }", PassInstanceAsArgument = true)]
        extern public string Trim();

        public string Trim(char[] trimChars)
        {
            if (trimChars == null || trimChars.Length == 0)
                trimChars = WhitespaceChars;
            return TrimHelper(trimChars, TrimBoth);
        }

        public string TrimEnd(char[] trimChars)
        {
            if (trimChars == null || trimChars.Length == 0)
                trimChars = WhitespaceChars;
            return TrimHelper(trimChars, TrimTail);
        }

        private string TrimHelper(char[] trimChars, int trimType)
        {
            var endIndex = this.Length - 1;
            var startIndex = 0;
            if (trimType != TrimTail)
            {
                while (startIndex < Length)
                {
                    var i = 0;
                    var c = this[startIndex];
                    while (i < trimChars.Length)
                    {
                        if (trimChars[i] == c)
                            break;
                        i++;
                    }
                    if (i == trimChars.Length)
                        break;
                    startIndex++;
                }
            }
            if (trimType != TrimHead)
            {
                while (startIndex <= endIndex)
                {
                    var c = this[endIndex];
                    var i = 0;
                    while (i < trimChars.Length)
                    {
                        if (trimChars[i] == c)
                            break;
                        i++;
                    }
                    if (i == trimChars.Length)
                        break;
                    endIndex--;
                }
            }
            var length = endIndex - startIndex + 1;
            if (length == Length)
                return this;
            if (length == 0)
                return Empty;
            return Substring(startIndex, length);
        }

        public string TrimStart(char[] trimChars)
        {
            if (trimChars == null || trimChars.Length == 0)
                trimChars = WhitespaceChars;
            return TrimHelper(trimChars, TrimHead);
        }

        public static bool operator ==(string a, string b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(string a, string b)
        {
            return !a.Equals(b);
        }

        [Runtime.CompilerServices.IndexerNameAttribute("Chars")]
        extern public char this[int index]
        {
            [Import("charCodeAt")]
            get;
        }

        [Import]
        extern public int Length
        {
            get;
        }
    }
}
