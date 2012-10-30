//
// Instruct IL2JS compiler to allow arbitrary imported/exported methods on type, even if it is 'ManagedOnly',
// and suppress type-based import/export for instance of that type when used as the implicit 'this' parameter.
//

using System;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public class RuntimeAttribute : Attribute
    {
        public bool IsRuntime { get; protected set; }

        public RuntimeAttribute(bool isRuntime)
        {
            IsRuntime = isRuntime;
        }
    }
}
