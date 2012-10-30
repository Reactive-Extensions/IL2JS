using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace plinq_devtests.Cancellation
{
    public static class SemaphoreSlimCancellationTests
    {

        public static bool RunAllTests()
        {
            bool passed = true;
            passed &= CancelBeforeWait();
            passed &= CancelAfterWait();
            passed &= SemaphoreSlim_MultipleWaitersWithSeparateTokens();
            return passed;
        }


        public static bool CancelBeforeWait()
        {
            TestHarness.TestLog("* SemaphoreSlimCancellationTests.CancelBeforeWait()");
            bool passed = true;

            SemaphoreSlim semaphoreSlim = new SemaphoreSlim(2);
            
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();
            CancellationToken ct = cs.Token;

            const int millisec = 100;
            TimeSpan timeSpan = new TimeSpan(100);
            passed &= TestHarnessAssert.EnsureOperationCanceledExceptionThrown(() => semaphoreSlim.Wait(ct), ct, "An OCE should have been thrown.");
            passed &= TestHarnessAssert.EnsureOperationCanceledExceptionThrown(() => semaphoreSlim.Wait(millisec, ct), ct, "An OCE should have been thrown.");
            passed &= TestHarnessAssert.EnsureOperationCanceledExceptionThrown(() => semaphoreSlim.Wait(timeSpan, ct), ct, "An OCE should have been thrown.");
            semaphoreSlim.Dispose();

            return passed;
        }

        public static bool CancelAfterWait()
        {
            TestHarness.TestLog("* SemaphoreSlimCancellationTests.CancelAfterWait()");
            bool passed = true;

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            SemaphoreSlim semaphoreSlim = new SemaphoreSlim(0); // semaphore that will block all waiters
            
            ThreadPool.QueueUserWorkItem(
                (args) =>
                {
                    Thread.Sleep(1000);
                    cancellationTokenSource.Cancel();
                }
                );

            //Now wait.. the wait should abort and an exception should be thrown
            passed &= TestHarnessAssert.EnsureOperationCanceledExceptionThrown(
                () => semaphoreSlim.Wait(cancellationToken),
                cancellationToken, "An OCE(null) should have been thrown that references the cancellationToken.");

            // the token should not have any listeners.
            // currently we don't expose this.. but it was verified manually

            return passed;
        }

        //Identified as a possible concern in bug 544743.
        private static bool SemaphoreSlim_MultipleWaitersWithSeparateTokens()
        {
            TestHarness.TestLog("* SemaphoreSlimCancellationTests.SemaphoreSlim_MultipleWaitersWithSeparateTokens()");
            bool passed = true;

            SemaphoreSlim semaphoreSlim = new SemaphoreSlim(0); // this semaphore will always be blocked for waiters.
            const int waitTimeoutMilliseconds = 1000;
            const int waitBeforeCancelMilliseconds = 300;

            CancellationTokenSource cts1 = new CancellationTokenSource();
            CancellationTokenSource cts2 = new CancellationTokenSource();
            bool wait1WokeUpNormally = false;
            bool wait2WokeUpNormally = false;
            OperationCanceledException wait1OCE = null;
            OperationCanceledException wait2OCE = null;
            int wait1ElapsedMilliseconds = -1;
            int wait2ElapsedMilliseconds = -1;

            CountdownEvent cde_allThreadsFinished = new CountdownEvent(2);

            //Queue up cancellation of CTS1.
            ThreadPool.QueueUserWorkItem(
                unused =>
                {
                    Thread.Sleep(waitBeforeCancelMilliseconds); // wait a little while.
                    cts1.Cancel();
                }
                );

            //Queue up a wait on mres(CTS1)
            ThreadPool.QueueUserWorkItem(
                unused =>
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    try
                    {
                        wait1WokeUpNormally = semaphoreSlim.Wait(waitTimeoutMilliseconds, cts1.Token);
                    }
                    catch (OperationCanceledException oce)
                    {
                        wait1OCE = oce;
                    }
                    finally
                    {
                        sw.Stop();
                    }
                    wait1ElapsedMilliseconds = (int)sw.Elapsed.TotalMilliseconds;

                    cde_allThreadsFinished.Signal();
                }
                );

            //Queue up a wait on mres(CTS2)
            ThreadPool.QueueUserWorkItem(
                unused =>
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    try
                    {
                        wait2WokeUpNormally = semaphoreSlim.Wait(waitTimeoutMilliseconds, cts2.Token);
                    }
                    catch (OperationCanceledException oce)
                    {
                        wait2OCE = oce;
                    }
                    finally
                    {
                        sw.Stop();
                    }
                    wait2ElapsedMilliseconds = (int)sw.Elapsed.TotalMilliseconds;
                    cde_allThreadsFinished.Signal();
                }
                );

            cde_allThreadsFinished.Wait();

            Console.WriteLine("        (first  wait duration [expecting <={0,4}]        ={1,4})", 500, wait1ElapsedMilliseconds);
            Console.WriteLine("        (second wait duration [expecting   {0,4} +-50ms] ={1,4})", waitTimeoutMilliseconds, wait2ElapsedMilliseconds);

            passed &= TestHarnessAssert.IsFalse(wait1WokeUpNormally, "The first wait should be canceled.");
            passed &= TestHarnessAssert.IsNotNull(wait1OCE, "The first wait should have thrown an OCE.");
            passed &= TestHarnessAssert.AreEqual(cts1.Token, OCEHelper.ExtractCT(wait1OCE), "The first wait should have thrown an OCE(cts1.token).");
            passed &= TestHarnessAssert.IsTrue(wait1ElapsedMilliseconds < 500, "[Warning: Timing Sensitive Test] The first wait should have canceled before 500ms elapsed.");


            passed &= TestHarnessAssert.IsFalse(wait2WokeUpNormally, "The second wait should not have woken up normally. It should have woken due to timeout.");
            passed &= TestHarnessAssert.IsNull(wait2OCE, "The second wait should not have thrown an OCE.");
            passed &= TestHarnessAssert.IsTrue(950 <= wait2ElapsedMilliseconds && wait2ElapsedMilliseconds <= 1050, "[Warning: Timing Sensitive Test] The second wait should have waited 1000ms +-50ms). Actual wait duration = " + wait2ElapsedMilliseconds);

            return passed;
        }

        
    }
}
