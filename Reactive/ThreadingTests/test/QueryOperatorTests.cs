using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Concurrent;

namespace plinq_devtests
{
    internal static class QueryOperatorTests
    {

        //
        // Aggregate
        //

        internal static bool RunAggregationTests()
        {
            bool passed = true;

            passed &= RunAggregationTest1_Sum1(1024);
            passed &= RunAggregationTest1_Sum2(1024);
            passed &= RunAggregationTest1_Sum3(1024);
            passed &= RunAggregationTest1_Sum4(1024);
            passed &= RunAggregationTest2(16);

            return passed;
        }

        private static bool RunAggregationTest1_Sum1(int count)
        {
            TestHarness.TestLog("* RunAggregationTest1_Sum1(count={0})", count);

            int expectSum = 0;
            int[] ints = new int[count];
            for (int i = 0; i < ints.Length; i++)
            {
                ints[i] = i;
                expectSum += i;
            }

            int realSum = ints.AsParallel().Aggregate<int>(
                delegate(int x, int y) { return x + y; });

            TestHarness.TestLog("  > Expect: {0}, real: {1}", expectSum, realSum);

            return realSum == expectSum;
        }

        private static bool RunAggregationTest1_Sum2(int count)
        {
            TestHarness.TestLog("* RunAggregationTest1_Sum2(count={0})", count);

            int expectSum = 0;
            int[] ints = new int[count];
            for (int i = 0; i < ints.Length; i++)
            {
                ints[i] = i;
                expectSum += i;
            }

            int realSum = ints.AsParallel().Aggregate<int, int>(
                0, delegate(int x, int y) { return x + y; });

            TestHarness.TestLog("  > Expect: {0}, real: {1}", expectSum, realSum);

            return realSum == expectSum;
        }

        private static bool RunAggregationTest1_Sum3(int count)
        {
            TestHarness.TestLog("* RunAggregationTest1_Sum3(count={0})", count);

            int expectSum = 0;
            int[] ints = new int[count];
            for (int i = 0; i < ints.Length; i++)
            {
                ints[i] = i;
                expectSum += i;
            }

            int realSum = ints.AsParallel().Aggregate<int, int, int>(
                0, delegate(int x, int y) { return x + y; }, delegate(int x) { return x; });

            TestHarness.TestLog("  > Expect: {0}, real: {1}", expectSum, realSum);

            return realSum == expectSum;
        }

        private class IntWrapper
        {
            public int Value { get; set; }
            public IntWrapper(int value)
            {
                Value = value;
            }
        }

        private static bool RunAggregationTest1_Sum4(int count)
        {
            TestHarness.TestLog("* RunAggregationTest1_Sum3(count={0})", count);

            int expectSum = 0;
            int[] ints = new int[count];
            for (int i = 0; i < ints.Length; i++)
            {
                ints[i] = i;
                expectSum += i;
            }

            int realSum = ints.AsParallel().Aggregate<int, IntWrapper, int>(
                () => new IntWrapper(0),
                (a,e) => { a.Value = a.Value + e; return a; },
                (a1,a2) => { a1.Value = a1.Value + a2.Value; return a1; },
                (a) => a.Value);

            TestHarness.TestLog("  > Expect: {0}, real: {1}", expectSum, realSum);

            return realSum == expectSum;
        }


        private static bool RunAggregationTest2(int count)
        {
            TestHarness.TestLog("* RunAggregationTest2(count={0})", count);

            int[] arr = new int[count];
            int count1 = arr.AsParallel().Select(x => x).Aggregate<int, int>(0, (acc, x) => acc + 1);
            int count2 = arr.AsParallel().Select(x => x).Aggregate<int,int,int>(0, (acc,x) => acc+1, res=>res);

            bool passed = true;
            if (count1 != arr.Length)
            {
                TestHarness.TestLog("  > Count1 expect: {0}, real: {1}", arr.Length, count1);
                passed = false;
            }

            if (count2 != arr.Length)
            {
                TestHarness.TestLog("  > Count2 expect: {0}, real: {1}", arr.Length, count2);
                passed = false;
            }

            return passed;
        }

        //
        // Sum
        //

        internal static bool RunSumTests()
        {
            bool passed = true;

            passed &= RunSumTest1(0);
            passed &= RunSumTest1(1024*8);
            passed &= RunSumTest1(1024*16);
            passed &= RunSumTestLongs1(0);
            passed &= RunSumTestLongs1(1024*8);
            passed &= RunSumTestLongs1(1024*1024);

            passed &= RunSumTestOrderBy1(0);
            passed &= RunSumTestOrderBy1(1024 * 8);

            return passed;
        }

        private static bool RunSumTest1(int count)
        {
            TestHarness.TestLog("* RunSumTest1(count={0})", count);
            int expectSum = 0;
            int[] ints = new int[count];
            for (int i = 0; i < ints.Length; i++)
            {
                ints[i] = i;
                expectSum += i;
            }

            int realSum = ints.AsParallel().Sum();

            TestHarness.TestLog("  > Expect: {0}, real: {1}", expectSum, realSum);

            return realSum == expectSum;
        }

        private static bool RunSumTestLongs1(int count)
        {
            TestHarness.TestLog("* RunSumTestLongs1(count={0})", count);
            long expectSum = 0;
            long[] longs = new long[count];
            for (int i = 0; i < longs.Length; i++)
            {
                longs[i] = i;
                expectSum += i;
            }

            long realSum = longs.AsParallel().Sum();

            TestHarness.TestLog("  > Expect: {0}, real: {1}", expectSum, realSum);

            return realSum == expectSum;
        }

        //
        // Tests summing an ordered list.
        //
        private static bool RunSumTestOrderBy1(int count)
        {
            TestHarness.TestLog("* RunSumTestOrderBy1(count={0})", count);
            int expectSum = 0;
            int[] ints = new int[count];
            for (int i = 0; i < ints.Length; i++)
            {
                ints[i] = i;
                expectSum += i;
            }

            int realSum = ints.AsParallel().OrderBy(x => x).Sum();

            TestHarness.TestLog("  > Expect: {0}, real: {1}", expectSum, realSum);

            return realSum == expectSum;
        }

        //
        // Average
        //

        internal static bool RunAvgTests()
        {
            bool passed = true;

            passed &= RunAvgTest1(1);
            passed &= RunAvgTest1(2);
            passed &= RunAvgTest1(4);
            passed &= RunAvgTest1(13);
            passed &= RunAvgTest1(1024*8);
            passed &= RunAvgTest1(1024*16);
            try {
                RunAvgTest1(0);// expect this to throw.
                passed = false; 
            } catch (InvalidOperationException) {
                passed &= true;
            }

            return passed;
        }

        private static bool RunAvgTest1(int count)
        {
            TestHarness.TestLog("* RunAvgTest1(count={0})", count);
            int[] ints = new int[count];
            double expectAvg = ((double)count - 1) / 2;

            for (int i = 0; i < ints.Length; i++) ints[i] = i;

            double realAvg = ints.AsParallel().Average();

            TestHarness.TestLog("  > Expect: {0}, real: {1}", expectAvg, realAvg);
            TestHarness.TestLog("  > LINQ says: {0}", Enumerable.Average(ints));

            return realAvg == expectAvg;
        }

        //
        // Min
        //

        internal static bool RunMinTests()
        {
            bool passed = true;

            passed &= RunMinTest1(0, 0);
            passed &= RunMinTest1(1, 0);
            passed &= RunMinTest1(2, 0);
            passed &= RunMinTest1(2, 1);
            passed &= RunMinTest1(1024 * 4, 1024 * 2 - 1);

            return passed;
        }

        private static bool RunMinTest1(int dataSize, int minSlot)
        {
            TestHarness.TestLog("* RunMinTest1(dataSize={0}, minSlot={1})", dataSize, minSlot);
            const int minNum = -100;
            int[] ints = new int[dataSize];

            for (int i = 0; i < ints.Length; i++) ints[i] = i;
            if (dataSize > 0) ints[minSlot] = minNum;

            bool passed = true;
            try
            {
                int min = ints.AsParallel().Min();
                if (dataSize == 0)
                {
                    passed = false;
                    TestHarness.TestLog("  > Expected an exception for empty input, got {0}", minNum);
                }
                else
                {
                    passed &= (minNum == min);
                    TestHarness.TestLog("  > Expect: {0}, real: {1}", minNum, min);
                }
            }
            catch (InvalidOperationException)
            {
                passed = (dataSize == 0);
                TestHarness.TestLog("  > Got an exception; expected? {0}  (dataSize=={1})", passed, dataSize);
            }

            return passed;
        }

        //
        // Max
        //

        internal static bool RunMaxTests()
        {
            bool passed = true;

            passed &= RunMaxTest1(0, 0);
            passed &= RunMaxTest1(1, 0);
            passed &= RunMaxTest1(2, 0);
            passed &= RunMaxTest1(2, 1);
            passed &= RunMaxTest1(1024 * 4, 1024 * 2 - 1);

            return passed;
        }

        private static bool RunMaxTest1(int dataSize, int maxSlot)
        {
            TestHarness.TestLog("* RunMaxTest1(dataSize={0}, maxSlot={1})", dataSize, maxSlot);
            int maxNum = dataSize + 100;
            int[] ints = new int[dataSize];

            for (int i = 0; i < ints.Length; i++) ints[i] = i;
            if (dataSize > 0) ints[maxSlot] = maxNum;

            bool passed = true;
            try
            {
                int max = ints.AsParallel().Max();
                if (dataSize == 0)
                {
                    passed = false;
                    TestHarness.TestLog("  > Expected an exception for empty input, got {0}", maxNum);
                }
                else
                {
                    passed &= (maxNum == max);
                    TestHarness.TestLog("  > Expect: {0}, real: {1}", maxNum, max);
                }
            }
            catch (InvalidOperationException)
            {
                passed = (dataSize == 0);
                TestHarness.TestLog("  > Got an exception; expected? {0}  (dataSize=={1})", passed, dataSize);
            }

            return passed;
        }

        //
        // Any and All
        //

        internal static bool RunAnyAllTests()
        {
            bool passed = true;

            passed &= RunAnyTest_AllFalse(1024);
            passed &= RunAnyTest_AllTrue(1024);
            passed &= RunAnyTest_OneTrue(1024, 512);
            passed &= RunAnyTest_OneTrue(1024, 0);
            passed &= RunAnyTest_OneTrue(1024, 1023);
            passed &= RunAnyTest_OneTrue(1024 * 1024, 1024 * 512);

            passed &= RunAllTest_AllFalse(1024);
            passed &= RunAllTest_AllTrue(1024);
            passed &= RunAllTest_OneFalse(1024, 512);
            passed &= RunAllTest_OneFalse(1024, 0);
            passed &= RunAllTest_OneFalse(1024, 1023);
            passed &= RunAllTest_OneFalse(1024 * 1024, 1024 * 512);

            return passed;
        }

        private static bool RunAnyTest_AllFalse(int size)
        {
            TestHarness.TestLog("* RunAnyTest_AllFalse(size={0})", size);
            bool[] bools = new bool[size];
            for (int i = 0; i < size; i++) bools[i] = false;

            bool expect = false;
            bool result = bools.AsParallel().Any(delegate(bool b) { return b; });

            TestHarness.TestLog("  > Expect: {0}, real: {1}", expect, result);

            return result == expect;
        }

        private static bool RunAnyTest_AllTrue(int size)
        {
            TestHarness.TestLog("* RunAnyTest_AllTrue(size={0})", size);
            bool[] bools = new bool[size];
            for (int i = 0; i < size; i++) bools[i] = true;

            bool expect = true;
            bool result = bools.AsParallel().Any(delegate(bool b) { return b; });

            TestHarness.TestLog("  > Expect: {0}, real: {1}", expect, result);

            return result == expect;
        }

        private static bool RunAnyTest_OneTrue(int size, int truePosition)
        {
            TestHarness.TestLog("* RunAnyTest_OneTrue(size={0}, truePosition={1})", size, truePosition);
            bool[] bools = new bool[size];
            for (int i = 0; i < size; i++) bools[i] = false;
            bools[truePosition] = true;

            bool expect = true;
            bool result = bools.AsParallel().Any(delegate(bool b) { return b; });

            TestHarness.TestLog("  > Expect: {0}, real: {1}", expect, result);

            return result == expect;
        }

        private static bool RunAllTest_AllFalse(int size)
        {
            TestHarness.TestLog("* RunAllTest_AllFalse(size={0})", size);
            bool[] bools = new bool[size];
            for (int i = 0; i < size; i++) bools[i] = false;

            bool expect = false;
            bool result = bools.AsParallel().All(delegate(bool b) { return b; });

            TestHarness.TestLog("  > Expect: {0}, real: {1}", expect, result);

            return result == expect;
        }

        private static bool RunAllTest_AllTrue(int size)
        {
            TestHarness.TestLog("* RunAllTest_AllTrue(size={0})", size);
            bool[] bools = new bool[size];
            for (int i = 0; i < size; i++) bools[i] = true;

            bool expect = true;
            bool result = bools.AsParallel().All(delegate(bool b) { return b; });

            TestHarness.TestLog("  > Expect: {0}, real: {1}", expect, result);

            return result == expect;
        }

        private static bool RunAllTest_OneFalse(int size, int falsePosition)
        {
            TestHarness.TestLog("* RunAllTest_OneFalse(size={0}, truePosition={1})", size, falsePosition);
            bool[] bools = new bool[size];
            for (int i = 0; i < size; i++) bools[i] = true;
            bools[falsePosition] = false;

            bool expect = false;
            bool result = bools.AsParallel().All(delegate(bool b) { return b; });

            TestHarness.TestLog("  > Expect: {0}, real: {1}", expect, result);

            return result == expect;
        }

        //
        // SequenceEqual
        //

        internal static bool RunSequenceEqualTests()
        {
            bool passed = true;

            passed &= RunSequenceEqualTest(1024, 1024);
            passed &= RunSequenceEqualTest(1024, 1024, 512);
            passed &= RunSequenceEqualTest(1024, 1024, 1024+512);
            passed &= RunSequenceEqualTest(1024, 512);
            passed &= RunSequenceEqualTest(1024, 512, 512);
            passed &= RunSequenceEqualTest(1024, 512, 1024+256);
            passed &= RunSequenceEqualTest(0, 0);
            passed &= RunSequenceEqualTest(1024 * 1027, 1024 * 1027);
            passed &= RunSequenceEqualTest(1024 * 1027, 1024 * 1027, 1024 * 5);
            passed &= RunSequenceEqualTest(1024 * 1027, 1024 * 1027, (1024 * 1027) + (1024 * 5));

            passed &= RunSequenceEqualTest2(0, 10, 0, 10);
            passed &= RunSequenceEqualTest2(0, 1000, 0, 1000);
            passed &= RunSequenceEqualTest2(0, 1, 0, 1);
            passed &= RunSequenceEqualTest2(0, 0, 0, 0);

            passed &= RunSequenceEqualTest2(0, 10, 1, 10);
            passed &= RunSequenceEqualTest2(0, 10, 1, 9);
            passed &= RunSequenceEqualTest2(0, 10, 1, 9);
            passed &= RunSequenceEqualTest2(0, 0, 0, 1);

            // Test for bugs 702440 and 702447
            passed &= RunSequenceEqualTest3(ParallelExecutionMode.Default);
            passed &= RunSequenceEqualTest3(ParallelExecutionMode.ForceParallelism);

            return passed;
        }

        private static bool RunSequenceEqualTest(int leftSize, int rightSize, params int[] notEqual)
        {
            TestHarness.TestLog("* RunSequenceEqualTest(leftSize={0}, rightSize={1}, notEqual={2})", leftSize, rightSize, notEqual);

            int[] left = new int[leftSize];
            for (int i = 0; i < leftSize; i++) left[i] = i*2;
            int[] right = new int[rightSize];
            for (int i = 0; i < rightSize; i++) right[i] = i*2;

            if (notEqual != null)
            {
                for (int i = 0; i < notEqual.Length; i++)
                {
                    // To make the elements not equal, we just invert them.
                    int idx = notEqual[i];
                    if (idx >= leftSize)
                        right[idx - leftSize] = -right[idx - leftSize];
                    else
                        left[idx] = -left[idx];
                }
            }

            bool expect = leftSize == rightSize && (notEqual == null || notEqual.Length == 0);
            bool result = left.AsParallel().AsOrdered().SequenceEqual(right.AsParallel().AsOrdered());

            if (expect != result)
            {
                TestHarness.TestLog("  > Expect: {0}, real: {1}", expect, result);
            }

            return result == expect;
        }

