//
// A reimplementation of Enum for the JavaScript runtime.
// Underlying representation is a pointer to a value representing enum.
//

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;
using System.Reflection;

namespace System
{
    [Runtime(true)]
    public abstract class Enum : IComparable
    {
        // Effectively implemented by compiler
        extern protected Enum();

        [Import(@"function(root, inst, target) {
                      if (target == null)
                          return 1;
                      if (inst.T !== target.T)
                          throw root.ArgumentException();
                      var l = inst.R();
                      var r = target.R();
                      if (l < r) return -1;
                      if (l > r) return 1;
                      return 0;
                  }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern public virtual int CompareTo(object target);

        [Import(@"function(root, inst, obj) {
                      if (obj == null)
                          throw root.NullReferenceException();
                      if (inst.T !== obj.T)
                          return false;
                      return inst.R() == obj.R(); 
                  }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        public extern override bool Equals(object obj);

        [Import(@"function(inst) { return inst.R(); }", PassInstanceAsArgument = true)]
        extern public override int GetHashCode();

        [Import(@"function(root, enumType, underlyingValue) {
                      var n = parseInt(underlyingValue);
                      if (isNaN(n))
                          throw root.InvalidOperationException();
                      return root.P(n, enumType);
                  }", PassRootAsArgument = true)]
        extern private static object PrimNewEnum(Type enumType, string underlyingValue);

        private static string[] PrimEnumFields(Type type)
        {
            var infos = type.FindAll(new FieldInfo(null, type, true, false, null, null, null, null));
            var res = new string[infos.Length*2];
            for (var i = 0; i < infos.Length; i++)
            {
                res[i*2] = infos[i].Name;
                res[i * 2 + 1] = ((Enum)infos[i].GetValue(null)).PrimValue();
            }
            return res;
        }

        public static object Parse(Type enumType, string value, bool ignoreCase)
        {
            if (ignoreCase)
                value = value.ToLower();
            var arr = PrimEnumFields(enumType);
            for (var i = 0; i < arr.Length; i += 2)
            {
                var nm = arr[i];
                if (value.Equals(nm, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                    return PrimNewEnum(enumType, arr[i + 1]);
            }
            throw new FormatException();
        }

        [Import("function(inst) { return inst.R().toString(); }", PassInstanceAsArgument = true)]
        extern private string PrimValue();

        public override string ToString()
        {
            var arr = PrimEnumFields(GetType());
            var val = PrimValue();
            for (var i = 0; i < arr.Length; i += 2)
            {
                if (arr[i + 1].Equals(val, StringComparison.Ordinal))
                    return arr[i];
            }

            if (val != null)
            {
                return "ENUM:" + val;
            }
            throw new InvalidOperationException();
        }
    }
}