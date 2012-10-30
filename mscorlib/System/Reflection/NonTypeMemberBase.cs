namespace System.Reflection
{
    public class NonTypeMemberBase : MemberInfo
    {
        protected readonly string slot;               // null => pattern
        protected readonly Type declType;             // not null
        protected readonly bool includeStatic;
        protected readonly bool includeInstance;
        protected readonly string simpleName;         // null => pattern & unknown
        protected readonly object[] customAttributes; // null => pattern

        internal NonTypeMemberBase(string slot, Type declType, bool includeStatic, bool includeInstance, string simpleName, object[] customAttributes)
        {
            this.slot = slot;
            this.declType = declType;
            this.includeStatic = includeStatic;
            this.includeInstance = includeInstance;
            this.simpleName = simpleName;
            this.customAttributes = customAttributes;
        }

        public override Type DeclaringType { get { return declType; } }

        public override string Name
        {
            get
            {
                if (simpleName == null)
                    throw new InvalidOperationException();
                return simpleName;
            }
        }

        internal override bool MatchedBy(MemberInfo concrete)
        {
            var ntmb = concrete as NonTypeMemberBase;
            if (ntmb == null)
                return false;
            if (ntmb.simpleName == null)
                throw new InvalidOperationException();
            if (!((includeStatic && ntmb.includeStatic) || (includeInstance && ntmb.includeInstance)))
                return false;
            if (simpleName != null && !simpleName.Equals(ntmb.simpleName, StringComparison.Ordinal))
                return false;
            return true;
        }

        protected override object[] PrimGetCustomAttributes()
        {
            if (customAttributes == null)
                throw new InvalidOperationException();
            return customAttributes;
        }

        public override MemberTypes MemberType
        {
            get { throw new NotImplementedException(); }
        }

        internal override MemberInfo Rehost(Type type)
        {
            return new NonTypeMemberBase(slot, type, includeStatic, includeInstance, simpleName, customAttributes);
        }
    }
}