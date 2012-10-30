namespace System.Reflection
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class AssemblyVersionAttribute : Attribute
    {
        public string Version { get; set; }

        public AssemblyVersionAttribute(string version)
        {
            Version = version;
        }
    }
}