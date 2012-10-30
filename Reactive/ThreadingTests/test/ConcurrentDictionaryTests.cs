using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

// CDS namespaces
using System.Collections.Concurrent;

namespace plinq_devtests
{
    internal static class ConcurrentDictionaryTests
    {
        internal static bool RunConcurrentDictionaryTests()
        {
            bool passed = true;

            passed &= RunDictionaryTest_Add1(1, 1, 1, 100000);
            passed &= RunDictionaryTest_Add1(5, 1, 1, 100000);
            passed &= RunDictionaryTest_Add1(1, 1, 2, 50000);
            passed &= RunDictionaryTest_Add1(1, 1, 5, 20000);
            passed &= RunDictionaryTest_Add1(4, 0, 4, 20000);
            passed &= RunDictionaryTest_Add1(16, 31, 4, 20000);
            passed &= RunDictionaryTest_Add1(64, 5, 5, 50000);
            passed &= RunDictionaryTest_Add1(5, 5, 5, 250000);

            passed &= RunDictionaryTest_Update1(1, 1, 100000);
            passed &= RunDictionaryTest_Update1(5, 1, 100000);
            passed &= RunDictionaryTest_Update1(1, 2, 50000);
            passed &= RunDictionaryTest_Update1(1, 5, 20001);
            passed &= RunDictionaryTest_Update1(4, 4, 20001);
            passed &= RunDictionaryTest_Update1(15, 5, 20001);
            passed &= RunDictionaryTest_Update1(64, 5, 50000);
            passed &= RunDictionaryTest_Update1(5, 5, 250000);

            passed &= RunDictionaryTest_Read1(1, 1, 100000);
            passed &= RunDictionaryTest_Read1(5, 1, 100000);
            passed &= RunDictionaryTest_Read1(1, 2, 50000);
            passed &= RunDictionaryTest_Read1(1, 5, 20001);
            passed &= RunDictionaryTest_Read1(4, 4, 20001);
            passed &= RunDictionaryTest_Read1(15, 5, 20001);
            passed &= RunDictionaryTest_Read1(64, 5, 50000);
            passed &= RunDictionaryTest_Read1(5, 5, 250000);

            passed &= RunDictionaryTest_Remove1(1, 1, 100000);
            passed &= RunDictionaryTest_Remove1(5, 1, 10000);
            passed &= RunDictionaryTest_Remove1(1, 5, 20001);
            passed &= RunDictionaryTest_Remove1(4, 4, 20001);
            passed &= RunDictionaryTest_Remove1(15, 5, 20001);
            passed &= RunDictionaryTest_Remove1(64, 5, 50000);

            passed &= RunDictionaryTest_Remove2(1);
            passed &= RunDictionaryTest_Remove2(10);
            passed &= RunDictionaryTest_Remove2(50000);

            passed &= RunDictionaryTest_Remove3();

            passed &= RunDictionaryTest(1, 1, 1, 100000, TestMethod.GetOrAdd);
            passed &= RunDictionaryTest(5, 1, 1, 100000, TestMethod.GetOrAdd);
            passed &= RunDictionaryTest(1, 1, 2, 50000, TestMethod.GetOrAdd);
            passed &= RunDictionaryTest(1, 1, 5, 20000, TestMethod.GetOrAdd);
            passed &= RunDictionaryTest(4, 0, 4, 20000, TestMethod.GetOrAdd);
            passed &= RunDictionaryTest(16, 31, 4, 20000, TestMethod.GetOrAdd);
            passed &= RunDictionaryTest(64, 5, 5, 50000, TestMethod.GetOrAdd);
            passed &= RunDictionaryTest(5, 5, 5, 250000, TestMethod.GetOrAdd);

            passed &= RunDictionaryTest(1, 1, 1, 100000, TestMethod.AddOrUpdate);
            passed &= RunDictionaryTest(5, 1, 1, 100000, TestMethod.AddOrUpdate);
            passed &= RunDictionaryTest(1, 1, 2, 50000, TestMethod.AddOrUpdate);
            passed &= RunDictionaryTest(1, 1, 5, 20000, TestMethod.AddOrUpdate);
            passed &= RunDictionaryTest(4, 0, 4, 20000, TestMethod.AddOrUpdate);
            passed &= RunDictionaryTest(16, 31, 4, 20000, TestMethod.AddOrUpdate);
            passed &= RunDictionaryTest(64, 5, 5, 50000, TestMethod.AddOrUpdate);
            passed &= RunDictionaryTest(5, 5, 5, 250000, TestMethod.AddOrUpdate);

            passed &= RunDictionaryTest_BugFix669376();
            return passed;
        }

