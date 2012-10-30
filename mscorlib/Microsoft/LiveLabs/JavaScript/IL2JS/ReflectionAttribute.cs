//
// Instruct IL2JS compiler to include a cirtain level of reflection data in type.
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using System;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Struct)]
    public class ReflectionAttribute : Attribute
    {
        public ReflectionLevel Level { get; protected set; }

        public ReflectionAttribute(ReflectionLevel level)
        {
            Level = level;
        }
    }
}