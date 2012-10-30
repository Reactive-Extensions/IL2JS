using System;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.WSH.Host
{
    [Flags]
    enum ScriptInfo : uint
    {
        IUnknown = 1,
        ITypeInfo = 2,
        All = IUnknown | ITypeInfo
    }
}
