//
// Heavily cut down class for properties
// (Unlike the BCL this is a concrete type).
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace System.Reflection
{
    public class PropertyInfo : NonTypeMemberBase
    {
        private readonly Type propertyType;   // null => unknown
        private readonly MethodInfo getter;   // null => pattern only or no getter
        private readonly MethodInfo setter;   // null => pattern only or no setter

        [Export("function(root, f) { root.ReflectionPropertyInfo = f; }", PassRootAsArgument = true)]
        public PropertyInfo(string slot, Type declType, bool includeStatic, bool includeInstance, string simpleName, object[] customAttributes, Type propertyType, MethodInfo getter, MethodInfo setter)
            : base(slot, declType, includeStatic, includeInstance, simpleName, customAttributes)
        {
            if (getter != null)
                getter.DefiningProperty = this;
            if (setter != null)
                setter.DefiningProperty = this;
            this.propertyType = propertyType;
            this.getter = getter;
            this.setter = setter;
        }

        public override MemberTypes MemberType { get { return MemberTypes.Property; } }

        public MethodInfo[] GetAccessors()
        {
            return new MethodInfo[] { getter, setter };
        }

        public MethodInfo GetGetMethod()
        {
            return getter;
        }

        public MethodInfo GetSetMethod()
        {
            return setter;
        }

        public object GetValue(object instance, object[] index)
        {
            if (CanRead)
                return getter.Invoke(instance, null);
            else
                throw new NotSupportedException();
        }

        public void SetValue(object instance, object value, object[] index)
        {
            if (CanWrite)
            {
                var args = new object[(index == null ? 0 : index.Length) + 1];
                if (index != null)
                {
                    for (var i = 0; i < index.Length; i++)
                        args[i] = index[i];
                }
                args[args.Length - 1] = value;
                setter.Invoke(instance, args);
            }
            else
                throw new NotSupportedException();
        }

        public bool CanRead
        {
            get { return getter != null; }
        }

        public bool CanWrite
        {
            get { return setter != null; }
        }

        public Type PropertyType
        {
            get
            {
                if (propertyType == null)
                    throw new InvalidOperationException();
                return propertyType;
            }
        }

        internal override bool MatchedBy(MemberInfo concrete)
        {
            var pi = concrete as PropertyInfo;
            if (pi == null)
                return false;
            if (pi.propertyType == null)
                throw new InvalidOperationException();
            if (!base.MatchedBy(pi))
                return false;
            if (propertyType != null && !propertyType.Equals(pi.propertyType))
                return false;
            var misses = 0;
            if (getter != null)
            {
                if (pi.getter == null || !getter.MatchedBy(pi.getter))
                    misses++;
            }
            if (setter != null)
            {
                if (pi.setter == null || !setter.MatchedBy(pi.setter))
                    misses++;
            }
            if (misses > 1)
                return false;
            return true;
        }

        internal override MemberInfo Rehost(Type type)
        {
            return new PropertyInfo(slot, type, includeStatic, includeInstance, simpleName, customAttributes, propertyType, getter, setter);
        }
    }
}
