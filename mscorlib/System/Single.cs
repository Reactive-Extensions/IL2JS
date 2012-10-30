//
// A reimplementation of Single for the JavaScript runtime.
// Underlying representation is a JavaScript number.
//

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    [Runtime(true)]
    public struct Single : IComparable, IComparable<float>, IEquatable<float>
    {
        public const float MinValue = -3.40282346638528859e+38f;
        public const float Epsilon = 1.401298E-45f;
        public const float MaxValue = 3.40282346638528859e+38f;
        public const float PositiveInfinity = 1.0f / 0.0f;
        public const float NegativeInfinity = -1.0f / 0.0f;
        public const float NaN = 1.0f / 0.0f;

        [Import(@"function(inst, value) {
                      var l = inst.R();
                      if (l < value) return -1;
                      if (l > value) return 1;
                      return 0;
                  }", PassInstanceAsArgument = true)]
        extern public int CompareTo(float value);

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
        extern public bool Equals(float obj);

        [Import(@"function(root, inst, obj) {
                      if (obj == null)
                          throw root.NullReferenceException();
                      if (inst.T.Id != obj.T.Id)
                          return false;
                      return inst.R() == obj.R(); 
                  }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern public override bool Equals(object obj);

        [Import(@"function(inst) { return Math.round(inst.R()); }", PassInstanceAsArgument = true)]
        extern public override int GetHashCode();

        public TypeCode GetTypeCode()
        {
            return TypeCode.Single;
        }

        [Import("function(f) { return !isNaN(f) && !isFinite(f); }")]
        extern public static bool IsInfinity(float f);

        [Import("function(f) { return isNaN(f); }")]
        extern public static bool IsNaN(float f);

        [Import("function(f) { return !isNaN(f) && !isFinite(f) && f < 0.0; }")]
        extern public static bool IsNegativeInfinity(float f);

        [Import("function(f) { return !isNaN(f) && !isFinite(f) && f > 0.0; }")]
        extern public static bool IsPositiveInfinity(float f);

        public static float Parse(string s)
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
                      if (isNaN(i))
                          return null;
                      return f;
                  }")]
        extern private static float? PrimParse(string s);

        [Import(@"function(inst) { return inst.R().toString(); }", PassInstanceAsArgument = true)]
        extern public override string ToString();

        public string ToString(string format)
        {
            return ToString();
        }

        public static bool TryParse(string s, out float result)
        {
            var r = PrimParse(s);
            if (r.HasValue)
            {
                result = r.Value;
                return true;
            }
            else
            {
                result = 0.0f;
                return false;
            }
        }
    }
}