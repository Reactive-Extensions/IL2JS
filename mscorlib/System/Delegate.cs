//
// A re-implementation of Delegate/MulticastDelegate for the JavaScript runtime.
// Most of the actual code lives in IL2JS/runtime.js.
// Underlying representation is a JavaScript function with additional fields.
//

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    [Runtime(true)]
    public abstract class Delegate : ICloneable
    {
        // SPECIAL CASE: Compiler implements constructor
        extern protected Delegate(object target, string method);

        // SPECIAL CASE: Compiler implements constructor
        extern protected Delegate(Type target, string method);

        public virtual object Clone()
        {
            return this;
        }

        [Import("function (root, delegates) { return root.CombineAllDelegates(delegates); }", PassRootAsArgument = true)]
        extern public static Delegate Combine(Delegate[] delegates);

        [Import("function (root, a, b) { return root.CombineDelegates(a, b); }", PassRootAsArgument = true)]
        extern public static Delegate Combine(Delegate a, Delegate b);

        public object DynamicInvoke(object[] args)
        {
            return DynamicInvokeImpl(args);
        }

        [Import("function (root, inst, args) { return root.DynamicInvokeDelegate(inst, args); }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern protected virtual object DynamicInvokeImpl(object[] args);

        [Import("function (root, inst, obj) { return root.EqualDelegates(inst, obj); }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern public override bool Equals(object obj);

        [Import("function (root, inst) { return root.HashDelegate(inst); }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern public override int GetHashCode();

        [Import("function (root, inst) { return root.GetDelegateInvocationList(inst); }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern public virtual Delegate[] GetInvocationList();

        [Import("function (root, inst) { return root.GetDelegateTarget(inst); }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern internal virtual object GetTarget();

        public static bool operator ==(Delegate d1, Delegate d2)
        {
            if (d1 == null)
                return d2 == null;
            return d1.Equals(d2);
        }

        public static bool operator !=(Delegate d1, Delegate d2)
        {
            if (d1 == null)
                return d2 != null;
            return !d1.Equals(d2);
        }

        [Import("function (root, source, value) { return root.RemoveAllDelegates(source, value); }", PassRootAsArgument = true)]
        extern public static Delegate Remove(Delegate source, Delegate value);

        object ICloneable.Clone()
        {
            return Clone();
        }

        public object Target { get { return GetTarget(); } }
    }
}