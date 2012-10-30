using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace plinq_devtests.Cancellation
{
    public static class CountdownEventCancellationTests
    {
        public static bool CancelBeforeWait()
        {
            TestHarness.TestLog("* CountdownEventCancellationTests.CancelBeforeWait()");
            bool passed = true;

            
            CountdownEvent countdownEvent = new CountdownEvent(2);
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();
            CancellationToken ct = cs.Token;

            const int millisec = 100;
            TimeSpan timeSpan = new TimeSpan(100);

            passed &= TestHarnessAssert.EnsureOperationCanceledExceptionThrown(() => countdownEvent.Wait(ct), ct, "An OCE should have been thrown.");
            passed &= TestHarnessAssert.EnsureOperationCanceledExceptionThrown(() => countdownEvent.Wait(millisec, ct), ct, "An OCE should have been thrown.");
            passed &= TestHarnessAssert.EnsureOperationCanceledExceptionThrown(() => countdownEvent.Wait(timeSpan, ct), ct, "An OCE should have been thrown.");

            countdownEvent.Dispose();
            return passed;
        }

        public static bool CancelAfterWait()
        {
            TestHarness.TestLog("* CountdownEventCancellationTests.CancelAfterWait()");
            bool passed = true;

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            CountdownEvent countdownEvent = new CountdownEvent(2); ;  // countdownEvent that will block all waiters

            ThreadPool.QueueUserWorkItem(
                (args) =>
                {
                    Thread.Sleep(1000);
                    cancellationTokenSource.Cancel();
                }
                );

            //Now wait.. the wait should abort and an exception should be thrown
            passed &= TestHarnessAssert.EnsureOperationCanceledExceptionThrown(
                () => countdownEvent.Wait(cancellationToken),
                cancellationToken, "An OCE(null) should have been thrown that references the cancellationToken.");

            // the token should not have any listeners.
            // currently we don't expose this.. but it was verified manually

            return passed;
        }
    }
}

