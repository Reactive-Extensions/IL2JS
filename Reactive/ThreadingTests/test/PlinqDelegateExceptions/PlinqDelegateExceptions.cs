// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
//
// <OWNER>mikelid</OWNER>
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using plinq_devtests.PLinqDelegateExceptions;

namespace plinq_devtests
{
    /// <summary>
    /// public so that the tests can also be called by other exe/wrappers eg VSTS test harness.
    /// </summary>
    public static class PlinqDelegateExceptions
    {
        public static bool RunUserDelegateExceptionTests()
        {
            bool passed = true;

            // ensure partitioners (plinq or manual) do not enumerate after disposing the source, else unexpected ODEs will be reported.
            passed &= Bug599487_PlinqChunkPartitioner_DontEnumerateAfterException();
            passed &= Bug599487_ManualChunkPartitioner_DontEnumerateAfterException();
            passed &= Bug599487_MoveNextAfterQueryOpeningFailsIsIllegal();

            // a couple of random operators.
            passed &= DistintOrderBySelect();
            passed &= SelectJoin();

            // the orderby was a particular problem for the June 2008 CTP.
            passed &= OrderBy(10);
            passed &= OrderBy(100);
            passed &= OrderBy(1000);

            // and try situations where only one user delegate exception occurs
            passed &= OrderBy_OnlyOneException(10);
            passed &= OrderBy_OnlyOneException(100);
            passed &= OrderBy_OnlyOneException(1000);

            // zip and ordering was also broken in June 2008 CTP, but this was due to the ordering component.
            passed &= ZipAndOrdering(10);
            passed &= ZipAndOrdering(100);
            passed &= ZipAndOrdering(1000);

            passed &= OperationCanceledExceptionsGetAggregated();

            

            return passed;
        }

        

        /// <summary>
        /// A basic test for a query that throws user delegate exceptions
        /// </summary>
        /// <returns></returns>
        internal static bool DistintOrderBySelect()
        {
            TestHarness.TestLog("* DistintOrderBySelect()");

            Exception caughtAggregateException = null;
            try
            {
                var query2 = new int[] { 1, 2, 3 }.AsParallel()
                    .Distinct()
                    .OrderBy(i => i)
                    .Select(i => UserDelegateException.Throw<int, int>(i));
                foreach (var x in query2)
                {
                }
            }
            catch (AggregateException e)
            {
                caughtAggregateException = e;
            }

            return caughtAggregateException != null;
        }


        /// <summary>
        /// Another basic test for a query that throws user delegate exceptions
        /// </summary>
        /// <returns></returns>
        internal static bool SelectJoin()
        {
            TestHarness.TestLog("* SelectJoin()");
            Exception caughtAggregateException = null;

            try
            {
                var query = new int[] { 1, 2, 3 }.AsParallel()
                    .Select( i => UserDelegateException.Throw<int, int>(i))
                    .Join(new int[] { 1 }.AsParallel(), i => i, j => j, (i, j) => 0);
                foreach (var x in query)
                {
                }
            }
            catch (AggregateException e)
            {
                caughtAggregateException = e;
            }

            return caughtAggregateException != null;
        }

        /// <summary>
        /// Heavily exercises OrderBy in the face of user-delegate exceptions. 
        /// On CTP-M1, this would deadlock for DOP=7,9,11,... on 4-core, but works for DOP=1..6 and 8,10,12, ...
        /// 
        /// In this test, every call to the key-selector delegate throws.
        /// </summary>
        internal static bool OrderBy(int range)
        {
            TestHarness.TestLog(String.Format("* OrderBy({0})", range));
            bool success = true;

            Console.Write("       DOP: ");
            for (int dop = 1; dop <= 30; dop++)
            {
                Console.Write(dop.ToString("00") + ".. ");
                if (dop % 10 == 0)
                    Console.Write(Environment.NewLine + "            ");
                    
                AggregateException caughtAggregateException = null;

                try
                {
                    var query = Enumerable.Range(0, range)
                        .AsParallel().WithDegreeOfParallelism(dop)
                        .OrderBy(i => UserDelegateException.Throw<int, int>(i));
                    foreach (int i in query)
                    {
                    }
                }
                catch (AggregateException e)
                {
                    caughtAggregateException = e;
                }

                success &= (caughtAggregateException != null);
            }

            Console.WriteLine();
            return success;
        }

