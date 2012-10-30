//
// JavaScript interop attributes
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
// NOTE: Must keep in sync with InteropTypes.cs
//

using System;

namespace Microsoft.LiveLabs.JavaScript.Interop
{
    /// <summary>
    /// Suppress an inherited <c>Export</c> attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Property | AttributeTargets.Event |
                    AttributeTargets.Method, Inherited = false)]
    public sealed class NotExportedAttribute : Attribute
    {
        public NotExportedAttribute()
        {
        }
    }
}