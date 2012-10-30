// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// TestLog.cs
//
// Part of the PLINQ dev unit test harness.
// need to split the TestHarness in two files in order to have the ManagedScheduler unit tests added to daily runs
// TestLog.cs is included in InternalSchedulerTest.csproj as well
//
// when a ManagedScheduler unit test is added here - it needs to be added into:
// qa\pfx\Functional\Scheduler\SchedulerTestSuites\DevUnitTests\DevUnitTestSuite.cs and to the 
// qa\pfx\Functional\Scheduler\SchedulerTestCases\DevUnitTestSuite.xml
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Parallel;
using System.Text;

namespace plinq_devtests
{


    internal partial class TestHarness
    {

        private static bool quietMode = false;
        internal static void TestLog(string msg, params object[] args)
        {
            if (!quietMode)
                Console.WriteLine("   - " + msg, args);
        }
    }

   
}