        private static bool RunDictionaryTest_Add1(int cLevel, int initSize, int threads, int addsPerThread)
        {
            TestHarness.TestLog(
                "* RunDictionaryTest_Add1(cLevel={0}, initSize={1}, threads={2}, addsPerThread={3})",
                cLevel, initSize, threads, addsPerThread);

            IDictionary<int, int> dict = new ConcurrentDictionary<int, int>(cLevel, 1);

            int count = threads;
            using (ManualResetEvent mre = new ManualResetEvent(false))
            {
                for (int i = 0; i < threads; i++)
                {
                    int ii = i;
                    ThreadPool.QueueUserWorkItem(
                        (o) =>
                        {
                            for (int j = 0; j < addsPerThread; j++)
                            {
                                dict.Add(j + ii * addsPerThread, -(j + ii * addsPerThread));
                            }
                            if (Interlocked.Decrement(ref count) == 0) mre.Set();
                        });
                }
                mre.WaitOne();
            }

            if (dict.Any(pair => pair.Key != -pair.Value))
            {
                TestHarness.TestLog("  > Invalid value for some key in the dictionary.");
                return false;
            }

            var gotKeys = dict.Select(pair => pair.Key).OrderBy(i => i).ToArray();
            var expectKeys = Enumerable.Range(0, threads * addsPerThread);

            if (!gotKeys.SequenceEqual(expectKeys))
            {
                TestHarness.TestLog("  > The set of keys in the dictionary is invalid.");
                return false;
            }

            // Finally, let's verify that the count is reported correctly.
            int expectedCount = threads * addsPerThread;
            if (dict.Count != expectedCount || dict.ToArray().Length != expectedCount || dict.ToList().Count() != expectedCount)
            {
                TestHarness.TestLog("  > Incorrect count of elements reported for the dictionary.");
                return false;
            }

            return true;
        }

        private static bool RunDictionaryTest_Update1(int cLevel, int threads, int updatesPerThread)
        {
            TestHarness.TestLog("* RunDictionaryTest_Update1(cLevel={0}, threads={1}, updatesPerThread={2})", cLevel, threads, updatesPerThread);
            IDictionary<int, int> dict = new ConcurrentDictionary<int, int>(cLevel, 1);

            for (int i = 1; i <= updatesPerThread; i++) dict[i] = i;

            int running = threads;
            using (ManualResetEvent mre = new ManualResetEvent(false))
            {
                for (int i = 0; i < threads; i++)
                {
                    int ii = i;
                    ThreadPool.QueueUserWorkItem(
                        (o) =>
                        {
                            for (int j = 1; j <= updatesPerThread; j++)
                            {
                                dict[j] = (ii + 2) * j;
                            }
                            if (Interlocked.Decrement(ref running) == 0) mre.Set();
                        });
                }
                mre.WaitOne();
            }

            if ((from pair in dict
                 let div = pair.Value / pair.Key
                 let rem = pair.Value % pair.Key
                 select rem != 0 || div < 2 || div > threads + 1)
                .Any(res => res))
            {
                TestHarness.TestLog("  > Invalid value for some key in the dictionary.");
                return false;
            }

            var gotKeys = dict.Select(pair => pair.Key).OrderBy(i => i).ToArray();
            var expectKeys = Enumerable.Range(1, updatesPerThread);
            if (!gotKeys.SequenceEqual(expectKeys))
            {
                TestHarness.TestLog("  > The set of keys in the dictionary is invalid.");
                return false;
            }
            return true;
        }

        private static bool RunDictionaryTest_Read1(int cLevel, int threads, int readsPerThread)
        {
            TestHarness.TestLog("* RunDictionaryTest_Read1(cLevel={0}, threads={1}, readsPerThread={2})", cLevel, threads, readsPerThread);
            IDictionary<int, int> dict = new ConcurrentDictionary<int, int>(cLevel, 1);

            for (int i = 0; i < readsPerThread; i += 2) dict[i] = i;

            int count = threads;
            bool passed = true;
            using (ManualResetEvent mre = new ManualResetEvent(false))
            {
                for (int i = 0; i < threads; i++)
                {
                    int ii = i;
                    ThreadPool.QueueUserWorkItem(
                        (o) =>
                        {
                            for (int j = 0; j < readsPerThread; j++)
                            {
                                int val = 0;
                                if (dict.TryGetValue(j, out val))
                                {
                                    if (j % 2 == 1 || j != val)
                                    {
                                        TestHarness.TestLog("  > Invalid element in the dictionary.");
                                        passed = false;
                                        break;
                                    }
                                }
                                else
                                {
                                    if (j % 2 == 0)
                                    {
                                        TestHarness.TestLog("  > Element missing from the dictionary");
                                        passed = false;
                                        break;
                                    }
                                }
                            }
                            if (Interlocked.Decrement(ref count) == 0) mre.Set();
                        });
                }
                mre.WaitOne();
            }

            return passed;
        }

