//
// JavaScript interop attributes
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
// NOTE: Must keep in sync with attributes in InteropTypes.cs
//

namespace Microsoft.LiveLabs.JavaScript.Interop
{
    /// <summary>
    /// Control how instance state may be distributed between .Net and JavaScript objects.
    /// </summary>
    public enum InstanceState
    {
        /// <summary>
        /// All instance state is kept within a .Net object.
        /// </summary>
        ManagedOnly = 0,

        /// <summary>
        /// Instance state is split between a .Net and JavaScript object.
        /// </summary>
        ManagedAndJavaScript = 1,

        /// <summary>
        /// All instance state is kept within a JavaScript object.
        /// </summary>
        JavaScriptOnly = 2,

        /// <summary>
        /// JavaScript object is created by an imported constructor. Additional .Net instance state
        /// is stored directly in JavaScript object. Reinterpreted as 'JavaScriptOnly' in managed
        /// interop mode.
        /// </summary>
        Merged = 3
    }
}
