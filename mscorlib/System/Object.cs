//
// A re-implementation of Object for the JavaScript runtime.
// Underlying representation is a JavaScript object with type field holding object's type's type structure.
//

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    [UsedType(true)] // always required
    // Make sure imports below work for all objects, regardless of state representation
    [Runtime(true)]
    public class Object
    {
        // Always inlined by compiler
        public Object()
        {
        }

        // Never called by runtime
        ~Object()
        {
        }

        // Reference equality
        [Used(true)] // invoked by runtime
        [Import("function(inst, obj) { return inst === obj; }", PassInstanceAsArgument = true)]
        extern public virtual bool Equals(object obj);

        public static bool Equals(object objA, object objB)
        {
            return (ReferenceEquals(objA, objB) || objA != null && objB != null && objA.Equals(objB));
        }

        // Object identity hash. Object id's are allocated lazily.
        [Used(true)] // invoked by runtime
        [Import(@"function(root, inst) {
                      if (inst.Id == null)
                          inst.Id = root.NextObjectId++;
                      return inst.Id;
                  }", PassRootAsArgument=true, PassInstanceAsArgument = true)]
        extern public virtual int GetHashCode();

        [Import("function(root, inst) { return inst.T; }", PassRootAsArgument = true, PassInstanceAsArgument = true)]
        extern public Type GetType();

        [Import(@"function (inst) { return inst.T.MemberwiseClone(inst); }", PassInstanceAsArgument = true)]
        extern protected object MemberwiseClone();

        [Import(@"function (objA, objB) { return objA === objB; }")]
        extern public static bool ReferenceEquals(object objA, object objB);

        public virtual string ToString()
        {
            return GetType().ToString();
        }
    }
}
