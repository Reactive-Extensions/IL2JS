//
// A re-implementation of Activator for the JavaScript runtime.
//

using Microsoft.LiveLabs.JavaScript.Interop;

namespace System
{
    public static class Activator
    {
        [Import(@"function(root, type) {
                      if (type.DefaultConstructor == null)
                          throw root.InvalidOperationException();
                      else
                          return type.DefaultConstructor();
                  }", PassRootAsArgument = true)]
        extern public static object CreateInstance(Type type);

        public static T CreateInstance<T>()
        {
            return (T)CreateInstance(typeof (T));
        }
    }
}
