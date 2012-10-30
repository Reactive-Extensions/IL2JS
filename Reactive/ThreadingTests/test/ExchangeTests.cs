using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Parallel;

namespace plinq_devtests
{
    class ExchangeTests
    {

        internal static bool RunPartitionMergeTests()
        {
            bool passed = true;

            for (int i = 1; i <= 128; i *= 2)
            {
                passed &= SimplePartitionMergeWhereScanTest1(1024 * 8, i, true);
                passed &= SimplePartitionMergeWhereScanTest1(1024 * 8, i, false);
                passed &= SimplePartitionMergeWhereScanTest1(1024 * 1024 * 2, i, true);
                passed &= SimplePartitionMergeWhereScanTest1(1024 * 1024 * 2, i, false);

                if (!passed)
                    break;
            }

            passed &= CheckWhereSelectComposition(1024 * 8);

            passed &= PartitioningTest(true, 2, 0, 900);
            passed &= PartitioningTest(false, 2, 0, 900);

            passed &= PartitioningTest(true, 1, 0, 10);
            passed &= PartitioningTest(false, 1, 0, 10);

            passed &= PartitioningTest(true, 1, 0, 500);
            passed &= PartitioningTest(false, 1, 0, 520);

            passed &= PartitioningTest(true, 4, 0, 500);
            passed &= PartitioningTest(false, 4, 0, 520);

            passed &= OrderedPipeliningTest1(4, true);
            passed &= OrderedPipeliningTest1(4, false);
            passed &= OrderedPipeliningTest1(10007, true);
            passed &= OrderedPipeliningTest1(10007, false);

            passed &= OrderedPipeliningTest2(true);
            passed &= OrderedPipeliningTest2(false);

            passed &= SequentialPipeliningTest(1007, false, false);
            passed &= SequentialPipeliningTest(1007, false, true);
            passed &= SequentialPipeliningTest(1007, true, false);
            passed &= SequentialPipeliningTest(1007, true, true);

            passed &= RunAsEnumerableTest();

            return passed;
        }

        private static bool SimplePartitionMergeWhereScanTest1(int dataSize, int partitions, bool pipeline)
        {
            TestHarness.TestLog("SimplePartitionMergeWhereScanTest1: {0}, {1}, {2}", dataSize, partitions, pipeline);

            int[] data = new int[dataSize];
            for (int i = 0; i < dataSize; i++) data[i] = i;

            WhereQueryOperator<int> whereOp = new WhereQueryOperator<int>(
                data, delegate(int x) { return (x % 2) == 0; }); // select only even elements

            IEnumerator<int> stream = whereOp.GetEnumerator();

            int count = 0;
            while (stream.MoveNext())
            {
                // @TODO: verify all the elements we expected are present.
                count++;
            }

            bool passed = count == (dataSize / 2);
            TestHarness.TestLog("  > count == dataSize/2? i.e. {0} == {1}? {2}", count, dataSize/2, passed);
            return passed;
        }


        private static bool CheckWhereSelectComposition(int dataSize)
        {
            TestHarness.TestLog("CheckWhereSelectComposition: {0}", dataSize);

            int[] data = new int[dataSize];
            for (int i = 0; i < dataSize; i++) data[i] = i;

            WhereQueryOperator<int> whereOp = new WhereQueryOperator<int>(
                data, delegate(int x) { return (x % 2) == 0; }); // select only even elements
            SelectQueryOperator<int, int> selectOp = new SelectQueryOperator<int, int>(
                whereOp, delegate(int x) { return x * 2; }); // just double the elements

            bool passed = true;

            // Verify composition of the tree:
            //     SELECT <-- WHERE <-- SCAN <-- {data}

            SelectQueryOperator<int, int> sel = selectOp as SelectQueryOperator<int, int>;
            passed &= sel != null;
            TestHarness.TestLog("  > {0}: SELECT is non-null", passed);

            WhereQueryOperator<int> where = sel.Child as WhereQueryOperator<int>;
            passed &= where != null;
            TestHarness.TestLog("  > {0}: WHERE is non-null", passed);

            ScanQueryOperator<int> scan = where.Child as ScanQueryOperator<int>;
            passed &= scan != null;
            TestHarness.TestLog("  > {0}: SCAN is non-null", passed);

            // Now verify the output is what we expect.

            int expectSum = 0;
            for (int i = 0; i < dataSize; i++)
                if ((i % 2) == 0)
                    expectSum += (i * 2);

            int realSum = 0;
            IEnumerator<int> e = selectOp.GetEnumerator();
            while (e.MoveNext())
            {
                realSum += e.Current;
            }

            passed = (realSum == expectSum);

            TestHarness.TestLog("  > {0}: actual sum {1} == expected sum {2}?", passed, realSum, expectSum);          

            return passed;
        }

