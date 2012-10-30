using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    /// <summary>
    /// A dummy replacement for the .NET 4.0 SecuritySafeCriticalAttribute. The dummy attribute makes the
    /// code compile, but we are likely losing the ability to be called from a partial trust environment.
    /// </summary>
    internal class SecuritySafeCriticalAttribute : Attribute
    {
    }
}
