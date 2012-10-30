using System;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.WSH.Host
{
    [Flags]
    enum ScriptItem
    {
        IsVisible = 0x2,
        IsSource = 0x4,
        GlobalMembers = 0x8,
        IsPersistent = 0x40,
        CodeOnly =0x200,
        NoCode = 0x400
    }
}
