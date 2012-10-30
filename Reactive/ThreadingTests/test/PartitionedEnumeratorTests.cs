using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq.Parallel;
using System.Reflection;
using System.Threading;

namespace plinq_devtests
{

    class PartitionedEnumeratorTests
    {

        internal static bool RunContiguousRangePartitionedTests()
        {
            bool passed = true;
            passed &= DrivePartitionAlgorithm_Sequential(new PartitionAlgorithmTest(typeof(PartitionedDataSource<>)));
            return passed;
        }

        internal static bool RunStripePartitionedTests()
        {
            bool passed = true;
            passed &= DrivePartitionAlgorithm_Sequential(new PartitionAlgorithmTest(typeof(StripePartitionedStream<>)));
            return passed;
        }

        internal static bool RunContiguousRangePartitionedTests_Parallel()
        {
            bool passed = true;
            passed &= DrivePartitionAlgorithm_Parallel(new PartitionAlgorithmTest(typeof(PartitionedDataSource<>)));
            return passed;
        }

        internal static bool RunStripePartitionedTests_Parallel()
        {
            bool passed = true;
            passed &= DrivePartitionAlgorithm_Parallel(new PartitionAlgorithmTest(typeof(StripePartitionedStream<>)));
            return passed;
        }

        private class StripePartitionedStream<TInputOutputKey> : PartitionedDataSource<TInputOutputKey>
        {
            internal StripePartitionedStream(IEnumerable<TInputOutputKey> source, int partitionCount) : base(source, partitionCount, true)
            {
            }
        }

        private static bool DrivePartitionAlgorithm_Sequential(PartitionAlgorithmTest t)
        {
            bool passed = true;

            //
            // These tests ensure that all of the source elements are represented in the output.
            // We use different combinations of consumption and data/partition sizes.
            //

            // -- IEnumerable data source --

            passed &= t.RunPartA1_IEnumerable(1024 * 2, 8); // even # of data elements
            passed &= t.RunPartA1_IEnumerable(1023 * 3, 8); // odd # of data elements
            passed &= t.RunPartA1_IEnumerable(1024 * 2, 3); // strange partition count
            passed &= t.RunPartA1_IEnumerable(0, 8); // empty data size
            passed &= t.RunPartA1_IEnumerable(1, 8); // small data size
            passed &= t.RunPartA1_IEnumerable(2048, 1); // one partition

            // Do a ref and value type:
            passed &= t.RunPartA2_IEnumerable_DrainOneThenNext<object>(1024, 4, delegate { return new object(); });
            passed &= t.RunPartA2_IEnumerable_DrainOneThenNext<float>(1024, 4, delegate(int i) { return (float)i; });
            passed &= t.RunPartA2_IEnumerable_DrainOneThenNext<object>(1024 * 8, 8, delegate { return new object(); });
            passed &= t.RunPartA2_IEnumerable_DrainOneThenNext<float>(1024 * 8, 8, delegate(int i) { return (float)i; });
            passed &= t.RunPartA3_IEnumerable_DrainAllEvenly<object>(1024, 4, delegate { return new object(); });
            passed &= t.RunPartA3_IEnumerable_DrainAllEvenly<float>(1024, 4, delegate(int i) { return (float)i; });
            passed &= t.RunPartA3_IEnumerable_DrainAllEvenly<object>(1024 * 8, 8, delegate { return new object(); });
            passed &= t.RunPartA3_IEnumerable_DrainAllEvenly<float>(1024 * 8, 8, delegate(int i) { return (float)i; });
            passed &= t.RunPartA3_IEnumerable_DrainAllEvenly<long>(1024 * 8, 8, delegate(int i) { return (long)i; });

            // @BUGBUG: disabled. Marshal.SizeOf throws for non-integral data types.
            // passed &= RunPartA3_IEnumerable_DrainAllEvenly<DateTime>(1024 * 8, 8, delegate(int i) { return new DateTime((long)i); });

            return passed;
        }

