//
// Instruct IL2JS compiler to supress type-based import/export on arguments/result
//

using System;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct |
                    AttributeTargets.Constructor | AttributeTargets.Property | AttributeTargets.Event | 
                    AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
    public class NoInteropAttribute : Attribute
    {
        public bool IsNoInterop { get; protected set; }

        public NoInteropAttribute(bool isNoInterop)
        {
            IsNoInterop = isNoInterop;
        }
    }
}
