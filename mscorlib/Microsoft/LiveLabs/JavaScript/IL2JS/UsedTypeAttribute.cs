//
// Instruct IL2JS compiler to include a type definition, even if it does not appear to be used,
// but without forcing that type's members to be used.
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using System;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Struct)]
    public class UsedTypeAttribute : Attribute
    {
        public bool IsUsed { get; protected set; }

        public UsedTypeAttribute(bool isUsed)
        {
            IsUsed = isUsed;
        }
    }
}