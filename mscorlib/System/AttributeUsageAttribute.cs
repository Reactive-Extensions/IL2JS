namespace System
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class AttributeUsageAttribute : Attribute
    {
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
        public AttributeTargets ValidOn { get; set;  }

        public AttributeUsageAttribute(AttributeTargets validOn)
        {
            AllowMultiple = false;
            Inherited = true;
            ValidOn = validOn;
        }
    }
}
