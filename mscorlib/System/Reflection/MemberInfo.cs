//
// Heavily cut down abstract base class for reflection members
//

namespace System.Reflection
{
    public abstract class MemberInfo
    {
        protected MemberInfo()
        {
        }

        public abstract Type DeclaringType { get; }

        public abstract string Name { get; }

        public virtual object[] GetCustomAttributes(bool inherit)
        {
            if (inherit)
                throw new NotSupportedException();
            return PrimGetCustomAttributes();
        }

        public virtual object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (inherit)
                throw new NotSupportedException();
            var attrs = PrimGetCustomAttributes();
            var n = 0;
            for (var i = 0; i < attrs.Length; i++)
            {
                if (attributeType.IsAssignableFrom(attrs[i].GetType()))
                    n++;
            }
            if (n == attrs.Length)
                return attrs;
            var res = new object[n];
            n = 0;
            for (var i = 0; i < attrs.Length; i++)
            {
                if (attributeType.IsAssignableFrom(attrs[i].GetType()))
                    res[n++] = attrs[i];
            }
            return res;
        }

        protected abstract object[] PrimGetCustomAttributes();

        public abstract MemberTypes MemberType { get; }

        internal abstract bool MatchedBy(MemberInfo concrete);

        internal abstract MemberInfo Rehost(Type type);
    }
}