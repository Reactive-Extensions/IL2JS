//
// JavaScript interop attributes
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
// NOTE: Must keep in sync with attributes in InteropTypes.cs
//

namespace Microsoft.LiveLabs.JavaScript.Interop
{
    /// <summary>
    /// How to alter the first character of a .Net name when deriving a JavaScript name.
    /// </summary>
    public enum Casing
    {
        /// <summary>
        /// First character's case is unchanged
        /// </summary>
        Exact = 0,
        /// <summary>
        /// First character's case is made lower-case
        /// </summary>
        Camel = 1,
        /// <summary>
        /// First character's case is made upper-case
        /// </summary>
        Pascal = 2,
    }
}
