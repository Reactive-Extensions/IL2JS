using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace plinq_devtests.Cancellation
{
    public static class OperationCanceledExceptionTests
    {
        public static bool OperationCanceledException_Basics()
        {
            TestHarness.TestLog("* OperationCanceledExceptionTests.OperationCanceledException_Basics()");
            bool passed = true;

#if !PFX_LEGACY_3_5
            CancellationToken ct1 = new CancellationTokenSource().Token;
            OperationCanceledException ex1 = new OperationCanceledException(ct1);
            passed &= TestHarnessAssert.AreEqual(ct1, OCEHelper.ExtractCT(ex1), "The exception should have the CancellationToken baked in.");

            CancellationToken ct2 = new CancellationTokenSource().Token;
            OperationCanceledException ex2 = new OperationCanceledException("message", ct2);
            passed &= TestHarnessAssert.AreEqual(ct2, OCEHelper.ExtractCT(ex2), "The exception should have the CancellationToken baked in.");

            CancellationToken ct3 = new CancellationTokenSource().Token;
            OperationCanceledException ex3 = new OperationCanceledException("message", new Exception("inner"), ct3);
            passed &= TestHarnessAssert.AreEqual(ct3, OCEHelper.ExtractCT(ex3), "The exception should have the CancellationToken baked in.");
#endif
            return passed;
        }
    }
}
