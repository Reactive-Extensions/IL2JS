//
// Heavily cut down class for methods.
// (Unlike the BCL this is a concrete type).
//

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System.Reflection
{
    public class MethodInfo : MethodBase
    {
        private readonly bool isVirtual;
        private readonly Type[] paramTypes;       // null => pattern & unknown
        private readonly bool returnTypeIsKnown;
        private readonly Type returnTypeOrNull;   // null => pattern & unknown | known to be void

        // NOTE: paramTypes does not include any implicit 'this' first parameter
        [Export("function(root, f) { root.ReflectionMethodInfo = f; }", PassRootAsArgument = true)]
        public MethodInfo(string slot, Type declType, bool includeStatic, bool includeInstance, string simpleName, object[] customAttributes, bool isVirtual, Type[] paramTypes, bool returnTypeIsKnown, Type returnTypeOrNull)
            : base(slot, declType, includeStatic, includeInstance, simpleName, customAttributes)
        {
            this.isVirtual = isVirtual;
            this.paramTypes = paramTypes;
            this.returnTypeIsKnown = returnTypeIsKnown;
            this.returnTypeOrNull = returnTypeOrNull;
        }

        public override MemberTypes MemberType { get { return MemberTypes.Method; } }

        public Type ReturnType
        {
            get
            {
                if (!returnTypeIsKnown)
                    throw new NotImplementedException();
                else if (returnTypeOrNull == null)
                    return typeof(void);
                else
                    return returnTypeOrNull;
            }
        }

        [return: NoInterop(true)]
        [Import(@"function(declType, isStatic, isVirtual, slot, paramTypes, returnTypeOrNull, obj, paramValues) {
                      var args = [];
                      for (var i = 0; i < paramTypes.length; i++)
                          args.push(paramTypes[i].A(paramValues[i]));
                      var res;
                      if (isStatic)
                          res = declType[slot].apply(null, args);
                      else if (isVirtual)
                          res = obj[""V"" + slot].apply(obj, args);
                      else
                          res = obj[slot].apply(obj, args);
                      return returnTypeOrNull == null ? null : returnTypeOrNull.B(res);                            
                  }")]
        extern private static object PrimInvoke(Type declType, bool isStatic, bool isVirtual, string slot, Type[] paramTypes, Type returnTypeOrNull, [NoInterop(true)]object obj, [NoInterop(true)]object[] paramValues);

        public override object Invoke(object obj, object[] paramValues)
        {
            if (slot == null || paramTypes == null)
                throw new InvalidOperationException();
            if (includeInstance)
            {
                if (obj == null)
                    throw new NullReferenceException();
                if (!declType.IsAssignableFrom(obj.GetType()))
                    throw new TargetException();
            }
            if (paramTypes.Length != (paramValues == null ? 0 : paramValues.Length))
                throw new InvalidOperationException();
            return PrimInvoke(declType, includeStatic, isVirtual, slot, paramTypes, returnTypeOrNull, obj, paramValues);
        }

        public PropertyInfo DefiningProperty { get; set; }
        public EventInfo DefiningEvent { get; set; }

        public MethodInfo GetBaseDefinition()
        {
            throw new NotImplementedException();
        }

        public override ParameterInfo[] GetParameters()
        {
            throw new NotImplementedException();
        }

        internal override bool MatchedBy(MemberInfo concrete)
        {
            var mi = concrete as MethodInfo;
            if (mi == null)
                return false;
            // NOTE: isVirtual is NOT part of pattern
            if (mi.paramTypes == null)
                throw new InvalidOperationException();
            if (!base.MatchedBy(mi))
                return false;
            if (paramTypes != null)
            {
                if (paramTypes.Length != mi.paramTypes.Length)
                    return false;
                for (var i = 0; i < paramTypes.Length; i++)
                {
                    if (!paramTypes[i].Equals(mi.paramTypes[i]))
                        return false;
                }
            }
            if (returnTypeIsKnown)
            {
                if ((returnTypeOrNull == null) != (mi.returnTypeOrNull == null))
                    return false;
                if (returnTypeOrNull != null)
                {
                    if (!returnTypeOrNull.Equals(mi.returnTypeOrNull))
                        return false;
                }
            }
            return true;
        }

        internal override MemberInfo Rehost(Type type)
        {
            return new MethodInfo(slot, type, includeStatic, includeInstance, simpleName, customAttributes, isVirtual, paramTypes, returnTypeIsKnown, returnTypeOrNull);
        }
    }
}