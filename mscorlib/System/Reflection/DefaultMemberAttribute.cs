namespace System.Reflection
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Class)]
    public sealed class DefaultMemberAttribute : Attribute
    {
        public string MemberName { get; private set; }

        public DefaultMemberAttribute(string memberName)
        {
            MemberName = memberName;
        }

    }
}