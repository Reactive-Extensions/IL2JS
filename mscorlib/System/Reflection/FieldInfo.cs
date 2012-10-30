//
// Heavily cut down class for fields.
// (Unlike the BCL this is a concrete type).
//

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System.Reflection
{
    public class FieldInfo : NonTypeMemberBase
    {
        private readonly Type fieldType;        // null => pattern & unknown
        private readonly int[] initialization;  // null => pattern | no initialization

        [Export("function(root, f) { root.ReflectionFieldInfo = f; }", PassRootAsArgument = true)]
        public FieldInfo(string slot, Type declType, bool includeStatic, bool includeInstance, string simpleName, object[] customAttributes, Type fieldType, int[] initialization)
            : base(slot, declType, includeStatic, includeInstance, simpleName, customAttributes)
        {
            this.fieldType = fieldType;
            this.initialization = initialization;
        }

        public override MemberTypes MemberType { get { return MemberTypes.Field; } }

        public Type FieldType
        {
            get
            {
                if (fieldType == null)
                    throw new NotImplementedException();
                return fieldType;
            }
        }

        [return: NoInterop(true)]
        [Import(@"function(declType, isStatic, slot, fieldType, obj) {
                      return fieldType.B(isStatic ? declType[""S"" + slot] : obj[""F"" + slot]);
                  }")]
        extern private static object PrimGetValue(Type declType, bool isStatic, string slot, Type fieldType, [NoInterop(true)]object obj);

        [Import(@"function(declType, isStatic, slot, fieldType, obj, value) {
                      if (isStatic)
                          declType[""S"" + slot] = fieldType.A(value);
                      else
                          obj[""F"" + slot] = filedType.A(value);
                  }")] 
        extern private static void PrimSetValue(Type declType, bool isStatic, string slot, Type fieldType, [NoInterop(true)]object obj, [NoInterop(true)]object value);

        public object GetValue(object obj)
        {
            if (slot == null || fieldType == null)
                throw new InvalidOperationException();
            if (includeInstance)
            {
                if (obj == null)
                    throw new NullReferenceException();
                if (!declType.IsAssignableFrom(obj.GetType()))
                    throw new TargetException();
            }
            return PrimGetValue(declType, includeStatic, slot, fieldType, obj);
        }

        public void SetValue(object obj, object value)
        {
            if (slot == null || fieldType == null)
                throw new InvalidOperationException();
            if (includeInstance)
            {
                if (obj == null)
                    throw new NullReferenceException();
                if (!declType.IsAssignableFrom(obj.GetType()))
                    throw new TargetException();
            }
            PrimSetValue(declType, includeInstance, slot, fieldType, obj, value);
        }

        public int[] Initialization { get { return initialization; } }

        // The field handle *is* the FieldInfo object
        [Import("function(handle) { return handle; }")]
        extern public static FieldInfo GetFieldFromHandle([NoInterop(true)]RuntimeFieldHandle handle);

        internal override bool MatchedBy(MemberInfo concrete)
        {
            var fi = concrete as FieldInfo;
            if (fi == null)
                return false;
            if (fi.fieldType == null)
                throw new InvalidOperationException();
            if (!base.MatchedBy(fi))
                return false;
            if (fieldType != null)
            {
                if (!fieldType.Equals(fi.fieldType))
                    return false;
            }
            return true;
        }

        internal override MemberInfo Rehost(Type type)
        {
            return new FieldInfo(slot, type, includeStatic, includeInstance, simpleName, customAttributes, fieldType, initialization);
        }
    }
}
 