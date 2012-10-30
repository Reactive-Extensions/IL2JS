using System.Reflection;

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class OutAttribute : Attribute
    {
        internal static Attribute GetCustomAttribute(ParameterInfo parameter)
        {
            if (!parameter.IsOut)
                return null;
            return new OutAttribute();
        }

        internal static bool IsDefined(ParameterInfo parameter)
        {
            return parameter.IsOut;
        }
    }
}