//
// A reimplementation of Int16 for the JavaScript runtime
// Underlying representation is a JavaScript number.
//

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    [Runtime(true)]
    public struct Int16 : IComparable, IComparable<short>, IEquatable<short>
    {
        public const short MaxValue = 32767;
        public const short MinValue = -32768;

        [Import(@"function(inst, value) {
                      var l = inst.R();
                      if (l < value) return -1;
                      if (l > value) return 1;
                      return 0;
                  }", PassInstanceAsArgument = true)]
        extern public int CompareTo(short value);

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
        extern public bool Equals(short obj);

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

        [Import(@"function(inst) { return inst.R().toString(); }", PassInstanceAsArgument = true)]
        extern public override string ToString();

        public string ToString(string format)
        {
            return ToString();
        }

        public static short Parse(string s)
        {
            if (s == null)
                throw new NullReferenceException();
            var r = PrimParse(s);
            if (!r.HasValue)
                throw new FormatException();
            return r.Value;
        }

        public static bool TryParse(string s, out short result)
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
                      if (isNaN(i) || i < -32768 || i > 32767)
                          return null;
                      return i;
                  }")]
        extern private static short? PrimParse(string s);

        public TypeCode GetTypeCode()
        {
            return TypeCode.Int16;
        }
    }
}