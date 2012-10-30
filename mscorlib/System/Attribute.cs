namespace System
{
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
    public abstract class Attribute
    {
        protected Attribute()
        {
        }
    }
}
