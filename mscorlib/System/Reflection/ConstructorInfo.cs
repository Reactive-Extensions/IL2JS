//
// Heavily cut down class for constructor methods.
// (Unlike the BCL this is a concrete type).
//

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System.Reflection
{
    public class ConstructorInfo : MethodBase
    {
        public static string ConstructorName;
        public static string TypeConstructorName;

        private readonly Type[] paramTypes;  // null => pattern & unknown

        static ConstructorInfo()
        {
            ConstructorName = ".ctor";
            TypeConstructorName = ".cctor";
        }

        // NOTE: paramTypes does not include implicit 'this' first parameter
        [Export("function(root, f) { root.ReflectionConstructorInfo = f; }", PassRootAsArgument = true)]
        public ConstructorInfo(string slot, Type declType, bool isInstance, object[] customAttributes, Type[] paramTypes)
            : base(slot, declType, !isInstance, isInstance, isInstance ? ConstructorName : TypeConstructorName, customAttributes)
        {
            this.paramTypes = paramTypes;
        }

        public override MemberTypes MemberType { get { return MemberTypes.Constructor; } }

        [return: NoInterop(true)]
        [Import(@"function(declType, slot, paramTypes, paramValues) {
                      var obj = new declType.I();
                      if (declType.W)
                          obj = root.P(obj, declType);
                      var args = [];
                      for (var i = 0; i < paramTypes.length; i++)
                          args.push(paramTypes.A(paramValues[i]));
                      obj[slot].apply(obj, args);
                      return obj;
                  }")]
        extern private static object PrimInvokeInstance(Type declType, string slot, Type[] paramTypes, [NoInterop(true)]object[] paramValues);

        [Import(@"function(declType, slot, paramTypes, paramValues) {
                      var args = [];
                      for (var i = 0; i < paramTypes.length; i++)
                          args.push(paramTypes.A(paramValues[i]));
                      declType[slot].apply(null, args);
                  }")]
        extern private static void PrimInvokeStatic(Type declType, string slot, Type[] paramTypes, [NoInterop(true)]object[] paramValues);

        public override object Invoke(object obj, object[] paramValues)
        {
            if (obj != null)
                throw new InvalidOperationException();
            return Invoke(paramValues);
        }

        public object Invoke(object[] paramValues)
        {
            if (slot == null || paramTypes == null)
                throw new InvalidOperationException();
            if (paramValues == null)
                throw new NullReferenceException();
            if (paramTypes.Length != paramValues.Length)
                throw new InvalidOperationException();
            if (includeInstance)
                return PrimInvokeInstance(declType, slot, paramTypes, paramValues);
            else
            {
                PrimInvokeStatic(declType, slot, paramTypes, paramValues);
                return null;
            }
        }

        public override ParameterInfo[] GetParameters()
        {
            throw new NotImplementedException();
        }

        internal override bool MatchedBy(MemberInfo concrete)
        {
            var ci = concrete as ConstructorInfo;
            if (ci == null)
                return false;
            if (ci.paramTypes == null)
                throw new InvalidOperationException();
            if (!base.MatchedBy(concrete))
                return false;
            if (paramTypes != null)
            {
                if (paramTypes.Length != ci.paramTypes.Length)
                    return false;
                for (var i = 0; i < paramTypes.Length; i++)
                {
                    if (!paramTypes[i].Equals(ci.paramTypes[i]))
                        return false;
                }
            }
            return true;
        }

        internal override MemberInfo Rehost(Type type)
        {
            return new ConstructorInfo(slot, type, includeInstance, customAttributes, paramTypes);
        }
    }
}