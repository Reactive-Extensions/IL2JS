//
// A re-implementation of ValueType for the JavaScript runtime.
// Underlying representation is a pointer containing a value.
//

using Microsoft.LiveLabs.JavaScript.IL2JS;
using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    [Runtime(true)]
    public abstract class ValueType
    {
        // Effectively implemented by compiler
        extern protected ValueType();

        // Structural equality
        [Import(@"function (inst, obj) {
                      if (inst.T !== obj.T)
                          return false;
                      return inst.T.Equals(inst.R(), obj.R());
                  }", PassInstanceAsArgument = true)]
        extern public override bool Equals(object obj);

        // Structural hash
        [Import(@"function (inst) { return inst.T.Hash(inst.R()); }", PassInstanceAsArgument = true)]
        extern public override int GetHashCode();
    }
}
