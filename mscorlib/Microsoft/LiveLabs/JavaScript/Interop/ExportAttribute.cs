//
// JavaScript interop attributes
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
// NOTE: Must keep in sync with attributes in InteropTypes.cs
//

using System;

namespace Microsoft.LiveLabs.JavaScript.Interop
{
    /// <summary>
    /// Implement a JavaScript function using a .Net method, property or event.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Delegate |
                    AttributeTargets.Constructor | AttributeTargets.Property |AttributeTargets.Event |
                    AttributeTargets.Method, Inherited = false)]
    public class ExportAttribute : NamingAttribute
	{
        /// <summary>
        /// One of:
        /// <list type="bullet">
        /// <item><description>The name of the JavaScript function to implement.</description></item>
        /// <item><description>A JavaScript function expression which assigns the .Net implementation to the JavaScript global or field.</description></item>
        /// </list>
        /// If <c>null</c>, a JavaScript name is derived fom the member name.
        /// Default <c>null</c>.
        /// </summary>
        public string Script { get; protected set; }

        /// <summary>
        /// If <c>true</c>, bind instance members into the <c>prototype</c> object of the JavaScript constructor
        /// function derived from the member's declaring type. If <c>false</c>, bind instance members into each
        /// JavaScript object upon construction or first export. Default is <c>false</c>.
        /// </summary>
        public bool BindToPrototype { get; set; }

        public ExportAttribute()
        {
        }

        public ExportAttribute(string script)
        {
            Script = script;
        }
    }
}
