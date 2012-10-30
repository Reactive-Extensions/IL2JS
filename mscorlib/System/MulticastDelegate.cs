//
// A re-implementation of Delegate/MulticastDelegate for the JavaScript runtime.
// Most of the actual code lives in IL2JS/runtime.js.
// Underlying representation is a JavaScript function with additional fields.
//

using Microsoft.LiveLabs.JavaScript.IL2JS;

namespace System
{
    [UsedType(true)]
    public abstract class MulticastDelegate : Delegate
    {
        // SPECIAL CASE: Compiler implements constructor
        extern protected MulticastDelegate(object target, string method);

        // SPECIAL CASE: Compiler implements constructor
        extern protected MulticastDelegate(Type target, string method);
    }
}