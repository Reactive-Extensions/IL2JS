//
// JavaScript interop attributes
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
// NOTE: Must keep in sync with attributes in InteropTypes.cs
//

namespace Microsoft.LiveLabs.JavaScript.Interop
{
    /// <summary>
    /// How an imported constructor with <c>Script</c> = <c>null</c> is implemented.
    /// </summary>
    public enum Creation
    {
        /// <summary>
        /// Invoke constructor function derived from type name.
        /// </summary>
        Constructor = 0,
        /// <summary>
        /// Return an empty JavaScript object (default constructors only).
        /// </summary>
        Object = 1,
        /// <summary>
        /// Return a JavaScipt array containing constructor arguments.
        /// </summary>
        Array = 2,
    }
}
