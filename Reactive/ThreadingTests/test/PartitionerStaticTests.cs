using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace plinq_devtests
{

    internal static class PartitionerStaticTests
    {
        #region Utility functions & private class
        //the following booleans should all be set to false in check in version
        internal static bool SelfDefinedAssertionOn = true;

        internal static void DebugMessage(bool DebugMessageOn, Action output)
        {
            if (DebugMessageOn)
                output();
            else { }
        }

        internal static void Assert(bool expression, string methodName)
        {
            if (SelfDefinedAssertionOn && !expression)
            {
                Console.WriteLine("Assertion failure in {0}", methodName);
            }
            else
                Debug.Assert(expression);
        }

        public class DisposeTrackingEnumerable<T> : IEnumerable<T>
        {
            protected IEnumerable<T> m_data;
            List<DisposeTrackingEnumerator<T>> s_enumerators = new List<DisposeTrackingEnumerator<T>>();

            public DisposeTrackingEnumerable(IEnumerable<T> enumerable)
            {
                m_data = enumerable;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                DisposeTrackingEnumerator<T> walker = new DisposeTrackingEnumerator<T>(m_data.GetEnumerator());
                lock (s_enumerators)
                {
                    s_enumerators.Add(walker);
                }
                return walker;
            }

            public IEnumerator<T> GetEnumerator()
            {
                DisposeTrackingEnumerator<T> walker = new DisposeTrackingEnumerator<T>(m_data.GetEnumerator());
                lock (s_enumerators)
                {
                    s_enumerators.Add(walker);
                }
                return walker;
            }

            public bool AreEnumeratorsDisposed()
            {
                for (int i = 0; i < s_enumerators.Count; i++)
                {
                    if (!s_enumerators[i].IsDisposed())
                    {
                        Console.WriteLine("enumerator {0} was not disposed.", i);
                        return false;
                    }
                }
                TestHarness.TestLog("underlying enumerator disposed properly");
                return true;
            }
        }

        /// <summary>
        /// This is the Enumerator that DisposeTtracking Enumerable generates when GetEnumerator is called.
        /// We are simply wrapping an Enumerator and tracking whether Dispose had been called or not.
        /// </summary>
        /// <typeparam name="T">The type of the element</typeparam>
        internal class DisposeTrackingEnumerator<T> : IEnumerator<T>
        {
            IEnumerator<T> m_elements;
            bool disposed;

            public DisposeTrackingEnumerator(IEnumerator<T> enumerator)
            {
                m_elements = enumerator;
                disposed = false;
            }

            public Boolean MoveNext()
            {
                return m_elements.MoveNext();
            }

            public T Current
            {
                get { return m_elements.Current; }
            }

            Object System.Collections.IEnumerator.Current
            {
                get { return m_elements.Current; }
            }

            /// <summary>
            /// Dispose the underlying Enumerator, and supresses finalization
            /// so that we will not throw.
            /// </summary>
            public void Dispose()
            {
                GC.SuppressFinalize(this);
                m_elements.Dispose();
                disposed = true;
            }

            public void Reset()
            {
                m_elements.Reset();
            }

            public bool IsDisposed()
            {
                return disposed;
            }
        }

        #endregion

        internal static bool RunPartitionerStaticTests()
        {
            bool passed = true;

            passed &= RunPartitionerStaticTest_Dispose();
            passed &= RunPartitionerStaticTest_DisposeException();
            passed &= RunPartitionerStaticTest_Exceptions();
            passed &= RunPartitionerStaticTest_EmptyPartitions();

            passed &= RunPartitionerStaticTest_StaticPartitioningIList(11, 8);
            passed &= RunPartitionerStaticTest_StaticPartitioningIList(8, 11);
            passed &= RunPartitionerStaticTest_StaticPartitioningIList(10, 10);
            passed &= RunPartitionerStaticTest_StaticPartitioningIList(10000, 1);
            passed &= RunPartitionerStaticTest_StaticPartitioningIList(10000, 4);
            passed &= RunPartitionerStaticTest_StaticPartitioningIList(10000, 357);

            passed &= RunPartitionerStaticTest_StaticPartitioningArray(11, 8);
            passed &= RunPartitionerStaticTest_StaticPartitioningArray(8, 11);
            passed &= RunPartitionerStaticTest_StaticPartitioningArray(10, 10);
            passed &= RunPartitionerStaticTest_StaticPartitioningArray(10000, 1);
            passed &= RunPartitionerStaticTest_StaticPartitioningArray(10000, 4);
            passed &= RunPartitionerStaticTest_StaticPartitioningArray(10000, 357);

            passed &= RunPartitionerStaticTest_LoadBalanceIList(11, 8);
            passed &= RunPartitionerStaticTest_LoadBalanceIList(8, 11);
            passed &= RunPartitionerStaticTest_LoadBalanceIList(11, 11);
            passed &= RunPartitionerStaticTest_LoadBalanceIList(10000, 1);
            passed &= RunPartitionerStaticTest_LoadBalanceIList(10000, 4);
            passed &= RunPartitionerStaticTest_LoadBalanceIList(10000, 23);

            passed &= RunPartitionerStaticTest_LoadBalanceArray(11, 8);
            passed &= RunPartitionerStaticTest_LoadBalanceArray(8, 11);
            passed &= RunPartitionerStaticTest_LoadBalanceArray(11, 11);
            passed &= RunPartitionerStaticTest_LoadBalanceArray(10000, 1);
            passed &= RunPartitionerStaticTest_LoadBalanceArray(10000, 4);
            passed &= RunPartitionerStaticTest_LoadBalanceArray(10000, 23);


            passed &= RunPartitionerStaticTest_LoadBalanceEnumerator(11, 8);
            passed &= RunPartitionerStaticTest_LoadBalanceEnumerator(8, 11);
            passed &= RunPartitionerStaticTest_LoadBalanceEnumerator(10, 10);
            passed &= RunPartitionerStaticTest_LoadBalanceEnumerator(10000, 1);
            passed &= RunPartitionerStaticTest_LoadBalanceEnumerator(10000, 4);
            passed &= RunPartitionerStaticTest_LoadBalanceEnumerator(10000, 37);

            return passed;
        }

        // In the official dev unit test run, this test should be commented out
        // - Each time we call GetDynamicPartitions method, we create an internal "reader enumerator" to read the 
        // source data, and we need to make sure that whenever the object returned by GetDynmaicPartitions is disposed,
        // the "reader enumerator" is also disposed.
        private static bool RunPartitionerStaticTest_DisposeException()
        {
            TestHarness.TestLog("RunPartitionerStaticTest_DisposeException: test ObjectDisposedException");

            var data = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var enumerable = new DisposeTrackingEnumerable<int>(data);
            var partitioner = Partitioner.Create(enumerable);
            var partition = partitioner.GetDynamicPartitions();
            IDisposable d = partition as IDisposable;
            if (d == null)
            {
                TestHarness.TestLog("failed casting to IDisposable");
                return false;
            }
            else
            {
                d.Dispose();
            }

            try
            {
                var enum1 = partition.GetEnumerator();
                TestHarness.TestLog("failed. Expecting ObjectDisposedException to be thrown");
                return false;
            }
            catch (ObjectDisposedException)
            { }
            return true;
        }

        private static bool RunPartitionerStaticTest_Dispose()
        {
            TestHarness.TestLog("RunPartitionerStaticTest_Dispose()");

            bool passed = true;

            IList<int> data = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            Console.WriteLine("foreach");
            DisposeTrackingEnumerable<int> source = new DisposeTrackingEnumerable<int>(data);
            foreach (int x in source) { }

            Console.WriteLine("P.ForEach: ");
            source = new DisposeTrackingEnumerable<int>(data);
            Parallel.ForEach(source, _ => { });
            passed &= source.AreEnumeratorsDisposed();

            Console.WriteLine("PLINQ.ForAll: ");
            source = new DisposeTrackingEnumerable<int>(data);
            ParallelEnumerable.ForAll(source.AsParallel(), _ => { });
            passed &= source.AreEnumeratorsDisposed();

            Console.WriteLine("Partitioner (GetPartitions-raw): ");
            source = new DisposeTrackingEnumerable<int>(data);
            Partitioner.Create(source).GetPartitions(1)[0].Dispose();
            passed &= source.AreEnumeratorsDisposed();

            Console.WriteLine("Partitioner (GetOrderablePartitions-raw): ");
            source = new DisposeTrackingEnumerable<int>(data);
            Partitioner.Create(source).GetOrderablePartitions(1)[0].Dispose();
            passed &= source.AreEnumeratorsDisposed();

            Console.WriteLine("Partitioner (GetDynamicPartitions-raw): ");
            source = new DisposeTrackingEnumerable<int>(data);
            ((IDisposable)Partitioner.Create(source).GetDynamicPartitions()).Dispose();
            passed &= source.AreEnumeratorsDisposed();


            Console.WriteLine("Partitioner (GetOrderableDynamicPartitions-raw): ");
            source = new DisposeTrackingEnumerable<int>(data);
            ((IDisposable)Partitioner.Create(source).GetOrderableDynamicPartitions()).Dispose();
            passed &= source.AreEnumeratorsDisposed();

            Console.WriteLine("Partitioner (TPL): ");
            source = new DisposeTrackingEnumerable<int>(data);
            Parallel.ForEach(Partitioner.Create(source), _ => { });
            passed &= source.AreEnumeratorsDisposed();

            Console.WriteLine("Partitioner (PLINQ): ");
            var partitions = Partitioner.Create(source);
            partitions.AsParallel().ForAll((x) => { });
            passed &= source.AreEnumeratorsDisposed();

            if (!passed)
            {
                TestHarness.TestLog("failed, underlying enumerator not disposed");
            }
            return passed;
        }

        private static bool RunPartitionerStaticTest_Exceptions()
        {
            TestHarness.TestLog("RunPartitionerStaticTest_Exceptions");

            TestHarness.TestLog("Testing ArgumentNullException with data==null");
            bool passed = true, gotException;
            // Test ArgumentNullException of source data
            OrderablePartitioner<int> partitioner;
            for (int algorithm = 0; algorithm < 5; algorithm++)
            {
                gotException = false;
                try
                {
                    partitioner = PartitioningWithAlgorithm<int>(null, algorithm);
                }
                catch (ArgumentNullException)
                {
                    gotException = true;
                }
                if (!gotException)
                {
                    TestHarness.TestLog("Failure in partitioning algorithm {0}, didn't catch ArgumentNullException", algorithm);
                    passed = false;
                }
            }
            // Test NotSupportedException of Reset: already tested in RunTestWithAlgorithm
            // Test InvalidOperationException: already tested in TestPartitioningCore

            // Test ArgumentOutOfRangeException of partitionCount==0
            TestHarness.TestLog("Testing ArgumentOutOfRangeException with partitionCount<=0");
            int[] data = new int[10000];
            for (int i = 0; i < 10000; i++)
                data[i] = i;

            //test GetOrderablePartitions method for 0-4 algorithms, try to catch ArgumentOutOfRangeException
            for (int algorithm = 0; algorithm < 5; algorithm++)
            {
                partitioner = PartitioningWithAlgorithm<int>(data, algorithm);
                gotException = false;
                try
                {
                    var partitions1 = partitioner.GetOrderablePartitions(0);
                }
                catch (ArgumentOutOfRangeException)
                {
                    gotException = true;
                }
                if (!gotException)
                {
                    TestHarness.TestLog("Failure in GetOrderablePartitions of algorithm {0}, didn't catch ArgumentOutOfRangeException", algorithm);
                    passed = false;
                }
            }
            return passed;
        }

        private static bool RunPartitionerStaticTest_EmptyPartitions()
        {
            TestHarness.TestLog("RunPartitionerStaticTest_EmptyPartitions: partitioning an empty list into 4 partitions");
            int[] data = new int[0];

            bool passed = true;
            // Test ArgumentNullException of source data
            OrderablePartitioner<int> partitioner;

            for (int algorithm = 0; algorithm < 5; algorithm++)
            {
                partitioner = PartitioningWithAlgorithm<int>(data, algorithm);
                //test GetOrderablePartitions
                var partitions1 = partitioner.GetOrderablePartitions(4);
                //verify all partitions are empty
                for (int i = 0; i < 4; i++)
                {
                    passed &= (!partitions1[i].MoveNext());
                }

                //test GetOrderableDynamicPartitions
                try
                {
                    var partitions2 = partitioner.GetOrderableDynamicPartitions();

                    //verify all partitions are empty
                    var newPartition = partitions2.GetEnumerator();
                    passed &= (!newPartition.MoveNext());
                }
                catch (NotSupportedException)
                {
                    Debug.Assert(IsStaticPartition(algorithm));
                }
            }
            if (!passed)
            {
                TestHarness.TestLog("failed: resulting partitions not empty as supposed to");
            }


            return passed;
        }

        private static bool RunPartitionerStaticTest_StaticPartitioningIList(int dataSize, int partitionCount)
        {
            TestHarness.TestLog("RunPartitionerStaticTest_StaticPartitioningIList({0}, {1})", dataSize, partitionCount);
            return RunTestWithAlgorithm(dataSize, partitionCount, 0);
        }

        private static bool RunPartitionerStaticTest_StaticPartitioningArray(int dataSize, int partitionCount)
        {
            TestHarness.TestLog("RunPartitionerStaticTest_StaticPartitioningArray({0}, {1})", dataSize, partitionCount);
            return RunTestWithAlgorithm(dataSize, partitionCount, 1);
        }

        private static bool RunPartitionerStaticTest_LoadBalanceIList(int dataSize, int partitionCount)
        {
            TestHarness.TestLog("RunPartitionerStaticTest_LoadBalanceIList({0}, {1})", dataSize, partitionCount);
            return RunTestWithAlgorithm(dataSize, partitionCount, 2);
        }

        private static bool RunPartitionerStaticTest_LoadBalanceArray(int dataSize, int partitionCount)
        {
            TestHarness.TestLog("RunPartitionerStaticTest_LoadBalanceArray({0}, {1})", dataSize, partitionCount);
            return RunTestWithAlgorithm(dataSize, partitionCount, 3);
        }

        private static bool RunPartitionerStaticTest_LoadBalanceEnumerator(int dataSize, int partitionCount)
        {
            TestHarness.TestLog("RunPartitionerStaticTest_LoadBalanceEnumerator({0}, {1})", dataSize, partitionCount);
            return RunTestWithAlgorithm(dataSize, partitionCount, 4);
        }

        private static bool IsStaticPartition(int algorithm)
        {
            return algorithm < 2;
        }

        private static bool RunTestWithAlgorithm(int dataSize, int partitionCount, int algorithm)
        {
            //we set up the KeyValuePair in the way that keys and values should always be the same
            //for all partitioning algorithms. So that we can use a bitmap (boolarray) to check whether
            //any elements are missing in the end.
            int[] data = new int[dataSize];
            for (int i = 0; i < dataSize; i++)
                data[i] = i;

            bool passed = true;
            IEnumerator<KeyValuePair<long, int>>[] partitionsUnderTest = new IEnumerator<KeyValuePair<long, int>>[partitionCount];

            //step 1: test GetOrderablePartitions
            DebugMessage(false, () => Console.WriteLine("Testing GetOrderablePartitions"));
            OrderablePartitioner<int> partitioner = PartitioningWithAlgorithm<int>(data, algorithm);
            var partitions1 = partitioner.GetOrderablePartitions(partitionCount);

            //convert it to partition array for testing
            for (int i = 0; i < partitionCount; i++)
                partitionsUnderTest[i] = partitions1[i];

            Assert(partitions1.Count == partitionCount, "RunPartitionerStaticTest_LoadBalanceIList");
            passed &= TestPartitioningCore(dataSize, partitionCount, data, IsStaticPartition(algorithm), partitionsUnderTest);

            //step 2: test GetOrderableDynamicPartitions
            DebugMessage(false, () => Console.WriteLine("Testing GetOrderableDynamicPartitions"));
            bool gotException = false;
            try
            {
                var partitions2 = partitioner.GetOrderableDynamicPartitions();
                for (int i = 0; i < partitionCount; i++)
                    partitionsUnderTest[i] = partitions2.GetEnumerator();
                passed &= TestPartitioningCore(dataSize, partitionCount, data, IsStaticPartition(algorithm), partitionsUnderTest);
            }
            catch (NotSupportedException)
            {
                //swallow this exception: static partitioning doesn't support GetOrderableDynamicPartitions
                gotException = true;
            }

            if (IsStaticPartition(algorithm) && !gotException)
            {
                TestHarness.TestLog("Failure: didn't catch \"NotSupportedException\" for static partitioning");
                passed = false;
            }

            return passed;
        }

        private static OrderablePartitioner<T> PartitioningWithAlgorithm<T>(T[] data, int algorithm)
        {
            switch (algorithm)
            {
                //static partitioning through IList
                case (0):
                    return Partitioner.Create((IList<T>)data, false);

                //static partitioning through Array
                case (1):
                    return Partitioner.Create(data, false);

                //dynamic partitioning through IList
                case (2):
                    return Partitioner.Create((IList<T>)data, true);

                //dynamic partitioning through Arrray
                case (3):
                    return Partitioner.Create(data, true);

                //dynamic partitioning through IEnumerator
                case (4):
                    return Partitioner.Create((IEnumerable<T>)data);
                default: throw new InvalidOperationException("no such partitioning algorithm");
            }
        }

        private static bool TestPartitioningCore(int dataSize, int partitionCount, int[] data, bool staticPartitioning,
            IEnumerator<KeyValuePair<long, int>>[] partitions)
        {
            bool[] boolarray = new bool[dataSize];
            bool passed = true,
                keysOrderedWithinPartition = true,
                keysOrderedAcrossPartitions = true;
            int enumCount = 0; //count how many elements are enumerated by all partitions
            Thread[] threadArray = new Thread[partitionCount];

            for (int i = 0; i < partitionCount; i++)
            {
                int my_i = i;
                threadArray[i] = new Thread(() =>
                {
                    DebugMessage(false, () => Console.WriteLine("partition {0} is assigned to thread {1}", my_i, Thread.CurrentThread.ManagedThreadId));
                    int localOffset = 0;
                    int lastElement = -1;

                    //variables to compute key/value consistency for static partitioning.
                    int quotient, remainder;
                    quotient = Math.DivRem(dataSize, partitionCount, out remainder);

                    bool gotException = false;
                    //call Current before MoveNext, should throw an exception
                    try
                    {
                        var temp = partitions[my_i].Current;
                    }
                    catch (InvalidOperationException)
                    {
                        gotException = true;
                    }
                    if (!gotException)
                    {
                        TestHarness.TestLog("Failure: didn't catch the InvalidOperationException when call Current before MoveNext");
                        passed = false;
                    }

                    while (partitions[my_i].MoveNext())
                    {
                        int key = (int)partitions[my_i].Current.Key,
                            value = partitions[my_i].Current.Value;

                        Assert(key == value, "TestPartitioningCore");
                        boolarray[key] = true;
                        Interlocked.Increment(ref enumCount);

                        //todo: check if keys are ordered increasingly within each partition.
                        keysOrderedWithinPartition &= (lastElement >= key);
                        lastElement = key;

                        //Only check this with static partitioning
                        //check keys are ordered across the partitions 
                        if (staticPartitioning)
                        {
                            int originalPosition;
                            if (my_i < remainder)
                                originalPosition = localOffset + my_i * (quotient + 1);
                            else
                                originalPosition = localOffset + remainder * (quotient + 1) + (my_i - remainder) * quotient;
                            keysOrderedAcrossPartitions &= originalPosition == value;
                        }
                        localOffset++;
                    }
                    DebugMessage(false, () => Console.WriteLine("partition {0} has {1} items", my_i, localOffset));
                }
                );
                threadArray[i].Start();
            }

            for (int i = 0; i < threadArray.Length; i++)
            {
                threadArray[i].Join();
            }

            if (keysOrderedWithinPartition)
                TestHarness.TestLog("Keys are not strictly ordered within each partition");


            // Only check this with static partitioning
            //check keys are ordered across the partitions 
            if (staticPartitioning && !keysOrderedAcrossPartitions)
            {
                TestHarness.TestLog("Keys are not strictly ordered across partitions");
                passed = false;
            }

            //check data count
            if (enumCount != dataSize)
            {
                TestHarness.TestLog("inconsistent count, requested {0}, added {1}", dataSize, enumCount);
                passed = false;
            }
            //check if any elements are missing
            if (!Array.TrueForAll(boolarray, a => a))
            {
                TestHarness.TestLog("inconsistent data: some elements are missing");
                passed = false;
            }
            return passed;
        }

    }
}
