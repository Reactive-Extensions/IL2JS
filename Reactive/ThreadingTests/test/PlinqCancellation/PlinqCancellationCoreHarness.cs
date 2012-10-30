using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using plinq_devtests.Cancellation;

namespace plinq_devtests
{
    public static class PlinqCancellationCoreHarness
    {
        public static bool RunPlinqCancellationTests()
        {
            bool passed = true;

            // a key part of cancellation testing is 'promptness'.  Those tests appear in pfxperfunittests.
            // the tests here are only regarding basic API correctness and sanity checking.
            passed &= MultiplesWithCancellationIsIllegal();
            passed &= CancellationTokenTest_Sorting_ToArray();
            passed &= CancellationTokenTest_NonSorting_AsynchronousMergerEnumeratorDispose();
            passed &= CancellationTokenTest_NonSorting_SynchronousMergerEnumeratorDispose();
            passed &= CancellationTokenTest_NonSorting_ToArray_ExternalCancel();

            passed &= PreCanceledToken_SimpleEnumerator();
            passed &= PreCanceledToken_ForAll();

            passed &= Cancellation_ODEIssue();

            passed &= CancellationSequentialWhere();
            passed &= CancellationSequentialElementAt();
            passed &= CancellationSequentialDistinct();

            passed &= BugFix545118_AggregatesShouldntWrapOCE();
            passed &= BugFix535510_CloningQuerySettingsForSelectMany();
            passed &= BugFix543310_ChannelCancellation_ProducerBlocked();
            passed &= Bugfix626345_PlinqShouldDisposeLinkedToken();
            passed &= Bugfix632544_OnlySuppressOCEifCTCanceled();
            passed &= Bugfix638383_ImmediateDispose();
            passed &= Bugfix640886_SetOperationsThrowAggregateOnCancelOrDispose_1();
            passed &= Bugfix640886_SetOperationsThrowAggregateOnCancelOrDispose_2();
            passed &= Bug667799_HashPartitioningCancellation();
            passed &= Bug695173_CancelThenDispose();
            passed &= Bug702254_CancellationCausingNoDataMustThrow();
            passed &= Bug702254Related_DontDoWorkIfTokenAlreadyCanceled();

            passed &= Bug720598_PreDisposedCTSPassedToPlinq();


            return passed;
        }

        

        public static bool PreCanceledToken_ForAll()
        {
            bool passed = true;
            TestHarness.TestLog("* PlinqCancellationTests.PreCanceledToken_ForAll()");

            OperationCanceledException caughtException = null;
            var cs = new CancellationTokenSource();
            cs.Cancel();

            int[] srcEnumerable = Enumerable.Range(0, 1000).ToArray();
            ThrowOnFirstEnumerable<int> throwOnFirstEnumerable = new ThrowOnFirstEnumerable<int>(srcEnumerable);

            try
            {
                throwOnFirstEnumerable
                    .AsParallel()
                    .WithCancellation(cs.Token)
                    .ForAll((x) => { Console.WriteLine(x); });
            }
            catch (OperationCanceledException ex)
            {
                caughtException = ex;
            }

            passed &= TestHarnessAssert.IsNotNull(caughtException, "an OCE should be throw during query opening");
            passed &= TestHarnessAssert.AreEqual(cs.Token, OCEHelper.ExtractCT(caughtException), "The OCE should reference the cancellation token.");

            return passed;
        }

        public static bool PreCanceledToken_SimpleEnumerator()
        {
            bool passed = true;
            TestHarness.TestLog("* PlinqCancellationTests.PreCanceledToken_SimpleEnumerator()");

            OperationCanceledException caughtException = null;
            var cs = new CancellationTokenSource();
            cs.Cancel();

            int[] srcEnumerable = Enumerable.Range(0, 1000).ToArray();
            ThrowOnFirstEnumerable<int> throwOnFirstEnumerable = new ThrowOnFirstEnumerable<int>(srcEnumerable);

            try
            {
                var query = throwOnFirstEnumerable
                    .AsParallel()
                    .WithCancellation(cs.Token);

                foreach(var item in query)
                {
                    
                }
            }
            catch (OperationCanceledException ex)
            {
                caughtException = ex;
            }

            passed &= TestHarnessAssert.IsNotNull(caughtException, "an OCE should be throw during query opening");
            passed &= TestHarnessAssert.AreEqual(cs.Token, OCEHelper.ExtractCT(caughtException), "The OCE should reference the cancellation token.");

            return passed;
        }