        /// <summary>
        /// Heavily exercises OrderBy, but only throws one user delegate exception to simulate an occassional failure.
        ///
        /// </summary>
        internal static bool OrderBy_OnlyOneException(int range)
        {
            TestHarness.TestLog(String.Format("* OrderBy_OnlyOneException({0})", range));
            bool success = true;

            Console.Write("       DOP: ");
            for (int dop = 1; dop <= 30; dop++)
            {
                Console.Write(dop.ToString("00") + ".. ");
                if (dop % 10 == 0)
                    Console.Write(Environment.NewLine + "            ");
                int indexForException = range/(2*dop); // eg 1000 items on 4-core, throws on item 125.

                AggregateException caughtAggregateException = null;

                try
                {
                    var query = Enumerable.Range(0, range)
                        .AsParallel().WithDegreeOfParallelism(dop)
                        .OrderBy(i => { 
                            UserDelegateException.ThrowIf(i == indexForException);
                            return i; }
                         );
                    foreach (int i in query)
                    {
                    }
                }
                catch (AggregateException e)
                {
                    caughtAggregateException = e;
                }


                success &= (caughtAggregateException != null);
                success &= (caughtAggregateException.InnerExceptions.Count == 1);
            }

            Console.WriteLine();
            return success;
        }

        /// <summary>
        /// Zip with ordering on showed issues, but it was due to the ordering component.
        /// This is included as a regression test for that particular repro.
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        internal static bool ZipAndOrdering(int range)
        {
            TestHarness.TestLog(String.Format("* ZipAndOrdering({0})", range));
            bool success = true;
            Console.Write("       DOP: ");
            for (int dop = 1; dop <= 30; dop++)
            {
                Console.Write(dop.ToString("00") + ".. ");
                if (dop % 10 == 0)
                    Console.Write(Environment.NewLine + "            ");
                AggregateException ex = null;
                try
                {
                    var enum1 = Enumerable.Range(1, range);
                    var enum2 = Enumerable.Repeat(1, range*2);

                    var query1 = enum1
                        .AsParallel()
                        .AsOrdered().WithDegreeOfParallelism(dop)
                        .Zip(enum2.AsParallel().AsOrdered(),
                             (a, b) => UserDelegateException.Throw<int, int, int>(a, b));

                    var output = query1.ToArray();
                }
                catch (AggregateException ae)
                {
                    ex = ae;
                }

                
                success &= (ex != null);
                success &= (false ==
                            PlinqDelegateExceptionHelpers.AggregateExceptionContains(ex,
                                                                                     typeof (OperationCanceledException)));
                success &=
                    (PlinqDelegateExceptionHelpers.AggregateExceptionContains(ex, typeof (UserDelegateException)));
            }
            Console.WriteLine();
            return success;
        }

        /// <summary>
        /// If the user delegate throws an OperationCanceledException, it should get aggregated.
        /// </summary>
        /// <returns></returns>
        internal static bool OperationCanceledExceptionsGetAggregated()
        {
            TestHarness.TestLog("* OperationCanceledExceptionsGetAggregated()");
            AggregateException caughtAggregateException = null;
            try
            {
                var enum1 = Enumerable.Range(1, 13);

                var query1 =
                    enum1
                        .AsParallel()
                        .Select<int,int>(i => { throw new OperationCanceledException();});
                var output = query1.ToArray();
            }
            catch (AggregateException ae)
            {
                caughtAggregateException = ae;
            }

            bool success = true;
            success &= caughtAggregateException != null;
            success &= PlinqDelegateExceptionHelpers.AggregateExceptionContains(caughtAggregateException,
                                                                                typeof (OperationCanceledException));
            return success;
        }

