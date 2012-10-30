//
// Refine the interop InstanceState for internal use
// NOTE: Must keep in sync with InstanceState.cs
//

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop
{
    public enum InteropStyle
    {
        // Shared with Microsoft.LiveLabs.JavaScript.Interop.InstanceState, but renamed to match how we interpret it
        Normal = 0, // ManagedOnly
        Keyed = 1, // ManagedAndJavaScript
        Proxied = 2, // JavaScriptOnly
        Primitive = 3, // Merged
        // Additional, for internal use only
        Nullable = 4,
        Pointer = 5,
        Delegate = 6,
        Array = 7
    }
}