        private static bool RunSequenceEqualTest2(int range1From, int range1Count, int range2From, int range2Count)
        {
            TestHarness.TestLog("* RunSequenceEqualTest2([{0}..{1}) vs [{2}..{3}))",
                range1From, range1From + range1Count, range2From, range2From + range2Count);

            bool expect = (range1From == range2From) && (range1Count == range2Count);
            IEnumerable<int>[] sources = {
                                             Enumerable.Range(range1From, range1Count),
                                             Enumerable.Range(range2From, range2Count)
                                         };
            List<ParallelQuery<int>>[] enumerables = new List<ParallelQuery<int>>[2];

            for (int t = 0; t < 2; t++)
            {
                IEnumerable<int> source = sources[t];

                enumerables[t] = new List<ParallelQuery<int>>
                {
                    source.AsParallel().AsOrdered(),
                    new LinkedList<int>(source).AsParallel().AsOrdered(),
                    source.AsParallel().AsOrdered().Where(i => true),
                    source.ToArray().AsParallel().AsOrdered().Where(i => true),
                    source.AsParallel().AsOrdered().Where(i=>true).Reverse().Reverse(),
                    source.Reverse().ToArray().AsParallel().AsOrdered().Where(i=>true).Reverse(),
                    source.AsParallel().AsOrdered().OrderBy(i => i),
                    source.AsParallel().AsOrdered().Select(i => i).Reverse().Reverse().Skip(0)
                };
            }

            foreach (ParallelQuery<int> ipe1 in enumerables[0])
            {
                foreach (ParallelQuery<int> ipe2 in enumerables[1])
                {
                    bool[] results = new bool[] { ipe1.SequenceEqual(ipe2), ipe2.SequenceEqual(ipe1) };
                    foreach (bool result in results)
                    {
                        if (result != expect)
                        {
                            TestHarness.TestLog("  > Expect: {0}, real: {1}", expect, result);
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public class DisposeExceptionEnumerable<T> : IEnumerable<T>
        {
            IEnumerable<T> m_enumerable;
            public DisposeExceptionEnumerable(IEnumerable<T> enumerable)
            {
                m_enumerable = enumerable;
            }

            public IEnumerator<T> GetEnumerator()
            {
                return new DisposeExceptionEnumerator<T>(m_enumerable.GetEnumerator());
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return new DisposeExceptionEnumerator<T>(m_enumerable.GetEnumerator());
            }
        }

        public class DisposeExceptionEnumerator<T> : IEnumerator<T>
        {
            IEnumerator<T> m_enumerator;
            public DisposeExceptionEnumerator(IEnumerator<T> enumerator)
            {
                m_enumerator = enumerator;
            }
            public T Current
            {
                get { return m_enumerator.Current; }
            }
            public void Dispose()
            {
                m_enumerator.Dispose();
                throw new ApplicationException();
            }

            object System.Collections.IEnumerator.Current
            {
                get { return m_enumerator.Current; }
            }
            public bool MoveNext()
            {
                return m_enumerator.MoveNext();
            }
            public void Reset()
            {
                m_enumerator.Reset();
            }
        }

        private static bool RunSequenceEqualTest3(ParallelExecutionMode mode)
        {
            TestHarness.TestLog("* RunSequenceEqualTest3(mode={0})", mode);
            int collectionLength = 1999;

            try
            {
                new DisposeExceptionEnumerable<int>(Enumerable.Range(0, collectionLength)).AsParallel()
                    .WithDegreeOfParallelism(2)
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .SequenceEqual(new DisposeExceptionEnumerable<int>(Enumerable.Range(0, collectionLength)).AsParallel());
            }
            catch (AggregateException)
            {
                    return true;
            }
            catch(Exception ex)
            {
                TestHarness.TestLog("> Failed: Expected AggregateException, got {0}", ex.GetType());
                return false;
            }

            TestHarness.TestLog("> Failed: Expected AggregateException, got no exception");
            return false;
        }
        //
        // TakeWhile and SkipWhile
        //

        internal static bool RunTakeSkipWhileTests()
        {
            bool passed = true;

            // TakeWhile:
            passed &= RunTakeWhile_AllFalse(1024);
            passed &= RunTakeWhile_AllTrue(1024);
            passed &= RunTakeWhile_SomeTrues(1024, 512);
            passed &= RunTakeWhile_SomeTrues(1024, 0);
            passed &= RunTakeWhile_SomeTrues(1024, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18);
            passed &= RunTakeWhile_SomeTrues(1024, 1023);
            passed &= RunTakeWhile_SomeTrues(1024 * 1024, 1024 * 512);
            passed &= RunTakeWhile_SomeFalses(1024, 512);
            passed &= RunTakeWhile_SomeFalses(1024, 0);
            passed &= RunTakeWhile_SomeFalses(1024, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18);
            passed &= RunTakeWhile_SomeFalses(1024, 1023);
            passed &= RunTakeWhile_SomeFalses(1024 * 1024, 1024 * 512);

            // SkipWhile:
            passed &= RunSkipWhile_AllFalse(1024);
            passed &= RunSkipWhile_AllTrue(1024);
            passed &= RunSkipWhile_SomeTrues(1024, 512);
            passed &= RunSkipWhile_SomeTrues(1024, 0);
            passed &= RunSkipWhile_SomeTrues(1024, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18);
            passed &= RunSkipWhile_SomeTrues(1024, 1023);
            passed &= RunSkipWhile_SomeTrues(1024 * 1024, 1024 * 512);
            passed &= RunSkipWhile_SomeTrues(1024, 512);
            passed &= RunSkipWhile_SomeTrues(1024, 0);
            passed &= RunSkipWhile_SomeTrues(1024, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18);
            passed &= RunSkipWhile_SomeTrues(1024, 1023);
            passed &= RunSkipWhile_SomeTrues(1024 * 1024, 1024 * 512);

            return passed;
        }

        //
        // TakeWhile
        //

        private static bool RunTakeWhile_AllFalse(int size)
        {
            TestHarness.TestLog("* RunTakeWhile_AllFalse(size={0})", size);
            int[] data = new int[size];
            for (int i = 0; i < size; i++) data[i] = i;

            ParallelQuery<int> q = data.AsParallel().TakeWhile(delegate(int x) { return false; });

            int count = 0;
            int expect = 0;

            foreach (int x in q)
            {
                count++;
            }

            TestHarness.TestLog("  > *{0}* Expect: {1}, real: {2}",
                expect == count ? "PASS" : "**FAIL**", expect, count);

            return count == expect;
        }

        private static bool RunTakeWhile_AllTrue(int size)
        {
            TestHarness.TestLog("* RunTakeWhile_AllTrue(size={0})", size);
            int[] data = new int[size];
            for (int i = 0; i < size; i++) data[i] = i;

            ParallelQuery<int> q = data.AsParallel().TakeWhile(delegate(int x) { return true; });

            int count = 0;
            int expect = size;

            foreach (int x in q)
            {
                count++;
            }

            TestHarness.TestLog("  > *{0}* Expect: {1}, real: {2}",
                expect == count ? "PASS" : "**FAIL**", expect, count);

            return count == expect;
        }

        private static bool RunTakeWhile_SomeTrues(int size, params int[] truePositions)
        {
            TestHarness.TestLog("* RunTakeWhile_SomeTrues(size={0}, truePositions.Length={1})", size, truePositions.Length);
            int[] data = new int[size];
            for (int i = 0; i < size; i++) data[i] = i;

            ParallelQuery<int> q = data.AsParallel().TakeWhile(delegate(int x) { return Array.IndexOf(truePositions, x) != -1; });

            int count = 0;

            // We expect TakeWhile to yield all elements up to (but not including) the smallest false
            // index in the array.
            int expect = 0;
            for (int i = 0; i < size; i++)
            {
                if (Array.IndexOf(truePositions, i) == -1)
                    break;
                expect++;
            }

            foreach (int x in q)
            {
                count++;
            }

            TestHarness.TestLog("  > *{0}* Expect: {1}, real: {2}",
                expect == count ? "PASS" : "**FAIL**", expect, count);

            return count == expect;
        }

        private static bool RunTakeWhile_SomeFalses(int size, params int[] falsePositions)
        {
            TestHarness.TestLog("* RunTakeWhile_SomeFalses(size={0}, falsePosition.Length={1})", size, falsePositions.Length);
            int[] data = new int[size];
            for (int i = 0; i < size; i++) data[i] = i;

            ParallelQuery<int> q = data.AsParallel().TakeWhile(delegate(int x) { return Array.IndexOf(falsePositions, x) == -1; });

            int count = 0;

            // We expect TakeWhile to yield all elements up to (but not including) the smallest false
            // index in the array.
            int expect = falsePositions[0];
            for (int i = 1; i < falsePositions.Length; i++)
                expect = Math.Min(expect, falsePositions[i]);

            foreach (int x in q)
            {
                count++;
            }

            TestHarness.TestLog("  > *{0}* Expect: {1}, real: {2}",
                expect == count ? "PASS" : "**FAIL**", expect, count);

            return count == expect;
        }

        //
        // SkipWhile
        //

        private static bool RunSkipWhile_AllFalse(int size)
        {
            TestHarness.TestLog("* RunSkipWhile_AllFalse(size={0})", size);
            int[] data = new int[size];
            for (int i = 0; i < size; i++) data[i] = i;

            ParallelQuery<int> q = data.AsParallel().SkipWhile(delegate(int x) { return false; });

            int count = 0;
            int expect = size;

            foreach (int x in q)
            {
                count++;
            }

            TestHarness.TestLog("  > *{0}* Expect: {1}, real: {2}",
                expect == count ? "PASS" : "**FAIL**", expect, count);

            return count == expect;
        }

        private static bool RunSkipWhile_AllTrue(int size)
        {
            TestHarness.TestLog("* RunSkipWhile_AllTrue(size={0})", size);
            int[] data = new int[size];
            for (int i = 0; i < size; i++) data[i] = i;

            ParallelQuery<int> q = data.AsParallel().SkipWhile(delegate(int x) { return true; });

            int count = 0;
            int expect = 0;

            foreach (int x in q)
            {
                count++;
            }

            TestHarness.TestLog("  > *{0}* Expect: {1}, real: {2}",
                expect == count ? "PASS" : "**FAIL**", expect, count);

            return count == expect;
        }

        private static bool RunSkipWhile_SomeTrues(int size, params int[] truePositions)
        {
            TestHarness.TestLog("* RunSkipWhile_SomeTrues(size={0}, truePositions.Length={1})", size, truePositions.Length);
            int[] data = new int[size];
            for (int i = 0; i < size; i++) data[i] = i;

            ParallelQuery<int> q = data.AsParallel().SkipWhile(delegate(int x) { return Array.IndexOf(truePositions, x) != -1; });

            int count = 0;

            // We expect SkipWhile to yield all elements after (and including) the smallest false index in the array.
            int expect = 0;
            for (int i = 0; i < size; i++)
            {
                if (Array.IndexOf(truePositions, i) == -1)
                    break;
                expect++;
            }
            expect = size - expect;

            foreach (int x in q)
            {
                count++;
            }

            TestHarness.TestLog("  > *{0}* Expect: {1}, real: {2}",
                expect == count ? "PASS" : "**FAIL**", expect, count);

            return count == expect;
        }

        private static bool RunSkipWhile_SomeFalses(int size, params int[] falsePositions)
        {
            TestHarness.TestLog("* RunSkipWhile_SomeFalses(size={0}, falsePosition.Length={1})", size, falsePositions.Length);
            int[] data = new int[size];
            for (int i = 0; i < size; i++) data[i] = i;

            ParallelQuery<int> q = data.AsParallel().SkipWhile(delegate(int x) { return Array.IndexOf(falsePositions, x) == -1; });

            int count = 0;

            // We expect SkipWhile to yield all elements after (and including) the smallest false index in the array.
            int expect = falsePositions[0];
            for (int i = 1; i < falsePositions.Length; i++)
                expect = Math.Min(expect, falsePositions[i]);
            expect = size - expect;

            foreach (int x in q)
            {
                count++;
            }

            TestHarness.TestLog("  > *{0}* Expect: {1}, real: {2}",
                expect == count ? "PASS" : "**FAIL**", expect, count);

            return count == expect;
        }

        //
        // Take and Skip
        //

        internal static bool RunTakeSkipTests()
        {
            bool passed = true;

            // TakeWhile:
            passed &= RunTakeTest1(16, 8);
            passed &= RunTakeTest1(1024, 32);
            passed &= RunTakeTest1(1024, 1024);
            passed &= RunTakeTest1(1024, 0);
            passed &= RunTakeTest1(0, 32);
            passed &= RunTakeTest1(1024 * 1024, 1024 * 64);

            passed &= RunTakeTest2_Range(0, 0, 0);
            passed &= RunTakeTest2_Range(0, 1, 1);
            passed &= RunTakeTest2_Range(0, 16, 0);
            passed &= RunTakeTest2_Range(0, 16, 8);
            passed &= RunTakeTest2_Range(0, 16, 16);
            passed &= RunTakeTest2_Range(16, 16, 0);
            passed &= RunTakeTest2_Range(16, 16, 8);
            passed &= RunTakeTest2_Range(16, 16, 16);
            passed &= RunTakeTest2_Range(0, 1024 * 1024, 0);
            passed &= RunTakeTest2_Range(0, 1024 * 1024, 1024);
            passed &= RunTakeTest2_Range(0, 1024 * 1024, 1024 * 1024);

            // SkipWhile:
            passed &= RunSkipTest1(1024, 32);
            passed &= RunSkipTest1(1024, 1024);
            passed &= RunSkipTest1(1024, 0);
            passed &= RunSkipTest1(0, 32);
            passed &= RunSkipTest1(32, 32);
            passed &= RunSkipTest1(1024 * 1024, 1024 * 64);

            passed &= RunSkipTest2_Range(0, 0, 0);
            passed &= RunSkipTest2_Range(0, 1, 1);
            passed &= RunSkipTest2_Range(0, 16, 0);
            passed &= RunSkipTest2_Range(0, 16, 8);
            passed &= RunSkipTest2_Range(0, 16, 16);
            passed &= RunSkipTest2_Range(16, 16, 0);
            passed &= RunSkipTest2_Range(16, 16, 8);
            passed &= RunSkipTest2_Range(16, 16, 16);
            passed &= RunSkipTest2_Range(0, 1024 * 1024, 0);
            passed &= RunSkipTest2_Range(0, 1024 * 1024, 1024);
            passed &= RunSkipTest2_Range(0, 1024 * 1024, 1024 * 1024);

            return passed;
        }

        //
        // Take
        //

        private static bool RunTakeTest1(int size, int take)
        {
            TestHarness.TestLog("* RunTakeTest1(size={0}, count={1})", size, take);
            int[] data = new int[size];
            for (int i = 0; i < size; i++) data[i] = i;

            ParallelQuery<int> q = data.AsParallel().Take(take);

            int seen = 0;
            int expect = Math.Min(size, take);
            bool passed = true;

            foreach (int x in q)
            {
                if (x >= expect)
                {
                    passed = false;
                    TestHarness.TestLog("  > FAILED: expected no values >= {0} (saw {1})", expect, x);
                }
                seen++;
            }

            passed &= (expect == seen);

            TestHarness.TestLog("  > *{0}* Expect: {1}, real: {2}", (expect == seen) ? "PASS" : "**FAIL**", expect, seen);

            return passed;
        }

        private static bool RunTakeTest2_Range(int start, int count, int take)
        {
            TestHarness.TestLog("* RunTakeTest2_Range(start={0}, count={1}, take={2})", start, count, take);

            ParallelQuery<int> q = ParallelEnumerable.Range(start, count).Take(take);

            int seen = 0;
            int expect = Math.Min(count, take);
            bool passed = true;

            foreach (int x in q)
            {
                if (x >= (start+expect))
                {
                    passed = false;
                    TestHarness.TestLog("  > FAILED: expected no values >= {0} (saw {1})", (start+expect), x);
                }
                seen++;
            }

            passed &= (expect == seen);

            TestHarness.TestLog("  > *{0}* Expect: {1}, real: {2}", (expect == seen) ? "PASS" : "**FAIL**", expect, seen);

            return passed;
        }

        //
        // Skip
        //

        private static bool RunSkipTest1(int size, int skip)
        {
            TestHarness.TestLog("* RunSkipTest1(size={0}, count={1})", size, skip);
            int[] data = new int[size];
            for (int i = 0; i < size; i++) data[i] = i;

            ParallelQuery<int> q = data.AsParallel().Skip(skip);

            int seen = 0;
            int expect = Math.Max(0, size - skip);
            bool passed = true;

            foreach (int x in q)
            {
                if (skip > x)
                {
                    passed = false;
                    TestHarness.TestLog("  > FAILED: expected no values < {0} (saw {1})", skip, x);
                }
                seen++;
            }

            passed &= (expect == seen);

            TestHarness.TestLog("  > *{0}* Expect: {1}, real: {2}", (expect == seen) ? "PASS" : "**FAIL**", expect, seen);

            return passed;
        }

        private static bool RunSkipTest2_Range(int start, int count, int skip)
        {
            TestHarness.TestLog("* RunSkipTest2_Range(start={0}, count={1}, skip={2})", start, count, skip);

            ParallelQuery<int> q = ParallelEnumerable.Range(start, count).Skip(skip);

            int seen = 0;
            int expect = Math.Max(0, count - skip);
            bool passed = true;

            foreach (int x in q)
            {
                if ((skip+start) > x)
                {
                    passed = false;
                    TestHarness.TestLog("  > FAILED: expected no values < {0} (saw {1})", skip, x);
                }
                seen++;
            }

            passed &= (expect == seen);

            TestHarness.TestLog("  > *{0}* Expect: {1}, real: {2}", (expect == seen) ? "PASS" : "**FAIL**", expect, seen);

            return passed;
        }

        //
        // Contains
        //

        internal static bool RunContainsTests()
        {
            bool passed = true;

            passed &= RunContainsTest_NoMatching(1024);
            passed &= RunContainsTest_AllMatching(1024);
            passed &= RunContainsTest_OneMatching(1024, 512);
            passed &= RunContainsTest_OneMatching(1024, 0);
            passed &= RunContainsTest_OneMatching(1024, 1023);
            passed &= RunContainsTest_OneMatching(1024 * 1024, 1024 * 512);

            return passed;
        }

        private static bool RunContainsTest_NoMatching(int size)
        {
            TestHarness.TestLog("* RunContainsTest_NoMatching(size={0})", size);

            int toFind = 103372;
            Random r = new Random(33);
            int[] data = new int[size];
            for (int i = 0; i < size; i++)
            {
                data[i] = r.Next();
                if (data[i] == toFind) data[i] += 1;
            }

            bool expect = false;
            bool result = data.AsParallel().Contains(toFind);

            TestHarness.TestLog("  > Expect: {0}, real: {1}", expect, result);

            return result == expect;
        }

        private static bool RunContainsTest_AllMatching(int size)
        {
            TestHarness.TestLog("* RunContainsTest_AllMatching(size={0})", size);

            int toFind = 103372;
            int[] data = new int[size];
            for (int i = 0; i < size; i++) data[i] = 103372;

            bool expect = true;
            bool result = data.AsParallel().Contains(toFind);

            TestHarness.TestLog("  > Expect: {0}, real: {1}", expect, result);

            return result == expect;
        }

        private static bool RunContainsTest_OneMatching(int size, int matchPosition)
        {
            TestHarness.TestLog("* RunContainsTest_OneMatching(size={0}, matchPosition={1})", size, matchPosition);

            int toFind = 103372;
            Random r = new Random(33);
            int[] data = new int[size];
            for (int i = 0; i < size; i++)
            {
                data[i] = r.Next();
                if (data[i] == toFind) data[i] += 1;
            }
            data[matchPosition] = toFind;

            bool expect = true;
            bool result = data.AsParallel().Contains(toFind);

            TestHarness.TestLog("  > Expect: {0}, real: {1}", expect, result);

            return result == expect;
        }

        //
        // Zip
        //

        internal static bool RunZipTests()
        {
            bool passed = true;

            passed &= RunZipTest1(0);
            passed &= RunZipTest1(1024 * 4);

            return passed;
        }

        private static bool RunZipTest1(int dataSize)
        {
            TestHarness.TestLog("RunZipTest1({0})", dataSize);

            int[] ints = new int[dataSize];
            int[] ints2 = new int[dataSize];
            for (int i = 0; i < ints.Length; i++)
            {
                ints[i] = i;
                ints2[i] = i;
            }

            ParallelQuery<Pair<int, int>> q = ints.AsParallel().Zip<int, int, Pair<int,int> >(ints2.AsParallel(), (i,j) => new Pair<int,int>(i,j));
            Pair<int, int>[] p = q.ToArray<Pair<int, int>>();

            bool passed = true;
            foreach (Pair<int, int> x in p)
            {
                if (x.First != x.Second)
                {
                    TestHarness.TestLog("  > Failed... {0} != {1}", x.First, x.Second);
                    passed = false;
                }
            }

            return passed;
        }

        //
        // Join
        //

        internal static bool RunJoinTests()
        {
            bool passed = true;

            // Tiny, and empty, input sizes.
            passed &= RunJoinTest1(0, 0);
            passed &= RunJoinTest1(1, 0);
            passed &= RunJoinTest1(32, 0);
            passed &= RunJoinTest1(0, 1);
            passed &= RunJoinTest1(0, 32);
            passed &= RunJoinTest1(1, 1);
            passed &= RunJoinTest1(32, 1);
            passed &= RunJoinTest1(1, 32);

            // Normal input sizes.
            passed &= RunJoinTest1(32, 32);
            passed &= RunJoinTest1(1024 * 8, 1024);
            passed &= RunJoinTest1(1024, 1024 * 8);
            passed &= RunJoinTest1(1024 * 8, 1024 * 8);
            passed &= RunJoinTest1(1024 * 512, 1024 * 256);
            passed &= RunJoinTest1(1024 * 256, 1024 * 512);
            passed &= RunJoinTest1(1024 * 1024 * 2, 1024 * 1024);

            // Tiny, and empty, input sizes.
            passed &= RunJoinTest2(0, 0);
            passed &= RunJoinTest2(1, 0);
            passed &= RunJoinTest2(32, 0);
            passed &= RunJoinTest2(0, 1);
            passed &= RunJoinTest2(0, 32);
            passed &= RunJoinTest2(1, 1);
            passed &= RunJoinTest2(32, 1);
            passed &= RunJoinTest2(1, 32);

            // Normal input sizes.
            passed &= RunJoinTest2(32, 32);
            passed &= RunJoinTest2(1024 * 8, 1024);
            passed &= RunJoinTest2(1024, 1024 * 8);
            passed &= RunJoinTest2(1024 * 8, 1024 * 8);
            passed &= RunJoinTest2(1024 * 512, 1024 * 256);
            passed &= RunJoinTest2(1024 * 256, 1024 * 512);
            passed &= RunJoinTest2(1024 * 1024 * 2, 1024 * 1024);

            // Other misc. tests.

            passed &= RunJoinTest3(0, 0);
            passed &= RunJoinTest3(1, 0);
            passed &= RunJoinTest3(0, 1);
            passed &= RunJoinTest3(1024 * 8, 1024);
            passed &= RunJoinTest3(1024, 1024 * 8);
            passed &= RunJoinTest3(1024 * 1024 * 2, 1024 * 1024);

            passed &= RunJoinTestWithWhere1(0, 0);
            passed &= RunJoinTestWithWhere1(1024 * 8, 1024);
            passed &= RunJoinTestWithWhere1(1024, 1024 * 8);

            passed &= RunJoinWithInnerJoinTest1(4, 4, 4, true);
            passed &= RunJoinWithInnerJoinTest1(1024*1024, 1024*4, 1024, true);
            passed &= RunJoinWithInnerJoinTest1(1024*1024, 1024*4, 1024, false);

            passed &= RunJoinTestWithTakeWhile(0, 0);
            passed &= RunJoinTestWithTakeWhile(1024 * 8, 1024);
            passed &= RunJoinTestWithTakeWhile(1024, 1024 * 8);

            return passed;
        }

        private static bool RunJoinTest1(int outerSize, int innerSize)
        {
            TestHarness.TestLog("RunJoinTest1(outerSize = {0}, innerSize = {1}) - async/pipeline", outerSize, innerSize);

            int[] left = new int[outerSize];
            int[] right = new int[innerSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i*8;

            Func<int, int> identityKeySelector = delegate(int x) { return x; };
            IEnumerable<Pair<int, int>> joinResults = left.AsParallel().Join<int, int, int, Pair<int, int>>(
                right.AsParallel(), identityKeySelector, identityKeySelector, delegate(int x, int y) { return new Pair<int, int>(x, y); });

            bool passed = true;

            TestHarness.TestLog("  > Invoking join of {0} outer elems with {1} inner elems", left.Length, right.Length);

            // Ensure pairs are of equal values.
            int cnt = 0;
            foreach (Pair<int, int> p in joinResults)
            {
                cnt++;
                if (!(passed &= p.First == p.Second))
                {
                    TestHarness.TestLog("  > *ERROR: pair members not equal -- {0} != {1}", p.First, p.Second);
                    break;
                }
            }

            // And that we have the correct number of elements.
            int expect = Math.Min((outerSize+7)/8, innerSize);
            passed &= cnt == expect;
            TestHarness.TestLog("  > Saw expected count? Saw = {0}, expect = {1}: {2}", cnt, expect, passed);

            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");
            return passed;
        }

        private static bool RunJoinTest2(int outerSize, int innerSize)
        {
            TestHarness.TestLog("RunJoinTest2(outerSize = {0}, innerSize = {1}) - sync/ no pipeline", outerSize, innerSize);

            int[] left = new int[outerSize];
            int[] right = new int[innerSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i*8;

            Func<int, int> identityKeySelector = delegate(int x) { return x; };
            ParallelQuery<Pair<int, int>> joinResults = left.AsParallel().Join<int, int, int, Pair<int, int>>(
                right.AsParallel(), identityKeySelector, identityKeySelector, delegate(int x, int y) { return new Pair<int, int>(x, y); });

            bool passed = true;

            TestHarness.TestLog("  > Invoking join of {0} outer elems with {1} inner elems", left.Length, right.Length);

            List<Pair<int, int>> r = joinResults.ToList<Pair<int, int>>();

            // Ensure pairs are of equal values.
            int cnt = 0;
            foreach (Pair<int, int> p in r)
            {
                cnt++;
                if (!(passed &= p.First == p.Second))
                {
                    TestHarness.TestLog("  > *ERROR: pair members not equal -- {0} != {1}", p.First, p.Second);
                    break;
                }
            }

            // And that we have the correct number of elements.
            int expect = Math.Min((outerSize+7)/8, innerSize);
            passed &= cnt == expect;
            TestHarness.TestLog("  > Saw expected count? Saw = {0}, expect = {1}: {2}", cnt, expect, passed);

            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");
            return passed;
        }

        private static bool RunJoinTest3(int outerSize, int innerSize)
        {
            TestHarness.TestLog("RunJoinTest3(outerSize = {0}, innerSize = {1}) - FORALL", outerSize, innerSize);

            int[] left = new int[outerSize];
            int[] right = new int[innerSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i*8;

            Func<int, int> identityKeySelector = delegate(int x) { return x; };
            ParallelQuery<Pair<int, int>> joinResults = left.AsParallel().Join<int, int, int, Pair<int, int>>(
                right.AsParallel(), identityKeySelector, identityKeySelector, delegate(int x, int y) { return new Pair<int, int>(x, y); });

            bool passed = true;

            TestHarness.TestLog("  > Invoking join of {0} outer elems with {1} inner elems", left.Length, right.Length);

            int cnt = 0;

            joinResults.ForAll<Pair<int, int>>(delegate(Pair<int, int> x) { Interlocked.Increment(ref cnt); });

            // Check that we have the correct number of elements.
            int expect = Math.Min((outerSize+7)/8, innerSize);
            passed &= cnt == expect;
            TestHarness.TestLog("  > Saw expected count? Saw = {0}, expect = {1}: {2}", cnt, expect, passed);

            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");
            return passed;
        }

        private static bool RunJoinTestWithWhere1(int outerSize, int innerSize)
        {
            TestHarness.TestLog("RunJoinTestWithWhere1(outerSize = {0}, innerSize = {1})", outerSize, innerSize);

            int[] left = new int[outerSize];
            int[] right = new int[innerSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i*8;

            Func<int, int> identityKeySelector = delegate(int x) { return x; };
            IEnumerable<Pair<int, int>> joinResults = left.AsParallel().Join<int, int, int, Pair<int, int>>(
                right.AsParallel().Where<int>(delegate(int x) { return (x % 16) == 0; }),
                identityKeySelector, identityKeySelector, delegate(int x, int y) { return new Pair<int, int>(x, y); });


            bool passed = true;

            TestHarness.TestLog("  > Invoking join of {0} outer elems with {1} inner elems", left.Length, right.Length);

            // Ensure pairs are of equal values.
            int cnt = 0;
            foreach (Pair<int, int> p in joinResults)
            {
                cnt++;
                if (!(passed &= p.First == p.Second))
                {
                    TestHarness.TestLog("  > *ERROR: pair members not equal -- {0} != {1}", p.First, p.Second);
                    break;
                }
            }

            // And that we have the correct number of elements.
            int expect = (outerSize/16) > innerSize ? outerSize : Math.Min(outerSize/16, innerSize);
            passed &= cnt == expect;
            TestHarness.TestLog("  > Saw expected count? Saw = {0}, expect = {1}: {2}", cnt, expect, passed);

            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");
            return passed;
        }

        private static bool RunJoinWithInnerJoinTest1(int dataSize, int innerLeftSize, int innerRightSize, bool innerAsLeft)
        {
            TestHarness.TestLog("RunJoinWithInnerJoinTest1(dataSize={0},innerLeftSize={1},innerRightSize={2},innerAsLeft = {3})",
                dataSize, innerLeftSize, innerRightSize, innerAsLeft);


            int[] data = new int[dataSize];

            int[] innerLeft = new int[innerLeftSize];
            int[] innerRight = new int[innerRightSize];

            for (int i = 0; i < data.Length; i++) data[i] = i;
            for (int i = 0; i < innerLeft.Length; i++) innerLeft[i] = i*2;
            for (int i = 0; i < innerRight.Length; i++) innerRight[i] = i*4;

            Func<int, int> identityKeySelector = delegate(int x) { return x; };

            bool passed = true;

            if (innerAsLeft)
            {
                IEnumerable<Pair<int, Pair<int, int>>> q = 
                    data.AsParallel().Join<int, Pair<int, int>, int, Pair<int, Pair<int, int>>>(
                        innerLeft.AsParallel()
                            .Join<int, int, int, Pair<int, int>>(innerRight.AsParallel(), identityKeySelector, identityKeySelector, delegate(int x, int y) { return new Pair<int, int>(x, y); }),
                        identityKeySelector, delegate(Pair<int, int> p) { return p.First; }, delegate(int x, Pair<int, int> p) { return new Pair<int,Pair<int, int>>(x,p); });

                int cnt = 0;
                foreach (Pair<int,Pair<int,int>> x in q)
                {
                    cnt++;
                    if (!(passed &= (x.First == x.Second.First && x.Second.First == x.Second.Second)))
                    {
                        TestHarness.TestLog("  > *ERROR: pair members not equal -- {0} // ( {1} // {2} )", x.First, x.Second.First, x.Second.Second);
                        break;
                    }
                }
            }
            else
            {

                IEnumerable<Pair<Pair<int,int>,int>> q = innerLeft.AsParallel()
                    .Join(innerRight.AsParallel(), identityKeySelector, identityKeySelector, delegate(int x, int y) { return new Pair<int, int>(x, y); })
                    .Join<Pair<int, int>, int, int, Pair<Pair<int, int>, int>>(
                        data.AsParallel(), delegate(Pair<int, int> p) { return p.First; }, identityKeySelector, delegate(Pair<int, int> p, int x) { return new Pair<Pair<int, int>, int>(p, x); });

                int cnt = 0;
                foreach (Pair<Pair<int,int>,int> x in q)
                {
                    cnt++;
                    if (!(passed &= (x.First.First == x.Second && x.First.First == x.First.Second)))
                    {
                        TestHarness.TestLog("  > *ERROR: pair members not equal -- ( {0} // {1} ) // {2}", x.First.First, x.First.Second, x.Second);
                        break;
                    }
                }
            }

            // @TODO: validate xcxount.

            //int expect = (outerSize/8) > innerSize ? outerSize : Math.Min(outerSize/8, innerSize);
            //passed &= cnt == expect;
            //TestHarness.TestLog("  > Saw expected count? Saw = {0}, expect = {1}: {2}", cnt, expect, passed);

            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");
            return passed;
        }

        //
        //  Running a join followed by a TakeWhile will ensure order preservation code paths are hit
        //  in combination with hash partitioning.
        //
        private static bool RunJoinTestWithTakeWhile(int outerSize, int innerSize)
        {
            TestHarness.TestLog("RunJoinTestWithTakeWhile(outerSize = {0}, innerSize = {1})", outerSize, innerSize);

            int[] left = new int[outerSize];
            int[] right = new int[innerSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i * 8;

            Func<int, int> identityKeySelector = delegate(int x) { return x; };
            ParallelQuery<Pair<int, int>> results = left.AsParallel().Join<int, int, int, Pair<int, int>>(
                    right.AsParallel().Where<int>(delegate(int x) { return (x % 16) == 0; }),
                    identityKeySelector, identityKeySelector, delegate(int x, int y) { return new Pair<int, int>(x, y); }).TakeWhile<Pair<int, int>>((x) => true);

            bool passed = true;

            TestHarness.TestLog("  > Invoking join of {0} outer elems with {1} inner elems", left.Length, right.Length);

            // Ensure pairs are of equal values.
            int cnt = 0;
            foreach (Pair<int, int> p in results)
            {
                cnt++;
                if (!(passed &= p.First == p.Second))
                {
                    TestHarness.TestLog("  > *ERROR: pair members not equal -- {0} != {1}", p.First, p.Second);
                    break;
                }
            }

            // And that we have the correct number of elements.
            int expect = (outerSize / 16) > innerSize ? outerSize : Math.Min(outerSize / 16, innerSize);
            passed &= cnt == expect;
            TestHarness.TestLog("  > Saw expected count? Saw = {0}, expect = {1}: {2}", cnt, expect, passed);

            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");
            return passed;
        }

        //
        // GroupJoin
        //

        internal static bool RunGroupJoinTests()
        {
            bool passed = true;

            // Tiny, and empty, input sizes.
            passed &= RunGroupJoinTest1(0, 0);
            passed &= RunGroupJoinTest1(1, 0);
            passed &= RunGroupJoinTest1(32, 0);
            passed &= RunGroupJoinTest1(0, 1);
            passed &= RunGroupJoinTest1(0, 32);
            passed &= RunGroupJoinTest1(1, 1);
            passed &= RunGroupJoinTest1(32, 1);
            passed &= RunGroupJoinTest1(1, 32);

            // Normal input sizes.
            passed &= RunGroupJoinTest1(32, 32);
            passed &= RunGroupJoinTest1(1024 * 8, 1024);
            passed &= RunGroupJoinTest1(1024, 1024 * 8);
            passed &= RunGroupJoinTest1(1024 * 8, 1024 * 8);
            passed &= RunGroupJoinTest1(1024 * 512, 1024 * 256);
            passed &= RunGroupJoinTest1(1024 * 256, 1024 * 512);
            passed &= RunGroupJoinTest1(1024 * 1024 * 2, 1024 * 1024);

            // Tiny, and empty, input sizes.
            passed &= RunGroupJoinTest2(0, 0);
            passed &= RunGroupJoinTest2(1, 0);
            passed &= RunGroupJoinTest2(32, 0);
            passed &= RunGroupJoinTest2(0, 1);
            passed &= RunGroupJoinTest2(0, 32);
            passed &= RunGroupJoinTest2(1, 1);
            passed &= RunGroupJoinTest2(32, 1);
            passed &= RunGroupJoinTest2(1, 32);

            // Normal input sizes.
            passed &= RunGroupJoinTest2(32, 32);
            passed &= RunGroupJoinTest2(1024 * 8, 1024);
            passed &= RunGroupJoinTest2(1024, 1024 * 8);
            passed &= RunGroupJoinTest2(1024 * 8, 1024 * 8);
            passed &= RunGroupJoinTest2(1024 * 512, 1024 * 256);
            passed &= RunGroupJoinTest2(1024 * 256, 1024 * 512);
            passed &= RunGroupJoinTest2(1024 * 1024 * 2, 1024 * 1024);

            return passed;
        }

        private static bool RunGroupJoinTest1(int outerSize, int innerSize)
        {
            TestHarness.TestLog("RunGroupJoinTest1(outerSize = {0}, innerSize = {1}) - async/pipeline", outerSize, innerSize);

            int[] left = new int[outerSize];
            int[] right = new int[innerSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i*8;

            Func<int, int> identityKeySelector = delegate(int x) { return x; };
            IEnumerable<Pair<int, IEnumerable<int>>> joinResults = left.AsParallel().GroupJoin<int, int, int, Pair<int, IEnumerable<int>>>(
                right.AsParallel(), identityKeySelector, identityKeySelector,
                delegate(int x, IEnumerable<int> y) { return new Pair<int, IEnumerable<int>>(x, y); });

            bool passed = true;

            TestHarness.TestLog("  > Invoking join of {0} outer elems with {1} inner elems", left.Length, right.Length);

            // Ensure pairs are of equal values.
            int cnt = 0;
            foreach (Pair<int, IEnumerable<int>> p in joinResults)
            {
                foreach (int p2 in p.Second)
                {
                    cnt++;
                    if (!(passed &= p.First == p2))
                    {
                        TestHarness.TestLog("  > *ERROR: pair members not equal -- {0} != {1}", p.First, p2);
                        break;
                    }
                }
            }

            // And that we have the correct number of elements.
            int expect = Math.Min((outerSize+7)/8, innerSize);
            passed &= cnt == expect;
            TestHarness.TestLog("  > Saw expected count? Saw = {0}, expect = {1}: {2}", cnt, expect, passed);

            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");
            return passed;
        }

        private static bool RunGroupJoinTest2(int outerSize, int innerSize)
        {
            TestHarness.TestLog("RunGroupJoinTest2(outerSize = {0}, innerSize = {1}) - sync/ no pipeline", outerSize, innerSize);

            int[] left = new int[outerSize];
            int[] right = new int[innerSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i*8;

            Func<int, int> identityKeySelector = delegate(int x) { return x; };
            ParallelQuery<Pair<int, IEnumerable<int>>> joinResults = left.AsParallel().GroupJoin<int, int, int, Pair<int, IEnumerable<int>>>(
                right.AsParallel(), identityKeySelector, identityKeySelector,
                delegate(int x, IEnumerable<int> y) { return new Pair<int, IEnumerable<int>>(x, y); });

            bool passed = true;

            TestHarness.TestLog("  > Invoking join of {0} outer elems with {1} inner elems", left.Length, right.Length);

            List<Pair<int, IEnumerable<int>>> r = joinResults.ToList<Pair<int, IEnumerable<int>>>();

            // Ensure pairs are of equal values.
            int cnt = 0;
            foreach (Pair<int, IEnumerable<int>> p in r)
            {
                foreach (int p2 in p.Second)
                {
                    cnt++;
                    if (!(passed &= p.First == p2))
                    {
                        TestHarness.TestLog("  > *ERROR: pair members not equal -- {0} != {1}", p.First, p2);
                        break;
                    }
                }
            }

            // And that we have the correct number of elements.
            int expect = Math.Min((outerSize+7)/8, innerSize);
            passed &= cnt == expect;
            TestHarness.TestLog("  > Saw expected count? Saw = {0}, expect = {1}: {2}", cnt, expect, passed);

            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");
            return passed;
        }

        //
        // Select
        //

        internal static bool RunSelectTests()
        {
            bool passed = true;

            passed &= RunSelectTest1(0);
            passed &= RunSelectTest1(1);
            passed &= RunSelectTest1(32);
            passed &= RunSelectTest1(1024);
            passed &= RunSelectTest1(1024 * 2);

            passed &= RunSelectTest2(0);
            passed &= RunSelectTest2(1);
            passed &= RunSelectTest2(32);
            passed &= RunSelectTest2(1024);
            passed &= RunSelectTest2(1024 * 2);

            passed &= RunIndexedSelectTest1(0);
            passed &= RunIndexedSelectTest1(1);
            passed &= RunIndexedSelectTest1(32);
            passed &= RunIndexedSelectTest1(1024);
            passed &= RunIndexedSelectTest1(1024 * 2);
            passed &= RunIndexedSelectTest1(1024 * 1024 * 4);

            passed &= RunIndexedSelectTest2(0);
            passed &= RunIndexedSelectTest2(1);
            passed &= RunIndexedSelectTest2(32);
            passed &= RunIndexedSelectTest2(1024);
            passed &= RunIndexedSelectTest2(1024 * 2);
            passed &= RunIndexedSelectTest2(1024 * 1024 * 4);

            return passed;
        }

        private static bool RunSelectTest1(int dataSize)
        {
            TestHarness.TestLog("RunSelectTest1(dataSize = {0}) - async/pipeline", dataSize);

            int[] data = new int[dataSize];
            for (int i = 0; i < data.Length; i++) data[i] = i;

            bool passed = true;

            // Select the square. We will validate it during results.
            IEnumerable<Pair<int, int>> q = data.AsParallel().Select<int, Pair<int, int>>(
                delegate(int x) { return new Pair<int, int>(x, x * x); });

            int cnt = 0;
            foreach (Pair<int,int> p in q)
            {
                if (p.Second != p.First * p.First)
                {
                    TestHarness.TestLog("  > **Failure: {0} is not the square of {1} ({2})",
                        p.Second, p.First, p.First * p.First);
                    passed = false;
                }
                cnt++;
            }

            passed &= (cnt == dataSize);
            TestHarness.TestLog("  > Saw expected count? Saw = {0}, expect = {1}: {2}", cnt, dataSize, passed);

            return passed;
        }

        private static bool RunSelectTest2(int dataSize)
        {
            TestHarness.TestLog("RunSelectTest2(dataSize = {0}) - NO pipelining", dataSize);

            int[] data = new int[dataSize];
            for (int i = 0; i < data.Length; i++) data[i] = i;

            bool passed = true;

            // Select the square. We will validate it during results.
            ParallelQuery<Pair<int, int>> q = data.AsParallel().Select<int, Pair<int, int>>(
                delegate(int x) { return new Pair<int, int>(x, x * x); });

            int cnt = 0;
            List<Pair<int, int>> r = q.ToList<Pair<int, int>>();
            foreach (Pair<int, int> p in r)
            {
                if (p.Second != p.First * p.First)
                {
                    TestHarness.TestLog("  > **Failure: {0} is not the square of {1} ({2})",
                        p.Second, p.First, p.First * p.First);
                    passed = false;
                }
                cnt++;
            }

            passed &= (cnt == dataSize);
            TestHarness.TestLog("  > Saw expected count? Saw = {0}, expect = {1}: {2}", cnt, dataSize, passed);

            return passed;
        }

        //
        // Uses an element's index to calculate an output value.  If order preservation isn't
        // working, this would PROBABLY fail.  Unfortunately, this isn't deterministic.  But choosing
        // larger input sizes increases the probability that it will.
        //

        private static bool RunIndexedSelectTest1(int dataSize)
        {
            TestHarness.TestLog("RunIndexedSelectTest1(dataSize = {0}) - async/pipelining", dataSize);

            int[] data = new int[dataSize];
            for (int i = 0; i < data.Length; i++) data[i] = i;

            bool passed = true;

            // Select the square. We will validate it during results.
            IEnumerable<Pair<int, Pair<int, int>>> q = data.AsParallel().AsOrdered().Select<int, Pair<int, Pair<int, int>>>(
                delegate(int x, int idx) {
                    return new Pair<int, Pair<int, int>>(x, new Pair<int, int>(idx, x * x)); });

            int cnt = 0;
            foreach (Pair<int, Pair<int, int>> p in q)
            {
                if (p.Second.First != cnt)
                {
                    TestHarness.TestLog("  > **Failure: results not increasing in index order (expect {0}, saw {1})", cnt, p.Second.First);
                    passed = false;
                }

                if (p.First != p.Second.First)
                {
                    TestHarness.TestLog("  > **Failure: expected element value {0} to equal index {1}", p.First, p.Second.First);
                    passed = false;
                }

                if (p.Second.Second != p.First * p.First)
                {
                    TestHarness.TestLog("  > **Failure: {0} is not the square of {1} ({2})",
                        p.Second.Second, p.First, p.First * p.First);
                    passed = false;
                }
                cnt++;
            }

            passed &= (cnt == dataSize);
            TestHarness.TestLog("  > Saw expected count? Saw = {0}, expect = {1}: {2}", cnt, dataSize, passed);

            return passed;
        }

        //
        // Uses an element's index to calculate an output value.  If order preservation isn't
        // working, this would PROBABLY fail.  Unfortunately, this isn't deterministic.  But choosing
        // larger input sizes increases the probability that it will.
        //

        private static bool RunIndexedSelectTest2(int dataSize)
        {
            TestHarness.TestLog("RunIndexedSelectTest2(dataSize = {0}) - NO pipelining", dataSize);

            int[] data = new int[dataSize];
            for (int i = 0; i < data.Length; i++) data[i] = i;

            bool passed = true;

            // Select the square. We will validate it during results.
            ParallelQuery<Pair<int, Pair<int, int>>> q = data.AsParallel().AsOrdered().Select<int, Pair<int, Pair<int, int>>>(
                delegate(int x, int idx) {
                    return new Pair<int, Pair<int, int>>(x, new Pair<int, int>(idx, x * x)); });

            int cnt = 0;
            List<Pair<int, Pair<int, int>>> r = q.ToList<Pair<int, Pair<int, int>>>();
            foreach (Pair<int, Pair<int, int>> p in r)
            {
                if (p.Second.First != cnt)
                {
                    TestHarness.TestLog("  > **Failure: results not increasing in index order (expect {0}, saw {1})", cnt, p.Second.First);
                    passed = false;
                }

                if (p.First != p.Second.First)
                {
                    TestHarness.TestLog("  > **Failure: expected element value {0} to equal index {1}", p.First, p.Second.First);
                    passed = false;
                }

                if (p.Second.Second != p.First * p.First)
                {
                    TestHarness.TestLog("  > **Failure: {0} is not the square of {1} ({2})",
                        p.Second.Second, p.First, p.First * p.First);
                    passed = false;
                }
                cnt++;
            }

            passed &= (cnt == dataSize);
            TestHarness.TestLog("  > Saw expected count? Saw = {0}, expect = {1}: {2}", cnt, dataSize, passed);

            return passed;
        }

        //
        // Where
        //

        internal static bool RunWhereTests()
        {
            bool passed = true;

            passed &= RunWhereTest1(0);
            passed &= RunWhereTest1(1);
            passed &= RunWhereTest1(32);
            passed &= RunWhereTest1(1024);
            passed &= RunWhereTest1(1024 * 2);

            passed &= RunWhereTest2(0);
            passed &= RunWhereTest2(1);
            passed &= RunWhereTest2(32);
            passed &= RunWhereTest2(1024);
            passed &= RunWhereTest2(1024 * 2);

            passed &= RunIndexedWhereTest1(0);
            passed &= RunIndexedWhereTest1(1);
            passed &= RunIndexedWhereTest1(32);
            passed &= RunIndexedWhereTest1(1024);
            passed &= RunIndexedWhereTest1(1024 * 2);
            passed &= RunIndexedWhereTest1(1024 * 1024 * 4);

            passed &= RunIndexedWhereTest2(0);
            passed &= RunIndexedWhereTest2(1);
            passed &= RunIndexedWhereTest2(32);
            passed &= RunIndexedWhereTest2(1024);
            passed &= RunIndexedWhereTest2(1024 * 2);
            passed &= RunIndexedWhereTest2(1024 * 1024 * 4);

            return passed;
        }

        private static bool RunWhereTest1(int dataSize)
        {
            TestHarness.TestLog("RunWhereTest1(dataSize = {0}) - async/pipeline", dataSize);

            int[] data = new int[dataSize];
            for (int i = 0; i < data.Length; i++) data[i] = i;

            bool passed = true;

            // Filter out odd elements.
            IEnumerable<int> q = data.AsParallel().Where<int>(
                delegate(int x) { return (x%2) == 0; });

            int cnt = 0;
            foreach (int p in q)
            {
                if ((p%2)!=0)
                {
                    TestHarness.TestLog("  > **Failure: {0} is odd, shouldn't be present", p);
                    passed = false;
                }
                cnt++;
            }

            passed &= (cnt == ((dataSize+1)/2));
            TestHarness.TestLog("  > Saw expected count? Saw = {0}, expect = {1}: {2}", cnt, ((dataSize+1)/2), passed);

            return passed;
        }

        private static bool RunWhereTest2(int dataSize)
        {
            TestHarness.TestLog("RunWhereTest2(dataSize = {0}) - async/pipeline", dataSize);

            int[] data = new int[dataSize];
            for (int i = 0; i < data.Length; i++) data[i] = i;

            bool passed = true;

            // Filter out odd elements.
            ParallelQuery<int> q = data.AsParallel().Where<int>(
                delegate(int x) { return (x % 2) == 0; });

            int cnt = 0;
            List<int> r = q.ToList<int>();
            foreach (int p in r)
            {
                if ((p % 2) != 0)
                {
                    TestHarness.TestLog("  > **Failure: {0} is odd, shouldn't be present", p);
                    passed = false;
                }
                cnt++;
            }

            passed &= (cnt == ((dataSize+1) / 2));
            TestHarness.TestLog("  > Saw expected count? Saw = {0}, expect = {1}: {2}", cnt, ((dataSize+1) / 2), passed);

            return passed;
        }

        //
        // Uses an element's index to calculate an output value.  If order preservation isn't
        // working, this would PROBABLY fail.  Unfortunately, this isn't deterministic.  But choosing
        // larger input sizes increases the probability that it will.
        //

        private static bool RunIndexedWhereTest1(int dataSize)
        {
            TestHarness.TestLog("RunIndexedWhereTest1(dataSize = {0}) - async/pipelining", dataSize);

            int[] data = new int[dataSize];
            for (int i = 0; i < data.Length; i++) data[i] = i;

            bool passed = true;

            // Filter out elements where index isn't equal to the value (shouldn't filter any!).
            ParallelQuery<int> q = data.AsParallel().AsOrdered().Where<int>(
                delegate(int x, int idx) { return (x == idx); });

            int cnt = 0;
            foreach (int p in q)
            {
                if (p != cnt)
                {
                    TestHarness.TestLog("  > **Failure: results not increasing in index order (expect {0}, saw {1})", cnt, p);
                    passed = false;
                }
                cnt++;
            }

            passed &= (cnt == dataSize);
            TestHarness.TestLog("  > Saw expected count? Saw = {0}, expect = {1}: {2}", cnt, dataSize, passed);

            return passed;
        }

        //
        // Uses an element's index to calculate an output value.  If order preservation isn't
        // working, this would PROBABLY fail.  Unfortunately, this isn't deterministic.  But choosing
        // larger input sizes increases the probability that it will.
        //

        private static bool RunIndexedWhereTest2(int dataSize)
        {
            TestHarness.TestLog("RunIndexedWhereTest2(dataSize = {0}) - not pipelined", dataSize);

            int[] data = new int[dataSize];
            for (int i = 0; i < data.Length; i++) data[i] = i;

            bool passed = true;

            // Filter out elements where index isn't equal to the value (shouldn't filter any!).
            ParallelQuery<int> q = data.AsParallel().AsOrdered().Where<int>(
                delegate(int x, int idx) { return (x == idx); });

            int cnt = 0;
            List<int> r = q.ToList<int>();
            foreach (int p in r)
            {
                if (p != cnt)
                {
                    TestHarness.TestLog("  > **Failure: results not increasing in index order (expect {0}, saw {1})", cnt, p);
                    passed = false;
                }
                cnt++;
            }

            passed &= (cnt == dataSize);
            TestHarness.TestLog("  > Saw expected count? Saw = {0}, expect = {1}: {2}", cnt, dataSize, passed);

            return passed;
        }

        //
        // SelectMany
        //

        internal static bool RunSelectManyTests()
        {
            bool passed = true;

            passed &= RunSelectManyTest1(0, 0);
            passed &= RunSelectManyTest1(1, 0);
            passed &= RunSelectManyTest1(0, 1);
            passed &= RunSelectManyTest1(1, 1);
            passed &= RunSelectManyTest1(32, 32);
            passed &= RunSelectManyTest1(1024 * 2, 1024);
            passed &= RunSelectManyTest1(1024, 1024 * 2);
            passed &= RunSelectManyTest1(1024 * 2, 1024 * 2);

            passed &= RunSelectManyTest2(0, 0);
            passed &= RunSelectManyTest2(1, 0);
            passed &= RunSelectManyTest2(0, 1);
            passed &= RunSelectManyTest2(1, 1);
            passed &= RunSelectManyTest2(32, 32);
            passed &= RunSelectManyTest2(1024 * 2, 1024);
            passed &= RunSelectManyTest2(1024, 1024 * 2);
            passed &= RunSelectManyTest2(1024 * 2, 1024 * 2);

            passed &= RunSelectManyTest3(10, ParallelExecutionMode.Default);
            passed &= RunSelectManyTest3(123, ParallelExecutionMode.Default);
            passed &= RunSelectManyTest3(10, ParallelExecutionMode.ForceParallelism);
            passed &= RunSelectManyTest3(123, ParallelExecutionMode.ForceParallelism);
            return passed;
        }

        private static bool RunSelectManyTest1(int outerSize, int innerSize)
        {
            TestHarness.TestLog("RunSelectManyTest1(outerSize = {0}, innerSize = {1}) - async/pipeline", outerSize, innerSize);

            int[] left = new int[outerSize];
            int[] right = new int[innerSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i*8;

            bool passed = true;

            TestHarness.TestLog("  > Invoking SelectMany of {0} outer elems with {1} inner elems", left.Length, right.Length);
            IEnumerable<int> results = left.AsParallel().AsOrdered().SelectMany<int, int, int>(x => right.AsParallel(), delegate(int x, int y) { return x + y; });

            // Just validate the count.
            int cnt = 0;
            foreach (int p in results)
                cnt++;

            int expect = outerSize*innerSize;
            passed &= (cnt == expect);
            TestHarness.TestLog("  > Saw expected count? Saw = {0}, expect = {1}: {2}", cnt, expect, passed);

            return passed;
        }

        private static bool RunSelectManyTest2(int outerSize, int innerSize)
        {
            TestHarness.TestLog("RunSelectManyTest2(outerSize = {0}, innerSize = {1}) - sync/ no pipeline", outerSize, innerSize);

            int[] left = new int[outerSize];
            int[] right = new int[innerSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i*8;

            bool passed = true;

            TestHarness.TestLog("  > Invoking SelectMany of {0} outer elems with {1} inner elems", left.Length, right.Length);
            ParallelQuery<int> results = left.AsParallel().AsOrdered().SelectMany<int, int, int>(x => right.AsParallel(), delegate(int x, int y) { return x + y; });
            List<int> r = results.ToList<int>();

            // Just validate the count.
            int cnt = 0;
            foreach (int p in r)
                cnt++;

            int expect = outerSize*innerSize;
            passed &= (cnt == expect);
            TestHarness.TestLog("  > Saw expected count? Saw = {0}, expect = {1}: {2}", cnt, expect, passed);

            return passed;
        }

        /// <summary>
        /// Tests OrderBy() followed by a SelectMany, where the outer input sequence
        /// contains duplicates.
        /// </summary>
        private static bool RunSelectManyTest3(int size, ParallelExecutionMode mode)
        {
            TestHarness.TestLog("RunSelectManyTest3(size = {0}", size);

            int[] srcOuter = Enumerable.Repeat(0, size).ToArray();
            int[] srcInner = Enumerable.Range(0, size).ToArray();

            IEnumerable<int> query = 
                srcOuter.AsParallel()
                .WithExecutionMode(mode)
                .OrderBy(x => x)
                .SelectMany(x => srcInner);

            int next = 0;
            foreach (var x in query)
            {
                if (x != next)
                {
                    TestHarness.TestLog("  Failed: expected {0} got {1}", next, x);
                    return false;
                }
                next = (next+1)%size;
            }

            return true;
        }


        //
        // ForAll
        //

        internal static bool RunForAllTests()
        {
            bool passed = true;

            passed &= RunForAllTest1(0);
            passed &= RunForAllTest1(1024);
            passed &= RunForAllTest1(1024 * 8);
            passed &= RunForAllTest1(1024 * 1024 * 2);

            return passed;
        }

        internal static bool RunForAllTest1(int dataSize)
        {
            TestHarness.TestLog("* RunForAllTest1({0}) - over an array", dataSize);

            int[] left = new int[dataSize];
            for (int i = 0; i < dataSize; i++) left[i] = i+1;

            long counter = 0;

            // Just ensure that an addition done in parallel (with interlocked ops) adds
            // up to the correct sum in the end, i.e. no item goes missing or gets added.

            left.AsParallel().AsOrdered().ForAll<int>(
                delegate(int x) { Interlocked.Add(ref counter, x); });

            long expect;
            checked {
                expect = (long)(((float)dataSize / 2) * (dataSize + 1));
            }

            TestHarness.TestLog("  > Expected a sum of {0} - found {1}", expect, counter);
            bool passed = (counter == expect);
            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");

            return passed;
        }

        //
        // OrderBy
        //

        internal static bool RunOrderByTests()
        {
            bool passed = true;

            // Non-pipelining tests (synchronous channels).

            passed &= RunOrderByTest1(0, false, DataDistributionType.AlreadyAscending);
            passed &= RunOrderByTest1(0, true, DataDistributionType.AlreadyAscending);

            passed &= RunOrderByTest1(50, false, DataDistributionType.AlreadyAscending);
            passed &= RunOrderByTest1(50, false, DataDistributionType.AlreadyDescending);
            passed &= RunOrderByTest1(50, false, DataDistributionType.Random);
            passed &= RunOrderByTest1(50, true, DataDistributionType.AlreadyAscending);
            passed &= RunOrderByTest1(50, true, DataDistributionType.AlreadyDescending);
            passed &= RunOrderByTest1(50, true, DataDistributionType.Random);

            passed &= RunOrderByTest1(1024 * 128, false, DataDistributionType.AlreadyAscending);
            passed &= RunOrderByTest1(1024 * 128, false, DataDistributionType.AlreadyDescending);
            passed &= RunOrderByTest1(1024 * 128, false, DataDistributionType.Random);
            passed &= RunOrderByTest1(1024 * 128, true, DataDistributionType.AlreadyAscending);
            passed &= RunOrderByTest1(1024 * 128, true, DataDistributionType.AlreadyDescending);
            passed &= RunOrderByTest1(1024 * 128, true, DataDistributionType.Random);

            // Pipelining tests (asynchronous channels).

            passed &= RunOrderByTest2(1024 * 128, false, DataDistributionType.AlreadyAscending);
            passed &= RunOrderByTest2(1024 * 128, false, DataDistributionType.AlreadyDescending);
            passed &= RunOrderByTest2(1024 * 128, false, DataDistributionType.Random);
            passed &= RunOrderByTest2(1024 * 128, true, DataDistributionType.AlreadyAscending);
            passed &= RunOrderByTest2(1024 * 128, true, DataDistributionType.AlreadyDescending);
            passed &= RunOrderByTest2(1024 * 128, true, DataDistributionType.Random);

            // Try some composition tests (i.e. wrapping).

            passed &= RunOrderByComposedWithWhere1(1024 * 128, false, DataDistributionType.Random);
            passed &= RunOrderByComposedWithWhere1(1024 * 128, true, DataDistributionType.Random);
            passed &= RunOrderByComposedWithWhere2(1024 * 128, false, DataDistributionType.Random);
            passed &= RunOrderByComposedWithWhere2(1024 * 128, true, DataDistributionType.Random);
            passed &= RunOrderByComposedWithJoinJoin(32, 32, false);
            passed &= RunOrderByComposedWithJoinJoin(32, 32, true);
            passed &= RunOrderByComposedWithJoinJoin(1024 * 512, 1024 * 128, false);
            passed &= RunOrderByComposedWithJoinJoin(1024 * 512, 1024 * 128, true);
            passed &= RunOrderByComposedWithWhereWhere1(1024 * 128, false, DataDistributionType.Random);
            passed &= RunOrderByComposedWithWhereWhere1(1024 * 128, true, DataDistributionType.Random);
            passed &= RunOrderByComposedWithWhereSelect1(1024 * 128, false, DataDistributionType.Random);
            passed &= RunOrderByComposedWithWhereSelect1(1024 * 128, true, DataDistributionType.Random);

            passed &= RunOrderByComposedWithOrderBy(1024 * 128, false, DataDistributionType.Random);
            passed &= RunOrderByComposedWithOrderBy(1024 * 128, true, DataDistributionType.Random);
            passed &= RunOrderByComposedWithOrderBy(1024 * 128, false, DataDistributionType.Random);
            passed &= RunOrderByComposedWithOrderBy(1024 * 128, true, DataDistributionType.Random);

            // Stable sort.
            passed &= RunStableSortTest1(1024);
            passed &= RunStableSortTest1(1024 * 128);

            return passed;
        }

        enum DataDistributionType
        {
            AlreadyAscending,
            AlreadyDescending,
            Random
        }

        private static int[] CreateOrderByInput(int dataSize, DataDistributionType type)
        {
            int[] data = new int[dataSize];
            switch (type)
            {
                case DataDistributionType.AlreadyAscending:
                    for (int i = 0; i < data.Length; i++) data[i] = i;
                    break;
                case DataDistributionType.AlreadyDescending:
                    for (int i = 0; i < data.Length; i++) data[i] = dataSize - i;
                    break;
                case DataDistributionType.Random:
                    Random rand = new Random();
                    for (int i = 0; i < data.Length; i++) data[i] = rand.Next();
                    break;
            }
            return data;
        }

        //-----------------------------------------------------------------------------------
        // Exercises basic OrderBy behavior by sorting a fixed set of integers. This always
        // uses synchronous channels internally, i.e. by not pipelining.
        //

        private static bool RunOrderByTest1(int dataSize, bool descending, DataDistributionType type)
        {
            TestHarness.TestLog("RunOrderByTest1(dataSize = {0}, descending = {1}, type = {2}) - synchronous/no pipeline",
                dataSize, descending, type);

            int[] data = CreateOrderByInput(dataSize, type);

            ParallelQuery<int> q;
            if (descending)
            {
                q = data.AsParallel().OrderByDescending<int, int>(
                    delegate(int x) { return x; });
            }
            else
            {
                q = data.AsParallel().OrderBy<int, int>(
                    delegate(int x) { return x; });
            }

            // Force synchronous execution before validating results.
            List<int> r = q.ToList<int>();

            int prev = descending ? int.MaxValue : int.MinValue;
            for (int i = 0; i < r.Count; i++)
            {
                int x = r[i];

                if (descending ? x > prev : x < prev)
                {
                    TestHarness.TestLog("  > **ERROR** {0} came before {1} -- but isn't what we expected", prev, x);
                    return false;
                }

                prev = x;
            }

            return true;
        }

        //-----------------------------------------------------------------------------------
        // Exercises basic OrderBy behavior by sorting a fixed set of integers. This always
        // uses asynchronous channels internally, i.e. by pipelining.
        //

        private static bool RunOrderByTest2(int dataSize, bool descending, DataDistributionType type)
        {
            TestHarness.TestLog("RunOrderByTest2(dataSize = {0}, descending = {1}, type = {2}) - asynchronous/pipeline",
                dataSize, descending, type);

            int[] data = CreateOrderByInput(dataSize, type);

            ParallelQuery<int> q;
            if (descending)
            {
                q = data.AsParallel().OrderByDescending<int, int>(
                    delegate(int x) { return x; });
            }
            else
            {
                q = data.AsParallel().OrderBy<int, int>(
                    delegate(int x) { return x; });
            }

            int prev = descending ? int.MaxValue : int.MinValue;
            foreach (int x in q)
            {
                if (descending ? x > prev : x < prev)
                {
                    TestHarness.TestLog("  > **ERROR** {0} came before {1} -- but isn't what we expected", prev, x);
                    return false;
                }

                prev = x;
            }

            return true;
        }

        //-----------------------------------------------------------------------------------
        // If sort is followed by another operator, we need to preserve key ordering all the
        // way back up to the merge. That is true even if some elements are missing in the
        // output data stream. This test tries to compose ORDERBY with WHERE. This test
        // processes output sequentially (not pipelined).
        //

        private static bool RunOrderByComposedWithWhere1(int dataSize, bool descending, DataDistributionType type)
        {
            TestHarness.TestLog("RunOrderByComposedWithWhere1(dataSize = {0}, descending = {1}, type = {2}) - sequential/no pipeline",
                dataSize, descending, type);

            int[] data = CreateOrderByInput(dataSize, type);

            ParallelQuery<int> q;

            // Create the ORDERBY:
            if (descending)
            {
                q = data.AsParallel().OrderByDescending<int, int>(
                    delegate(int x) { return x; });
            }
            else
            {
                q = data.AsParallel().OrderBy<int, int>(
                    delegate(int x) { return x; });
            }

            // Wrap with a WHERE:
            q = q.Where<int>(delegate(int x) { return (x % 2) == 0; });

            // Force synchronous execution before validating results.
            List<int> results = q.ToList<int>();

            int prev = descending ? int.MaxValue : int.MinValue;
            for (int i = 0; i < results.Count; i++)
            {
                int x = results[i];

                if (descending ? x > prev : x < prev)
                {
                    TestHarness.TestLog("  > **ERROR** {0} came before {1} -- but isn't what we expected", prev, x);
                    return false;
                }

                prev = x;
            }

            return true;
        }

        //-----------------------------------------------------------------------------------
        // If sort is followed by another operator, we need to preserve key ordering all the
        // way back up to the merge. That is true even if some elements are missing in the
        // output data stream. This test tries to compose ORDERBY with WHERE. This test
        // processes output asynchronously via pipelining.
        //

        private static bool RunOrderByComposedWithWhere2(int dataSize, bool descending, DataDistributionType type)
        {
            TestHarness.TestLog("RunOrderByComposedWithWhere2(dataSize = {0}, descending = {1}, type = {2}) - async/pipeline",
                dataSize, descending, type);

            int[] data = CreateOrderByInput(dataSize, type);

            ParallelQuery<int> q;

            // Create the ORDERBY:
            if (descending)
            {
                q = data.AsParallel().OrderByDescending<int, int>(
                    delegate(int x) { return x; });
            }
            else
            {
                q = data.AsParallel().OrderBy<int, int>(
                    delegate(int x) { return x; });
            }

            // Wrap with a WHERE:
            q = q.Where<int>(delegate(int x) { return (x % 2) == 0; });

            int prev = descending ? int.MaxValue : int.MinValue;
            foreach (int x in q)
            {
                if (descending ? x > prev : x < prev)
                {
                    TestHarness.TestLog("  > **ERROR** {0} came before {1} -- but isn't what we expected", prev, x);
                    return false;
                }

                prev = x;
            }

            return true;
        }

        private static bool RunOrderByComposedWithJoinJoin(int outerSize, int innerSize, bool descending)
        {
            TestHarness.TestLog("RunOrderByComposedWithJoinJoin(outerSize = {0}, innerSize = {1}, descending = {2})", outerSize, innerSize, descending);

            // Generate data in the reverse order in which it'll be sorted.
            DataDistributionType type = descending ? DataDistributionType.AlreadyAscending : DataDistributionType.AlreadyDescending;

            int[] left = CreateOrderByInput(outerSize, type);
            int[] right = CreateOrderByInput(innerSize, type);
            int[] middle = new int[Math.Min(outerSize, innerSize)];
            if (descending)
                for (int i = middle.Length; i > 0; i--)
                    middle[i - 1] = i;
            else
                for (int i = 0; i < middle.Length; i++)
                    middle[i] = i;

            Func<int, int> identityKeySelector = delegate(int x) { return x; };

            // Create the sort object.
            ParallelQuery<int> sortedLeft;
            if (descending)
            {
                sortedLeft = left.AsParallel().OrderByDescending<int, int>(identityKeySelector);
            }
            else
            {
                sortedLeft = left.AsParallel().OrderBy<int, int>(identityKeySelector);
            }

            // and now the join...
            ParallelQuery<Pair<int, int>> innerJoin = sortedLeft.Join<int, int, int, Pair<int, int>>(
                right.AsParallel(), identityKeySelector, identityKeySelector, delegate(int x, int y) { return new Pair<int, int>(x, y); });
            ParallelQuery<int> outerJoin = innerJoin.Join<Pair<int, int>, int, int, int>(
                middle.AsParallel(), delegate(Pair<int, int> p) { return p.First; }, identityKeySelector, delegate(Pair<int, int> x, int y) { return x.First; });

            bool passed = true;

            TestHarness.TestLog("  > Invoking join of {0} outer elems with {1} inner elems", left.Length, right.Length);

            // Ensure pairs are of equal values, and that they are in ascending or descending order.
            int cnt = 0;
            int? last = null;
            foreach (int p in outerJoin)
            {
                cnt++;
                if (!(passed &= (last == null || ((last.Value <= p && !descending) || (last.Value >= p && descending)))))
                {
                    TestHarness.TestLog("  > *ERROR: sort order not correct: last = {0}, curr = {1}", last.Value, p);
                    break;
                }
                last = p;
            }

            TestHarness.TestLog("  ** {0}   ({1} elems)", passed ? "Success" : "FAIL", cnt);
            return passed;
        }

        //-----------------------------------------------------------------------------------
        // If sort is followed by another operator, we need to preserve key ordering all the
        // way back up to the merge. That is true even if some elements are missing in the
        // output data stream. This test tries to compose ORDERBY with two WHEREs. This test
        // processes output sequentially (not pipelined).
        //

        private static bool RunOrderByComposedWithWhereWhere1(int dataSize, bool descending, DataDistributionType type)
        {
            TestHarness.TestLog("RunOrderByComposedWithWhereWhere1(dataSize = {0}, descending = {1}, type = {2}) - sequential/no pipeline",
                dataSize, descending, type);

            int[] data = CreateOrderByInput(dataSize, type);

            ParallelQuery<int> q;

            // Create the ORDERBY:
            if (descending)
            {
                q = data.AsParallel().OrderByDescending<int, int>(
                    delegate(int x) { return x; });
            }
            else
            {
                q = data.AsParallel().OrderBy<int, int>(
                    delegate(int x) { return x; });
            }

            // Wrap with a WHERE:
            q = q.Where<int>(delegate(int x) { return (x % 2) == 0; });
            // Wrap with another WHERE:
            q = q.Where<int>(delegate(int x) { return (x % 4) == 0; });

            // Force synchronous execution before validating results.
            List<int> results = q.ToList<int>();

            int prev = descending ? int.MaxValue : int.MinValue;
            foreach (int x in results)
            {
                if (descending ? x > prev : x < prev)
                {
                    TestHarness.TestLog("  > **ERROR** {0} came before {1} -- but isn't what we expected", prev, x);
                    return false;
                }

                prev = x;
            }

            return true;
        }

        //-----------------------------------------------------------------------------------
        // If sort is followed by another operator, we need to preserve key ordering all the
        // way back up to the merge. That is true even if some elements are missing in the
        // output data stream. This test tries to compose ORDERBY with WHERE and SELECT.
        // This test processes output sequentially (not pipelined).
        //
        // This is particularly interesting because the SELECT completely loses the original
        // type information in the tree, yet the merge is able to put things back in order.
        //

        private static bool RunOrderByComposedWithWhereSelect1(int dataSize, bool descending, DataDistributionType type)
        {
            TestHarness.TestLog("RunOrderByComposedWithWhereSelect1(dataSize = {0}, descending = {1}, type = {2}) - sequential/no pipeline",
                dataSize, descending, type);

            int[] data = CreateOrderByInput(dataSize, type);

            ParallelQuery<int> q0;

            // Create the ORDERBY:
            if (descending)
            {
                q0 = data.AsParallel().OrderByDescending<int, int>(
                    delegate(int x) { return x; });
            }
            else
            {
                q0 = data.AsParallel().OrderBy<int, int>(
                    delegate(int x) { return x; });
            }

            // Wrap with a WHERE:
            q0 = q0.Where<int>(delegate(int x) { return (x % 2) == 0; });

            // Wrap with a SELECT:
            ParallelQuery<string> q1 = q0.Select<int, string>(delegate(int x) { return x.ToString(); });

            // Force synchronous execution before validating results.
            List<string> results = q1.ToList<string>();

            int prev = descending ? int.MaxValue : int.MinValue;
            foreach (string xs in results)
            {
                int x = int.Parse(xs);

                if (descending ? x > prev : x < prev)
                {
                    TestHarness.TestLog("  > **ERROR** {0} came before {1} -- but isn't what we expected", prev, x);
                    return false;
                }

                prev = x;
            }

            return true;
        }

        //-----------------------------------------------------------------------------------
        // Nested sorts.
        //

        private static bool RunOrderByComposedWithOrderBy(int dataSize, bool descending, DataDistributionType type)
        {
            TestHarness.TestLog("RunOrderByComposedWithOrderBy(dataSize = {0}, descending = {1}, type = {2}) - sequential/no pipeline",
                dataSize, descending, type);

            int[] data = CreateOrderByInput(dataSize, type);

            ParallelQuery<int> q;

            // Create the ORDERBY:
            if (!descending)
            {
                q = data.AsParallel().OrderByDescending<int, int>(
                    delegate(int x) { return x; });
            }
            else
            {
                q = data.AsParallel().OrderBy<int, int>(
                    delegate(int x) { return x; });
            }

            // Wrap with a WHERE:
            q = q.Where<int>(delegate(int x) { return (x % 2) == 0; });

            // And wrap with another ORDERBY:
            if (descending)
            {
                q = q.OrderByDescending<int, int>(delegate(int x) { return x; });
            }
            else
            {
                q = q.OrderBy<int, int>(delegate(int x) { return x; });
            }

            // Force synchronous execution before validating results.
            List<int> results = q.ToList<int>();

            int prev = descending ? int.MaxValue : int.MinValue;
            for (int i = 0; i < results.Count; i++)
            {
                int x = results[i];

                if (descending ? x > prev : x < prev)
                {
                    TestHarness.TestLog("  > **ERROR** {0} came before {1} -- but isn't what we expected", prev, x);
                    return false;
                }

                prev = x;
            }

            return true;
        }

        //-----------------------------------------------------------------------------------
        // Stable sort implementation: uses indices, OrderBy, ThenBy.
        //

        class SSC
        {
            public int SortKey;
            public int Index;
            public SSC(int key, int idx)
            {
                SortKey = key; Index = idx;
            }
        }

        class SSD
        {
            public SSC c;
            public int idx;
            public SSD(SSC c, int idx)
            {
                this.c = c; this.idx = idx;
            }
        }

        private static bool RunStableSortTest1(int dataSize)
        {
            TestHarness.TestLog("RunStableSortTest1(dataSize = {0}) - synchronous/no pipeline", dataSize);

            SSC[] clist = new SSC[dataSize];
            for (int i = 0; i < clist.Length; i++) {
                clist[i] = new SSC((dataSize-i) / (dataSize/5), i);
            }

            bool passed = true;

            IEnumerable<SSC> clistSorted = clist.AsParallel().Select<SSC,SSD>((c,i)=> new SSD(c,i)).OrderBy<SSD,int>((c)=>c.c.SortKey).ThenBy<SSD,int>((c)=>c.idx).Select((c)=>c.c);
            int lastKey = -1, lastIdx = -1;
            foreach (SSC c in clistSorted) {
                if (c.SortKey < lastKey)
                {
                    passed = false;
                    TestHarness.TestLog("  > Keys not in ascending order: {0} expected to be <= {1}", lastKey, c.SortKey);
                }
                else if (c.SortKey == lastKey && c.Index < lastIdx)
                {
                    passed = false;
                    TestHarness.TestLog("  > Instability on equal keys: {0} expected to be <= {1}", lastIdx, c.Index);
                }

                lastKey = c.SortKey;
                lastIdx = c.Index;
            }

            return passed;
        }

        //
        // ToArray
        //

        internal static bool RunToArrayTests()
        {
            bool passed = true;

            passed &= RunToArray(1);
            passed &= RunToArray(8);
            passed &= RunToArray(1297);

            return passed;
        }

        private static bool RunToArray(int size)
        {
            TestHarness.TestLog("* RunToArray(size={0}) *", size);

            int[] xx = new int[size];
            for (int i = 0; i < size; i++) xx[i] = i;

            int[] a = xx.AsParallel().AsOrdered().Select(x=>x).ToArray<int>();

            bool passed = (size == a.Length);
            TestHarness.TestLog("  > Resulting array size is {0} -- expected {1}", a.Length, size);

            int prev = -1;
            foreach (int x in a)
            {
                if (x != prev + 1)
                {
                    TestHarness.TestLog("  > Missing element in sequence - went from {0} to {1}", prev, x);
                    passed = false;
                }
                prev = x;
            }

            return passed;
        }

        //
        // ToDictionary
        //

        internal static bool RunToDictionaryTests()
        {
            bool passed = true;

            passed &= RunToDictionary1();
            passed &= RunToDictionary2();

            return passed;
        }


        // A helper class: a custom IEqualityComparer
        class ModularCongruenceComparer : IEqualityComparer<int>
        {
            private int m_Mod;

            public ModularCongruenceComparer(int mod)
            {
                m_Mod = mod;
            }

            private int leastPositiveResidue(int x)
            {
                return ((x % m_Mod) + m_Mod) % m_Mod;
            }

            public bool Equals(int x, int y)
            {
                return leastPositiveResidue(x) == leastPositiveResidue(y);
            }

            public int GetHashCode(int x)
            {
                return leastPositiveResidue(x).GetHashCode();
            }

            public int GetHashCode(object obj)
            {
                return GetHashCode((int)obj);
            }
        }

        // Tests ToDictionary<TSource,TKey> with the default equality comparator
        private static bool RunToDictionary1()
        {
            TestHarness.TestLog("* RunToDictionary1() *");

            int size = 10000;
            int[] xx = new int[size];
            for (int i = 0; i < size; i++) xx[i] = i;

            Dictionary<int, int> a = xx.AsParallel().ToDictionary<int, int>(v => (2 * v));

            bool passed = (size == a.Count);
            TestHarness.TestLog("  > Resulting Dictionary size is {0} -- expected {1}", a.Count, size);

            for (int i = 0; i < size; i++)
            {
                if (a[2 * i] != i)
                {
                    TestHarness.TestLog("  > Value a[{0}] is {1} -- expected {2}", 2 * i, a[2 * i], i);
                    passed = false;
                }
            }

            return passed;
        }

        // Tests ToDictionary<TSource,TKey,TElement> with a custom equality comparator
        private static bool RunToDictionary2()
        {
            TestHarness.TestLog("* RunToDictionary2() *");

            int size = 1009;
            int p = 7; // GCD(size,p) = 1
            int[] xx = new int[size];
            for (int i = 0; i < size; i++) xx[i] = i;

            Dictionary<int, int> a = xx.AsParallel().ToDictionary<int, int, int>(
                v => ((p * v) % size),
                v => (v + 1),
                new ModularCongruenceComparer(size));

            bool passed = (size == a.Count);
            TestHarness.TestLog("  > Resulting Dictionary size is {0} -- expected {1}", a.Count, size);

            // Since GCD(size,p) = 1, we know that no two elements of xx should be mapped to the same index in a[]
            for (int i = 0; i < size; i++)
            {
                if (a[ (p * i) % size ] != i + 1)
                {
                    TestHarness.TestLog("  > Value a[{0}] is {1} -- expected {2}", (p * i) % size, a[(p * i) % size], i + 1);
                    passed = false;
                }
            }

            return passed;
        }

        //
        // ToLookup
        //

        internal static bool RunToLookupTests()
        {
            bool passed = true;

            passed &= RunToLookup1();
            passed &= RunToLookup2();
            passed &= RunToLookupOnInput(new string[] { });
            passed &= RunToLookupOnInput(new string[] { null, null, null });
            passed &= RunToLookupOnInput(new string[] { "aaa", "aaa", "aaa" });
            passed &= RunToLookupOnInput(new string[] { "aaa", null, "abb", "abc", null, "aaa" });

            return passed;
        }

        // Tests ToLookup<TSource,TKey> with the default equality comparator
        private static bool RunToLookup1()
        {
            TestHarness.TestLog("* RunToLookup1() *");

            int M = 1000;
            int size = 2 * M;

            int[] xx = new int[size];
            for (int i = 0; i < size; i++) xx[i] = i;

            ILookup<int, int> lookup = xx.AsParallel().ToLookup<int, int>(v => (v % M));

            bool passed = (M == lookup.Count);
            TestHarness.TestLog("  > Resulting Lookup size is {0} -- expected {1}", lookup.Count, M);

            // Test this[] on Lookup
            for (int i = 0; i < M; i++)
            {
                int[] vals = lookup[i].ToArray();
                
                if (vals.Length != 2)
                {
                    TestHarness.TestLog("  > Resulting Lookup size is {0} -- expected {1}", vals.Length, 2);
                    passed = false;
                    continue;
                }

                int mn = Math.Min(vals[0], vals[1]), mx = Math.Max(vals[0], vals[1]);
                if (mn != i || mx != i + M)
                {
                    TestHarness.TestLog("  > For key {0} got values ({0},{1}) -- expected ({2},{3})", mn, mx, i, i + M);
                    passed = false;
                }             
            }

            // Test GetEnumerator() on Lookup
            HashSet<int> seen = new HashSet<int>();
            foreach(IGrouping<int, int> grouping in lookup)
            {
                if (grouping.Key < 0 || grouping.Key >= M)
                {
                    TestHarness.TestLog("  > Lookup enumerator returned key {0}, when expected range is [{0},{1})", grouping.Key, 0, M);
                    passed = false;
                }

                if (seen.Contains(grouping.Key))
                {
                    TestHarness.TestLog("  > Lookup enumerator returned duplicate (key,value) pairs with the same key = {0}", grouping.Key);
                    passed = false;
                }
                seen.Add(grouping.Key);
            }

            // Test that invalid key access returns an empty enumerable.
            IEnumerable<int> ie = lookup[-1];
            if (ie.Count() != 0)
            {
                TestHarness.TestLog("  > Lookup did not return empty sequence for an invalid key");
                passed = false;
            }

            return passed;
        }

        // Tests ToLookup<TSource,TKey,TElement> with a custom equality comparator
        private static bool RunToLookup2()
        {
            TestHarness.TestLog("* RunToLookup2() *");

            int M = 1000; // M is prime
            int size = 2 * M;

            int[] xx = new int[size];
            for (int i = 0; i < size; i++) xx[i] = i;

            ILookup<int, int> lookup = xx.AsParallel().ToLookup<int, int, int>(
                v => (v % M), 
                v => (v + 1), 
                new ModularCongruenceComparer(M));

            bool passed = (M == lookup.Count);
            TestHarness.TestLog("  > Resulting Lookup size is {0} -- expected {1}", lookup.Count, M);

            for (int i = 0; i < M; i++)
            {
                int[] vals = lookup[i].ToArray();

                if (vals.Length != 2)
                {
                    TestHarness.TestLog("  > Resulting Lookup size is {0} -- expected {1}", vals.Length, 2);
                    passed = false;
                    continue;
                }

                int mn = Math.Min(vals[0], vals[1]), mx = Math.Max(vals[0], vals[1]);
                if (mn != 1 + i || mx != 1 + i + M)
                {
                    TestHarness.TestLog("  > For key {0} got values ({0},{1}) -- expected ({2},{3})", mn, mx, 1 + i, 1 + i + M);
                    passed = false;
                }
            }

            return passed;
        }

        private static bool RunToLookupOnInput(string[] list)
        {
            TestHarness.TestLog("* RunToLookupOnInput() *");

            ILookup<string, string> lookupPlinq = list.AsParallel().Select(i => i).ToLookup<string, string, string>(
                i => i,  i=> i);
            ILookup<string, string> lookupLinq = Enumerable.ToLookup<string,string,string>(list, i => i, i => i);

            if (lookupPlinq.Count != lookupLinq.Count)
            {
                TestHarness.TestLog("  > Resulting Lookup size is {0} -- expected {1}", lookupPlinq.Count, list.Length);
                return false;
            }

            foreach(IGrouping<string,string> grouping in lookupLinq)
            {
                if (!lookupPlinq.Contains(grouping.Key))
                {
                    TestHarness.TestLog("  > Lookup does not contain key {0}", grouping.Key);
                    return false;
                }

                if (!lookupPlinq[grouping.Key].OrderBy(i => i).SequenceEqual(grouping.OrderBy(i => i)))
                {
                    TestHarness.TestLog("  > Lookup sequence for key {0} is not correct.", grouping.Key);
                    return false;
                }
            }

            int count = 0;
            foreach (IGrouping<string, string> grouping in lookupPlinq)
            {
                if (!lookupLinq.Contains(grouping.Key))
                {
                    TestHarness.TestLog("  > Lookup contains extra key {0}", grouping.Key);
                    return false;
                }
                count++;
            }

            if (count != lookupLinq.Count)
            {
                TestHarness.TestLog("  > Lookup enumerator iterated over {0} elements -- {1} expected", count, lookupLinq.Count);
                return false;
            }

            return true;
        }

        //
        // exceptions...
        //

        internal static bool RunExceptionTests()
        {
            bool passed = true;

            passed &= RunExceptionTestSync1();
            passed &= RunExceptionTestAsync1();
            passed &= RunExceptionTestForAll1();
            passed &= RunExceptionDispose();

            return passed;
        }

        private static bool RunExceptionTestSync1()
        {
            TestHarness.TestLog("* RunExceptionTestSync1 *");

            int[] xx = new int[1024];
            for (int i = 0; i < xx.Length; i++) xx[i] = i;
            ParallelQuery<int> q = xx.AsParallel().Select<int, int>(
                delegate(int x) { if ((x % 250) == 249) { throw new Exception("Fail!"); } return x; });

            bool pass = false;
            try
            {
                List<int> aa = q.ToList<int>();
            }
            catch (Exception e)
            {
                
                AggregateException pfe = e as AggregateException;
                if (pfe != null)
                {
                    pass = true;
                    //for (int i = 0; i < pfe.InnerExceptions.Count; i++)
                    //{
                    //    TestHarness.TestLog("  > InnerException[{0}] = {1}", i, pfe.InnerExceptions[i]);
                    //}
                }
                else
                {
                    TestHarness.TestLog("  > Incorrect caught exception of type {0}", e.GetType());
                    TestHarness.TestLog("  > Ex details = {0}", e);
                }
            }

            return pass;
        }

        private static bool RunExceptionTestAsync1()
        {
            TestHarness.TestLog("* RunExceptionTestAsync1 *");

            int[] xx = new int[1024];
            for (int i = 0; i < xx.Length; i++) xx[i] = i;
            ParallelQuery<int> q = xx.AsParallel().Select<int, int>(
                delegate(int x) { if ((x % 250) == 249) { throw new Exception("Fail!");  } return x; });

            bool pass = false;
            try
            {
                foreach (int y in q) { }
            }
            catch (Exception e)
            {
                AggregateException pfe = e as AggregateException;
                if (pfe != null)
                {
                    pass = true;
                    //for (int i = 0; i < pfe.InnerExceptions.Count; i++)
                    //{
                    //    TestHarness.TestLog("  > InnerException[{0}] = {1}", i, pfe.InnerExceptions[i]);
                    //}
                }
                else
                {
                    TestHarness.TestLog("  > Incorrect caught exception of type {0}", e.GetType());
                    TestHarness.TestLog("  > Ex details = {0}", e);
                }
            }

            return pass;
        }

        private static bool RunExceptionTestForAll1()
        {
            TestHarness.TestLog("* RunExceptionTestForAll1 *");

            int[] xx = new int[1024];
            for (int i = 0; i < xx.Length; i++) xx[i] = i;
            ParallelQuery<int> q = xx.AsParallel().Select<int, int>(
                delegate(int x) { if ((x % 250) == 249) { throw new Exception("Fail!"); } return x; });

            bool pass = false;
            try
            {
                q.ForAll<int>(delegate(int x) { });
            }
            catch (Exception e)
            {
                AggregateException pfe = e as AggregateException;
                if (pfe != null)
                {
                    pass = true;
                    //for (int i = 0; i < pfe.InnerExceptions.Count; i++)
                    //{
                    //    TestHarness.TestLog("  > InnerException[{0}] = {1}", i, pfe.InnerExceptions[i]);
                    //}
                }
                else
                {
                    TestHarness.TestLog("  > Incorrect caught exception of type {0}", e.GetType());
                    TestHarness.TestLog("  > Ex details = {0}", e);
                }
            }

            return pass;
        }

        internal static bool RunExceptionDispose()
        {
            TestHarness.TestLog("* RunExceptionDispose *");
            bool passed = true;

            // Case 1:
            // Begin execution (with an unhandled exception); we will call Dispose
            // before consuming the output, and thus should see an exception out of it.
            // Specifially, we want to:
            //    MoveNext and get an item before exceptions throw
            //    then wait and ensure the fully async enumeration has completed
            //    then call Dispose.. which should throw an AggregateException
            // Approach: last yielded item has a delay before production, then throws.
            //           consumer reads first item easily before the exception, then waits till after the exception.
            //           This is timing-dependent.. a deterministic approach could poll until the query has accumulated a TPL-exception (tricky)
            {
                AggregateException caughtException = null;
                int[] a = new int[8096]; a[8095] = 99; //need at least 512 to fill up one of the async-channel buffers.
                var q = a.AsParallel()
                    .WithDegreeOfParallelism(2) // DOP=2 to reduce racing and striping affecting this test.
                                                // We can't use DOP=1 because PLINQ would not create a worker task in that case.
                    .Select<int, int>(x => { if (x == 99) { Thread.Sleep(100); throw new Exception("foo"); } else return x; });
                var e = q.GetEnumerator();
                e.MoveNext();
                Thread.Sleep(500); // wait to give the producers time to finish and throw
                try { e.Dispose(); }
                catch (AggregateException aex) {
                    caughtException = aex;
                    /* expected; swallow */
                }

                if (System.Linq.Parallel.Scheduling.DefaultDegreeOfParallelism > 1)
                {
                    passed &= TestHarnessAssert.IsNotNull(caughtException, "[Note: timing-sensitive test] An AggregateException should have been thrown out of Dispose");
                }
                else
                {
                    TestHarness.TestLog("    Test does not apply to the DOP=1 case.");
                }
            }

            // Case 2:
            // Begin execution (with an unhandled exception); we will enumerate and
            // catch the exception, and then verify that Dispose doesn't throw.

            {
                AggregateException caughtException = null;
                int[] a = new int[8]; a[7] = 99;
                var q = a.AsParallel().WithDegreeOfParallelism(1).
                    Select<int, int>(x => { if (x != 0) throw new Exception("foo"); else return x; });
                var e = q.GetEnumerator();
                try { while (e.MoveNext()) { } }
                catch (AggregateException aex) 
                {
                    caughtException = aex;
                    /* expected; swallow */ 
                }
                passed &= TestHarnessAssert.IsNotNull(caughtException, "An AggregateException should have been thrown out of MoveNext");

                e.Dispose(); /* shouldn't throw */
            }

            return passed;
        }

        //
        // Range
        //

        internal static bool RunRangeTests()
        {
            bool passed = true;

            passed &= RunRangeTest1(0, 100);
            passed &= RunRangeTest1(50, 75);
            passed &= RunRangeTest1(-10, 1033);
            passed &= RunRangeTest1(100, 0);
            passed &= RunRangeTest1(int.MaxValue, 1);
            passed &= RunRangeTest1(int.MaxValue-9, 10);

            return passed;
        }

        private static bool RunRangeTest1(int from, int count)
        {
            TestHarness.TestLog("* RunRangeTest1(from={0}, count={1})  *", from, count);

            bool passed = true;
            
            passed &= Enumerable.Range(from, count).SequenceEqual(
                ParallelEnumerable.Range(from, count).AsSequential().OrderBy(i => i));

            passed &= Enumerable.Range(from, count).SequenceEqual(
                ParallelEnumerable.Range(from, count).Select(i => i).AsSequential().OrderBy(i => i));

            passed &= Enumerable.Range(from, count).SequenceEqual(
                ParallelEnumerable.Range(from, count).AsSequential().OrderBy(i=>i));

            passed &= Enumerable.Range(from, count).Take(count / 2).SequenceEqual(
                ParallelEnumerable.Range(from, count).Take(count / 2).AsSequential().OrderBy(i => i));

            passed &= Enumerable.Range(from, count).Skip(count / 2).SequenceEqual(
                ParallelEnumerable.Range(from, count).Skip(count / 2).AsSequential().OrderBy(i => i));

            passed &= Enumerable.Range(from, count).Skip(count / 2).FirstOrDefault() ==
                ParallelEnumerable.Range(from, count).Skip(count / 2).FirstOrDefault();

            passed &= Enumerable.Range(from, count).Take(count / 2).LastOrDefault() ==
                ParallelEnumerable.Range(from, count).Take(count / 2).LastOrDefault();

            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");
            return passed;
        }

        //
        // Repeat
        //

        internal static bool RunRepeatTests()
        {
            bool passed = true;

            passed &= RunRepeatTest1<int>(0, 10779, EqualityComparer<int>.Default);
            passed &= RunRepeatTest1<int>(1024 * 8, 10779, EqualityComparer<int>.Default);
            passed &= RunRepeatTest1<int>(1024 * 8, 10779, EqualityComparer<int>.Default);
            passed &= RunRepeatTest1<int>(73 * 7, 10779, EqualityComparer<int>.Default);
            passed &= RunRepeatTest1<int>(73 * 7, 0, EqualityComparer<int>.Default);
            passed &= RunRepeatTest1<object>(0, null, EqualityComparer<object>.Default);
            passed &= RunRepeatTest1<object>(1024 * 8, null, EqualityComparer<object>.Default);
            passed &= RunRepeatTest1<string>(1024 * 1024, "hello", EqualityComparer<string>.Default);

            Random r = new Random(103);
            for (int i = 0; i < 10; i++)
            {
                passed &= RunRepeatTest1<string>(r.Next(1024 * 8), "hello", EqualityComparer<string>.Default);
            }

            return passed;
        }

        private static bool RunRepeatTest1<T>(int count, T element, IEqualityComparer<T> cmp)
        {
            TestHarness.TestLog("* RunRepeatTest1<{0}>(count={1}, element={2})  --  used in a query *", typeof(T), count, element);

            bool passed = true;

            int cnt = 0;
            ParallelQuery<T> q = ParallelEnumerable.Repeat(element, count).Select<T, T>(
                delegate(T e) { return e; });
            foreach (T e in q)
            {
                cnt++;

                if (!cmp.Equals(e, element))
                {
                    TestHarness.TestLog("  > Expected {0} but found {1} instead", element, e);
                    passed = false;
                }
            }

            TestHarness.TestLog("  > Total should be {0} -- real total is {1}", count, cnt);
            passed &= cnt == count;

            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");
            return passed;
        }

        //
        // Cast and OfType
        //

        internal static bool RunCastAndOfTypeTests()
        {
            bool passed = true;

            passed &= RunCastTest1(0, true);
            passed &= RunCastTest1(1024 * 32, true);
            passed &= RunCastTest1(0, false);
            passed &= RunCastTest1(1024 * 32, false);
            passed &= RunOfTypeTest1(0, true);
            passed &= RunOfTypeTest1(1024 * 32, true);
            passed &= RunOfTypeTest1(0, false);
            passed &= RunOfTypeTest1(1024 * 32, false);

            return passed;
        }

        private static bool RunCastTest1(int count, bool useTheRightType)
        {
            TestHarness.TestLog("* RunCastTest1(count={0}, useTheRightType={1})  --  used in a query *", count, useTheRightType);

            bool passed = true;

            // Fill with ints or strings, depending on whether the test is using the correct type.
            System.Collections.ArrayList data = new System.Collections.ArrayList();
            for (int i = 0; i < count; i++)
            {
                data.Add(useTheRightType ? (object)i : "boom");
            }

            // Now check the output.
            int cnt = 0;
            ParallelQuery<int> q = data.AsParallel().Cast<int>();
            try
            {
                foreach (int e in q)
                {
                    cnt++;
                }

                passed &= useTheRightType || count == 0; // If no exception was thrown, there was a problem.
                TestHarness.TestLog("  > no exception thrown - expected? {0}", passed);
            }
            catch (AggregateException aex)
            {
                TestHarness.TestLog("  > caught exceptions - expected? {0}", !useTheRightType);
                passed &= (!useTheRightType);

                if (!aex.InnerExceptions.All(e => e is InvalidCastException))
                {
                    TestHarness.TestLog("  > some exceptions are not InvalidCastException: {0}", aex.ToString());
                    passed = false;
                }
            }

            if (useTheRightType)
            {
                TestHarness.TestLog("  > Total should be {0} -- real total is {1}", count, cnt);
                passed &= cnt == count;
            }

            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");
            return passed;
        }

        private static bool RunOfTypeTest1(int count, bool useTheRightType)
        {
            TestHarness.TestLog("* RunOfTypeTest1(count={0}, useTheRightType={1})  --  used in a query *", count, useTheRightType);

            bool passed = true;

            // Fill with ints or strings, depending on whether the test is using the correct type.
            System.Collections.ArrayList data = new System.Collections.ArrayList();
            for (int i = 0; i < count; i++)
            {
                data.Add(useTheRightType ? (object)i : "boom");
            }

            // Now check the output.
            int cnt = 0;
            ParallelQuery<int> q = data.AsParallel().OfType<int>();
            foreach (int e in q)
            {
                cnt++;
            }

            if (useTheRightType)
            {
                TestHarness.TestLog("  > Total should be {0} -- real total is {1}", count, cnt);
                passed &= cnt == count;
            }
            else
            {
                TestHarness.TestLog("  > Total should be {0} -- real total is {1}", 0, cnt);
                passed &= cnt == 0;
            }

            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");
            return passed;
        }

        //
        // Thenby
        //

        internal static bool RunThenByTests()
        {
            bool passed = true;

            // Simple Thenby tests.
            passed &= RunThenByTest1(1024 * 128, false);
            passed &= RunThenByTest1(1024 * 128, true);
            passed &= RunThenByTest2(1024 * 128, false);
            passed &= RunThenByTest2(1024 * 128, true);

            // composition tests (WHERE, WHERE/WHERE, WHERE/SELECT).
            passed &= RunThenByComposedWithWhere1(1024 * 128, false);
            passed &= RunThenByComposedWithWhere1(1024 * 128, true);
            passed &= RunThenByComposedWithJoinJoin(32, 32, false);
            passed &= RunThenByComposedWithJoinJoin(32, 32, true);
            passed &= RunThenByComposedWithJoinJoin(1024 * 512, 1024 * 128, false);
            passed &= RunThenByComposedWithJoinJoin(1024 * 512, 1024 * 128, true);
            passed &= RunThenByComposedWithWhereWhere1(1024 * 128, false);
            passed &= RunThenByComposedWithWhereWhere1(1024 * 128, true);
            passed &= RunThenByComposedWithWhereSelect1(1024 * 128, false);
            passed &= RunThenByComposedWithWhereSelect1(1024 * 128, true);

            // Multiple levels.
            passed &= RunThenByTestRecursive1(8, false);


            passed &= RunThenByTestRecursive1(1024 * 128, false);
            passed &= RunThenByTestRecursive1(1024 * 128, true);
            passed &= RunThenByTestRecursive2(1024 * 128, false);
            passed &= RunThenByTestRecursive2(1024 * 128, true);

            return passed;
        }

        private static int CompareInts(int x, int y, bool descending)
        {
            int c = x.CompareTo(y);
            if (descending)
                return -c;
            return c;
        }

        private static bool RunThenByTest1(int dataSize, bool descending)
        {
            TestHarness.TestLog("RunThenByTest1(dataSize = {0}, descending = {1}) - synchronous/no pipeline", dataSize, descending);

            Random rand = new Random();

            // We sort on a very dense (lots of dups) set of ints first, and then
            // a more volatile set, to ensure we end up stressing secondary sort logic.

            Pair<int, int>[] pairs = new Pair<int, int>[dataSize];
            for (int i = 0; i < dataSize; i++)
            {
                pairs[i].First = rand.Next(0, dataSize / 250);
                pairs[i].Second = 10 - (i % 10);
            }

            ParallelQuery<Pair<int, int>> q;
            if (descending)
            {
                q = pairs.AsParallel().OrderByDescending<Pair<int,int>, int>(delegate(Pair<int,int> x) { return x.First; })
                        .ThenByDescending<Pair<int,int>, int>(delegate(Pair<int,int> x) { return x.Second; });
            }
            else
            {
                q = pairs.AsParallel<Pair<int, int>>().OrderBy<Pair<int, int>, int>(delegate(Pair<int,int> x) { return x.First; })
                    .ThenBy<Pair<int,int>, int>(delegate(Pair<int,int> x) { return x.Second; });
            }

            // Force synchronous execution before validating results.
            List<Pair<int, int>> r = q.ToList<Pair<int, int>>();

            for (int i = 1; i < r.Count; i++)
            {
                if (CompareInts(r[i-1].First, r[i].First, descending) == 0 &&
                    CompareInts(r[i-1].Second, r[i].Second, descending) > 0)
                {
                    TestHarness.TestLog("  > **ERROR** {0}.{1} came before {2}.{3} -- but isn't what we expected",
                        r[i-1].First, r[i-1].Second, r[i].First, r[i].Second);
                    return false;
                }
            }

            return true;
        }

        //-----------------------------------------------------------------------------------
        // Exercises basic OrderBy behavior by sorting a fixed set of integers. This always
        // uses asynchronous channels internally, i.e. by pipelining.
        //

        private static bool RunThenByTest2(int dataSize, bool descending)
        {

            TestHarness.TestLog("RunThenByTest2(dataSize = {0}, descending = {1}) - asynchronous/pipeline", dataSize, descending);

            Random rand = new Random();

            // We sort on a very dense (lots of dups) set of ints first, and then
            // a more volatile set, to ensure we end up stressing secondary sort logic.

            Pair<int, int>[] pairs = new Pair<int, int>[dataSize];
            for (int i = 0; i < dataSize; i++)
            {
                pairs[i].First = rand.Next(0, dataSize / 250);
                pairs[i].Second = 10 - (i % 10);
            }

            ParallelQuery<Pair<int, int>> q;
            if (descending)
            {
                q = pairs.AsParallel<Pair<int, int>>().OrderByDescending<Pair<int,int>, int>(delegate(Pair<int,int> x) { return x.First; }).
                    ThenByDescending<Pair<int,int>, int>(delegate(Pair<int,int> x) { return x.Second; });
            }
            else
            {
                q = pairs.AsParallel<Pair<int, int>>().OrderBy<Pair<int, int>, int>(delegate(Pair<int,int> x) { return x.First; })
                    .ThenBy<Pair<int,int>, int>(delegate(Pair<int,int> x) { return x.Second; });
            }

            List<Pair<int, int>> r = new List<Pair<int, int>>();

            foreach (Pair<int, int> x in q)
            {
                r.Add(x);
            }

            for (int i = 1; i < r.Count; i++)
            {
                if (CompareInts(r[i-1].First, r[i].First, descending) == 0 &&
                    CompareInts(r[i-1].Second, r[i].Second, descending) > 0)
                {
                    TestHarness.TestLog("  > **ERROR** {0}.{1} came before {2}.{3} -- but isn't what we expected",
                        r[i-1].First, r[i-1].Second, r[i].First, r[i].Second);
                    return false;
                }
            }

            return true;
        }

        //-----------------------------------------------------------------------------------
        // If sort is followed by another operator, we need to preserve key ordering all the
        // way back up to the merge. That is true even if some elements are missing in the
        // output data stream. This test tries to compose ORDERBY with WHERE. This test
        // processes output sequentially (not pipelined).
        //

        private static bool RunThenByComposedWithWhere1(int dataSize, bool descending)
        {
            TestHarness.TestLog("RunThenByComposedWithWhere1(dataSize = {0}, descending = {1}) - sequential/no pipeline",
                dataSize, descending);

            // We sort on a very dense (lots of dups) set of ints first, and then
            // a more volatile set, to ensure we end up stressing secondary sort logic.
            Random rand = new Random();

            Pair<int, int>[] pairs = new Pair<int, int>[dataSize];
            for (int i = 0; i < dataSize; i++)
            {
                pairs[i].First = rand.Next(0, dataSize / 250);
                pairs[i].Second = 10 - (i % 10);
            }

            ParallelQuery<Pair<int, int>> q;

            // Create the ORDERBY:
            if (descending)
            {
                q = pairs.AsParallel<Pair<int, int>>()
                    .OrderByDescending<Pair<int,int>, int>(delegate(Pair<int,int> x) { return x.First; })
                    .ThenByDescending<Pair<int,int>, int>(delegate(Pair<int,int> x) { return x.Second; });
            }
            else
            {
                q = pairs.AsParallel<Pair<int, int>>()
                    .OrderBy<Pair<int, int>, int>(delegate(Pair<int, int> x) { return x.First; })
                    .ThenBy<Pair<int, int>, int>(delegate(Pair<int,int> x) { return x.Second; });
            }

            // Wrap with a WHERE:
            q = q.Where<Pair<int, int>>(delegate(Pair<int, int> x) { return (x.First % 2) == 0; });

            // Force synchronous execution before validating results.
            List<Pair<int, int>> r = q.ToList<Pair<int, int>>();

            for (int i = 1; i < r.Count; i++)
            {
                if (CompareInts(r[i-1].First, r[i].First, descending) == 0 &&
                    CompareInts(r[i-1].Second, r[i].Second, descending) > 0)
                {
                    TestHarness.TestLog("  > **ERROR** {0} came before {1} -- but isn't what we expected", r[i - 1], r[i]);
                    return false;
                }
            }

            return true;
        }


        //-----------------------------------------------------------------------------------
        // If sort is followed by another operator, we need to preserve key ordering all the
        // way back up to the merge. That is true even if some elements are missing in the
        // output data stream. This test tries to compose ORDERBY with WHERE. This test
        // processes output asynchronously via pipelining.
        //

        private static bool RunThenByComposedWithWhere2(int dataSize, bool descending)
        {
            TestHarness.TestLog("RunThenByComposedWithWhere2(dataSize = {0}, descending = {1}) - async/pipeline",
                dataSize, descending);

            // We sort on a very dense (lots of dups) set of ints first, and then
            // a more volatile set, to ensure we end up stressing secondary sort logic.
            Random rand = new Random();

            Pair<int, int>[] pairs = new Pair<int, int>[dataSize];
            for (int i = 0; i < dataSize; i++)
            {
                pairs[i].First = rand.Next(0, dataSize / 250);
                pairs[i].Second = 10 - (i % 10);
            }

            ParallelQuery<Pair<int, int>> q;

            // Create the ORDERBY:
            if (descending)
            {
                q = pairs.AsParallel<Pair<int, int>>()
                    .OrderByDescending<Pair<int, int>, int>(delegate(Pair<int, int> x) { return x.First; })
                    .ThenByDescending<Pair<int, int>, int>(delegate(Pair<int,int> x) { return x.Second; });
            }
            else
            {
                q = pairs.AsParallel<Pair<int, int>>()
                    .OrderBy<Pair<int, int>, int>(delegate(Pair<int, int> x) { return x.First; })
                    .ThenBy<Pair<int, int>, int>(delegate(Pair<int,int> x) { return x.Second; });
            }

            // Wrap with a WHERE:
            q = q.Where<Pair<int, int>>(delegate(Pair<int, int> x) { return (x.First % 2) == 0; });

            List<Pair<int, int>> r = new List<Pair<int, int>>();
            foreach (Pair<int, int> x in q)
                r.Add(x);

            for (int i = 1; i < r.Count; i++)
            {
                if (CompareInts(r[i-1].First, r[i].First, descending) == 0 &&
                    CompareInts(r[i-1].Second, r[i].Second, descending) > 0)
                {
                    TestHarness.TestLog("  > **ERROR** {0} came before {1} -- but isn't what we expected", r[i - 1], r[i]);
                    return false;
                }
            }

            return true;
        }

        private static bool RunThenByComposedWithJoinJoin(int outerSize, int innerSize, bool descending)
        {
            TestHarness.TestLog("RunThenByComposedWithJoinJoin(outerSize = {0}, innerSize = {1}, descending = {2})", outerSize, innerSize, descending);

            // Generate data in the reverse order in which it'll be sorted.
            DataDistributionType type = descending ? DataDistributionType.AlreadyAscending : DataDistributionType.AlreadyDescending;

            int[] leftPartOne = CreateOrderByInput(outerSize, type);
            int[] leftPartTwo = CreateOrderByInput(outerSize, DataDistributionType.Random);
            Pair<int, int>[] left = new Pair<int, int>[outerSize];
            for (int i = 0; i < outerSize; i++)
                left[i] = new Pair<int, int>(leftPartOne[i] / 1024, leftPartTwo[i]);

            int[] right = CreateOrderByInput(innerSize, type);
            int[] middle = new int[Math.Min(outerSize, innerSize)];
            if (descending)
                for (int i = middle.Length; i > 0; i--)
                    middle[i - 1] = i;
            else
                for (int i = 0; i < middle.Length; i++)
                    middle[i] = i;

            Func<int, int> identityKeySelector = delegate(int x) { return x; };

            // Create the sort object.
            ParallelQuery<Pair<int, int>> sortedLeft;
            if (descending)
            {
                sortedLeft = left.AsParallel()
                    .OrderByDescending<Pair<int, int>, int>(delegate(Pair<int, int> p) { return p.First; })
                    .ThenByDescending<Pair<int, int>, int>(delegate(Pair<int, int> p) { return p.Second; });
            }
            else
            {
                sortedLeft = left.AsParallel()
                    .OrderBy<Pair<int, int>, int>(delegate(Pair<int, int> p) { return p.First; })
                    .ThenBy<Pair<int, int>, int>(delegate(Pair<int, int> p) { return p.Second; });
            }

            // and now the join...
            ParallelQuery<Pair<int, int>> innerJoin = sortedLeft.Join<Pair<int, int>, int, int, Pair<int, int>>(
                right.AsParallel(), delegate(Pair<int, int> p) { return p.First; }, identityKeySelector,
                delegate(Pair<int, int> x, int y) { return x; });
            ParallelQuery<Pair<int, int>> outerJoin = innerJoin.Join<Pair<int, int>, int, int, Pair<int, int>>(
                middle.AsParallel(), delegate(Pair<int, int> p) { return p.First; }, identityKeySelector,
                delegate(Pair<int, int> x, int y) { return x; });

            bool passed = true;

            TestHarness.TestLog("  > Invoking join of {0} outer elems with {1} inner elems", left.Length, right.Length);

            // Ensure pairs are of equal values, and that they are in ascending or descending order.
            int cnt = 0, secondaryCnt = 0;
            Pair<int, int>? last = null;
            foreach (Pair<int, int> p in outerJoin)
            {
                cnt++;
                if (!(passed &= (last == null || ((last.Value.First <= p.First && !descending) || (last.Value.First >= p.First && descending)))))
                {
                    TestHarness.TestLog("  > *ERROR: outer sort order not correct: last = {0}, curr = {1}", last.Value.First, p.First);
                    break;
                }
                if (last != null && last.Value.First == p.First) secondaryCnt++;
                if (!(passed &= (last == null || (last.Value.First != p.First) || ((last.Value.Second <= p.Second && !descending) || (last.Value.Second >= p.Second && descending)))))
                {
                    TestHarness.TestLog("  > *ERROR: inner sort order not correct: last = {0}, curr = {1}", last.Value.Second, p.Second);
                    break;
                }
                last = p;
            }

            TestHarness.TestLog("  ** {0}   ({1} elems, {2} req'd secondary sort)", passed ? "Success" : "FAIL", cnt, secondaryCnt);
            return passed;
        }

        //-----------------------------------------------------------------------------------
        // If sort is followed by another operator, we need to preserve key ordering all the
        // way back up to the merge. That is true even if some elements are missing in the
        // output data stream. This test tries to compose ORDERBY with two WHEREs. This test
        // processes output sequentially (not pipelined).
        //

        private static bool RunThenByComposedWithWhereWhere1(int dataSize, bool descending)
        {
            TestHarness.TestLog("RunThenByComposedWithWhereWhere1(dataSize = {0}, descending = {1}) - sequential/no pipeline",
                dataSize, descending);

            // We sort on a very dense (lots of dups) set of ints first, and then
            // a more volatile set, to ensure we end up stressing secondary sort logic.
            Random rand = new Random();

            Pair<int, int>[] pairs = new Pair<int, int>[dataSize];
            for (int i = 0; i < dataSize; i++)
            {
                pairs[i].First = rand.Next(0, dataSize / 250);
                pairs[i].Second = 10 - (i % 10);
            }

            ParallelQuery<Pair<int, int>> q;

            // Create the ORDERBY:
            if (descending)
            {
                q = pairs.AsParallel<Pair<int, int>>()
                        .OrderByDescending<Pair<int, int>, int>(delegate(Pair<int, int> x) { return x.First; })
                        .ThenByDescending<Pair<int, int>, int>(delegate(Pair<int,int> x) { return x.Second; });
            }
            else
            {
                q = pairs.AsParallel<Pair<int, int>>()
                        .OrderBy<Pair<int, int>, int>(delegate(Pair<int, int> x) { return x.First; })
                        .ThenBy<Pair<int, int>, int>(delegate(Pair<int,int> x) { return x.Second; });
            }

            // Wrap with a WHERE:
            q = q.Where<Pair<int, int>>(delegate(Pair<int, int> x) { return (x.First % 2) == 0; });
            // Wrap with another WHERE:
            q = q.Where<Pair<int, int>>(delegate(Pair<int, int> x) { return (x.First % 4) == 0; });

            // Force synchronous execution before validating results.
            List<Pair<int, int>> r = q.ToList<Pair<int, int>>();

            for (int i = 1; i < r.Count; i++)
            {
                if (CompareInts(r[i-1].First, r[i].First, descending) == 0 &&
                    CompareInts(r[i-1].Second, r[i].Second, descending) > 0)
                {
                    TestHarness.TestLog("  > **ERROR** {0} came before {1} -- but isn't what we expected", r[i - 1], r[i]);
                    return false;
                }
            }

            return true;
        }

        //-----------------------------------------------------------------------------------
        // If sort is followed by another operator, we need to preserve key ordering all the
        // way back up to the merge. That is true even if some elements are missing in the
        // output data stream. This test tries to compose ORDERBY with WHERE and SELECT.
        // This test processes output sequentially (not pipelined).
        //
        // This is particularly interesting because the SELECT completely loses the original
        // type information in the tree, yet the merge is able to put things back in order.
        //

        private static bool RunThenByComposedWithWhereSelect1(int dataSize, bool descending)
        {
            TestHarness.TestLog("RunThenByComposedWithWhereSelect1(dataSize = {0}, descending = {1}) - sequential/no pipeline",
                dataSize, descending);

            // We sort on a very dense (lots of dups) set of ints first, and then
            // a more volatile set, to ensure we end up stressing secondary sort logic.
            Random rand = new Random();

            Pair<int, int>[] pairs = new Pair<int, int>[dataSize];
            for (int i = 0; i < dataSize; i++)
            {
                pairs[i].First = rand.Next(0, dataSize / 250);
                pairs[i].Second = 10 - (i % 10);
            }

            ParallelQuery<Pair<int, int>> q0;

            // Create the ORDERBY:
            if (descending)
            {
                q0 = pairs.AsParallel<Pair<int, int>>()
                        .OrderByDescending<Pair<int, int>, int>(delegate(Pair<int, int> x) { return x.First; })
                        .ThenByDescending<Pair<int, int>, int>(delegate(Pair<int,int> x) { return x.Second; });
            }
            else
            {
                q0 = pairs.AsParallel<Pair<int, int>>()
                        .OrderBy<Pair<int,int>, int>(delegate(Pair<int,int> x) { return x.First; })
                        .ThenBy<Pair<int,int>, int>(
                    delegate(Pair<int,int> x) { return x.Second; });
            }

            // Wrap with a WHERE:
            q0 = q0.Where<Pair<int, int>>(delegate(Pair<int, int> x) { return (x.First % 2) == 0; });

            // Wrap with a SELECT:
            ParallelQuery<string> q1 = q0.Select<Pair<int, int>, string>(delegate(Pair<int, int> x) { return x.First + "." + x.Second; });

            // Force synchronous execution before validating results.
            List<string> r = q1.ToList<string>();

            for (int i = 1; i < r.Count; i++)
            {
                int i0idx = r[i-1].IndexOf('.');
                int i1idx = r[i].IndexOf('.');
                Pair<int, int> i0 = new Pair<int, int>(
                    int.Parse(r[i-1].Substring(0, i0idx)), int.Parse(r[i-1].Substring(i0idx+1)));
                Pair<int, int> i1 = new Pair<int, int>(
                    int.Parse(r[i].Substring(0, i1idx)), int.Parse(r[i].Substring(i1idx+1)));;

                if (CompareInts(i0.First, i1.First, descending) == 0 &&
                    CompareInts(i0.Second, i1.Second, descending) > 0)
                {
                    TestHarness.TestLog("  > **ERROR** {0} came before {1} -- but isn't what we expected", i0, i1);
                    return false;
                }
            }

            return true;
        }

        private static bool RunThenByTestRecursive1(int dataSize, bool descending)
        {
            TestHarness.TestLog("RunThenByTestRecursive1(dataSize = {0}, descending = {1}) - synchronous/no pipeline", dataSize, descending);

            Random rand = new Random();

            // We sort on a very dense (lots of dups) set of ints first, and then
            // a more volatile set, to ensure we end up stressing secondary sort logic.

            Pair<int, Pair<int, int>>[] pairs = new Pair<int, Pair<int, int>>[dataSize];
            for (int i = 0; i < dataSize; i++)
            {
                pairs[i].First = rand.Next(0, dataSize / 250);
                pairs[i].Second = new Pair<int, int>(10 - (i % 10), 3 - (i % 3));
            }

            ParallelQuery<Pair<int, Pair<int, int>>> q;
            if (descending)
            {
                q = pairs.AsParallel<Pair<int, Pair<int, int>>>()
                    .OrderByDescending<Pair<int, Pair<int, int>>, int>(delegate(Pair<int,Pair<int,int>> x) { return x.First; })
                    .ThenByDescending<Pair<int,Pair<int, int>>, int>(delegate(Pair<int,Pair<int, int>> x) { return x.Second.First; })
                    .ThenByDescending<Pair<int,Pair<int,int>>,int>(delegate(Pair<int,Pair<int,int>> x) { return x.Second.Second; });
            }
            else
            {
                q = pairs.AsParallel<Pair<int, Pair<int, int>>>()
                    .OrderBy<Pair<int, Pair<int, int>>, int>(delegate(Pair<int, Pair<int, int>> x) { return x.First; })
                    .ThenBy<Pair<int, Pair<int, int>>, int>(delegate(Pair<int, Pair<int, int>> x) { return x.Second.First; })
                    .ThenBy<Pair<int, Pair<int, int>>, int>(delegate(Pair<int,Pair<int,int>> x) { return x.Second.Second; });
            }

            // Force synchronous execution before validating results.
            List<Pair<int, Pair<int, int>>> r = q.ToList<Pair<int, Pair<int, int>>>();

            for (int i = 1; i < r.Count; i++)
            {
                if (CompareInts(r[i-1].First, r[i].First, descending) == 0 &&
                    CompareInts(r[i-1].Second.First, r[i].Second.First, descending)  == 0 &&
                    CompareInts(r[i-1].Second.Second, r[i].Second.Second, descending) > 0)
                {
                    TestHarness.TestLog("  > **ERROR** {0}.{1} came before {2}.{3} -- but isn't what we expected",
                        r[i-1].First, r[i-1].Second, r[i].First, r[i].Second);
                    return false;
                }
            }

            return true;
        }

        private static bool RunThenByTestRecursive2(int dataSize, bool descending)
        {
            TestHarness.TestLog("RunThenByTestRecursive2(dataSize = {0}, descending = {1}) - asynchronous/pipelining", dataSize, descending);

            Random rand = new Random();

            // We sort on a very dense (lots of dups) set of ints first, and then
            // a more volatile set, to ensure we end up stressing secondary sort logic.

            Pair<int, Pair<int, int>>[] pairs = new Pair<int, Pair<int, int>>[dataSize];
            for (int i = 0; i < dataSize; i++)
            {
                pairs[i].First = rand.Next(0, dataSize / 250);
                pairs[i].Second = new Pair<int, int>(10 - (i % 10), 3 - (i % 3));
            }

            ParallelQuery<Pair<int, Pair<int, int>>> q;
            if (descending)
            {
                q = pairs.AsParallel<Pair<int, Pair<int, int>>>()
                    .OrderByDescending<Pair<int, Pair<int, int>>, int>(delegate(Pair<int, Pair<int, int>> x) { return x.First; })
                    .ThenByDescending<Pair<int, Pair<int, int>>, int>(delegate(Pair<int, Pair<int, int>> x) { return x.Second.First; })
                    .ThenByDescending<Pair<int, Pair<int, int>>, int>(delegate(Pair<int, Pair<int, int>> x) { return x.Second.Second; });
            }
            else
            {
                q = pairs.AsParallel<Pair<int, Pair<int, int>>>()
                    .OrderBy<Pair<int, Pair<int, int>>, int>(delegate(Pair<int, Pair<int, int>> x) { return x.First; })
                    .ThenBy<Pair<int, Pair<int, int>>, int>(delegate(Pair<int, Pair<int, int>> x) { return x.Second.First; })
                    .ThenBy<Pair<int, Pair<int, int>>, int>(delegate(Pair<int, Pair<int, int>> x) { return x.Second.Second; });
            }

            // Force synchronous execution before validating results.
            List<Pair<int, Pair<int, int>>> r = new List<Pair<int, Pair<int, int>>>();
            foreach (Pair<int, Pair<int,int>> x in q)
                r.Add(x);

            for (int i = 1; i < r.Count; i++)
            {
                if (CompareInts(r[i-1].First, r[i].First, descending) == 0 &&
                    CompareInts(r[i-1].Second.First, r[i].Second.First, descending)  == 0 &&
                    CompareInts(r[i-1].Second.Second, r[i].Second.Second, descending) > 0)
                {
                    TestHarness.TestLog("  > **ERROR** {0}.{1} came before {2}.{3} -- but isn't what we expected",
                        r[i-1].First, r[i-1].Second, r[i].First, r[i].Second);
                    return false;
                }
            }

            return true;
        }

        //
        // GroupBy
        //

        internal static bool RunGroupByTests()
        {
            bool passed = true;

            passed &= RunGroupByTest1(0, 7);
            passed &= RunGroupByTest1(1, 7);
            passed &= RunGroupByTest1(7, 7);
            passed &= RunGroupByTest1(8, 7);
            passed &= RunGroupByTest1(1024, 7);
            passed &= RunGroupByTest1(1024*8, 7);
            passed &= RunGroupByTest1(1024*1024, 7);

            passed &= RunGroupByTest2(0, 7);
            passed &= RunGroupByTest2(1, 7);
            passed &= RunGroupByTest2(7, 7);
            passed &= RunGroupByTest2(8, 7);
            passed &= RunGroupByTest2(1024, 7);
            passed &= RunGroupByTest2(1024*8, 7);
            passed &= RunGroupByTest2(1024*1024, 7);

            passed &= RunGroupByTest3(1024);
            passed &= RunGroupByTest3(1024*8);

            passed &= RunOrderByThenGroupByTest(1024);
            passed &= RunOrderByThenGroupByTest(1024 * 8);

            passed &= RunGroupByTest4(1000);

            return passed;
        }

        internal static bool RunGroupByTest1(int dataSize, int modNumber)
        {
            TestHarness.TestLog("* RunGroupByTest1({0}) - not pipelined", dataSize);

            int[] left = new int[dataSize];
            for (int i = 0; i < dataSize; i++) left[i] = i+1;

            // We will group by the number mod the 'modNumber' argument.
            ParallelQuery<IGrouping<int, int>> q = left.AsParallel<int>().GroupBy<int, int>(
                delegate(int x) { return x % modNumber; });

            bool passed = true;
            List<IGrouping<int, int>> r = q.ToList();

            // We expect the size to be less than or equal to the mod number.
            passed &= (r.Count <= modNumber);
            TestHarness.TestLog("  > Expected total count to <= {0}, actual == {1}", modNumber, r.Count);

            List<int> seen = new List<int>();
            foreach (IGrouping<int, int> g in r)
            {
                // Ensure each number in the grouping has the same mod. We also remember the
                // groupings seen, so we are sure there aren't any dups.
                if (seen.Contains(g.Key))
                {
                    TestHarness.TestLog("  > saw a grouping by this key already: {0}", g.Key);
                    passed = false;
                }

                foreach (int x in g)
                {
                    if ((x % modNumber) != g.Key)
                    {
                        TestHarness.TestLog("  > {0} was grouped under {1}, but when modded it == {2}", x, g.Key, x % modNumber);
                        passed = false;
                    }
                }

                seen.Add(g.Key);
            }

            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");
            return passed;
        }

        internal static bool RunGroupByTest2(int dataSize, int modNumber)
        {
            TestHarness.TestLog("* RunGroupByTest1({0}) - WITH pipelining", dataSize);

            int[] left = new int[dataSize];
            for (int i = 0; i < dataSize; i++) left[i] = i+1;

            // We will group by the number mod the 'modNumber' argument.
            ParallelQuery<IGrouping<int, int>> q = left.AsParallel<int>().GroupBy<int, int>(
                delegate(int x) { return x % modNumber; });

            bool passed = true;

            int cnt = 0;
            List<int> seen = new List<int>();
            foreach (IGrouping<int, int> g in q)
            {
                cnt++;
                // Ensure each number in the grouping has the same mod. We also remember the
                // groupings seen, so we are sure there aren't any dups.
                if (seen.Contains(g.Key))
                {
                    TestHarness.TestLog("  > saw a grouping by this key already: {0}", g.Key);
                    passed = false;
                }

                foreach (int x in g)
                {
                    if ((x % modNumber) != g.Key)
                    {
                        TestHarness.TestLog("  > {0} was grouped under {1}, but when modded it == {2}", x, g.Key, x % modNumber);
                        passed = false;
                    }
                }

                seen.Add(g.Key);
            }

            // We expect the size to be less than or equal to the mod number.
            passed &= (cnt <= modNumber);
            TestHarness.TestLog("  > Expected total count to <= {0}, actual == {1}", modNumber, cnt);

            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");
            return passed;
        }

        internal static bool RunGroupByTest3(int dataSize)
        {
            TestHarness.TestLog("* RunGroupByTest3({0}) - names, grouped by 1st character", dataSize);

            string[] names = new string[] { "balmer","duffy","gates","jobs","silva","brumme","gray","grover","yedlin" };

            Random r = new Random(33); // use constant seed for predictable test runs.
            string[] data = new string[dataSize];
            for (int i = 0; i < dataSize; i++) data[i] = names[r.Next(names.Length)];

            // We will group by the 1st character.
            ParallelQuery<IGrouping<char, string>> q = data.AsParallel<string>().GroupBy<string, char>(
                delegate(string x) { return x[0]; });

            bool passed = true;

            List<char> seen = new List<char>();
            foreach (IGrouping<char, string> g in q)
            {
                // Ensure each number in the grouping has the same 1st char. We also remember the
                // groupings seen, so we are sure there aren't any dups.
                if (seen.Contains(g.Key))
                {
                    TestHarness.TestLog("  > saw a grouping by this key already: {0}", g.Key);
                    passed = false;
                }

                foreach (string x in g)
                {
                    if (x[0] != g.Key)
                    {
                        TestHarness.TestLog("  > {0} was grouped under {1}, but its 1st char is {2}", x, g.Key, x[0]);
                        passed = false;
                    }
                }

                seen.Add(g.Key);
            }

            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");
            return passed;
        }

        internal static bool RunGroupByTest4(int dataSize)
        {
            TestHarness.TestLog("* RunGroupByTest4({0})", dataSize);

            ParallelQuery<int> parallelSrc = Enumerable.Range(0, dataSize).AsParallel().AsOrdered();

            IEnumerable<IGrouping<int, int>>[] queries = new[] {
                parallelSrc.GroupBy(x => x%2),
                parallelSrc.GroupBy(x => x%2, x => x)
            };

            foreach (IEnumerable<IGrouping<int, int>> query in queries)
            {
                int expectKey = 0;
                foreach (IGrouping<int, int> group in query)
                {
                    if (group.Key != expectKey)
                    {
                        TestHarness.TestLog("FAIL: expected key {0} got {1}", expectKey, group.Key);
                        return false;
                    }

                    int expectVal = expectKey;
                    foreach (int elem in group)
                    {
                        if (expectVal != elem)
                        {
                            TestHarness.TestLog("FAIL: expected value {0} got {1}", expectVal, elem);
                            return false;
                        }
                        expectVal += 2;
                    }

                    expectKey++;
                }
            }

            TestHarness.TestLog("  ** Success");
            return true;
        }
        internal static bool RunOrderByThenGroupByTest(int dataSize)
        {
            TestHarness.TestLog("* RunOrderByThenGroupByTest({0}) - sort names, grouped by 1st character", dataSize);

            string[] names = new string[] { "balmer", "duffy", "gates", "jobs", "silva", "brumme", "gray", "grover", "yedlin" };

            Random r = new Random(33); // use constant seed for predictable test runs.
            string[] data = new string[dataSize];
            for (int i = 0; i < dataSize; i++) data[i] = names[r.Next(names.Length)];

            // We will sort and then group by the 1st character.
            ParallelQuery<IGrouping<char, string>> q = data.AsParallel<string>().OrderBy(
                (e) => e).GroupBy<string, char>(delegate(string x) { return x[0]; });

            bool passed = true;

            List<char> seen = new List<char>();
            foreach (IGrouping<char, string> g in q)
            {
                // Ensure each number in the grouping has the same 1st char. We also remember the
                // groupings seen, so we are sure there aren't any dups.
                if (seen.Contains(g.Key))
                {
                    TestHarness.TestLog("  > saw a grouping by this key already: {0}", g.Key);
                    passed = false;
                }

                foreach (string x in g)
                {
                    if (x[0] != g.Key)
                    {
                        TestHarness.TestLog("  > {0} was grouped under {1}, but its 1st char is {2}", x, g.Key, x[0]);
                        passed = false;
                    }
                }

                seen.Add(g.Key);
            }

            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");
            return passed;
        }

        //
        // Union
        //

        internal static bool RunUnionTests()
        {
            bool passed = true;

            passed &= RunUnionTest1(0, 0);
            passed &= RunUnionTest1(1, 0);
            passed &= RunUnionTest1(0, 1);
            passed &= RunUnionTest1(4, 4);
            passed &= RunUnionTest1(1024, 4);
            passed &= RunUnionTest1(4, 1024);
            passed &= RunUnionTest1(1024, 1024);
            passed &= RunUnionTest1(1024*4, 1024);
            passed &= RunUnionTest1(1024, 1024*4);
            passed &= RunUnionTest1(1024*1024, 1024*1024);
            passed &= RunOrderedUnionTest1();

            return passed;
        }

        internal static bool RunUnionTest1(int leftDataSize, int rightDataSize)
        {
            TestHarness.TestLog("* RunUnionTest1(leftSize={0}, rightSize={1}) - union of names", leftDataSize, rightDataSize);

            string[] names1 = new string[] { "balmer","duffy","gates","jobs","silva","brumme","gray","grover","yedlin" };
            string[] names2 = new string[] { "balmer","duffy","gates","essey","crocker","smith","callahan","jimbob","beebop" };

            Random r = new Random(33); // use constant seed for predictable test runs.
            string[] leftData = new string[leftDataSize];
            for (int i = 0; i < leftDataSize; i++) leftData[i] = names1[r.Next(names1.Length)];
            string[] rightData = new string[rightDataSize];
            for (int i = 0; i < rightDataSize; i++) rightData[i] = names2[r.Next(names2.Length)];

            // Just get the union of thw two sets. We expect every name in the left and right
            // to be found in the final set, with no dups.
            ParallelQuery<string> q = leftData.AsParallel().Union<string>(rightData.AsParallel());

            bool passed = true;

            // Build a list of seen names, ensuring we don't see dups.
            List<string> seen = new List<string>();
            foreach (string n in q)
            {
                // Ensure we haven't seen this name before.
                if (seen.Contains(n))
                {
                    passed = false;
                    TestHarness.TestLog("  ** NotUnique: {0} is not unique, already seen (failure)", n);
                }

                seen.Add(n);
            }

            // Now ensure we saw all unique elements from both.
            foreach (string n in leftData)
            {
                if (!seen.Contains(n))
                {
                    passed = false;
                    TestHarness.TestLog("  ** NotSeen: {0} wasn't found in the query, though it was in the left data", n);
                }
            }
            foreach (string n in rightData)
            {
                if (!seen.Contains(n))
                {
                    passed = false;
                    TestHarness.TestLog("  ** NotSeen: {0} wasn't found in the query, though it was in the right data", n);
                }
            }

            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");
            return passed;
        }


        internal static bool RunOrderedUnionTest1()
        {
            TestHarness.TestLog("* RunOrderedUnionTest1()");

            bool passed = true;
            for (int len = 1; len <= 300; len += 3)
            {
                var data =
                    Enumerable.Repeat(0, len)
                    .Concat(new int[] { 1 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 2 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 1 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 2 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 3 })
                    .Concat(Enumerable.Repeat(0, len));


                int[][] outputs = {
                    data.AsParallel().AsOrdered().Union(Enumerable.Empty<int>().AsParallel()).ToArray(),
                    Enumerable.Empty<int>().AsParallel().AsOrdered().Union(data.AsParallel().AsOrdered()).ToArray(),
                    data.AsParallel().AsOrdered().Union(data.AsParallel()).ToArray(),
                    Enumerable.Empty<int>().AsParallel().Union(data.AsParallel().AsOrdered()).OrderBy(i=>i).ToArray(),
                    data.AsParallel().Union(data.AsParallel()).OrderBy(i=>i).ToArray(),
                };

                foreach (var output in outputs)
                {
                    if (!Enumerable.Range(0, 4).SequenceEqual(output))
                    {
                        TestHarness.TestLog("  ** Incorrect output");
                        passed = false;
                        break;
                    }
                }

                if (!passed)
                {
                    break;
                }
            }
            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");

            return passed;
        }

        //
        // Intersect
        //
    
        internal static bool RunIntersectTests()
        {
            bool passed = true;

            passed &= RunIntersectTest1(0, 0);
            passed &= RunIntersectTest1(1, 0);
            passed &= RunIntersectTest1(0, 1);
            passed &= RunIntersectTest1(4, 4);
            passed &= RunIntersectTest1(1024, 4);
            passed &= RunIntersectTest1(4, 1024);
            passed &= RunIntersectTest1(1024, 1024);
            passed &= RunIntersectTest1(1024*4, 1024);
            passed &= RunIntersectTest1(1024, 1024*4);
            passed &= RunIntersectTest1(1024*1024, 1024*1024);
            passed &= RunIntersectTest2();
            passed &= RunOrderedIntersectTest1();
            passed &= RunOrderedIntersectTest2();

            return passed;
        }

        internal static bool RunIntersectTest1(int leftDataSize, int rightDataSize)
        {
            TestHarness.TestLog("* RunIntersectTest1(leftSize={0}, rightSize={1}) - intersect of names", leftDataSize, rightDataSize);

            string[] names1 = new string[] { "balmer","duffy","gates","jobs","silva","brumme","gray","grover","yedlin" };
            string[] names2 = new string[] { "balmer","duffy","gates","essey","crocker","smith","callahan","jimbob","beebop" };

            Random r = new Random(33); // use constant seed for predictable test runs.
            string[] leftData = new string[leftDataSize];
            for (int i = 0; i < leftDataSize; i++) leftData[i] = names1[r.Next(names1.Length)];
            string[] rightData = new string[rightDataSize];
            for (int i = 0; i < rightDataSize; i++) rightData[i] = names2[r.Next(names2.Length)];

            // Just get the intersection of thw two sets. We expect every name in the left and right
            // to be found in the final set, with no dups.
            ParallelQuery<string> q = leftData.AsParallel().Intersect<string>(rightData.AsParallel());

            bool passed = true;

            // Build a list of seen names, ensuring we don't see dups.
            List<string> seen = new List<string>();
            foreach (string n in q)
            {
                // Ensure we haven't seen this name before.
                if (seen.Contains(n))
                {
                    passed = false;
                    TestHarness.TestLog("  ** NotUnique: {0} is not unique, already seen (failure)", n);
                }
                // Ensure the data exists in both sources.
                if (Array.IndexOf(leftData, n) == -1)
                {
                    passed = false;
                    TestHarness.TestLog("  ** NotInLeft: {0} isn't in the left data source", n);
                }
                if (Array.IndexOf(rightData, n) == -1)
                {
                    passed = false;
                    TestHarness.TestLog("  ** NotInRight: {0} isn't in the right data source", n);
                }

                seen.Add(n);
            }

            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");
            return passed;
        }

        /// <summary>
        /// Unordered Intersect with a custom equality comparer
        /// </summary>
        internal static bool RunIntersectTest2()
        {
            TestHarness.TestLog("* RunIntersectTest2()");

            string[] first = { "Tim", "Bob", "Mike", "Robert" };
            string[] second = { "ekiM", "bBo" };

            var comparer = new AnagramEqualityComparer();

            string[] expected = first.Except(second, comparer).ToArray();
            string[] actual = first.AsParallel().AsOrdered().Except(second.AsParallel().AsOrdered(), comparer).ToArray();

            bool passed = expected.SequenceEqual(actual);
            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");
            return passed;
        }          

        internal static bool RunOrderedIntersectTest1()
        {
            TestHarness.TestLog("* RunOrderedIntersectTest1()");

            bool passed = true;
            for (int len = 1; len <= 300; len += 3)
            {
                var data =
                    Enumerable.Repeat(0, len)
                    .Concat(new int[] { 1 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 2 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 1 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 2 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 3 })
                    .Concat(Enumerable.Repeat(0, len));

                var output = data.AsParallel().AsOrdered().Intersect(data.AsParallel()).ToArray();
                if (!Enumerable.Range(0, 4).SequenceEqual(output))
                {
                    TestHarness.TestLog("  ** Incorrect output");
                    passed = false;
                    break;
                }
            }
            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");

            return passed;
        }

        /// <summary>
        /// Ordered Intersect with a custom equality comparer
        /// </summary>
        internal static bool RunOrderedIntersectTest2()
        {
            TestHarness.TestLog("* RunOrderedIntersectTest2()");

            string[] first = { "Tim", "Bob", "Mike", "Robert" };
            string[] second = { "ekiM", "bBo" };

            var comparer = new AnagramEqualityComparer();

            string[] expected = first.Except(second, comparer).ToArray(); 
            string[] actual = first.AsParallel().AsOrdered().Except(second.AsParallel().AsOrdered(), comparer).ToArray();

            bool passed = expected.SequenceEqual(actual);
            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");
            return passed;
        }


        //
        // Except
        //

        internal static bool RunExceptTests()
        {
            bool passed = true;

            passed &= RunExceptTest1(0, 0);
            passed &= RunExceptTest1(1, 0);
            passed &= RunExceptTest1(0, 1);
            passed &= RunExceptTest1(4, 4);
            passed &= RunExceptTest1(1024, 4);
            passed &= RunExceptTest1(4, 1024);
            passed &= RunExceptTest1(1024, 1024);
            passed &= RunExceptTest1(1024*4, 1024);
            passed &= RunExceptTest1(1024, 1024*4);
            passed &= RunExceptTest1(1024*1024, 1024*1024);
            passed &= RunOrderedExceptTest1();

            return passed;
        }

        internal static bool RunExceptTest1(int leftDataSize, int rightDataSize)
        {
            TestHarness.TestLog("* RunExceptTest1(leftSize={0}, rightSize={1}) - except of names", leftDataSize, rightDataSize);

            string[] names1 = new string[] { "balmer","duffy","gates","jobs","silva","brumme","gray","grover","yedlin" };
            string[] names2 = new string[] { "balmer","duffy","gates","essey","crocker","smith","callahan","jimbob","beebop" };

            Random r = new Random(33); // use constant seed for predictable test runs.
            string[] leftData = new string[leftDataSize];
            for (int i = 0; i < leftDataSize; i++) leftData[i] = names1[r.Next(names1.Length)];
            string[] rightData = new string[rightDataSize];
            for (int i = 0; i < rightDataSize; i++) rightData[i] = names2[r.Next(names2.Length)];

            // Just get the exception of thw two sets.
            ParallelQuery<string> q = leftData.AsParallel().Except<string>(rightData.AsParallel());

            bool passed = true;

            // Build a list of seen names, ensuring we don't see dups.
            List<string> seen = new List<string>();
            foreach (string n in q)
            {
                // Ensure we haven't seen this name before.
                if (seen.Contains(n))
                {
                    passed = false;
                    TestHarness.TestLog("  ** NotUnique: {0} is not unique, already seen (failure)", n);
                }
                // Ensure the data DOES NOT exist in the right source.
                if (Array.IndexOf(rightData, n) != -1)
                {
                    passed = false;
                    TestHarness.TestLog("  ** FoundInRight: {0} found in the right data source, error", n);
                }

                seen.Add(n);
            }

            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");
            return passed;
        }

        internal static bool RunOrderedExceptTest1()
        {
            TestHarness.TestLog("* RunOrderedExceptTest1()");

            bool passed = true;
            for (int len = 1; len <= 300; len += 3)
            {
                var data =
                    Enumerable.Repeat(0, len)
                    .Concat(new int[] { 1 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 2 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 1 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 2 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 3 })
                    .Concat(Enumerable.Repeat(0, len));

                var output = data.AsParallel().AsOrdered().Except(Enumerable.Empty<int>().AsParallel()).ToArray();
                if (!Enumerable.Range(0, 4).SequenceEqual(output))
                {
                    TestHarness.TestLog("  ** Incorrect output");
                    passed = false;
                    break;
                }
            }
            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");

            return passed;
        }

        //
        // Distinct
        //

        internal static bool RunDistinctTests()
        {
            bool passed = true;

            passed &= RunDistinctTest1(0);
            passed &= RunDistinctTest1(1);
            passed &= RunDistinctTest1(4);
            passed &= RunDistinctTest1(1024);
            passed &= RunDistinctTest1(1024*4);
            passed &= RunDistinctTest1(1024*1024);
            passed &= RunOrderedDistinctTest1();

            return passed;
        }

        internal static bool RunDistinctTest1(int dataSize)
        {
            TestHarness.TestLog("* RunDistinctTest1(dataSize={0}) - distinct names", dataSize);

            string[] names1 = new string[] { "balmer","duffy","gates","jobs","silva","brumme","gray","grover","yedlin" };

            Random r = new Random(33); // use constant seed for predictable test runs.
            string[] data = new string[dataSize];
            for (int i = 0; i < dataSize; i++) data[i] = names1[r.Next(names1.Length)];

            // Find the distinct elements.
            ParallelQuery<string> q = data.AsParallel().Distinct<string>();

            bool passed = true;

            // Build a list of seen names, ensuring we don't see dups.
            List<string> seen = new List<string>();
            foreach (string n in q)
            {
                // Ensure we haven't seen this name before.
                if (seen.Contains(n))
                {
                    passed = false;
                    TestHarness.TestLog("  ** NotUnique: {0} is not unique, already seen (failure)", n);
                }

                seen.Add(n);
            }

            // Now ensure we saw all elements at least once.
            foreach (string n in data)
            {
                if (!seen.Contains(n))
                {
                    passed = false;
                    TestHarness.TestLog("  ** NotSeen: {0} wasn't found in the query, though it was in the data", n);
                }
            }

            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");
            return passed;
        }

        internal static bool RunOrderedDistinctTest1()
        {
            TestHarness.TestLog("* RunOrderedDistinctTest1()");

            bool passed = true;
            for (int len = 1; len <= 300; len += 3)
            {
                var data =
                    Enumerable.Repeat(0, len)
                    .Concat(new int[] { 1 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 2 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 1 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 2 })
                    .Concat(Enumerable.Repeat(0, len))
                    .Concat(new int[] { 3 })
                    .Concat(Enumerable.Repeat(0, len));

                var output = data.AsParallel().AsOrdered().Distinct().ToArray();
                if (!Enumerable.Range(0, 4).SequenceEqual(output))
                {
                    TestHarness.TestLog("  ** Incorrect output");
                    passed = false;
                    break;
                }
            }
            TestHarness.TestLog("  ** {0}", passed ? "Success" : "FAIL");

            return passed;
        }

        //
        // Concat
        //

        internal static bool RunConcatTests()
        {
            bool passed = true;

            // W/ pipelining.
            passed &= RunConcatTest1(0, 0);
            passed &= RunConcatTest1(0, 1);
            passed &= RunConcatTest1(1, 0);
            passed &= RunConcatTest1(1, 1);
            passed &= RunConcatTest1(1024, 1024);
            passed &= RunConcatTest1(0, 1024);
            passed &= RunConcatTest1(1024, 0);

            // @TODO: reenable this test when deadlock problem is solved.
            //passed &= RunConcatTest1(1023 * 1024, 1023 * 1024);

            // W/out pipelining.
            passed &= RunConcatTest2(0, 0);
            passed &= RunConcatTest2(0, 1);
            passed &= RunConcatTest2(1, 0);
            passed &= RunConcatTest2(1, 1);
            passed &= RunConcatTest2(1024, 1024);
            passed &= RunConcatTest2(0, 1024);
            passed &= RunConcatTest2(1024, 0);
            passed &= RunConcatTest2(1023 * 1024, 1023 * 1024);

            return passed;
        }

        private static bool RunConcatTest1(int leftSize, int rightSize)
        {
            TestHarness.TestLog("* RunConcatTest1(leftSize={0}, rightSize={1}) -- pipelined", leftSize, rightSize);
            int[] leftData = new int[leftSize];
            for (int i = 0; i < leftSize; i++) leftData[i] = i;
            int[] rightData = new int[rightSize];
            for (int i = 0; i < rightSize; i++) rightData[i] = i;

            ParallelQuery<int> q = leftData.AsParallel().AsOrdered().Concat(rightData.AsParallel());

            int cnt = 0;
            bool passed = true;

            foreach (int x in q)
            {
                if (cnt < leftSize)
                {
                    if (x != leftData[cnt])
                    {
                        TestHarness.TestLog("  > Expected element {0} to == {1} (from left); got {2} instead",
                            cnt, leftData[cnt], x);
                        passed = false;
                    }
                }
                else
                {
                    if (x != rightData[cnt - leftSize])
                    {
                        TestHarness.TestLog("  > Expected element {0} to == {1} (from right); got {2} instead",
                            cnt, rightData[cnt - leftSize], x);
                        passed = false;
                    }
                }

                cnt++;
            }

            passed &= (cnt == leftSize + rightSize);
            TestHarness.TestLog("  > Expect: {0}, real: {1}", leftSize + rightSize, cnt);

            return passed;
        }

        private static bool RunConcatTest2(int leftSize, int rightSize)
        {
            TestHarness.TestLog("* RunConcatTest2(leftSize={0}, rightSize={1}) -- w/out pipelining", leftSize, rightSize);
            int[] leftData = new int[leftSize];
            for (int i = 0; i < leftSize; i++) leftData[i] = i;
            int[] rightData = new int[rightSize];
            for (int i = 0; i < rightSize; i++) rightData[i] = i;

            ParallelQuery<int> q = leftData.AsParallel().AsOrdered().Concat(rightData.AsParallel());
            List<int> r = q.ToList();

            int cnt = 0;
            bool passed = true;

            foreach (int x in r)
            {
                if (cnt < leftSize)
                {
                    if (x != leftData[cnt])
                    {
                        TestHarness.TestLog("  > Expected element {0} to == {1} (from left); got {2} instead",
                            cnt, leftData[cnt], x);
                        passed = false;
                    }
                }
                else
                {
                    if (x != rightData[cnt - leftSize])
                    {
                        TestHarness.TestLog("  > Expected element {0} to == {1} (from right); got {2} instead",
                            cnt, rightData[cnt - leftSize], x);
                        passed = false;
                    }
                }

                cnt++;
            }

            passed &= (cnt == leftSize + rightSize);
            TestHarness.TestLog("  > Expect: {0}, real: {1}", leftSize + rightSize, cnt);

            return passed;
        }

        //
        // Reverse
        //

        internal static bool RunReverseTests()
        {
            bool passed = true;

            passed &= RunReverseTest1(0);
            passed &= RunReverseTest1(33);
            passed &= RunReverseTest1(1024);
            passed &= RunReverseTest1(1024*1024);

            passed &= RunReverseTest2_Range(0, 0);
            passed &= RunReverseTest2_Range(0, 33);
            passed &= RunReverseTest2_Range(33, 33);
            passed &= RunReverseTest2_Range(33, 66);
            passed &= RunReverseTest2_Range(0, 1024 * 3);
            passed &= RunReverseTest2_Range(1024, 1024 * 1024 * 3);

            return passed;
        }

        private static bool RunReverseTest1(int size)
        {
            TestHarness.TestLog("* RunReverseTest1(size={0})", size);
            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i;

            ParallelQuery<int> q = ints.AsParallel().AsOrdered().Reverse();

            bool passed = true;
            int cnt = 0;
            int last = size;
            foreach (int x in q)
            {
                if (x != last - 1)
                {
                    TestHarness.TestLog("  > Elems not in decreasing order: this={0}, last={1}", x, last);
                    passed = false;
                }
                last = x;
                cnt++;
            }

            passed &= (cnt == size);
            TestHarness.TestLog("  > Expect: {0}, real: {1}", size, cnt);

            return passed;
        }

        private static bool RunReverseTest2_Range(int start, int count)
        {
            TestHarness.TestLog("* RunReverseTest2_Range(start={0}, count={1})", start, count);

            ParallelQuery<int> q = ParallelEnumerable.Range(start, count).AsOrdered().Reverse();

            bool passed = true;
            int seen = 0;
            int last = (start + count);
            foreach (int x in q)
            {
                if (x != last - 1)
                {
                    TestHarness.TestLog("  > Elems not in decreasing order: this={0}, last={1}", x, last);
                    passed = false;
                }
                last = x;
                seen++;
            }

            passed &= (seen == count);
            TestHarness.TestLog("  > Expect: {0}, real: {1}", count, seen);

            return passed;
        }

        //
        // DefaultIfEmpty
        //

        internal static bool RunDefaultIfEmptyTests()
        {
            bool passed = true;

            for (int i = 0; i <= 33; i++)
            {
                passed &= RunDefaultIfEmptyTest1(i);
            }
            passed &= RunDefaultIfEmptyTest1(1024);
            passed &= RunDefaultIfEmptyTest1(1024 * 1024);

            passed &= RunDefaultIfEmptyOrderByTest1(0);
            passed &= RunDefaultIfEmptyOrderByTest1(1024);
            passed &= RunDefaultIfEmptyOrderByTest2(0);
            passed &= RunDefaultIfEmptyOrderByTest2(1024);

            return passed;
        }

        private static bool RunDefaultIfEmptyTest1(int size)
        {
            TestHarness.TestLog("* RunDefaultIfEmptyTest1(size={0})", size);
            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i;

            ParallelQuery<int> q = ints.AsParallel().DefaultIfEmpty();

            bool passed = true;
            int cnt = 0;
            foreach (int x in q)
            {
                if (size == 0 && x != default(int))
                {
                    TestHarness.TestLog("  > Only element should be {0} for empty inputs: saw {1}", default(int), x);
                    passed = false;
                }
                cnt++;
            }

            int expect = size == 0 ? 1 : size;
            passed &= (cnt == expect);
            TestHarness.TestLog("  > Expect: {0}, real: {1}", expect, cnt);
            TestHarness.TestLog("  > {0}",  passed ? "PASS" : "**FAILED**");

            return passed;
        }

        private static bool RunDefaultIfEmptyOrderByTest1(int size)
        {
            TestHarness.TestLog("* RunDefaultIfEmptyOrderByTest1(size={0})", size);
            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i;

            ParallelQuery<int> q = ints.AsParallel().OrderBy<int, int>(x => x).DefaultIfEmpty();

            bool passed = true;
            int cnt = 0;
            int last = -1;
            foreach (int x in q)
            {
                if (size == 0 && x != default(int))
                {
                    TestHarness.TestLog("  > Only element should be {0} for empty inputs: saw {1}", default(int), x);
                    passed = false;
                }
                if (x < last)
                {
                    TestHarness.TestLog("  > Sort wasn't processed correctly: curr = {0}, but last = {1}", x, last);
                    passed = false;
                }
                last = x;

                cnt++;
            }

            int expect = size == 0 ? 1 : size;
            passed &= (cnt == expect);
            TestHarness.TestLog("  > Expect: {0}, real: {1}", expect, cnt);
            TestHarness.TestLog("  > {0}", passed ? "PASS" : "**FAILED**");

            return passed;
        }

        private static bool RunDefaultIfEmptyOrderByTest2(int size)
        {
            TestHarness.TestLog("* RunDefaultIfEmptyOrderByTest2(size={0})", size);
            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i;

            ParallelQuery<int> q = ints.AsParallel().DefaultIfEmpty().
                OrderBy<int, int>(e => e);

            bool passed = true;
            int cnt = 0;
            int last = -1;
            foreach (int x in q)
            {
                if (size == 0 && x != default(int))
                {
                    TestHarness.TestLog("  > Only element should be {0} for empty inputs: saw {1}", default(int), x);
                    passed = false;
                }
                if (x < last)
                {
                    TestHarness.TestLog("  > Sort wasn't processed correctly: curr = {0}, but last = {1}", x, last);
                    passed = false;
                }
                last = x;
                cnt++;
            }

            int expect = size == 0 ? 1 : size;
            passed &= (cnt == expect);
            TestHarness.TestLog("  > Expect: {0}, real: {1}", expect, cnt);
            TestHarness.TestLog("  > {0}", passed ? "PASS" : "**FAILED**");

            return passed;
        }

        //
        // First and FirstOrDefault
        //

        internal static bool RunFirstTests()
        {
            bool passed = true;

            passed &= RunFirstTest1(0, false);
            passed &= RunFirstTest1(1024, false);
            passed &= RunFirstTest1(1024 * 1024, false);
            passed &= RunFirstTest1(0, true);
            passed &= RunFirstTest1(1024, true);
            passed &= RunFirstTest1(1024 * 1024, true);

            passed &= RunFirstOrDefaultTest1(0, false);
            passed &= RunFirstOrDefaultTest1(1024, false);
            passed &= RunFirstOrDefaultTest1(1024 * 1024, false);
            passed &= RunFirstOrDefaultTest1(0, true);
            passed &= RunFirstOrDefaultTest1(1024, true);
            passed &= RunFirstOrDefaultTest1(1024 * 1024, true);

            return passed;
        }

        private static bool RunFirstTest1(int size, bool usePredicate)
        {
            TestHarness.TestLog("* RunFirstTest1(size={0}, usePredicate={1})", size, usePredicate);
            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i;

            int predSearch = 1033;

            bool passed = true;
            bool expectExcept = (size == 0 || (usePredicate && size <= predSearch));

            try
            {
                int q;
                if (usePredicate)
                {
                    Func<int, bool> pred = delegate(int x) { return (x >= predSearch); };
                    q = ints.AsParallel().First(pred);
                }
                else
                {
                    q = ints.AsParallel().First();
                }
                    

                if (expectExcept)
                {
                    passed = false;
                    TestHarness.TestLog("  > Failure: Expected an exception, but didn't get one");
                }

                int expectReturn = usePredicate ? predSearch : 0;
                if (q != expectReturn)
                {
                    TestHarness.TestLog("  > Expected return value of {0}, saw {1} instead", expectReturn, q);
                    passed = false;
                }
            }
            catch (InvalidOperationException ioex)
            {
                if (!expectExcept)
                {
                    passed = false;
                    TestHarness.TestLog("  > Failure: Got exception, but didn't expect it  {0}", ioex);
                }
            }

            TestHarness.TestLog("  > {0}", passed ? "PASS" : "**FAILED**");

            return passed;
        }

        private static bool RunFirstOrDefaultTest1(int size, bool usePredicate)
        {
            TestHarness.TestLog("* RunFirstOrDefaultTest1(size={0}, usePredicate={1})", size, usePredicate);
            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i+1;

            int predSearch = 1033;
            bool passed = true;
            bool expectDefault = (size == 0 || (usePredicate && size <= predSearch));
            
            int q;
            if (usePredicate)
            {
                Func<int, bool> pred = delegate(int x) { return (x >= predSearch); };
                q = ints.AsParallel().FirstOrDefault(pred);
            }
            else
            {
                q = ints.AsParallel().FirstOrDefault();
            }

            int expectReturn = expectDefault ? 0 : (usePredicate ? predSearch : 1);
            if (q != expectReturn)
            {
                TestHarness.TestLog("  > Expected return value of {0}, saw {1} instead", expectReturn, q);
                passed = false;
            }

            TestHarness.TestLog("  > {0}", passed ? "PASS" : "**FAILED**");

            return passed;
        }

        //
        // Last and LastOrDefault
        //

        internal static bool RunLastTests()
        {
            bool passed = true;

            passed &= RunLastTest1(0, false);
            passed &= RunLastTest1(1024, false);
            passed &= RunLastTest1(1024 * 1024, false);
            passed &= RunLastTest1(0, true);
            passed &= RunLastTest1(1024, true);
            passed &= RunLastTest1(1024 * 1024, true);

            passed &= RunLastOrDefaultTest1(0, false);
            passed &= RunLastOrDefaultTest1(1024, false);
            passed &= RunLastOrDefaultTest1(1024 * 1024, false);
            passed &= RunLastOrDefaultTest1(0, true);
            passed &= RunLastOrDefaultTest1(1024, true);
            passed &= RunLastOrDefaultTest1(1024 * 1024, true);

            return passed;
        }

        private static bool RunLastTest1(int size, bool usePredicate)
        {
            TestHarness.TestLog("* RunLastTest1(size={0}, usePredicate={1})", size, usePredicate);
            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i;

            int predLo = 1033;
            int predHi = 1050;

            bool passed = true;
            bool expectExcept = (size == 0 || (usePredicate && size <= predLo));

            try
            {
                int q;
                if (usePredicate)
                {
                    Func<int, bool> pred = delegate(int x) { return (x >= predLo && x <= predHi); };
                    q = ints.AsParallel().Last(pred);
                }
                else
                {
                    q = ints.AsParallel().Last();
                }

                if (expectExcept)
                {
                    passed = false;
                    TestHarness.TestLog("  > Failure: Expected an exception, but didn't get one");
                }

                int expectReturn = usePredicate ? Math.Min(predHi, size - 1) : size - 1;
                if (q != expectReturn)
                {
                    TestHarness.TestLog("  > Expected return value of {0}, saw {1} instead", expectReturn, q);
                    passed = false;
                }
            }
            catch (InvalidOperationException ioex)
            {
                if (!expectExcept)
                {
                    passed = false;
                    TestHarness.TestLog("  > Failure: Got exception, but didn't expect it  {0}", ioex);
                }
            }

            TestHarness.TestLog("  > {0}", passed ? "PASS" : "**FAILED**");

            return passed;
        }

        private static bool RunLastOrDefaultTest1(int size, bool usePredicate)
        {
            TestHarness.TestLog("* RunLastOrDefaultTest1(size={0}, usePredicate={1})", size, usePredicate);
            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i + 1;

            int predLo = 1033;
            int predHi = 1050;

            bool passed = true;
            bool expectDefault = (size == 0 || (usePredicate && size <= (predLo+1)));

            int q;
            if (usePredicate)
            {
                Func<int, bool> pred = delegate(int x) { return (x >= predLo && x <= predHi); };
                q = ints.AsParallel().LastOrDefault(pred);
            }
            else
            {
                q = ints.AsParallel().LastOrDefault();
            }

            int expectReturn = expectDefault ? 0 : (usePredicate ? Math.Min(predHi, size) : size);
            if (q != expectReturn)
            {
                TestHarness.TestLog("  > Expected return value of {0}, saw {1} instead", expectReturn, q);
                passed = false;
            }

            TestHarness.TestLog("  > {0}", passed ? "PASS" : "**FAILED**");

            return passed;
        }

        //
        // Single and SingleOrDefault
        //

        internal static bool RunSingleTests()
        {
            bool passed = true;

            passed &= RunSingleTest1(0, false);
            passed &= RunSingleTest1(1, false);
            passed &= RunSingleTest1(1024, false);
            passed &= RunSingleTest1(1024 * 1024, false);
            passed &= RunSingleTest1(0, true);
            passed &= RunSingleTest1(1, true);
            passed &= RunSingleTest1(1024, true);
            passed &= RunSingleTest1(1024 * 1024, true);

            passed &= RunSingleOrDefaultTest1(0, false);
            passed &= RunSingleOrDefaultTest1(1, false);
            passed &= RunSingleOrDefaultTest1(1024, false);
            passed &= RunSingleOrDefaultTest1(1024 * 1024, false);
            passed &= RunSingleOrDefaultTest1(0, true);
            passed &= RunSingleOrDefaultTest1(1, true);
            passed &= RunSingleOrDefaultTest1(1024, true);
            passed &= RunSingleOrDefaultTest1(1024 * 1024, true);

            return passed;
        }

        private static bool RunSingleTest1(int size, bool usePredicate)
        {
            TestHarness.TestLog("* RunSingleTest1(size={0}, usePredicate={1})", size, usePredicate);
            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i;

            int predNum = 1023;

            bool passed = true;
            bool expectExcept = usePredicate ? predNum != (size - 1) : size != 1;

            try
            {
                int q;
                if (usePredicate)
                {
                    Func<int, bool> pred = delegate(int x) { return x >= predNum; };
                    q = ints.AsParallel().Single(pred);
                }
                else {
                    q = ints.AsParallel().Single();
                }

                if (expectExcept)
                {
                    passed = false;
                    TestHarness.TestLog("  > Failure: Expected an exception, but didn't get one");
                }
                else
                {
                    int expectReturn = usePredicate ? predNum : 0;
                    if (q != expectReturn)
                    {
                        TestHarness.TestLog("  > Expected return value of {0}, saw {1} instead", expectReturn, q);
                        passed = false;
                    }
                }
            }
            catch (InvalidOperationException ioex)
            {
                if (!expectExcept)
                {
                    passed = false;
                    TestHarness.TestLog("  > Failure: Got exception, but didn't expect it  {0}", ioex);
                }
            }

            TestHarness.TestLog("  > {0}", passed ? "PASS" : "**FAILED**");

            return passed;
        }

        private static bool RunSingleOrDefaultTest1(int size, bool usePredicate)
        {
            TestHarness.TestLog("* RunSingleOrDefaultTest1(size={0}, usePredicate={1})", size, usePredicate);
            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i + 1;

            int predNum = 1023;

            bool passed = true;
            bool expectDefault = usePredicate ? predNum >= (size + 1) : size == 0;
            bool expectExcept = usePredicate ? predNum < size : size > 1;
            try
            {
                int q;
                if (usePredicate)
                {
                    Func<int, bool> pred = delegate(int x) { return x >= predNum; };
                    q = ints.AsParallel().SingleOrDefault(pred);
                }
                else
                {
                    q = ints.AsParallel().SingleOrDefault();
                }

                if (expectExcept)
                {
                    passed = false;
                    TestHarness.TestLog("  > Failure: Expected an exception, but didn't get one");
                }
                else
                {
                    int expectReturn = expectDefault ? 0 : (usePredicate ? predNum : 1);
                    if (q != expectReturn)
                    {
                        TestHarness.TestLog("  > Expected return value of {0}, saw {1} instead", expectReturn, q);
                        passed = false;
                    }
                }
            }
            catch (InvalidOperationException ioex)
            {
                if (!expectExcept)
                {
                    passed = false;
                    TestHarness.TestLog("  > Failure: Got exception, but didn't expect it  {0}", ioex);
                }
            }

            TestHarness.TestLog("  > {0}", passed ? "PASS" : "**FAILED**");

            return passed;
        }

        //
        // ElementAt and ElementAtOrDefault
        //

        internal static bool RunElementAtTests()
        {
            bool passed = true;

            passed &= RunElementAtTest1(1024, 512);
            passed &= RunElementAtTest1(0, 512);
            passed &= RunElementAtTest1(1, 512);
            passed &= RunElementAtTest1(1024, 1024);
            passed &= RunElementAtTest1(1024*1024, 1024);

            passed &= RunElementAtOrDefaultTest1(1024, 512);
            passed &= RunElementAtOrDefaultTest1(0, 512);
            passed &= RunElementAtOrDefaultTest1(1, 512);
            passed &= RunElementAtOrDefaultTest1(1024, 1024);
            passed &= RunElementAtOrDefaultTest1(1024 * 1024, 1024);

            return passed;
        }

        private static bool RunElementAtTest1(int size, int elementAt)
        {
            TestHarness.TestLog("* RunElementAtTest1(size={0}, elementAt={1})", size, elementAt);
            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i;

            bool passed = true;
            bool expectExcept = elementAt >= size;
            try
            {
                int q = ints.AsParallel().ElementAt(elementAt);

                if (expectExcept)
                {
                    passed = false;
                    TestHarness.TestLog("  > Failure: Expected an exception, but didn't get one");
                }
                else
                {
                    if (q != ints[elementAt])
                    {
                        TestHarness.TestLog("  > Expected return value of {0}, saw {1} instead", ints[elementAt], q);
                        passed = false;
                    }
                }
            }
            catch (ArgumentOutOfRangeException ioex)
            {
                if (!expectExcept)
                {
                    passed = false;
                    TestHarness.TestLog("  > Failure: Got exception, but didn't expect it  {0}", ioex);
                }
            }

            TestHarness.TestLog("  > {0}", passed ? "PASS" : "**FAILED**");

            return passed;
        }

        private static bool RunElementAtOrDefaultTest1(int size, int elementAt)
        {
            TestHarness.TestLog("* RunElementAtOrDefaultTest1(size={0}, elementAt={1})", size, elementAt);
            int[] ints = new int[size];
            for (int i = 0; i < size; i++) ints[i] = i;

            int q = ints.AsParallel().ElementAtOrDefault(elementAt);

            bool passed = true;
            int expectValue = (elementAt >= size) ? default(int) : ints[elementAt];
            if (q != expectValue)
            {
                TestHarness.TestLog("  > Expected return value of {0}, saw {1} instead", expectValue, q);
                passed = false;
            }

            TestHarness.TestLog("  > {0}", passed ? "PASS" : "**FAILED**");

            return passed;
        }


        //
        // Custom Partitioner tests
        //

        internal static bool RunPartitionerTests()
        {
            bool passed = true;

            passed &= RunPartitionerTest1(0);
            passed &= RunPartitionerTest1(1);
            passed &= RunPartitionerTest1(999);
            passed &= RunPartitionerTest1(1024);

            passed &= RunOrderablePartitionerTest1(0, true, true);
            passed &= RunOrderablePartitionerTest1(1, true, true);
            passed &= RunOrderablePartitionerTest1(999, true, true);
            passed &= RunOrderablePartitionerTest1(1024, true, true);

            passed &= RunOrderablePartitionerTest1(1024, false, true);
            passed &= RunOrderablePartitionerTest1(1024, true, false);
            passed &= RunOrderablePartitionerTest1(1024, false, false);

            return passed;
        }

        private static bool RunPartitionerTest1(int size)
        {
            TestHarness.TestLog("* RunPartitionerTest1(size={0})", size);
            int[] arr = Enumerable.Range(0, size).ToArray();
            Partitioner<int> partitioner = new ListPartitioner<int>(arr);

            bool passed = true;

            // Without ordering:
            int[] res = partitioner.AsParallel().Select(x => -x).ToArray().Select(x => -x).OrderBy(x=>x).ToArray();
            if (!res.OrderBy(i => i).SequenceEqual(arr))
            {
                TestHarness.TestLog("  > Failure: Incorrect output {0}", String.Join(" ", res.Select(x=>x.ToString()).ToArray()));
                passed = false;
            }

            // With ordering, expect an exception:
            bool gotException = false;
            try
            {
                partitioner.AsParallel().AsOrdered().Select(x => -x).ToArray();
            }
            catch (InvalidOperationException)
            {
                gotException = true;
            }

            if (!gotException)
            {
                TestHarness.TestLog("  > Failure: Expected an exception, but didn't get one");
                passed = false;
            }

            return passed;
        }

        private static bool RunOrderablePartitionerTest1(int size, bool keysIncreasingInEachPartition, bool keysNormalized)
        {
            TestHarness.TestLog("* RunOrderablePartitionerTest1(size={0}, keysIncreasingInEachPartition={1}, keysNormalized={2})",
                size, keysIncreasingInEachPartition, keysNormalized);

            int[] arr = Enumerable.Range(0, size).ToArray();
            Partitioner<int> partitioner = new OrderableListPartitioner<int>(arr, keysIncreasingInEachPartition, keysNormalized);

            bool passed = true;

            // Without ordering:
            int[] res = partitioner.AsParallel().Select(x => -x).ToArray().Select(x => -x).OrderBy(x=>x).ToArray();
            if (!res.OrderBy(i => i).SequenceEqual(arr))
            {
                TestHarness.TestLog("  > Failure: Incorrect output");
                passed = false;
            }

            // With ordering:
            int[] resOrdered = partitioner.AsParallel().AsOrdered().Select(x => -x).ToArray().Select(x => -x).ToArray();
            if (!resOrdered.SequenceEqual(arr))
            {
                TestHarness.TestLog("  > Failure: Incorrect output");
                passed = false;
            }

            return passed;
        }

    
        //
        // An orderable partitioner for lists, used by the partitioner tests
        //
        class OrderableListPartitioner<TSource> : OrderablePartitioner<TSource>
        {
            private readonly IList<TSource> m_input;
            private readonly bool m_keysOrderedInEachPartition;
            private readonly bool m_keysNormalized;

            public OrderableListPartitioner(IList<TSource> input, bool keysOrderedInEachPartition, bool keysNormalized)
                : base(keysOrderedInEachPartition, false, keysNormalized)
            {
                m_input = input;
                m_keysOrderedInEachPartition = keysOrderedInEachPartition;
                m_keysNormalized = keysNormalized;
            }

            public override IList<IEnumerator<KeyValuePair<long, TSource>>> GetOrderablePartitions(int partitionCount)
            {
                IEnumerable<KeyValuePair<long, TSource>> dynamicPartitions = GetOrderableDynamicPartitions();
                IEnumerator<KeyValuePair<long, TSource>>[] partitions = new IEnumerator<KeyValuePair<long, TSource>>[partitionCount];

                for (int i = 0; i < partitionCount; i++)
                {
                    partitions[i] = dynamicPartitions.GetEnumerator();
                }
                return partitions;
            }

            public override IEnumerable<KeyValuePair<long, TSource>> GetOrderableDynamicPartitions()
            {
                return new ListDynamicPartitions(m_input, m_keysOrderedInEachPartition, m_keysNormalized);
            }

            private class ListDynamicPartitions : IEnumerable<KeyValuePair<long, TSource>>
            {
                private IList<TSource> m_input;
                private int m_pos = 0;
                private bool m_keysOrderedInEachPartition;
                private bool m_keysNormalized;

                internal ListDynamicPartitions(IList<TSource> input, bool keysOrderedInEachPartition, bool keysNormalized)
                {
                    m_input = input;
                    m_keysOrderedInEachPartition = keysOrderedInEachPartition;
                    m_keysNormalized = keysNormalized;
                }

                public IEnumerator<KeyValuePair<long, TSource>> GetEnumerator()
                {
                    while (true)
                    {
                        int elemIndex = Interlocked.Increment(ref m_pos) - 1;

                        if (elemIndex >= m_input.Count)
                        {
                            yield break;
                        }

                        if (!m_keysOrderedInEachPartition)
                        {
                            elemIndex = m_input.Count - 1 - elemIndex;
                        }

                        long key = m_keysNormalized ? elemIndex : (elemIndex * 2);

                        yield return new KeyValuePair<long, TSource>(key, m_input[elemIndex]);
                    }
                }

                IEnumerator IEnumerable.GetEnumerator()
                {
                    return ((IEnumerable<KeyValuePair<long, TSource>>)this).GetEnumerator();
                }
            }
        }

        // An unordered partitioner for lists, used by the partitioner tests.
        class ListPartitioner<TSource> : Partitioner<TSource>
        {
            private OrderablePartitioner<TSource> m_partitioner;

            public ListPartitioner(IList<TSource> source) {
                m_partitioner = new OrderableListPartitioner<TSource>(source, true, true);
            }
    
            public override IList<IEnumerator<TSource>> GetPartitions(int partitionCount)
            {
                return m_partitioner.GetPartitions(partitionCount);
            }

            public override IEnumerable<TSource> GetDynamicPartitions()
            {
                return m_partitioner.GetDynamicPartitions();
            }
        }


        internal static bool RunDopTests()
        {
            bool passed = true;

            passed &= RunDopTest(1, false);
            passed &= RunDopTest(4, false);
            passed &= RunDopTest(63, false);
            passed &= RunDopTest(64, true);

            passed &= RunDopTestSta(1, false);
            passed &= RunDopTestSta(4, false);
            passed &= RunDopTestSta(63, false);
            passed &= RunDopTestSta(64, true);

            return passed;
        }

        //
        // A simple test to check whether PLINQ appears to work with a particular DOP
        //
        private static bool RunDopTest(int dop, bool expectException)
        {
            TestHarness.TestLog("* RunDopTest(dop={0},expectException={1})", dop, expectException);
            int[] arr = Enumerable.Repeat(5, 1000).ToArray();

            int real = 0;

            try
            {
                real = arr.AsParallel().WithDegreeOfParallelism(dop)
                     .Select(x => 2 * x)
                     .Sum();

                int expect = arr.Length * 10;
                if (real != expect)
                {
                    TestHarness.TestLog("  > Incorrect result: expected {0} got {1}", expect, real);
                    return false;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                return expectException;
            }

            return !expectException;
        }

        //
        // A simple test to check whether PLINQ appears to work with a particular DOP,
        // when the query executes within an STA thread.
        //
        private static bool RunDopTestSta(int dop, bool expectException)
        {
            TestHarness.TestLog("* RunDopTestSta(dop={0},expectException={1})", dop, expectException);

            bool passed = false;
            ManualResetEvent mre = new ManualResetEvent(false);
            Thread t = new Thread(() =>
            {
                try
                {
                        var src = new int[] { 3, 6, 4, 2, 8, 7, 1, 5 };
                        var q = Enumerable.Range(0, 10000).AsParallel().WithDegreeOfParallelism(dop).Select(i =>
                        {
                            mre.WaitOne(); // simulates heavywork to force the consumer to wait
                            return i;
                        });
                        foreach (var x in q) { }

                        passed = !expectException;
                }
                catch (ArgumentOutOfRangeException)
                {
                    passed = expectException;
                }
            });
            mre.Set();

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();

            return passed;
        }

        //
        // A comparer that considers two strings equal if they are anagrams of each other
        //
        private class AnagramEqualityComparer : IEqualityComparer<string>
        {
            public bool Equals(string a, string b)
            {
                return a.ToCharArray().OrderBy(c => c).SequenceEqual(b.ToCharArray().OrderBy(c => c));
            }

            public int GetHashCode(string str)
            {
                return new string(str.ToCharArray().OrderBy(c => c).ToArray()).GetHashCode();
            }
        }
    }

}
