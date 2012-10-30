//
// Instruct IL2JS compiler to use given static method as entry point
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

using System;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    [AttributeUsage(AttributeTargets.Method)]
    
    public class EntryPointAttribute : Attribute { }
}