        internal static bool DrivePartitionAlgorithm_Parallel(PartitionAlgorithmTest t)
        {
            bool passed = true;

            // -- IEnumerable data source --

            // Concurrent:
            passed &= t.RunPartC1_IEnumerable_InParallel<object>(1024 * 8, 8, delegate { return new object(); });
            passed &= t.RunPartC1_IEnumerable_InParallel<float>(1024 * 8, 8, delegate(int i) { return (float)i; });
            passed &= t.RunPartC1_IEnumerable_InParallel<object>(1024 * 8, 4, delegate { return new object(); });
            passed &= t.RunPartC1_IEnumerable_InParallel<float>(1024 * 8, 4, delegate(int i) { return (float)i; });

            return passed;
        }
    }

    class PartitionAlgorithmTest
    {
        internal PartitionAlgorithmTest(Type ty)
        {
            m_partitionType = ty;
        }

        private Type m_partitionType;

        internal bool RunPartA1_IEnumerable(int dataSize, int partitions)
        {
            TestHarness.TestLog("PART a1: Part enum (default) test w/ int[]");

            TestHarness.TestLog("  > Allocating {0} integers", dataSize);

            int[] data = new int[dataSize];
            for (int i = 0; i < dataSize; i++) data[i] = i;

            // Test out the default partitioned enumerator.
            TestHarness.TestLog("  > Constructing {0} partitions", partitions);
            PartitionedStream<int, int> part = new PartitionedDataSource<int>(data, partitions, false);

            // Walk our enumerators and ensure that all of the original elements are present.
            bool[] dataChecks = new bool[data.Length];
            for (int i = 0; i < part.PartitionCount; i++)
            {
                TestHarness.TestLog("  > Walking enumerator #{0}...", i);
                IEnumerator<int> e = part[i].AsClassicEnumerator();
                int cnt = 0;
                while (e.MoveNext())
                {
                    dataChecks[e.Current] = true;
                    cnt++;
                }
                TestHarness.TestLog("    {0} elements", cnt);
            }

            // Ensure we saw everything.
            for (int i = 0; i < dataChecks.Length; i++)
            {
                Debug.Assert(dataChecks[i], string.Format("  > ** ERROR: Missing element #{0}", i));
            }
            return true;
        }

        internal delegate T ElementFactory<T>(int index);

        internal bool RunPartA2_IEnumerable_DrainOneThenNext<T>(int dataSize, int partitions, ElementFactory<T> ctor)
        {
            TestHarness.TestLog("PART a2: Part enum (default) test w/ {0}[] -- drain one then next", typeof(T).Name);

            TestHarness.TestLog("  > Allocating {0} elements", dataSize);
            T[] data = new T[dataSize];
            for (int i = 0; i < dataSize; i++) data[i] = ctor(i);

            TestHarness.TestLog("  > Constructing {0} partitions", partitions);
            PartitionedStream<T, int> part = new PartitionedDataSource<T>(data, partitions, false);

            // Walk our enumerators and ensure that all of the original elements are present.
            bool[] dataChecks = new bool[data.Length];
            for (int i = 0; i < part.PartitionCount; i++)
            {
                TestHarness.TestLog("  > Walking enumerator #{0}...", i);
                IEnumerator<T> e = part[i].AsClassicEnumerator();
                int cnt = 0;
                while (e.MoveNext())
                {
                    // NOTE!!! If the factory method ever returns dups, this test will fail.
                    int idx = Array.IndexOf(data, e.Current);
                    Debug.Assert(idx != -1, string.Format("**ERROR: Element not found in original list! {0}", e.Current));
                    dataChecks[idx] = true;
                    cnt++;
                }
                TestHarness.TestLog("    {0} elements", cnt);
            }

            // Ensure we saw everything.
            for (int i = 0; i < dataChecks.Length; i++)
            {
                Debug.Assert(dataChecks[i], string.Format("  > ** ERROR: Missing element #{0}", i));
            }
            return true;
        }

