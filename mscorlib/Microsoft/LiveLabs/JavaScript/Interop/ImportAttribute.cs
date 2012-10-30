//
// JavaScript interop attributes
// NOTE: Appears in both IL2JS's mscorlib and CLR's JSTypes assemblies
// NOTE: Must keep in sync with attributes in InteropTypes.cs
//

using System;

namespace Microsoft.LiveLabs.JavaScript.Interop
{
    /// <summary>
    /// Implement a .Net method, property, or event using JavaScript
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Constructor |
                    AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Method, Inherited = false)]
    public class ImportAttribute : NamingAttribute
    {
        /// <summary>
        /// One of:
        /// <list type="bullet">
        /// <item><description>The name of the JavaScript function implementing the method.</description></item>
        /// <item><description>A JavaScript function expression implementing the method inline.</description></item>
        /// </list>
        /// If <c>null</c>, a JavaScript name is derived from the member name.
        /// Default <c>nulll</c>.
        /// </summary>
        public string Script { get; protected set; }

        /// <summary>
        /// How an imported constructor with <c>Script</c> = <c>null</c> is implemented.
        /// <list type="bullet">
        /// <item><description><c>Constructor</c> = invoke constructor function derived from type name.</description></item>
        /// <item><description><c>Object</c> = return an empty JavaScript object (default constructors only).</description></item>
        /// <item><description><c>Array</c> = return a JavaScript array containing constructor arguments.</description></item>
        /// </list>
        /// Default <c>Constructor</c>.
        /// </summary>
        public Creation Creation { get; set; }

        /// <summary>
        /// If <c>true</c>, imported events allow any number of listeners regardless of underlying JavaScript
        /// implementation.
        /// Default <c>false</c>.
        /// </summary>
        public bool SimulateMulticastEvents { get; set; }

        public ImportAttribute()
        {
        }

        public ImportAttribute(string script)
        {
            Script = script;
        }
    }
}
