//
// A reimplementation of Int32 for the JavaScript runtime.
// Underlying representation is either JavaScript numbers 0 or 1, *OR* JavaScript booleans false or true.
//

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    [Runtime(true)]
    public struct Boolean : IComparable, IComparable<bool>, IEquatable<bool>
    {
        internal const string TrueLiteral = "True";
        internal const string FalseLiteral = "False";

        public static string TrueString;
        public static string FalseString;

        static Boolean()
        {
            TrueString = TrueLiteral;
            FalseString = FalseLiteral;
        }

        [Import("function(inst) { return inst.R() ? 1 : 0; }", PassInstanceAsArgument = true)]
        extern public override int GetHashCode();

        [Import(@"function(inst) { return inst.R() ? ""True"" : ""False""; }", PassInstanceAsArgument = true)]
        extern public override string ToString();

        [Import(@"function(root, inst, obj) {
                      if (obj == null)
                          throw root.NullReferenceException();
                      if (inst.T !== obj.T)
                          return false;
                      return inst.R() ? 0 : 1 == obj.R() ? 0 : 1; 
                  }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern public override bool Equals(object obj);

        [Import("function(inst, obj) { return inst.R() ? 0 : 1 == obj ? 0 : 1; }", PassInstanceAsArgument = true)]
        extern public bool Equals(bool obj);

        [Import(@"function(root, inst, value) {
                      if (value == null)
                          return 1;
                      if (inst.T !== value.T)
                          throw root.ArgumentException();
                      var l = inst.R();
                      var r = value.R();
                      if (!l && r) return -1;
                      if (l && !r) return 1;
                      return 0;
                  }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern public int CompareTo(object obj);

        [Import(@"function(inst, value) {
                      var l = inst.R();
                      if (!l && value) return -1;
                      if (l && !value) return 1;
                      return 0;
                  }", PassInstanceAsArgument = true)]
        extern public int CompareTo(bool value);

        public static bool Parse(string value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            var result = default(bool);
            if (!TryParse(value, out result))
                throw new FormatException();
            return result;
        }

        public static bool TryParse(string value, out bool result)
        {
            if (value != null)
            {
                if (String.Equals(TrueLiteral, value, StringComparison.OrdinalIgnoreCase))
                {
                    result = true;
                    return true;
                }
                if (String.Equals(FalseLiteral, value, StringComparison.OrdinalIgnoreCase))
                {
                    result = false;
                    return true;
                }
                value = value.Trim();
                if (String.Equals(TrueLiteral, value, StringComparison.OrdinalIgnoreCase))
                {
                    result = true;
                    return true;
                }
                if (String.Equals(FalseLiteral, value, StringComparison.OrdinalIgnoreCase))
                {
                    result = false;
                    return true;
                }
            }
            result = false;
            return false;
        }

        public TypeCode GetTypeCode()
        {
            return TypeCode.Boolean;
        }
    }
}