        /// <summary>
        /// The plinq chunk partitioner calls takes an IEnumerator over the source, and disposes the enumerator when it is
        /// finished.
        /// If an exception occurs, the calling enumerator disposes the enumerator... but then other callers generate ODEs.
        /// These ODEs either should not occur (prefered), or should not get into the aggregate exception.
        /// 
        /// Also applies to the standard stand-alone chunk partitioner.
        /// Does not apply to other partitioners unless an exception in one consumer would cause flow-on exception in others.
        /// </summary>
        /// <returns></returns>
        private static bool Bug599487_PlinqChunkPartitioner_DontEnumerateAfterException()
        {
            TestHarness.TestLog("* Bug599487_PlinqChunkPartitioner_DontEnumerateAfterException()");
            bool success = true;
            try
            {
                Enumerable.Range(1, 10)
                    .AsParallel()
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select(x => { if (x == 4) throw new ApplicationException("manual exception"); return x; })
                    .Zip(Enumerable.Range(1, 10).AsParallel(), (a, b) => a + b)
                 .AsParallel()
                 .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                 .ToArray();
            }
            catch (AggregateException e)
            {
                if (!e.Flatten().InnerExceptions.All(ex => ex.GetType() == typeof(ApplicationException)))
                {
                    success = false;
                    TestHarness.TestLog("  FAIL. only a single ApplicationException should appear in the aggregate:");
                    foreach (var exception in e.Flatten().InnerExceptions)
                    {
                        TestHarness.TestLog("     exception = " + exception);
                    }
                }
            }

            return success;
        }

        /// <summary>
        /// The stand-alone chunk partitioner calls takes an IEnumerator over the source, and disposes the enumerator when it is
        /// finished.
        /// If an exception occurs, the calling enumerator disposes the enumerator... but then other callers generate ODEs.
        /// These ODEs either should not occur (prefered), or should not get into the aggregate exception.
        /// 
        /// Also applies to the plinq stand-alone chunk partitioner.
        /// Does not apply to other partitioners unless an exception in one consumer would cause flow-on exception in others.
        /// </summary>
        /// <returns></returns>
        private static bool Bug599487_ManualChunkPartitioner_DontEnumerateAfterException()
        {
            TestHarness.TestLog("* Bug599487_ManualChunkPartitioner_DontEnumerateAfterException()");
            bool success = true;
            try
            {
                Partitioner.Create(
                    Enumerable.Range(1, 10)
                        .AsParallel()
                        .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                        .Select(x => { if (x == 4) throw new ApplicationException("manual exception"); return x; })
                        .Zip(Enumerable.Range(1, 10).AsParallel(), (a, b) => a + b)
                    )
                    .AsParallel()
                    .ToArray();
            }
            catch (AggregateException e)
            {
                if (!e.Flatten().InnerExceptions.All(ex => ex.GetType() == typeof(ApplicationException)))
                {
                    success = false;
                    TestHarness.TestLog("  FAIL. only a single ApplicationException should appear in the aggregate:");
                    foreach (var exception in e.Flatten().InnerExceptions)
                    {
                        TestHarness.TestLog("     exception = " + exception);
                    }
                }
            }

            return success;
        }

        private static bool Bug599487_MoveNextAfterQueryOpeningFailsIsIllegal()
        {
            TestHarness.TestLog("* MoveNextAfterQueryOpeningFailsIsIllegal()");
            bool success = true;
            var query = Enumerable.Range(0, 10)
                .AsParallel()
                .Select<int, int>(x => { throw new ApplicationException(); })
                .OrderBy(x=>x);
            
            IEnumerator<int> enumerator = query.GetEnumerator();

            //moveNext will cause queryOpening to fail (no element generated)
            success &= TestHarnessAssert.EnsureExceptionThrown(
                () => enumerator.MoveNext(),
                typeof(AggregateException), "An AggregateException(containing ApplicationException) should be thrown");
            
            //moveNext after queryOpening failed
            success &= TestHarnessAssert.EnsureExceptionThrown(
                () => enumerator.MoveNext(),
                typeof(InvalidOperationException), "A second attempt to enumerate should cause InvalidOperationException");

            return success;
        }
    }
}
