//
// Heavily cut down abstract base class for reflection methods.
//

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System.Reflection
{
    public abstract class MethodBase : NonTypeMemberBase
    {
        protected MethodBase(string slot, Type declType, bool includeStatic, bool includeInstance, string simpleName, object[] customAttributes)
            : base(slot, declType, includeStatic, includeInstance, simpleName, customAttributes)
        {
        }

        // The method handle *is* the method or constructor info object
        [Import("function(handle) { return handle; }")]
        extern public static MethodBase GetMethodFromHandle([NoInterop(true)]RuntimeMethodHandle handle);

        public abstract object Invoke(object obj, object[] paramValues);

        public abstract ParameterInfo[] GetParameters();
    }
}