//
// Instruct IL2JS compiler to include a definition, even if it does not appear to be used
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using System;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property)]
    public class UsedAttribute : Attribute
    {
        public bool IsUsed { get; protected set; }


        public UsedAttribute(bool isUsed)
        {
            IsUsed = isUsed;
        }
    }
}