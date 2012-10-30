//
// JavaScript interop attributes
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
// NOTE: Must keep in sync with attributes in InteropTypes.cs
//

using System;

namespace Microsoft.LiveLabs.JavaScript.Interop
{
    /// <summary>
    /// Options for sharing the implementation of types between .Net and JavaScript.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class InteropAttribute : Attribute
    {
        /// <summary>
        /// If non-<c>null</c>, a JavaScript function which, given an assembly name and JavaScript object,
        /// returns the fully-qualified type name best representing that object. Returned type name must
        /// be a subtype of this class.
        /// Default <c>null</c>.
        /// </summary>
        public string Script { get; private set; }

        /// <summary>
        /// Control how instance state may be distributed between .Net and JavaScript objects.
        /// <list type="bullet">
        /// <item><description><c>ManagedOnly</c> = all instance state is kept within a .Net object.</description></item>
        /// <item><description><c>ManagedAndJavaScript</c> = instance state is split between a .Net and JavaScript object.</description></item>
        /// <item><description><c>JavaScriptOnly</c> = all instance state is kept within a JavaScript object.</description></item>
        /// <item><description><c>Merged</c> = used by IL2JS runtime system.</description></item>
        /// </list>
        /// Default <c>ManagedOnly</c>.
        /// </summary>
        public InstanceState State { get; set; }

        /// <summary>
        /// If non-<c>null</c>, the default JavaScript field to hold keys in <c>Keyed</c> types.
        /// If <c>null</c>, <c>Keyed</c> types must have exactly one property annotated with <c>ImportKey</c>.
        /// Default <c>null</c>.
        /// </summary>
        public string DefaultKey { get; set; }

        /// <summary>
        /// If <c>true</c>, the JavaScript <c>undefined</c> value is represented by a default .Net object.
        /// If <c>false</c>, <c>undefined</c> is represented by the .Net <c>null</c> value. Only significant
        /// for types with <c>State</c> = <c>JavaScriptOnly</c>.
        /// Default <c>false</c>.
        /// </summary>
        public bool UndefinedIsNotNull { get; set; }

        public InteropAttribute()
        {
        }

        public InteropAttribute(string script)
        {
            Script = script;
        }
    }
}