        private static bool MultiplesWithCancellationIsIllegal()
        {
            bool passed = true;
            TestHarness.TestLog("* PlinqCancellationTests.MultiplesWithCancellationIsIllegal()");

            InvalidOperationException caughtException = null;
            try
            {
                CancellationTokenSource cs = new CancellationTokenSource();
                CancellationToken ct = cs.Token;
                var query = Enumerable.Range(1, 10).AsParallel().WithDegreeOfParallelism(2).WithDegreeOfParallelism(2);
                query.ToArray();
            }
            catch (InvalidOperationException ex)
            {
                caughtException = ex;
                //Console.WriteLine("IOE caught. message = " + ex.Message);
            }

            passed &= TestHarnessAssert.IsNotNull(caughtException, "An exception should be thrown.");

            return passed;
        }

        private static bool CancellationTokenTest_Sorting_ToArray()
        {
            bool passed = true;
            TestHarness.TestLog("* PlinqCancellationTests.CancellationTokenTest_Sorting_ToArray()");

            int size = 10000;
            CancellationTokenSource tokenSource = new CancellationTokenSource();

            ThreadPool.QueueUserWorkItem(
                (arg) =>
                {
                    Thread.Sleep(500);
                    tokenSource.Cancel();
                });

            OperationCanceledException caughtException = null;
            try
            {
                // This query should run for at least a few seconds due to the sleeps in the select-delegate
                var query =
                    Enumerable.Range(1, size).AsParallel()
                        .WithCancellation(tokenSource.Token)
                        .Select(
                        i =>
                        {
                            Thread.Sleep(1);
                            return i;
                        });

                query.ToArray();
            }
            catch (OperationCanceledException ex)
            {
                caughtException = ex;
            }

            passed &= TestHarnessAssert.IsNotNull(caughtException, "An OCE should be thrown");
            passed &= TestHarnessAssert.AreEqual(tokenSource.Token, OCEHelper.ExtractCT(caughtException),
                                                     "The OCE should reference the external token.");
            return passed;
        }

