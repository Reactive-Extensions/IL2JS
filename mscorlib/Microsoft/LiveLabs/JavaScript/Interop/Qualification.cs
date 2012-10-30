//
// JavaScript interop attributes
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
// NOTE: Must keep in sync with InteropTypes.cs
//

namespace Microsoft.LiveLabs.JavaScript.Interop
{
    /// <summary>
    /// How JavaScript names should be qualified when derived from .Net static member names.
    /// </summary>
    public enum Qualification
    {
        /// <summary>
        /// JavaScript name includes member name only.
        /// </summary>
        None = 0,
        /// <summary>
        /// JavaScript name includes global object (if non-<c>null</c>), namespace, type and member name.
        /// </summary>
        Full = 1,
        /// <summary>
        /// JavaScript name includes type and member name.
        /// </summary>
        Type = 2
    }
}
