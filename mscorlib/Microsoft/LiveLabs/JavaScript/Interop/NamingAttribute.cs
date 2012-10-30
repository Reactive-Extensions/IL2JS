//
// JavaScript interop attributes
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
// NOTE: Must keep in sync with InteropTypes.cs
//

using System;

namespace Microsoft.LiveLabs.JavaScript.Interop
{
    /// <summary>
    /// Provide properties inherited by <c>Import</c> and <c>Export</c> attributes on inner declarations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Constructor |
                    AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Method, Inherited = false)]
    public class NamingAttribute : Attribute
    {
        /// <summary>
        /// How JavaScript names should be qualified  when derived from .Net static member names.
        /// <list type="bullet">
        /// <item><description><c>None</c> = member name only</description></item>
        /// <item><description><c>Type</c> = type and member name</description></item>
        /// <item><description><c>Full</c> = global object (if non-<c>null</c>), namespace, type and member names</description></item>
        /// </list>
        /// Default <c>None</c>.
        /// </summary>
        public Qualification Qualification { get; set; }

        /// <summary>
        /// How first character of each namespace component should be re-capitalized for JavaScript.
        /// <list type="bullet">
        /// <item><description><c>Exact</c> = no change</description></item>
        /// <item><description><c>Camel</c> = lower-case</description></item>
        /// <item><description><c>Pascal</c> = upper-case</description></item>
        ///</list>
        /// Default <c>Exact</c>.
        /// </summary>
        public Casing NamespaceCasing { get; set; }

        /// <summary>
        /// How first character of type name should be re-capitalized for JavaScript.
        /// <list type="bullet">
        /// <item><description><c>Exact</c> = no change</description></item>
        /// <item><description><c>Camel</c> = lower-case</description></item>
        /// <item><description><c>Pascal</c> = upper-case</description></item>
        ///</list>
        /// Default <c>Exact</c>.
        /// </summary>
        public Casing TypeNameCasing { get; set; }

        /// <summary>
        /// How first character of get/set/add/remove prefix of methods which implement properties/events
        /// should be re-capitalized for JavaScript.
        /// <list type="bullet">
        /// <item><description><c>Exact</c> = no change</description></item>
        /// <item><description><c>Camel</c> = lower-case</description></item>
        /// <item><description><c>Pascal</c> = upper-case</description></item>
        ///</list>
        /// Default <c>Camel</c>.
        /// </summary>
        public Casing PrefixNameCasing { get; set; }

        /// <summary>
        /// How first character of member names should be re-capitalized for JavaScript.
        /// <list type="bullet">
        /// <item><description><c>Exact</c> = no change</description></item>
        /// <item><description><c>Camel</c> = lower-case</description></item>
        /// <item><description><c>Pascal</c> = upper-case</description></item>
        ///</list>
        /// Default <c>Camel</c>.
        /// </summary>
        public Casing MemberNameCasing { get; set; }

        /// <summary>
        /// If <c>true</c>, do not include the get_/set_/add_/remove_ prefix of methods which
        /// implement properities/events in JavaScirpt. Default <c>false</c>.
        /// </summary>
        public bool RemoveAccessorPrefix { get; set; }

        /// <summary>
        /// If <c>true</c>, do not include the underscore in prefix of methods which implement properties/events
        /// in JavaScript. Default <c>false</c>.
        /// </summary>
        public bool RemoveAccessorUnderscore { get; set; }

        /// <summary>
        /// Set the name of the global object to holding static members. Default <c>null</c>.
        /// </summary>
        public string GlobalObject { get; set; }

        /// <summary>
        /// If <c>true</c>, JavaScript method accepts instance as an explicit argument rather than using the
        /// implicit <c>this</c> argument.
        /// Default <c>false</c>.
        /// </summary>
        public bool PassInstanceAsArgument { get; set; }

        /// <summary>
        /// If <c>true</c>, parameter arrays are inlined when invoking an imported method from .Net, and outlined
        /// when invoking an exported method from JavaScript.
        /// Default <c>false</c>.
        /// </summary>
        public bool InlineParamsArray { get; set; }

        /// <summary>
        /// For internal use only.
        /// </summary>
        public bool PassRootAsArgument { get; set; }

        public NamingAttribute()
        {
        }
    }
}
