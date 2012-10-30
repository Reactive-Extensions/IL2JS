//
// Type used to distinguish 'importing' constructors from 'normal' constructors, and hold 
// some interop control methods. Two representations:
//  - In IL2JS: Underlying representation is underlying JavaScript object.
//  - In CLR: Captures enough context to access the underlying JavaScript object via the runtime.
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
//

#if IL2JS
using Microsoft.LiveLabs.JavaScript.IL2JS;
#else
using System;
using Microsoft.LiveLabs.JavaScript.ManagedInterop;
#endif

namespace Microsoft.LiveLabs.JavaScript.Interop
{
#if IL2JS
    // The instance will be an unmanaged JavaScript object in the process of being imported or constructed.
    // We'll pass this instance directly through to the HasField and GetField methods below.
    [Runtime(true)]
#endif
    public class JSContext
    {
#if !IL2JS
        private ManagedInterop.Runtime runtime;
        private Type type;
        public string Key { get; private set; }
        public int Id { get; private set; }

        internal JSContext(ManagedInterop.Runtime runtime, Type type, string key, int id)
        {
            this.runtime = runtime;
            this.type = type;
            Key = key;
            Id = id;
        }
#endif


#if IL2JS
        // Make sure object, which could be using any state representation, is passed unchanged to Disconnect
        [Import("function(root, obj) { root.Disconnect(obj); }", PassRootAsArgument = true)]
        extern public static void Disconnect([NoInterop(true)]object obj);
#else
        public static void Disconnect(object obj)
        {
            InteropContextManager.Disconnect(obj);
        }
#endif

#if IL2JS
        // Instance is passed directly
        [Import("function(inst, name) { return inst[name] !== undefined; }", PassInstanceAsArgument = true)]
        extern public bool HasField(string name);
#else
        public bool HasField(string name)
        {
            return runtime.HasField(type, Key, Id, name);
        }
#endif

#if IL2JS
        // Instance is passed directly, however result will be imported according to T as we desire
        [Import("function(inst, name) { return inst[name]; }", PassInstanceAsArgument = true)]
        extern public T GetField<T>(string name);
#else
        public T GetField<T>(string name)
        {
            return (T)runtime.GetField(type, typeof(T), Key, Id, name);
        }
#endif
    }
}