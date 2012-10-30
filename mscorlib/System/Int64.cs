//
// A remimplementation of Int64 for the JavaScript runtime.
//
// TODO: JavaScript represents 9223372036854775807 as 9223372036854776000 and -9223372036854775808
//       as -9223372036854776000, so this is not a true Int64.
//

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    [Runtime(true)]
    public struct Int64 : IComparable, IComparable<long>, IEquatable<long>
    {
        public const long MaxValue = 9223372036854775807; // NOTE: A lie, see above.
        public const long MinValue = -9223372036854775808; // NOTE: A lie, see above.

        [Import(@"function(inst, value) {
                      var l = inst.R();
                      if (l < value) return -1;
                      if (l > value) return 1;
                      return 0;
                  }", PassInstanceAsArgument = true)]
        extern public int CompareTo(long value);

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
        extern public bool Equals(long obj);

        [Import(@"function(root, inst, obj) {
                      if (obj == null)
                          throw root.NullReferenceException();
                      if (inst.T != obj.T)
                          return false;
                      return inst.R() == obj.R(); 
                  }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern public override bool Equals(object obj);

        [Import(@"function(inst) { return inst.R() % 2147483647; }", PassInstanceAsArgument = true)]
        extern public override int GetHashCode();

        [Import(@"function(inst) { return inst.R().toString(); }", PassInstanceAsArgument = true)]
        extern public override string ToString();

        public string ToString(string format)
        {
            return ToString();
        }

        public static long Parse(string s)
        {
            if (s == null)
                throw new NullReferenceException();
            var r = PrimParse(s);
            if (!r.HasValue)
                throw new FormatException();
            return r.Value;
        }

        public static bool TryParse(string s, out long result)
        {
            var r = PrimParse(s);
            if (r.HasValue)
            {
                result = r.Value;
                return true;
            }
            else
            {
                result = 0;
                return false;
            }
        }

        [Import(@"function (s) {
                      var i = parseInt(s);
                      if (isNaN(i) || i < -9223372036854775808 || i > 9223372036854775807)
                          return null;
                      return i;
                  }")]
        extern private static long? PrimParse(string s);

        public TypeCode GetTypeCode()
        {
            return TypeCode.Int64;
        }
    }
}