        internal bool RunPartA3_IEnumerable_DrainAllEvenly<T>(int dataSize, int partitions, ElementFactory<T> ctor)
        {
            TestHarness.TestLog("PART a3: Part enum (default) test w/ {0}[] -- drain all evenly", typeof(T).Name);

            TestHarness.TestLog("  > Allocating {0} elements", dataSize);
            T[] data = new T[dataSize];
            for (int i = 0; i < dataSize; i++) data[i] = ctor(i);

            TestHarness.TestLog("  > Constructing {0} partitions", partitions);
            PartitionedStream<T, int> part = new PartitionedDataSource<T>(data, partitions, false);

            // Walk our enumerators and ensure that all of the original elements are present.
            bool[] dataChecks = new bool[data.Length];
            bool[] done = new bool[partitions];
            int cnt = 0;
            int[] counts = new int[partitions];
            while (!Array.TrueForAll(done, delegate(bool b) { return b; }))
            {
                for (int i = 0; i < part.PartitionCount; i++)
                {
                    if (cnt % (dataSize / 10) == 0) TestHarness.TestLog("  > Consumed {0} elems...", cnt);
                    IEnumerator<T> e = part[i].AsClassicEnumerator();
                    if (e.MoveNext())
                    {
                        // NOTE!!! If the factory method ever returns dups, this test will fail.
                        int idx = Array.IndexOf(data, e.Current);
                        Debug.Assert(idx != -1, string.Format("**ERROR: Element not found in original list! '{0}'", e.Current));
                        dataChecks[idx] = true;
                        cnt++;
                        counts[i]++;
                    }
                    else
                    {
                        done[i] = true;
                    }
                }
            }
            TestHarness.TestLog("  > Consumed {0} elems total", cnt);
            for (int i = 0; i < partitions; i++)
            {
                TestHarness.TestLog("      (Partition {0} : {1} elems)", i, counts[i]);
            }

            // Ensure we saw everything.
            for (int i = 0; i < dataChecks.Length; i++)
            {
                Debug.Assert(dataChecks[i], string.Format("  > ** ERROR: Missing element #{0}", i));
            }
            return true;
        }

        internal bool RunPartC1_IEnumerable_InParallel<T>(int dataSize, int partitions, ElementFactory<T> ctor)
        {
            TestHarness.TestLog("PART c1: Part enum (default) test w/ {0}[] -- PARALLEL", typeof(T).Name);

            TestHarness.TestLog("  > Allocating {0} elements", dataSize);
            T[] data = new T[dataSize];
            for (int i = 0; i < dataSize; i++) data[i] = ctor(i);

            TestHarness.TestLog("  > Constructing {0} partitions", partitions);
            PartitionedStream<T,int> part = new PartitionedDataSource<T>(data, partitions, false);

            // Walk our enumerators and ensure that all of the original elements are present.
            int done = partitions;
            bool[,] dataChecks = new bool[partitions, data.Length];
            ManualResetEvent startEvent = new ManualResetEvent(false);
            ManualResetEvent doneEvent = new ManualResetEvent(false);

            int[] counts = new int[partitions];

            for (int i = 0; i < part.PartitionCount; i++)
            {
                int my_i = i;
                ThreadPool.QueueUserWorkItem(delegate {
                    startEvent.WaitOne();

                    TestHarness.TestLog("  > Walking enumerator #{0} on thread {1}...", my_i, Thread.CurrentThread.ManagedThreadId);
                    IEnumerator<T> e = part[my_i].AsClassicEnumerator();
                    while (e.MoveNext())
                    {
                        // NOTE!!! If the factory method ever returns dups, this test will fail.
                        int idx = Array.IndexOf(data, e.Current);
                        Debug.Assert(idx != -1, string.Format("**ERROR: Element not found in original list! {0}", e.Current));
                        dataChecks[my_i, idx] = true;
                        counts[my_i]++;
                    }
                    TestHarness.TestLog("  > {0} elements on thread {1}", counts[my_i], Thread.CurrentThread.ManagedThreadId);

                    if (Interlocked.Decrement(ref done) == 0)
                        doneEvent.Set();
                });
            }

            startEvent.Set();
            doneEvent.WaitOne();

            int sum = 0;
            foreach (int s in counts) sum += s;
            TestHarness.TestLog("  > Consumed {0} elems total", sum);
            for (int i = 0; i < partitions; i++)
                TestHarness.TestLog("      (Partition {0} : {1} elems)", i, counts[i]);

            // Ensure we saw everything.
            for (int i = 0; i < dataChecks.GetLength(0); i++)
            {
                bool b = false;
                for (int j = 0; j < partitions; j++)
                    b |= dataChecks[j, i];
                Debug.Assert(b, string.Format("  > ** ERROR: Missing element #{0}", i));
            }
            return true;
        }

    }
}
