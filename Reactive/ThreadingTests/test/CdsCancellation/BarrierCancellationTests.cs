using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace plinq_devtests.Cancellation
{
    public static class BarrierCancellationTests
    {
        public static bool CancelBeforeWait()
        {
            TestHarness.TestLog("* BarrierCancellationTests.CancelBeforeWait()");
            bool passed = true;

            Barrier barrier = new Barrier(3);
            
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();
            CancellationToken ct = cs.Token;
            
            const int millisec = 100;
            TimeSpan timeSpan = new TimeSpan(100);
            
            passed &= TestHarnessAssert.EnsureOperationCanceledExceptionThrown(() => barrier.SignalAndWait(ct), ct, "An OCE should have been thrown.");
            passed &= TestHarnessAssert.EnsureOperationCanceledExceptionThrown(() => barrier.SignalAndWait(millisec, ct), ct, "An OCE should have been thrown.");
            passed &= TestHarnessAssert.EnsureOperationCanceledExceptionThrown(() => barrier.SignalAndWait(timeSpan, ct), ct, "An OCE should have been thrown.");

            barrier.Dispose();
            return passed;
        }
       
        public static bool CancelAfterWait()
        {
            TestHarness.TestLog("* BarrierCancellationTests.CancelAfterWait()");
            bool passed = true;

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            const int numberParticipants = 3;
            Barrier barrier = new Barrier(numberParticipants);
            
            ThreadPool.QueueUserWorkItem(
                (args) =>
                {
                    Thread.Sleep(1000);
                    cancellationTokenSource.Cancel();
                }
                );

            //Now wait.. the wait should abort and an exception should be thrown
            passed &= TestHarnessAssert.EnsureOperationCanceledExceptionThrown(
                () => barrier.SignalAndWait(cancellationToken),
                cancellationToken, "An OCE(null) should have been thrown that references the cancellationToken.");

            //Test that backout occured.
            passed &= TestHarnessAssert.AreEqual(numberParticipants, barrier.ParticipantsRemaining,
                                                 "All participants should remain as the current one should have backout out its signal");

            // the token should not have any listeners.
            // currently we don't expose this.. but it was verified manually
            
            return passed;
        }
    }
}