        private static bool RunDictionaryTest_Remove1(int cLevel, int threads, int removesPerThread)
        {
            TestHarness.TestLog("* RunDictionaryTest_Remove1(cLevel={0}, threads={1}, removesPerThread={2})", cLevel, threads, removesPerThread);
            ConcurrentDictionary<int, int> dict = new ConcurrentDictionary<int, int>(cLevel, 1);

            int N = 2 * threads * removesPerThread;

            for (int i = 0; i < N; i++) dict[i] = -i;

            // The dictionary contains keys [0..N), each key mapped to a value equal to the key.
            // Threads will cooperatively remove all even keys.

            int running = threads;
            bool passed = true;

            using (ManualResetEvent mre = new ManualResetEvent(false))
            {
                for (int i = 0; i < threads; i++)
                {
                    int ii = i;
                    ThreadPool.QueueUserWorkItem(
                        (o) =>
                        {
                            for (int j = 0; j < removesPerThread; j++)
                            {
                                int value;
                                int key = 2 * (ii + j * threads);
                                if (!dict.TryRemove(key, out value))
                                {
                                    TestHarness.TestLog("  > Failed to remove an element, which should be in the dictionary.");
                                    passed = false;
                                    break;
                                }

                                if (value != -key)
                                {
                                    TestHarness.TestLog("  > Invalid value for some key in the dictionary.");
                                    passed = false;
                                    break;
                                }
                            }
                            if (Interlocked.Decrement(ref running) == 0) mre.Set();
                        });
                }
                mre.WaitOne();
            }

            if (!passed) return false;

            var res = dict.ToArray();
            if (res.Any(pair => pair.Key != -pair.Value))
            {
                TestHarness.TestLog("  > Invalid value for some key in the dictionary.");
                return false;
            }

            IEnumerable<int> gotKeys = res.Select(pair => pair.Key).OrderBy(i => i);
            IEnumerable<int> expectKeys = Enumerable.Range(0, threads * removesPerThread).Select(i => 2 * i + 1);
            if (!gotKeys.SequenceEqual(expectKeys))
            {
                TestHarness.TestLog("  > The set of keys in the dictionary is invalid.");
                return false;
            }

            // Finally, let's verify that the count is reported correctly.
            int expectedCount = expectKeys.Count();
            if (dict.Count != expectedCount || dict.ToArray().Length != expectedCount || dict.ToList().Count() != expectedCount)
            {
                TestHarness.TestLog("  > Incorrect count of elements reported for the dictionary.");
                return false;
            }

            return true;
        }

        private static bool RunDictionaryTest_Remove2(int removesPerThread)
        {
            TestHarness.TestLog("* RunDictionaryTest_Remove2(removesPerThread={0})", removesPerThread);
            ConcurrentDictionary<int, int> dict = new ConcurrentDictionary<int, int>();

            for (int i = 0; i < removesPerThread; i++) dict[i] = -i;

            // The dictionary contains keys [0..N), each key mapped to a value equal to the key.
            // Threads will cooperatively remove all even keys.

            int running = 2;
            bool passed = true;

            bool[][] seen = new bool[2][];
            for (int i = 0; i < 2; i++) seen[i] = new bool[removesPerThread];

            using (ManualResetEvent mre = new ManualResetEvent(false))
            {
                for (int t = 0; t < 2; t++)
                {
                    int thread = t;
                    ThreadPool.QueueUserWorkItem(
                        (o) =>
                        {
                            for (int key = 0; key < removesPerThread; key++)
                            {
                                int value;
                                if (dict.TryRemove(key, out value))
                                {
                                    seen[thread][key] = true;

                                    if (value != -key)
                                    {
                                        TestHarness.TestLog("  > Invalid value for some key in the dictionary.");
                                        passed = false;
                                        break;
                                    }
                                }
                            }
                            if (Interlocked.Decrement(ref running) == 0) mre.Set();
                        });
                }
                mre.WaitOne();
            }

            if (!passed) return false;

            if (dict.Count != 0)
            {
                TestHarness.TestLog("  > Expected the dictionary to be empty.");
                return false;
            }

            for (int i = 0; i < removesPerThread; i++)
            {
                if (seen[0][i] == seen[1][i])
                {
                    TestHarness.TestLog("  > Two threads appear to have removed the same element.");
                    return false;
                }
            }

            return true;
        }

