using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.LiveLabs.CoreExTests
{
    [Flags]
    public enum EnumerableState
    {
        CreatingEnumerator = 0x00001,
        CreatedEnumerator = 0x00002,
        Disposed = 0x00004,
        Resetted = 0x00008,
        Fresh = 0x00010,
        MovingNext = 0x00020,
        MovedNext = 0x00040,
        AtEnd = 0x00080,
        Resetting = 0x00100,
        Disposing = 0x00200,
        GettingCurrent = 0x00400,
        GotCurrent = 0x00800,

        Threw = 0x80000,
    }
}
