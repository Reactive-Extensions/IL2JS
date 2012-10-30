//
// A reimplementation of Char for the JavaScript runtime.
// Underlying representation is a JavaScript number representing Unicode char code.
//

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    [Runtime(true)]
    public struct Char : IComparable, IComparable<char>, IEquatable<char>
    {
        public const char MaxValue = (char)0xFFFF;
        public const char MinValue = (char)0x0000;

        [Import(@"function(inst, value) {
                      var l = inst.R();
                      if (l < value) return -1;
                      if (l > value) return 1;
                      return 0;
                  }", PassInstanceAsArgument = true)]
        extern public int CompareTo(char value);

        [Import(@"function(root, inst, value) {
                      if (value == null)
                          return 1;
                      if (inst.T !== value.T)
                          throw root.ArgumentException();
                      var l = inst.R();
                      var r = value.R();
                      if (l < r) return -1;
                      if (l > r) return 1;
                      return 0;
                  }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern public int CompareTo(object value);

        [Import(@"function(inst, obj) { return inst.R() == obj; }", PassInstanceAsArgument = true)]
        extern public bool Equals(char obj);

        [Import(@"function(root, inst, obj) {
                      if (obj == null)
                          throw root.NullReferenceException();
                      if (inst.T !== obj.T)
                          return false;
                      return inst.R() == obj.R(); 
                  }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern public override bool Equals(object obj);

        [Import(@"function(inst) { return inst.R(); }", PassInstanceAsArgument = true)]
        extern public override int GetHashCode();

        [Import(@"function (c) {
                     var r = parseInt(String.fromCharCode(c));
                     return isNaN(r) ? -1.0 : r;
                  }")]
        extern public static double GetNumericValue(char c);
        
        public static double GetNumericValue(string s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (index < 0 || index >= s.Length)
                throw new ArgumentOutOfRangeException("index");
            return GetNumericValue(s[index]);
        }

        public TypeCode GetTypeCode()
        {
            return TypeCode.Char;
        }

        public static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        public static bool IsDigit(string s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (index < 0 || index >= s.Length)
                throw new ArgumentOutOfRangeException("index");
            return IsDigit(s[index]);
        }

        public static bool IsLetter(char c)
        {
            return c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z';
        }

        public static bool IsLetter(string s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (index < 0 || index >= s.Length)
                throw new ArgumentOutOfRangeException("index");
            return IsLetter(s[index]);
        }

        public static bool IsLetterOrDigit(char c)
        {
            return c >= '0' && c <= '9' || c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z';
        }

        public static bool IsLetterOrDigit(string s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (index < 0 || index >= s.Length)
                throw new ArgumentOutOfRangeException("index");
            return IsLetterOrDigit(s[index]);
        }

        public static bool IsLower(char c)
        {
            return c >= 'a' && c <= 'z';
        }

        public static bool IsLower(string s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (index < 0 || index >= s.Length)
                throw new ArgumentOutOfRangeException("index");
            return IsLower(s[index]);
        }

        public static bool IsNumber(char c)
        {
            return c >= '0' && c <= '9';
        }

        public static bool IsNumber(string s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (index < 0 || index >= s.Length)
                throw new ArgumentOutOfRangeException("index");
            return IsNumber(s[index]);
        }

        public static bool IsUpper(char c)
        {
            return c >= 'A' && c <= 'Z';
        }
        
        public static bool IsUpper(string s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (index < 0 || index >= s.Length)
                throw new ArgumentOutOfRangeException("index");
            return IsUpper(s[index]);
        }

        public static bool IsWhiteSpace(char c)
        {
            return c == ' ' || (c >= '\t' && c <= '\r') || c == '\x00a0' || c == '\x0085';
        }

        public static bool IsWhiteSpace(string s, int index)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (index < 0 || index >= s.Length)
                throw new ArgumentOutOfRangeException("index");
            return IsWhiteSpace(s[index]);
        }

        public static char Parse(string s)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            if (s.Length != 1)
                throw new FormatException();
            return s[0];
        }

        [Import("function (c) { return String.fromCharCode(c).toLower().charCodeAt(0); }")]
        extern public static char ToLower(char c);

        public static char ToLowerInvariant(char c)
        {
            return ToLower(c);
        }

        [Import(@"function(inst) { return String.fromCharCode(inst.R()); }", PassInstanceAsArgument = true)]
        extern public override string ToString();

        [Import("function (c) { return String.fromCharCode(c); }")]
        extern public static string ToString(char c);

        [Import("function (c) { return String.fromCharCode(c).toUpper().charCodeAt(0); }")]
        extern public static char ToUpper(char c);

        public static char ToUpperInvariant(char c)
        {
            return ToUpper(c);
        }

        public static bool TryParse(string s, out char result)
        {
            if (s == null || s.Length != 1)
            {
                result = '\0';
                return false;
            }
            result = s[0];
            return true;
        }
    }
}