        private static bool CancellationTokenTest_NonSorting_AsynchronousMergerEnumeratorDispose()
        {
            bool passed = true;
            TestHarness.TestLog("* PlinqCancellationTests.CancellationTokenTest_NonSorting_AsynchronousMergerEnumeratorDispose()");

            int size = 10000;
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            Exception caughtException = null;

            var query =
                    Enumerable.Range(1, size).AsParallel()
                        .WithCancellation(tokenSource.Token)
                        .Select(
                        i =>
                        {
                            Thread.Sleep(1000);
                            return i;
                        });

            IEnumerator<int> enumerator = query.GetEnumerator();

            ThreadPool.QueueUserWorkItem(
                (arg) =>
                {
                    Thread.Sleep(500);
                    enumerator.Dispose();
                });

            try
            {
                // This query should run for at least a few seconds due to the sleeps in the select-delegate
                for (int j = 0; j < 1000; j++)
                {
                    enumerator.MoveNext();
                }
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            passed &= TestHarnessAssert.IsNotNull(caughtException, "An ObjectDisposedException or OperationCanceledException should be thrown");
            //Console.WriteLine("Exception thrown = " + caughtException.GetType().ToString());
            return passed;
        }

        private static bool CancellationTokenTest_NonSorting_SynchronousMergerEnumeratorDispose()
        {
            bool passed = true;
            TestHarness.TestLog("* PlinqCancellationTests.CancellationTokenTest_NonSorting_SynchronousMergerEnumeratorDispose()");

            int size = 10000;
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            Exception caughtException = null;

            var query =
                    Enumerable.Range(1, size).AsParallel()
                        .WithCancellation(tokenSource.Token)
                        .Select(
                        i =>
                        {
                            Thread.Sleep(100);
                            return i;
                        }).WithMergeOptions(ParallelMergeOptions.FullyBuffered);

            IEnumerator<int> enumerator = query.GetEnumerator();

            ThreadPool.QueueUserWorkItem(
                (arg) =>
                {
                    Thread.Sleep(1000);
                    enumerator.Dispose();
                });

            try
            {
                // This query should run for at least a few seconds due to the sleeps in the select-delegate
                for (int j = 0; j < 1000; j++)
                {
                    enumerator.MoveNext();
                }
            }
            catch (ObjectDisposedException ex)
            {
                caughtException = ex;
            }

            passed &= TestHarnessAssert.IsNotNull(caughtException, "An ObjectDisposedException should be thrown");
            
            return passed;
        }

        private static bool CancellationTokenTest_NonSorting_ToArray_ExternalCancel()
        {
            bool passed = true;
            TestHarness.TestLog("* PlinqCancellationTests.CancellationTokenTest_NonSorting_ToArray_ExternalCancel()");

            int size = 10000;
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            OperationCanceledException caughtException = null;

            ThreadPool.QueueUserWorkItem(
                (arg) =>
                {
                    Thread.Sleep(1000);
                    tokenSource.Cancel();
                });

            try
            {
                int[] output = Enumerable.Range(1, size).AsParallel()
                   .WithCancellation(tokenSource.Token)
                   .Select(
                   i =>
                   {
                       Thread.Sleep(100);
                       return i;
                   }).ToArray();
            }
            catch (OperationCanceledException ex)
            {
                caughtException = ex;
            }

            passed &= TestHarnessAssert.IsNotNull(caughtException, "An ObjectDisposedException should be thrown");
            passed &= TestHarnessAssert.AreEqual(tokenSource.Token, OCEHelper.ExtractCT(caughtException), "The OCE should reference the external cancellation token.");

            return passed;
        }

        /// <summary>
        /// 
        /// Bug535510:
        ///   This bug occured because the QuerySettings structure was not being deep-cloned during 
        ///   query-opening.  As a result, the concurrent inner-enumerators (for the RHS operators)
        ///   that occur in SelectMany were sharing CancellationState that they should not have.
        ///   The result was that enumerators could falsely believe they had been canceled when 
        ///   another inner-enumerator was disposed.
        ///   
        ///   Note: the failure was intermittent.  this test would fail about 1 in 2 times on mikelid1 (4-core).
        /// </summary>
        /// <returns></returns>
        private static bool BugFix535510_CloningQuerySettingsForSelectMany()
        {
            bool passed = true;
            TestHarness.TestLog("* PlinqCancellationTests.BugFix535510_CloningQuerySettingsForSelectMany()");

            var plinq_src = ParallelEnumerable.Range(0, 1999).AsParallel();
            Exception caughtException = null;

            try
            {
                var inner = ParallelEnumerable.Range(0, 20).AsParallel().Select(_item => _item);
                var output = plinq_src
                    .SelectMany(
                        _x => inner,
                        (_x, _y) => _x
                    )
                    .ToArray();
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            passed &= TestHarnessAssert.IsNull(caughtException, "No exception should occur.");
            return passed;
        }


        // Issue identified in Bug 543310 and also tracked as task 'channel cancellation'
        // Use of the async channel can block both the consumer and producer threads.. before the cancellation work
        // these had no means of being awoken.
        //
        // However, only the producers need to wake up on cancellation as the consumer 
        // will wake up once all the producers have gone away (via AsynchronousOneToOneChannel.SetDone())
        //
        // To specifically verify this test, we want to know that the Async channels were blocked in TryEnqueChunk before Dispose() is called
        //  -> this was verified manually, but is not simple to automate
        private static bool BugFix543310_ChannelCancellation_ProducerBlocked()
        {
            bool passed = true;
            TestHarness.TestLog("* PlinqCancellationTests.BugFix543310_ChannelCancellation_ProducerBlocked()");

            
            Console.Write("        Query running (should be few seconds max)..");
                var query1 = Enumerable.Range(0, 100000000)  //provide 100million elements to ensure all the cores get >64K ints. Good up to 1600cores
                    .AsParallel()
                    .Select(x => x);
                var enumerator1 = query1.GetEnumerator();
                enumerator1.MoveNext();
                Thread.Sleep(1000); // give the pipelining time to fill up some buffers.
                enumerator1.MoveNext();
                enumerator1.Dispose(); //can potentially hang
            
            Console.WriteLine("  Done (success).");
            return passed;
        }


        /// <summary>
        /// Bug545118:
        ///   This bug occurred because aggregations like Sum or Average would incorrectly
        ///   wrap OperationCanceledException with AggregateException.
        /// </summary>
        private static bool BugFix545118_AggregatesShouldntWrapOCE()
        {
            TestHarness.TestLog("* PlinqCancellationTests.BugFix545118_AggregatesShouldntWrapOCE()");

            var cs = new CancellationTokenSource();
            cs.Cancel();

            // Expect OperationCanceledException rather than AggregateException or something else
            try
            {
                Enumerable.Range(0, 1000).AsParallel().WithCancellation(cs.Token).Sum(x => x);
            }
            catch(OperationCanceledException)
            {
                return true;
            }
            catch(Exception e)
            {
                TestHarness.TestLog("  > Failed: got {0}, expected OperationCanceledException", e.GetType().ToString());
                return false;
            }

            TestHarness.TestLog("  > Failed: no exception occured, expected OperationCanceledException");
            return false;
        }

       


        // After running a plinq query, we expect the internal cancellation token source to have been disposed.
        // The critical thing is that there are no callbacks left hanging on the external tokens callback list, else we 
        // could be causing an accumulation of junk that will not GC until the external token goes away (possibly never).
        private static bool Bugfix626345_PlinqShouldDisposeLinkedToken()
        {
            bool passed = true;
            TestHarness.TestLog("* PlinqCancellationTests.Bugfix626345_PlinqShouldDisposeLinkedToken()");

#if DEBUG
            // Only run these in debug mode, because they rely on a debug-only property.
            PropertyInfo callbackCountProperty =
                typeof(CancellationTokenSource).
                    GetProperty("CallbackCount", BindingFlags.Instance | BindingFlags.NonPublic);
            if (callbackCountProperty == null)
            {
                TestHarness.TestLog("    - Error: CancellationTokenSource.CallbackCount property not found; was it removed?");
                return false;
            }
            Func<CancellationTokenSource, int> getCallbackCount =
                _ => (int)callbackCountProperty.GetValue(_, null);

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;

            // ForAll
            Enumerable.Range(1, 10).AsParallel().WithCancellation(ct).ForAll((x) => { });
            passed &= TestHarnessAssert.IsTrue(getCallbackCount(cts) == 0, "The callback list should be empty.");

            // Built-in-aggregation
            Enumerable.Range(1, 10).AsParallel().WithCancellation(ct).Average();
            passed &= TestHarnessAssert.IsTrue(getCallbackCount(cts) == 0, "The callback list should be empty.");

            // Manual aggregation
            Enumerable.Range(1, 10).AsParallel().WithCancellation(ct).Aggregate((a, b) => a + b);
            passed &= TestHarnessAssert.IsTrue(getCallbackCount(cts) == 0, "The callback list should be empty.");

            // ToArray  (uses ToList, which enumerates the query with foreach())
            Enumerable.Range(1, 10).AsParallel().WithCancellation(ct).ToArray();
            passed &= TestHarnessAssert.IsTrue(getCallbackCount(cts) == 0, "The callback list should be empty.");

            // AsynchronousChannelMergeEnumerator
            foreach (int x in Enumerable.Range(1, 10).AsParallel().WithCancellation(ct).Select(xx => xx))
            {

            }
            passed &= TestHarnessAssert.IsTrue(getCallbackCount(cts) == 0, "The callback list should be empty.");

            // SynchronousChannelMergeEnumerator
            foreach (int x in Enumerable.Range(1, 10).AsParallel().WithCancellation(ct).WithMergeOptions(ParallelMergeOptions.FullyBuffered))
            {

            }
            passed &= TestHarnessAssert.IsTrue(getCallbackCount(cts) == 0, "The callback list should be empty.");

            // Fallback to sequential 
            foreach (int x in Enumerable.Range(1, 10).AsParallel().WithCancellation(ct).Where(xx => true).Skip(4))
            {

            }
            passed &= TestHarnessAssert.IsTrue(getCallbackCount(cts) == 0, "The callback list should be empty.");
#endif

            return passed;
        }

        // Plinq supresses OCE(externalCT) occuring in worker threads and then throws a single OCE(ct)
        // if a manual OCE(ct) is thrown but ct is not canceled, Plinq should not suppress it, else things
        // get confusing...
        // ONLY an OCE(ct) for ct.IsCancellationRequested=true is co-operative cancellation
        private static bool Bugfix632544_OnlySuppressOCEifCTCanceled()
        {
            bool passed = true;
#if !PFX_LEGACY_3_5
            TestHarness.TestLog("* PlinqCancellationTests.Bugfix632544_OnlySuppressOCEifCTCanceled()");

            AggregateException caughtException = null;
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken externalToken = cts.Token;
            try
            {
                Enumerable.Range(1, 10).AsParallel()
                    .WithCancellation(externalToken)
                    .Select(
                      x =>
                      {
                          if (x % 2 == 0) throw new OperationCanceledException(externalToken);
                          return x;
                      }
                    )
                 .ToArray();
            }
            catch(AggregateException ae)
            {
                caughtException = ae;
            }

            passed &= TestHarnessAssert.IsNotNull(caughtException, "We expect this OCE(ct) to merely be aggregated.");
#endif
            return passed;
        }


        // a specific repro where inner queries would see an ODE on the merged cancellation token source
        // when the implementation involved disposing and recreating the token on each worker thread
        private static bool Cancellation_ODEIssue()
        {
            bool passed = true;
            AggregateException caughtException = null;
            try
            {
                Enumerable.Range(0, 1999).ToArray()
                .AsParallel().AsUnordered()
                .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                .Zip<int, int, int>(
                    Enumerable.Range(1000, 20).Select<int, int>(_item => (int)_item).AsParallel().AsUnordered(),
                    (first, second) => { throw new OperationCanceledException(); })
               .ForAll(x => { });
            }
            catch (AggregateException ae)
            {
                caughtException = ae;
            }

            //the failure was an ODE coming out due to an ephemeral disposed merged cancellation token source.
            passed &= TestHarnessAssert.IsTrue(caughtException != null,
                                               "We expect an aggregate exception with OCEs in it.");

            return passed;
        }

        private static bool CancellationSequentialWhere()
        {
            TestHarness.TestLog("* PlinqCancellationTests.CancellationSequentialWhere()");
            IEnumerable<int> src = Enumerable.Repeat(0, int.MaxValue);
            CancellationTokenSource tokenSrc = new CancellationTokenSource();

            var q = src.AsParallel().WithCancellation(tokenSrc.Token).Where(x => false).TakeWhile(x => true);

            bool success = false;
            Task task = Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        foreach (var x in q) { }

                        TestHarness.TestLog("  > Failed: OperationCanceledException was not caught.");
                    }
                    catch (OperationCanceledException oce)
                    {
                        if (OCEHelper.ExtractCT(oce) == tokenSrc.Token)
                        {
                            success = true;
                        }
                        else
                        {
                            TestHarness.TestLog("  > Failed: Wrong cancellation token.");
                        }
                    }
                }
            );

