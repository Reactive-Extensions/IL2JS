//
// Instruct IL2JS compiler to ignore a definition
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using System;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Struct)]
    public class IgnoreAttribute : Attribute { }
}