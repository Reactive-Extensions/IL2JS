using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Runtime.Serialization;
using System.Security;
using System.Collections;
using System.Collections.Concurrent;

namespace plinq_devtests
{
    /// <summary>The class that contains the unit tests of the LazyInit.</summary>
    internal static class ConcurrentBagTests
    {

        /// <summary>
        /// Run all unit tests
        /// </summary>
        /// <returns>True if succeeded, false otherwise</returns>
        internal static bool RunConcurrentBagTests()
        {

            ConcurrentBag<int> bag = null;
            bool passed = true;

            passed &= RunConcurrentBagTest1_Ctor(new int[] { 1, 2, 3 }, null);
            passed &= RunConcurrentBagTest1_Ctor(null, typeof(ArgumentNullException));

            passed &= RunConcurrentBagTest2_Add(1, 10);
            passed &= RunConcurrentBagTest2_Add(64, 100);
            passed &= RunConcurrentBagTest2_Add(128, 1000);

            bag = CreateBag(100);
            passed &= RunConcurrentBagTest3_TakeOrPeek(bag, 1, 100, true);

            bag = CreateBag(100);
            passed &= RunConcurrentBagTest3_TakeOrPeek(bag, 64, 10, false);

            bag = CreateBag(1000);
            passed &= RunConcurrentBagTest3_TakeOrPeek(bag, 128, 100, true);

            passed &= RunConcurrentBagTest4_AddAndTake(64);
            passed &= RunConcurrentBagTest4_AddAndTake(128);
            passed &= RunConcurrentBagTest4_AddAndTake(256);

            bag = CreateBag(10);
            passed &= RunConcurrentBagTest5_CopyTo(bag, null, 0, typeof(ArgumentNullException));
            passed &= RunConcurrentBagTest5_CopyTo(bag, new int[10], -1, typeof(ArgumentOutOfRangeException));
            passed &= RunConcurrentBagTest5_CopyTo(bag, new int[10], 10, typeof(ArgumentException));
            passed &= RunConcurrentBagTest5_CopyTo(bag, new int[10], 8, typeof(ArgumentException));
            passed &= RunConcurrentBagTest5_CopyTo(bag, new int[10, 5], 8, typeof(ArgumentException));
            passed &= RunConcurrentBagTest5_CopyTo(bag, new int[10], 0, null);

            passed &= RunConcurrentBagTest6_GetEnumerator();

            passed &= RunConcurrentBagTest7_BugFix575975();

            passed &= RunConcurrentBagTest8_IPCC();
            return passed;
        }

        /// <summary>
        /// Test bag constructor
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="exceptionType"></param>
        /// <param name="shouldThrow"></param>
        /// <returns>True if succeeded, false otherwise</returns>
        private static bool RunConcurrentBagTest1_Ctor(int[] collection, Type exceptionType)
        {
            TestHarness.TestLog("* RunConcurrentBagTest1_Ctor()");
            bool thrown = false;
            try
            {
                ConcurrentBag<int> bag = new ConcurrentBag<int>(collection);
                if (bag.Count != collection.Length)
                {
                    TestHarness.TestLog("Constructor failed, the bag count doesn't match the given collection count.");
                    return false;
                }
            }
            catch (Exception e)
            {
                if (exceptionType != null && !e.GetType().Equals(exceptionType))
                {
                    TestHarness.TestLog("Constructor failed, excpetions type do not match");
                    return false;
                }
                else if (exceptionType == null)
                {
                    TestHarness.TestLog("Constructor failed, it threw un expected exception");
                    return false;
                }
                thrown = true;
            }
            if (exceptionType != null && !thrown)
            {
                TestHarness.TestLog("Constructor failed, it didn't throw the expected exception");
                return false;
            }
            TestHarness.TestLog("Constructor succeeded.");
            return true;
        }

        /// <summary>
        /// Test bag addition
        /// </summary>
        /// <param name="threadsCount"></param>
        /// <param name="itemsPerThread"></param>
        /// <returns>True if succeeded, false otherwise</returns>
        private static bool RunConcurrentBagTest2_Add(int threadsCount, int itemsPerThread)
        {
            TestHarness.TestLog("* RunConcurrentBagTest1_Add(" + threadsCount + "," + itemsPerThread + ")");
            int failures = 0;
            ConcurrentBag<int> bag = new ConcurrentBag<int>();

            Thread[] threads = new Thread[threadsCount];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(() =>
                {
                    for (int j = 0; j < itemsPerThread; j++)
                    {
                        try
                        {
                            bag.Add(j);
                        }
                        catch
                        {
                            Interlocked.Increment(ref failures);
                        }
                    }
                });

                threads[i].Start();
            }

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
            }

