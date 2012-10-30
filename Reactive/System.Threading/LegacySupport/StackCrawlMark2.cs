using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Threading
{
    /// <summary>
    /// A dummy replacement for the .NET internal class StackCrawlMark.
    /// </summary>
    internal struct StackCrawlMark2
    {
        internal static StackCrawlMark2 LookForMyCaller
        {
            get { return new StackCrawlMark2(); }
        }
    }
}
