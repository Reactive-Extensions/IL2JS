namespace System.Reflection
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblyCultureAttribute : Attribute
    {
        public string Culture { get; set; }

        public AssemblyCultureAttribute(string culture)
        {
            Culture = culture;
        }
    }
}