            if (failures > 0)
            {
                TestHarness.TestLog("Add failed, " + failures + " threads threw  unexpected exceptions");
                return false;
            }
            if (bag.Count != itemsPerThread * threadsCount)
            {
                TestHarness.TestLog("Add failed, the bag count doesn't match the expected count");
                return false;
            }
            TestHarness.TestLog("Add succeeded");
            return true;
        }

        /// <summary>
        /// Test bag Take and Peek operations
        /// </summary>
        /// <param name="bag"></param>
        /// <param name="threadsCount"></param>
        /// <param name="itemsPerThread"></param>
        /// <param name="take"></param>
        /// <returns>True if succeeded, false otherwise</returns>
        private static bool RunConcurrentBagTest3_TakeOrPeek(ConcurrentBag<int> bag, int threadsCount, int itemsPerThread, bool take)
        {
            TestHarness.TestLog("* RunConcurrentBagTest3_TakeOrPeek(" + threadsCount + "," + itemsPerThread + ")");
            int bagCount = bag.Count;
            int succeeded = 0;
            int failures = 0;
            Thread[] threads = new Thread[threadsCount];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(() =>
                {
                    for (int j = 0; j < itemsPerThread; j++)
                    {
                        try
                        {
                            int data;
                            bool result = false;
                            if (take)
                            {
                                result = bag.TryTake(out data);
                            }
                            else
                            {
                                result = bag.TryPeek(out data);
                            }
                            if (result)
                            {
                                Interlocked.Increment(ref succeeded);
                            }
                            else
                            {
                                Interlocked.Increment(ref failures);
                            }

                        }
                        catch
                        {
                            Interlocked.Increment(ref failures);
                        }
                    }
                });

                threads[i].Start();
            }

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
            }

            if (take)
            {
                if (bag.Count != bagCount - succeeded)
                {
                    TestHarness.TestLog("TryTake failed, the remaing count doesn't match the expected count");
                    return false;
                }
            }
            else if (failures > 0)
            {
                TestHarness.TestLog("TryPeek failed, Unexpected exceptions has been thrown");
                return false;
            }
            TestHarness.TestLog("Try Take/peek succeeded");
            return true;


        }

        internal struct Interval
        {
            public Interval(int start, int end)
            {
                m_start = start;
                m_end = end;
            }
            internal int m_start;
            internal int m_end;
        }
        /// <summary>
        /// Test parallel Add/Take, insert uniqe elements in the bag, and each element should be removed once
        /// </summary>
        /// <param name="threadsCount"></param>
        /// <returns>True if succeeded, false otherwise</returns>
        private static bool RunConcurrentBagTest4_AddAndTake(int threadsCount)
        {
            TestHarness.TestLog("* RunConcurrentBagTest4_AddAndTake(" + threadsCount + " )");

            ConcurrentBag<int> bag = new ConcurrentBag<int>();

            Thread[] threads = new Thread[threadsCount];
            int start = 0;
            int end = 10;

            int[] validation = new int[(end - start) * threads.Length / 2];
            for (int i = 0; i < threads.Length; i += 2)
            {
                Interval v = new Interval(start, end);
                threads[i] = new Thread((o) => { Interval n = (Interval)o; Add(bag, n.m_start, n.m_end); });
                threads[i].Start(v);
                threads[i + 1] = new Thread(() => Take(bag, end - start - 1, validation));
                threads[i + 1].Start();

                int step = end - start;
                start = end;
                end += step;

            }

            for (int i = 1; i < threads.Length; i++)
            {
                threads[i].Join();
            }

            int valu = -1;

            //validation
            for (int i = 0; i < validation.Length; i++)
            {
                if (validation[i] > 1)
                {
                    TestHarness.TestLog("Add/Take failed, item " + i + " has been taken more than one");
                    // return false;
                }
                else if (validation[i] == 0)
                {
                    if (!bag.TryTake(out valu))
                    {
                        TestHarness.TestLog("Add/Take failed, the list is not empty and TryTake returned false");
                        return false;
                    }

                }
            }

            if (bag.Count > 0 || bag.TryTake(out valu))
            {
                TestHarness.TestLog("Add/Take failed, this list is not empty after all remove operations");
                return false;
            }
            TestHarness.TestLog("Add/Take succeeded");
            return true;
        }

       

        private static void Add(ConcurrentBag<int> bag, int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                bag.Add(i);
            }
        }
        private static void Take(ConcurrentBag<int> bag, int count, int[] validation)
        {
            for (int i = 0; i < count; i++)
            {
                int valu = -1;

                if (bag.TryTake(out valu) && validation != null)
                {
                    Interlocked.Increment(ref validation[valu]);
                }
            }
        }

        /// <summary>
        /// Test copyTo method
        /// </summary>
        /// <param name="bag"></param>
        /// <param name="array"></param>
        /// <param name="index"></param>
        /// <param name="exceptionType"></param>
        /// <param name="shouldThrow"></param>
        /// <returns>True if succeeded, false otherwise</returns>
        private static bool RunConcurrentBagTest5_CopyTo(ConcurrentBag<int> bag, Array array, int index, Type exceptionType)
        {
            TestHarness.TestLog("* RunConcurrentBagTest5_CopyTo(" + array + " )");
            bool thrown = false;
            try
            {
                ICollection collection = bag as ICollection;
                collection.CopyTo(array, index);
            }
            catch (Exception e)
            {
                if (exceptionType != null && !e.GetType().Equals(exceptionType))
                {
                    TestHarness.TestLog("CopyTo failed, exceptions types do not match.");
                    return false;
                }
                else if (exceptionType == null)
                {
                    TestHarness.TestLog("CopyTo failed, it threw unexpected excpetion." + e);
                    return false;
                }
                thrown = true;
            }

            if (exceptionType != null && !thrown)
            {
                TestHarness.TestLog("CopyTo failed, it didn't throw the expected excpetion.");
                return false;
            }
            TestHarness.TestLog("CopyTo succeeded.");
            return true;
        }

        /// <summary>
        /// Test enumeration
        /// </summary>
        /// <returns>True if succeeded, false otherwise</returns>
        private static bool RunConcurrentBagTest6_GetEnumerator()
        {
            TestHarness.TestLog("* RunConcurrentBagTest6_GetEnumerator()");
            ConcurrentBag<int> bag = new ConcurrentBag<int>();
            for (int i = 0; i < 100; i++)
            {
                bag.Add(i);
            }

            try
            {
                int count = 0;
                foreach (int x in bag)
                {
                    count++;
                }
                if (count != bag.Count)
                {
                    TestHarness.TestLog("GetEnumeration failed, the enumeration count doesn't match the bag count");
                    return false;
                }
            }
            catch
            {

                TestHarness.TestLog("GetEnumeration failed, it threw unexpected exception");
                return false;
            }
            TestHarness.TestLog("GetEnumeration succeeded.");
            return true; ;
        }

        private static bool RunConcurrentBagTest7_BugFix575975()
        {
            TestHarness.TestLog("* RunConcurrentBagTest7_BugFix575975");
            BlockingCollection<int> bc = new BlockingCollection<int>(new ConcurrentBag<int>());
            bool succeeded = true;
            Thread[] threads = new Thread[4];
            for (int t = 0; t < threads.Length; t++)
            {
                threads[t] = new Thread((obj) =>
                {
                    int index = (int)obj;
                    for (int i = 0; i < 100000; i++)
                    {
                        if (index < threads.Length / 2)
                        {
                            for (int j = 0; j < 1000; j++) Math.Min(j, j - 1);
                            bc.Add(i);
                        }
                        else
                        {
                            try
                            {
                                bc.Take();
                            }
                            catch // Take must not fail
                            {
                                succeeded = false;
                                break;
                            }
                        }
                    }

                });
                threads[t].Start(t);
            }
            for (int t = 0; t < threads.Length; t++)
            {
                threads[t].Join();
            }

            TestHarness.TestLog("BugFix575975 {0}", succeeded ? "succeeded" : "failed");
            return succeeded;
        }

        /// <summary>
        /// Test IPCC implementation
        /// </summary>
        /// <returns>true if succeeded, false otherwise</returns>
        private static bool RunConcurrentBagTest8_IPCC()
        {
            TestHarness.TestLog("* RunConcurrentBagTest8_IPCC");
            ConcurrentBag<int> bag = new ConcurrentBag<int>();
            IProducerConsumerCollection<int> ipcc = bag as IProducerConsumerCollection<int>;
            if (ipcc == null)
            {
                TestHarness.TestLog("*Failed, ConcurrentBag<T> doesn't implement IPCC<T>");
                return false;
            }

            if (!ipcc.TryAdd(1))
            {
                TestHarness.TestLog("*Failed, IPCC<T>.TryAdd failed");
                return false;
            }

            if (bag.Count != 1)
            {
                TestHarness.TestLog("*Failed, The count doesn't match, expected 1, actual {0}",bag.Count);
                return false;
            }

            int result = -1; 
            if (!ipcc.TryTake(out result) || result != 1)
            {
                TestHarness.TestLog("*Failed, IPCC<T>.TryTake failed");
                return false;
            }

            if (bag.Count != 0)
            {
                TestHarness.TestLog("*Failed, The count doesn't match, expected 0, actual {0}", bag.Count);
                return false;
            }
            TestHarness.TestLog("*Succeeded");
            return true;

        }

        /// <summary>
        /// Create a ComcurrentBag object
        /// </summary>
        /// <param name="numbers">number of the elements in the bag</param>
        /// <returns>The bag object</returns>
        internal static ConcurrentBag<int> CreateBag(int numbers)
        {
            ConcurrentBag<int> bag = new ConcurrentBag<int>();
            for (int i = 0; i < numbers; i++)
            {
                bag.Add(i);
            }
            return bag;
        }
    }
}