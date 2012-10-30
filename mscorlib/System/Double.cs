//
// A reimplementation of Double for the JavaScript runtime.
// Underlying representation is a JavaScript number.
//

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    [Runtime(true)]
    public struct Double : IComparable, IComparable<double>, IEquatable<double>
    {
        public const double MinValue = -1.7976931348623157E+308;
        public const double MaxValue = 1.7976931348623157E+308;
        public const double Epsilon = 4.94065645841247E-324;
        public const double NegativeInfinity = -1.0 / 0.0;
        public const double PositiveInfinity = 1.0 / 0.0;
        public const double NaN = 1.0 / 0.0;

        [Import(@"function(inst, value) {
                      var l = inst.R();
                      if (l < value) return -1;
                      if (l > value) return 1;
                      return 0;
                  }", PassInstanceAsArgument = true)]
        extern public int CompareTo(double value);

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
        extern public bool Equals(double obj);

        [Import(@"function(root, inst, obj) {
                      if (obj == null)
                          throw root.NullReferenceException();
                      if (inst.T !== obj.T)
                          return false;
                      return inst.R() == obj.R(); 
                  }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern public override bool Equals(object obj);

        [Import(@"function(inst) { return Math.round(inst.R()); }", PassInstanceAsArgument = true)]
        extern public override int GetHashCode();

        public TypeCode GetTypeCode()
        {
            return TypeCode.Double;
        }

        [Import("function(f) { return !isNaN(f) && !isFinite(f); }")]
        extern public static bool IsInfinity(double f);

        [Import("function(f) { return isNaN(f); }")]
        extern public static bool IsNaN(double f);

        internal static bool IsNegative(double d)
        {
            return d < 0;
        }

        [Import("function(f) { return !isNaN(f) && !isFinite(f) && f < 0.0; }")]
        extern public static bool IsNegativeInfinity(double f);

        [Import("function(f) { return !isNaN(f) && !isFinite(f) && f > 0.0; }")]
        extern public static bool IsPositiveInfinity(double f);

        public static double Parse(string s)
        {
            if (s == null)
                throw new NullReferenceException();
            var r = PrimParse(s);
            if (!r.HasValue)
                throw new FormatException();
            return r.Value;
        }

        [Import(@"function (s) {
                      var f = parseFloat(s);
                      if (isNaN(f))
                          return null;
                      return f;
                  }")]
        extern private static double? PrimParse(string s);

        [Import(@"function(inst) { return inst.R().toString(); }", PassInstanceAsArgument = true)]
        extern public override string ToString();

        public string ToString(string format)
        {
            return ToString();
        }

        public static bool TryParse(string s, out double result)
        {
            var r = PrimParse(s);
            if (r.HasValue)
            {
                result = r.Value;
                return true;
            }
            else
            {
                result = 0.0;
                return false;
            }
        }
    }
}