            // We wait for 100 ms. If we canceled the token source immediately, the cancellation
            // would occur at the query opening time. The goal of this test is to test cancellation
            // at query execution time.
            Thread.Sleep(100);

            tokenSrc.Cancel();
            task.Wait();

            return success;
        }


        private static bool CancellationSequentialElementAt()
        {
            TestHarness.TestLog("* PlinqCancellationTests.CancellationSequentialElementAt()");
            IEnumerable<int> src = Enumerable.Repeat(0, int.MaxValue);
            CancellationTokenSource tokenSrc = new CancellationTokenSource();

            bool success = false;
            Task task = Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        int res = src.AsParallel()
                            .WithCancellation(tokenSrc.Token)
                            .Where(x => true)
                            .TakeWhile(x => true)
                            .ElementAt(int.MaxValue - 1);

                        TestHarness.TestLog("  > Failed: OperationCanceledException was not caught.");
                    }
                    catch (OperationCanceledException oce)
                    {
                        if (OCEHelper.ExtractCT(oce) == tokenSrc.Token)
                        {
                            success = true;
                        }
                        else
                        {
                            TestHarness.TestLog("  > Failed: Wrong cancellation token.");
                        }
                    }
                }
            );

            // We wait for 100 ms. If we canceled the token source immediately, the cancellation
            // would occur at the query opening time. The goal of this test is to test cancellation
            // at query execution time.
            Thread.Sleep(100);

            tokenSrc.Cancel();
            task.Wait();

            return success;
        }


        private static bool CancellationSequentialDistinct()
        {
            TestHarness.TestLog("* PlinqCancellationTests.CancellationSequentialDistinct()");
            IEnumerable<int> src = Enumerable.Repeat(0, int.MaxValue);
            CancellationTokenSource tokenSrc = new CancellationTokenSource();

            bool success = false;
            Task task = Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        var q = src.AsParallel()
                            .WithCancellation(tokenSrc.Token)
                            .Distinct()
                            .TakeWhile(x => true);

                        foreach (var x in q) { }

                        TestHarness.TestLog("  > Failed: OperationCanceledException was not caught.");
                    }
                    catch (OperationCanceledException oce)
                    {
                        if (OCEHelper.ExtractCT(oce) == tokenSrc.Token)
                        {
                            success = true;
                        }
                        else
                        {
                            TestHarness.TestLog("  > Failed: Wrong cancellation token.");
                        }
                    }
                }
            );

            // We wait for 100 ms. If we canceled the token source immediately, the cancellation
            // would occur at the query opening time. The goal of this test is to test cancellation
            // at query execution time.
            Thread.Sleep(100);

            tokenSrc.Cancel();
            task.Wait();

            return success;
        }

        //CancellationChanges introduce a bug causing ODE if a queryEnumator is disposed before moveNext is called.
        private static bool Bugfix638383_ImmediateDispose()
        {
            const bool passed = true;
            TestHarness.TestLog("* PlinqCancellationTests.Bugfix638383_ImmediateDispose()");

            var queryEnumerator = Enumerable.Range(1, 10).AsParallel().Select(x => x).GetEnumerator();
            queryEnumerator.Dispose();

            return passed; // success is not throwing an exception.
        }

        // REPRO 1 -- cancellation
        private static bool Bugfix640886_SetOperationsThrowAggregateOnCancelOrDispose_1()
        {
            bool passed = true;
            TestHarness.TestLog("* PlinqCancellationTests.Bugfix640886_SetOperationsThrowAggregateOnCancelOrDispose_1()");

            var mre = new ManualResetEvent(false);
            var plinq_src =
                Enumerable.Range(0, 5000000).Select(x =>
                {
                    if (x == 0) mre.Set();
                    return x;
                });

            Task t = null;
            try
            {
                CancellationTokenSource cs = new CancellationTokenSource();
                var plinq = plinq_src
                    .AsParallel().WithCancellation(cs.Token)
                    .WithDegreeOfParallelism(1)
                    .Union(Enumerable.Range(0, 10).AsParallel());

                var walker = plinq.GetEnumerator();

                t = Task.Factory.StartNew(() =>
                {
                    mre.WaitOne();
                    cs.Cancel();
                });
                while (walker.MoveNext())
                {
                    Thread.Sleep(1);
                    var item = walker.Current;
                }
                walker.MoveNext();
                passed &= TestHarnessAssert.Fail("OperationCanceledException was expected, but no exception occured.");
            }
            catch (OperationCanceledException)
            {
                //This is expected.                
            }

            catch (Exception e)
            {
                passed &= TestHarnessAssert.Fail("OperationCanceledException was expected, but a different exception occured.");
                TestHarness.TestLog(e.ToString());
            }

            if (t != null) t.Wait();


            return passed;
        }

        // throwing a fake OCE(ct) when the ct isn't canceled should produce an AggregateException.
        private static bool Bugfix640886_SetOperationsThrowAggregateOnCancelOrDispose_2()
        {
            bool passed = true;
            TestHarness.TestLog("* PlinqCancellationTests.Bugfix640886_SetOperationsThrowAggregateOnCancelOrDispose_2()");

           try
            {
                CancellationTokenSource cs = new CancellationTokenSource();
                var plinq = Enumerable.Range(0, 50)
                    .AsParallel().WithCancellation(cs.Token)
                    .WithDegreeOfParallelism(1)
#if PFX_LEGACY_3_5
                    .Union(Enumerable.Range(0, 10).AsParallel().Select<int,int>(x=>{throw new OperationCanceledException();}));
#else
                    .Union(Enumerable.Range(0, 10).AsParallel().Select<int, int>(x => { throw new OperationCanceledException(cs.Token); }));
#endif


                var walker = plinq.GetEnumerator();
                while (walker.MoveNext())
                {
                    Thread.Sleep(1);
                }
                walker.MoveNext();
                passed &= TestHarnessAssert.Fail("AggregateException was expected, but no exception occured.");
            }
            catch (OperationCanceledException)
            {
                passed &= TestHarnessAssert.Fail("AggregateExcption was expected, but an OperationCanceledException occured.");
            }
            catch (AggregateException)
            {
                // expected
            }

            catch (Exception e)
            {
                passed &= TestHarnessAssert.Fail("AggregateExcption was expected, but some other exception occured.");
                TestHarness.TestLog(e.ToString());
            }

            return passed;
        }


        
        // Changes made to hash-partitioning (April'09) lost the cancellation checks during the 
        // main repartitioning loop (matrix building).
        private static bool Bug667799_HashPartitioningCancellation()
        {
            bool passed = true;
            OperationCanceledException caughtException = null;
            TestHarness.TestLog("* PlinqCancellationTests.Bug667799_HashPartitioningCancellation()");

            CancellationTokenSource cs = new CancellationTokenSource();

            //Without ordering
            var queryUnordered = Enumerable.Range(0, int.MaxValue)
                .Select(x => { if (x == 0) cs.Cancel(); return x; })
                .AsParallel()
                .WithCancellation(cs.Token)
                .Intersect(Enumerable.Range(0, 1000000).AsParallel());

            try
            {
                foreach (var item in queryUnordered)
                {
                }

            }
            catch (OperationCanceledException oce)
            {
                caughtException = oce;
            }

            passed &= TestHarnessAssert.IsNotNull(caughtException, "(unordered case) The query execution should be canceled.");

            caughtException = null;

            //With ordering
            var queryOrdered = Enumerable.Range(0, int.MaxValue)
               .Select(x => { if (x == 0) cs.Cancel(); return x; })
               .AsParallel().AsOrdered()
               .WithCancellation(cs.Token)
               .Intersect(Enumerable.Range(0, 1000000).AsParallel());

            try
            {
                foreach (var item in queryOrdered)
                {
                }

            }
            catch (OperationCanceledException oce)
            {
                caughtException = oce;
            }

            passed &= TestHarnessAssert.IsNotNull(caughtException, "(ordered case) The query execution should be canceled.");
            return passed;
        }

        // If a query is cancelled and immediately disposed, the dispose should not throw an OCE.
        private static bool Bug695173_CancelThenDispose()
        {
            TestHarness.TestLog("* PlinqCancellationTests.Bug695173_CancelThenDispose()");
            try
            {
                CancellationTokenSource cancel = new CancellationTokenSource();
                var q = ParallelEnumerable.Range(0, 1000).WithCancellation(cancel.Token).Select(x => x);
                IEnumerator<int> e = q.GetEnumerator();
                e.MoveNext();

                cancel.Cancel();
                e.Dispose();
            }
            catch (Exception e)
            {
                TestHarness.TestLog("> Failed. Expected no exception, got " + e.GetType());
                return false;
            }
            return true;
        }

        private static bool Bug702254_CancellationCausingNoDataMustThrow()
        {
            TestHarness.TestLog("* PlinqCancellationTests.Bug702254_CancellationCausingNoDataMustThrow()");
            bool passed = true;
            OperationCanceledException oce = null;
            
            CancellationTokenSource cs = new CancellationTokenSource();
            
            var query = Enumerable.Range(0, 100000000)
            .Select(x =>
            {
                if (x == 0) cs.Cancel();
                return x;
            })
            .AsParallel()
            .WithCancellation(cs.Token)
            .Select(x=>x);

            try
            {
                foreach (var item in query) //We expect an OperationCancelledException during the MoveNext
                {
                }
            }
            catch(OperationCanceledException ex)
            {
                oce = ex;
            }

            passed &= TestHarnessAssert.IsNotNull(oce, "An OCE should be thrown.");
            return passed;
        }

        private static bool Bug702254Related_DontDoWorkIfTokenAlreadyCanceled()
        {
            TestHarness.TestLog("* PlinqCancellationTests.Bug702254Related_DontDoWorkIfTokenAlreadyCanceled()");
            bool passed = true;
            OperationCanceledException oce = null;
            
            CancellationTokenSource cs = new CancellationTokenSource();
            var query = Enumerable.Range(0, 100000000)
            .Select(x =>
            {
                if(x > 0) // to avoid the "Error:unreachable code detected"
                    throw new ApplicationException("User-delegate exception.");
                return x;
            })
            .AsParallel()
            .WithCancellation(cs.Token)
            .Select(x=>x);

            cs.Cancel();
            try
            {
                foreach (var item in query) //We expect an OperationCancelledException during the MoveNext
                {
                }
            }
            catch(OperationCanceledException ex)
            {
                oce = ex;
            }

            passed &= TestHarnessAssert.IsNotNull(oce, "An OCE should be thrown.");
            return passed;
        }

        // To help the user, we will check if a cancellation token passed to WithCancellation() is 
        // not backed by a disposed CTS.  This will help them identify incorrect cts.Dispose calls, but 
        // doesn't solve all their problems if they don't manage CTS lifetime correctly.
        // We test via a few random queries that have shown inconsistent behaviour in the past.
        private static bool Bug720598_PreDisposedCTSPassedToPlinq()
        {
            bool passed = true;
            ArgumentException ae1 = null;
            ArgumentException ae2 = null;
            ArgumentException ae3 = null;
            TestHarness.TestLog("* PlinqCancellationTests.Bug720598_PreDisposedCTSPassedToPlinq()");

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;
            cts.Dispose();  // Early dispose
            try
            {
                Enumerable.Range(1, 10).AsParallel()
                    .WithCancellation(ct)
                    .OrderBy(x => x)
                    .ToArray();
            }
            catch (Exception ex)
            {
                ae1 = (ArgumentException)ex;
            }

            try
            {
                Enumerable.Range(1, 10).AsParallel()
                    .WithCancellation(ct)
                    .Last();
            }
            catch (Exception ex)
            {
                ae2 = (ArgumentException)ex;
            }

            try
            {
                Enumerable.Range(1, 10).AsParallel()
                    .WithCancellation(ct)
                    .OrderBy(x => x)
                    .Last();
            }
            catch (Exception ex)
            {
                ae3 = (ArgumentException)ex;
            }

            passed &= TestHarnessAssert.IsNotNull(ae1, "An AE should have been thrown[1].");
            passed &= TestHarnessAssert.IsNotNull(ae2, "An AE should have been thrown[2].");
            passed &= TestHarnessAssert.IsNotNull(ae3, "An AE should have been thrown[3].");

            return passed;
        }
    }

    // ---------------------------
    // Helper classes
    // ---------------------------

    internal class ThrowOnFirstEnumerable<T> : IEnumerable<T>
    {

        private readonly IEnumerable<T> _innerEnumerable;

        public ThrowOnFirstEnumerable(IEnumerable<T> innerEnumerable)
        {
            _innerEnumerable = innerEnumerable;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new ThrowOnFirstEnumerator<T>(_innerEnumerable.GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    
    internal class ThrowOnFirstEnumerator<T> : IEnumerator<T>
    {
        private IEnumerator<T> _innerEnumerator;
        
        public ThrowOnFirstEnumerator(IEnumerator<T> sourceEnumerator)
        {
            _innerEnumerator = sourceEnumerator;
        }

        public void Dispose()
        {
            _innerEnumerator.Dispose();
        }

        public bool MoveNext()
        {
            throw new InvalidOperationException("ThrowOnFirstEnumerator throws on the first MoveNext");
        }

        public T Current
        {
            get { return _innerEnumerator.Current; }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public void Reset()
        {
            _innerEnumerator.Reset();
        }
    }
}
