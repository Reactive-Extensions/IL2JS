//
// JavaScript interop attributes
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
// NOTE: Must keep in sync with attributes in InteropTypes.cs
//

using System;

namespace Microsoft.LiveLabs.JavaScript.Interop
{
    /// <summary>
    /// Implement a .Net read-only property as a JavaScript field, and use that field as the <c>Keyed</c> object's
    /// unique key.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public sealed class ImportKeyAttribute : ImportAttribute
    {
        public ImportKeyAttribute()
        {
        }

        public ImportKeyAttribute(string script)
        {
            Script = script;
        }
    }
}
