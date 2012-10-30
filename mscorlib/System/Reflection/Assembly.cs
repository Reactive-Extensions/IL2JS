//
// A reimplementation of Assembly for the JavaScript runtime.
// Underlying representation is a proxy for the actual runtime <assembly> structure.
//

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System.Reflection
{
    [UsedType(true)]
    [Interop(State = InstanceState.JavaScriptOnly)]
    public class Assembly
    {
        // Instances constructed by runtime only via import
        public Assembly(JSContext ctxt)
        {
        }

        public override bool Equals(object obj)
        {
            var o = obj as Assembly;
            return o != null && Equals(o);
        }

        [Import("function(inst) { return inst.Id; }", PassInstanceAsArgument = true)]
        public extern override int GetHashCode();

        [Import("function(inst, o) { return inst === o; }", PassInstanceAsArgument = true)]
        public extern bool Equals(Assembly o);

        [Import("function(type) { return type.Z; }")]
        extern public static Assembly GetAssembly(Type type);

        [Import("function(inst) { return inst.N; }", PassInstanceAsArgument = true)]
        extern internal string GetFullName();

        [Import("function(root, inst, name) { return root.TryResolveType(inst, name, 3); }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern public virtual Type GetType(string name);

        public virtual Type GetType(string name, bool throwOnError)
        {
            return GetType(name);
        }

        public Type GetType(string name, bool throwOnError, bool ignoreCase)
        {
            if (ignoreCase)
                throw new NotSupportedException();
            return GetType(name);
        }

        [Import("function(root, assemblyString) { return root.LoadAssembly(null, assemblyString); }", PassRootAsArgument = true)]
        extern public static Assembly Load(string assemblyString);

        public override string ToString()
        {
            return FullName;
        }

        public virtual string FullName { get { return GetFullName(); } }

        public object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }

        public object[] GetCustomAttributes(bool inherit)
        {
            throw new NotImplementedException();
        }

        public bool IsDefined(Type attributeType, bool inherit)
        {
            throw new NotImplementedException();
        }
    }
}