        private static bool RunDictionaryTest_Remove3()
        {
            TestHarness.TestLog("* RunDictionaryTest_Remove3()");
            ConcurrentDictionary<int, int> dict = new ConcurrentDictionary<int, int>();

            dict[99] = -99;

            ICollection<KeyValuePair<int, int>> col = dict;

            // Make sure we cannot "remove" a key/value pair which is not in the dictionary
            for (int i = 0; i < 1000; i++)
            {
                if (i != 99)
                {
                    if (col.Remove(new KeyValuePair<int, int>(i, -99)) || col.Remove(new KeyValuePair<int, int>(99, -i)))
                    {
                        TestHarness.TestLog("  > Removed a key/value pair which was not supposed to be in the dictionary.");
                        return false;
                    }
                }
            }

            // Can we remove a key/value pair successfully?
            if (!col.Remove(new KeyValuePair<int, int>(99, -99)))
            {
                TestHarness.TestLog("  > Failed to remove a key/value pair which was supposed to be in the dictionary.");
                return false;
            }

            // Make sure the key/value pair is gone
            if (col.Remove(new KeyValuePair<int, int>(99, -99)))
            {
                TestHarness.TestLog("  > Removed a key/value pair which was not supposed to be in the dictionary.");
                return false;
            }

            // And that the dictionary is empty. We will check the count in a few different ways:
            if (dict.Count != 0 || dict.ToArray().Count() != 0 || dict.ToList().Count() != 0)
            {
                TestHarness.TestLog("  > Incorrect count of elements reported for the dictionary.");
                return false;
            }

            return true;
        }

        private enum TestMethod
        {
            GetOrAdd,
            AddOrUpdate
        }
        
        static private string PrintTestMethod(TestMethod testMethod)
        {
            switch (testMethod)
            {
                case (TestMethod.GetOrAdd):
                    return "GetOrAdd";
                case (TestMethod.AddOrUpdate):
                    return "AddOrUpdate";
                default:
                    return "";
            }
        }
        private static bool RunDictionaryTest(int cLevel, int initSize, int threads, int addsPerThread, TestMethod testMethod)
        {
            TestHarness.TestLog("* RunDictionaryTest_{0}, Level={1}, initSize={2}, threads={3}, addsPerThread={4})",
                            PrintTestMethod(testMethod), cLevel, initSize, threads, addsPerThread);

            ConcurrentDictionary<int, int> dict = new ConcurrentDictionary<int, int>(cLevel, 1);

            int count = threads;
            using (ManualResetEvent mre = new ManualResetEvent(false))
            {
                for (int i = 0; i < threads; i++)
                {
                    int ii = i;
                    ThreadPool.QueueUserWorkItem(
                        (o) =>
                        {
                            for (int j = 0; j < addsPerThread; j++)
                            {
                                //call either of the two overloads of GetOrAdd
                                if (j + ii % 2 == 0)
                                {
                                    dict.GetOrAdd(j, -j);
                                }
                                else
                                {
                                    dict.GetOrAdd(j, x => -x);
                                }
                            }
                            if (Interlocked.Decrement(ref count) == 0) mre.Set();
                        });
                }
                mre.WaitOne();
            }

            bool passed = true;

            if (dict.Any(pair => pair.Key != -pair.Value))
            {
                TestHarness.TestLog("  > Invalid value for some key in the dictionary.");
                passed = false;
            }


            var gotKeys = dict.Select(pair => pair.Key).OrderBy(i => i).ToArray();
            var expectKeys = Enumerable.Range(0, addsPerThread);

            if (!gotKeys.SequenceEqual(expectKeys))
            {
                TestHarness.TestLog("  > The set of keys in the dictionary is invalid.");
                passed = false;
            }

            // Finally, let's verify that the count is reported correctly.
            int expectedCount = addsPerThread;
            int count1 = dict.Count, count2 = dict.ToArray().Length,
                count3 = dict.ToList().Count;
            if (count1 != expectedCount || count2 != expectedCount || count3 != expectedCount)
            {
                TestHarness.TestLog("  > Incorrect count of elements reported for the dictionary. Expected {0}, Dict.Count {1}, ToArray.Length {2}, ToList.Count {3}",
                    expectedCount, count1, count2, count3);
                passed = false;
            }

            return passed;
        }

        private static bool RunDictionaryTest_BugFix669376()
        {
            TestHarness.TestLog("* RunDictionaryTest_BugFix669376");

            var cd = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            cd["test"] = 10;
            if (cd.ContainsKey("TEST"))
            {
                return true;
            }
            else
            {
                TestHarness.TestLog("  > Customized comparer didn't work");
                return false;
            }
        }

    }
}
