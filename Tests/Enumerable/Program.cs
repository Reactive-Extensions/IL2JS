using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.LiveLabs.JavaScript.Tests
{
    static class TestEnumerable
    {

        public static void Main()
        {
            TestLogger.Log("Testing sum...");
            TestLogger.Log("0123456780abcdef".Where(Char.IsDigit).Select(c => c - '0').Sum());

            TestLogger.Log("Testing first...");
            TestLogger.Log("0123456780abcdef".Where(Char.IsDigit).Select(c => c - '0').First());
        }
    }
}