        private static bool PartitioningTest(bool stripedPartitioning, int partitions, int minLen, int maxLen)
        {
            TestHarness.TestLog("PartitioningTest: {0}, {1}, {2}, {3}", stripedPartitioning, partitions, minLen, maxLen);

            for (int len = minLen; len < maxLen; len++)
            {
                int[] arr = Enumerable.Range(0, len).ToArray();
                IEnumerable<int> query;

                if (stripedPartitioning)
                {
                    query = arr.AsParallel().AsOrdered().WithDegreeOfParallelism(partitions).Take(len).Select(i => i);
                }
                else
                {
                    query = arr.AsParallel().AsOrdered().WithDegreeOfParallelism(partitions).Select(i => i);
                }

                if (!arr.SequenceEqual(query))
                {
                    TestHarness.TestLog("  ** FAILED: incorrect output for array of length {0}", len);
                    return false;
                }
            }

            TestHarness.TestLog("  ** Success");
            return true;
        }

        /// <summary>
        /// Checks whether an ordered pipelining merge produces the correct output.
        /// </summary>
        private static bool OrderedPipeliningTest1(int dataSize, bool buffered)
        {
            TestHarness.TestLog("OrderedPipeliningTest1: dataSize={0}, buffered={1}", dataSize, buffered);
            ParallelMergeOptions merge = buffered ? ParallelMergeOptions.FullyBuffered : ParallelMergeOptions.NotBuffered;

            IEnumerable<int> src = Enumerable.Range(0, dataSize);
            if (!Enumerable.SequenceEqual(src.AsParallel().AsOrdered().WithMergeOptions(merge).Select(x => x), src))
            {
                TestHarness.TestLog("> FAILED: Incorrect output.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether an ordered pipelining merge pipelines the results
        /// instead of running in a stop-and-go fashion.
        /// </summary>
        private static bool OrderedPipeliningTest2(bool buffered)
        {
            TestHarness.TestLog("OrderedPipeliningTest2: buffered={0}", buffered);
            ParallelMergeOptions merge = buffered ? ParallelMergeOptions.AutoBuffered : ParallelMergeOptions.NotBuffered;

            IEnumerable<int> src = Enumerable.Range(0, int.MaxValue)
                .Select(x => { if (x == 1000000) throw new Exception(); return x; });


            try
            {
                int expect = 0;
                int got = Enumerable.First(src.AsParallel().AsOrdered().WithMergeOptions(merge).Select(x => x));
                if (got != expect)
                {
                    TestHarness.TestLog("> FAILED: Expected {0}, got {1}.", expect, got);
                    return false;
                }
            }
            catch (Exception e)
            {
                TestHarness.TestLog("> FAILED: Caught an exception: {0}.", e.GetType());
            }

            return true;
        }

        /// <summary>
        /// Verifies that a pipelining merge does not create any helper tasks in the DOP=1 case.
        /// </summary>
        private static bool SequentialPipeliningTest(int inputSize, bool buffered, bool ordered)
        {
            TestHarness.TestLog("SequentialPipeliningTest: inputSize={0}, buffered={1}, ordered={2}", inputSize, buffered, ordered);
            ParallelMergeOptions merge = buffered ? ParallelMergeOptions.AutoBuffered : ParallelMergeOptions.NotBuffered;

            bool success = true;
            int consumerThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            System.Linq.ParallelQuery<int> src =
                System.Linq.ParallelEnumerable.Range(0, inputSize);
            if (ordered)
            {
                src = src.AsOrdered();
            }

            src = 
                src.WithMergeOptions(merge)
                .WithDegreeOfParallelism(1)
                .Select(
                    x =>
                    {
                        if (System.Threading.Thread.CurrentThread.ManagedThreadId != consumerThreadId)
                        {
                            success = false;
                        }
                        return x;
                    });

            foreach (var x in src) { }

            if (!success)
            {
                TestHarness.TestLog("> The producer task executed on a wrong thread.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Verifies that AsEnumerable causes subsequent LINQ operators to bind to LINQ-to-objects
        /// </summary>
        /// <returns></returns>
        private static bool RunAsEnumerableTest()
        {
            TestHarness.TestLog("AsEnumerableTest()");
            IEnumerable<int> src = Enumerable.Range(0, 100).AsParallel().AsEnumerable().Select(x => x);

            bool passed = !(src is ParallelQuery<int>);

            if (!passed) TestHarness.TestLog("> Failed. AsEnumerable() didn't prevent the Select operator from binding to PLINQ.");
            return passed;
        }
    }
}
