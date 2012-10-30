using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// CDS namespaces
using System.Collections.Concurrent;

namespace plinq_devtests
{
    internal static class CdsTests
    {

        //
        // ConcurrentStack<T>
        //

        internal static bool RunConcurrentStackTests()
        {
            bool passed = true;

            passed &= RunConcurrentStackTest0_Empty(0);
            passed &= RunConcurrentStackTest0_Empty(16);
            passed &= RunConcurrentStackTest1_PushAndPop(0, 0);
            passed &= RunConcurrentStackTest1_PushAndPop(5, 0);
            passed &= RunConcurrentStackTest1_PushAndPop(5, 2);
            passed &= RunConcurrentStackTest1_PushAndPop(5, 5);
            passed &= RunConcurrentStackTest1_PushAndPop(1024, 512);
            passed &= RunConcurrentStackTest1_PushAndPop(1024, 1024);
            passed &= RunConcurrentStackTest2_ConcPushAndPop(8, 1024 * 1024, 0);
            passed &= RunConcurrentStackTest2_ConcPushAndPop(8, 1024 * 1024, 1024 * 512);
            passed &= RunConcurrentStackTest2_ConcPushAndPop(8, 1024 * 1024, 1024 * 1024);
            passed &= RunConcurrentStackTest3_Clear(0);
            passed &= RunConcurrentStackTest3_Clear(16);
            passed &= RunConcurrentStackTest3_Clear(1024);
            passed &= RunConcurrentStackTest4_Enumerator(0);
            passed &= RunConcurrentStackTest4_Enumerator(16);
            passed &= RunConcurrentStackTest4_Enumerator(1024);
            passed &= RunConcurrentStackTest5_CtorAndCopyToAndToArray(0);
            passed &= RunConcurrentStackTest5_CtorAndCopyToAndToArray(16);
            passed &= RunConcurrentStackTest5_CtorAndCopyToAndToArray(1024);
            passed &= RunConcurrentStackTest6_PushRange(8, 10);
            passed &= RunConcurrentStackTest6_PushRange(16, 100);
            passed &= RunConcurrentStackTest6_PushRange(128, 100);
            passed &= RunConcurrentStackTest7_PopRange(8, 10);
            passed &= RunConcurrentStackTest7_PopRange(16, 100);
            passed &= RunConcurrentStackTest7_PopRange(128, 100);

            return passed;
        }

        // Just validates the stack correctly reports that it's empty.
        private static bool RunConcurrentStackTest0_Empty(int count)
        {
            TestHarness.TestLog("* RunConcurrentStackTest0_Empty()");

            ConcurrentStack<int> s = new ConcurrentStack<int>();
            for (int i = 0; i < count; i++)
                s.Push(i);

            bool isEmpty = s.IsEmpty;
            int sawCount = s.Count;

            TestHarness.TestLog("  > IsEmpty={0} (expect {1}), Count={2} (expect {3})", isEmpty, count == 0, sawCount, count);

            return isEmpty == (count == 0) && sawCount == count;
        }

        // Pushes and pops a certain number of times, and validates the resulting count.
        // These operations happen sequentially in a somewhat-interleaved fashion. We use
        // a BCL stack on the side to validate contents are correctly maintained.
        private static bool RunConcurrentStackTest1_PushAndPop(int pushes, int pops)
        {
            TestHarness.TestLog("* RunConcurrentStackTest1_PushAndPop(pushes={0}, pops={1})", pushes, pops);

            Random r = new Random(33);
            ConcurrentStack<int> s = new ConcurrentStack<int>();
            Stack<int> s2 = new Stack<int>();

            int donePushes = 0, donePops = 0;
            while (donePushes < pushes || donePops < pops)
            {
                for (int i = 0; i < r.Next(1, 10); i++)
                {
                    if (donePushes == pushes)
                        break;

                    int val = r.Next();
                    s.Push(val);
                    s2.Push(val);
                    donePushes++;

                    int sc = s.Count, s2c = s2.Count;
                    if (sc != s2c)
                    {
                        TestHarness.TestLog("  > test failed - stack counts differ: s = {0}, s2 = {1}", sc, s2c);
                        return false;
                    }
                }
                for (int i = 0; i < r.Next(1, 10); i++)
                {
                    if (donePops == pops)
                        break;
                    if ((donePushes - donePops) <= 0)
                        break;

                    int e0, e1, e2;
                    bool b0 = s.TryPeek(out e0);
                    bool b1 = s.TryPop(out e1);
                    e2 = s2.Pop();
                    donePops++;

                    if (!b0 || !b1)
                    {
                        TestHarness.TestLog("  > stack was unexpectedly empty, wanted #{0}  (peek={1}, pop={2})", e2, b0, b1);
                        return false;
                    }

                    if (e0 != e1 || e1 != e2)
                    {
                        TestHarness.TestLog("  > stack contents differ, got #{0} (peek)/{1} (pop) but expected #{2}", e0, e1, e2);
                        return false;
                    }

                    int sc = s.Count, s2c = s2.Count;
                    if (sc != s2c)
                    {
                        TestHarness.TestLog("  > test failed - stack counts differ: s = {0}, s2 = {1}", sc, s2c);
                        return false;
                    }
                }
            }

            int expected = pushes - pops;
            int endCount = s.Count;
            TestHarness.TestLog("  > expected = {0}, real = {1}: passed? {2}", expected, endCount, expected == endCount);

            return expected == endCount;
        }

        // Pushes and pops a certain number of times, and validates the resulting count.
        // These operations happen sconcurrently.
        private static bool RunConcurrentStackTest2_ConcPushAndPop(int threads, int pushes, int pops)
        {
            TestHarness.TestLog("* RunConcurrentStackTest2_ConcPushAndPop(threads={0}, pushes={1}, pops={2})", threads, pushes, pops);

            ConcurrentStack<int> s = new ConcurrentStack<int>();
            ManualResetEvent mre = new ManualResetEvent(false);
            Thread[] tt = new Thread[threads];

            // Create all threads.
            for (int k = 0; k < tt.Length; k++)
            {
                tt[k] = new Thread(delegate()
                {
                    Random r = new Random(33);
                    mre.WaitOne();

                    int donePushes = 0, donePops = 0;
                    while (donePushes < pushes || donePops < pops)
                    {
                        for (int i = 0; i < r.Next(1, 10); i++)
                        {
                            if (donePushes == pushes)
                                break;

                            s.Push(r.Next());
                            donePushes++;
                        }
                        for (int i = 0; i < r.Next(1, 10); i++)
                        {
                            if (donePops == pops)
                                break;
                            if ((donePushes - donePops) <= 0)
                                break;

                            int e;
                            if (s.TryPop(out e))
                                donePops++;
                        }
                    }
                });
                tt[k].Start();
            }

            // Kick 'em off and wait for them to finish.
            mre.Set();
            foreach (Thread t in tt)
                t.Join();

            // Validate the count.
            int expected = threads * (pushes - pops);
            int endCount = s.Count;
            TestHarness.TestLog("  > expected = {0}, real = {1}: passed? {2}", expected, endCount, expected == endCount);

            return expected == endCount;
        }

        // Just validates clearing the stack's contents.
        private static bool RunConcurrentStackTest3_Clear(int count)
        {
            TestHarness.TestLog("* RunConcurrentStackTest3_Clear()");

            ConcurrentStack<int> s = new ConcurrentStack<int>();
            for (int i = 0; i < count; i++)
                s.Push(i);

            s.Clear();

            bool isEmpty = s.IsEmpty;
            int sawCount = s.Count;

            TestHarness.TestLog("  > IsEmpty={0}, Count={1}", isEmpty, sawCount);

            return isEmpty && sawCount == 0;
        }

        // Just validates enumerating the stack.
        private static bool RunConcurrentStackTest4_Enumerator(int count)
        {
            TestHarness.TestLog("* RunConcurrentStackTest4_Enumerator()");

            ConcurrentStack<int> s = new ConcurrentStack<int>();
            for (int i = 0; i < count; i++)
                s.Push(i);

            // Test enumerator.
            int j = count - 1;
            foreach (int x in s)
            {
                // Clear the stack to ensure concurrent modifications are dealt w/.
                if (x == count - 1)
                {
                    int e;
                    while (s.TryPop(out e)) ;
                }
                if (x != j)
                {
                    TestHarness.TestLog("  > expected #{0}, but saw #{1}", j, x);
                    return false;
                }
                j--;
            }

            if (j > 0)
            {
                TestHarness.TestLog("  > did not enumerate all elements in the stack");
                return false;
            }

            return true;
        }

        // Instantiates the stack w/ the enumerator ctor and validates the resulting copyto & toarray.
        private static bool RunConcurrentStackTest5_CtorAndCopyToAndToArray(int count)
        {
            TestHarness.TestLog("* RunConcurrentStackTest5_CtorAndCopyToAndToArray()");

            int[] arr = new int[count];
            for (int i = 0; i < count; i++) arr[i] = i;
            ConcurrentStack<int> s = new ConcurrentStack<int>(arr);

            // try toarray.
            int[] sa1 = s.ToArray();
            if (sa1.Length != arr.Length)
            {
                TestHarness.TestLog("  > ToArray resulting array is diff length: got {0}, wanted {1}",
                    sa1.Length, arr.Length);
                return false;
            }
            for (int i = 0; i < sa1.Length; i++)
            {
                if (sa1[i] != arr[count - i - 1])
                {
                    TestHarness.TestLog("  > ToArray returned an array w/ diff contents: got {0}, wanted {1}",
                        sa1[i], arr[count - i - 1]);
                    return false;
                }
            }

            int[] sa2 = new int[count];
            s.CopyTo(sa2, 0);
            if (sa2.Length != arr.Length)
            {
                TestHarness.TestLog("  > CopyTo(int[]) resulting array is diff length: got {0}, wanted {1}",
                    sa2.Length, arr.Length);
                return false;
            }
            for (int i = 0; i < sa2.Length; i++)
            {
                if (sa2[i] != arr[count - i - 1])
                {
                    TestHarness.TestLog("  > CopyTo(int[]) returned an array w/ diff contents: got {0}, wanted {1}",
                        sa2[i], arr[count - i - 1]);
                    return false;
                }
            }

            object[] sa3 = new object[count]; // test array variance.
            ((System.Collections.ICollection)s).CopyTo(sa3, 0);
            if (sa3.Length != arr.Length)
            {
                TestHarness.TestLog("  > CopyTo(object[]) resulting array is diff length: got {0}, wanted {1}",
                    sa3.Length, arr.Length);
                return false;
            }
            for (int i = 0; i < sa3.Length; i++)
            {
                if ((int)sa3[i] != arr[count - i - 1])
                {
                    TestHarness.TestLog("  > CopyTo(object[]) returned an array w/ diff contents: got {0}, wanted {1}",
                        sa3[i], arr[count - i - 1]);
                    return false;
                }
            }

            return true;
        }

        //Tests COncurrentSTack.PushRange
        private static bool RunConcurrentStackTest6_PushRange(int NumOfThreads, int localArraySize)
        {
            TestHarness.TestLog("* RunConcurrentStackTest6_PushRange({0},{1})", NumOfThreads, localArraySize);
            ConcurrentStack<int> stack = new ConcurrentStack<int>();

            Thread[] threads = new Thread[NumOfThreads];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread((obj) =>
                {
                    int index = (int)obj;
                    int[] array = new int[localArraySize];
                    for (int j = 0; j < localArraySize; j++)
                    {
                        array[j] = index + j;
                    }

                    stack.PushRange(array);
                });

                threads[i].Start(i * localArraySize);

            }

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
            }

            //validation
            for (int i = 0; i < threads.Length; i++)
            {
                int lastItem = -1;
                for (int j = 0; j < localArraySize; j++)
                {
                    int currentItem = 0;
                    if (!stack.TryPop(out currentItem))
                    {
                        TestHarness.TestLog(" > Failed, TryPop returned false");
                        return false;
                    }
                    if (lastItem > -1 && lastItem - currentItem != 1)
                    {
                        TestHarness.TestLog(" > Failed {0} - {1} shouldn't be consecutive", lastItem, currentItem);
                    }

                    lastItem = currentItem;

                }
            }

            return true;
        }


        //Tests ConcurrentStack.PopRange by pushing consecutove numbers and run n threads each thread tries to pop m itmes
        // the popped m items should be consecutive
        private static bool RunConcurrentStackTest7_PopRange(int NumOfThreads, int elementsPerThread)
        {
            TestHarness.TestLog("* RunConcurrentStackTest7_PopRange({0},{1})", NumOfThreads, elementsPerThread);
            ConcurrentStack<int> stack = new ConcurrentStack<int>(Enumerable.Range(1, NumOfThreads * elementsPerThread));


            Thread[] threads = new Thread[NumOfThreads];

            int[] array = new int[threads.Length * elementsPerThread];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread((obj) =>
                {
                    int index = (int)obj;

                    int res;
                    if ((res = stack.TryPopRange(array, index, elementsPerThread)) != elementsPerThread)
                    {
                        TestHarness.TestLog(" > Failed TryPopRange didn't return the full range ");
                    }

                });

                threads[i].Start(i * elementsPerThread);

            }

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i].Join();
            }

            // validation
            for (int i = 0; i < NumOfThreads; i++)
            {
                for (int j = 1; j < elementsPerThread; j++)
                {
                    int currentIndex = i * elementsPerThread + j;
                    if (array[currentIndex - 1] - array[currentIndex] != 1)
                    {
                        TestHarness.TestLog(" > Failed {0} - {1} shouldn't be consecutive", array[currentIndex - 1], array[currentIndex]);
                        return false;
                    }
                }
            }

            return true;

        }

        //
        // ConcurrentQueue<T>
        //

        internal static bool RunConcurrentQueueTests()
        {
            bool passed = true;

            passed &= RunBugFix570046();
            passed &= RunConcurrentQueueTest0_Empty(0);
            passed &= RunConcurrentQueueTest0_Empty(16);
            passed &= RunConcurrentQueueTest1_EnqAndDeq(0, 0);
            passed &= RunConcurrentQueueTest1_EnqAndDeq(5, 0);
            passed &= RunConcurrentQueueTest1_EnqAndDeq(5, 2);
            passed &= RunConcurrentQueueTest1_EnqAndDeq(5, 5);
            passed &= RunConcurrentQueueTest1_EnqAndDeq(1024, 512);
            passed &= RunConcurrentQueueTest1_EnqAndDeq(1024, 1024);
            passed &= RunConcurrentQueueTest1b_TryPeek(512);
            passed &= RunConcurrentQueueTest2_ConcEnqAndDeq(8, 1024 * 1024, 0);
            passed &= RunConcurrentQueueTest2_ConcEnqAndDeq(8, 1024 * 1024, 1024 * 512);
            passed &= RunConcurrentQueueTest2_ConcEnqAndDeq(8, 1024 * 1024, 1024 * 1024);
            passed &= RunConcurrentQueueTest4_Enumerator(0);
            passed &= RunConcurrentQueueTest4_Enumerator(16);
            passed &= RunConcurrentQueueTest4_Enumerator(1024);
            passed &= RunConcurrentQueueTest5_CtorAndCopyToAndToArray(0);
            passed &= RunConcurrentQueueTest5_CtorAndCopyToAndToArray(16);
            passed &= RunConcurrentQueueTest5_CtorAndCopyToAndToArray(1024);

            return passed;
        }

        /// <summary>
        /// Bugfix 570046: Enumerating a ConcurrentQueue while simultaneously enqueueing and dequeueing somteimes returns a null value
        /// </summary>
        /// <returns></returns>
        /// <remarks>to stress test this bug fix: wrap task t1 and t2 with while (true), but DO NOT CHECKIN!
        /// </remarks>
        private static bool RunBugFix570046()
        {
            bool passed = true;
            TestHarness.TestLog("* RunBugFix570046:  Enumerating a ConcurrentQueue while simultaneously enqueueing and dequeueing somteimes returns a null value");
            var q = new ConcurrentQueue<int?>();

            var t1 = Task.Factory.StartNew(
             () =>
             {
                 for (int i = 0; i < 1000000; i++)
                 {
                     q.Enqueue(i);
                     int? o;
                     if (!q.TryDequeue(out o))
                     {
                         TestHarness.TestLog("Failed! TryDequeue should never return false in this test");
                         passed = false;
                     }
                 }
             });

            var t2 = Task.Factory.StartNew(
             () =>
             {
                 foreach (var item in q)
                 {
                     if (item == null)
                     {
                         TestHarness.TestLog("Failed! Enumerating should never return null value");
                         passed = false;
                     }
                 }

             });

            t2.Wait();
            return passed;
        }


        // Just validates the queue correctly reports that it's empty.
        private static bool RunConcurrentQueueTest0_Empty(int count)
        {
            TestHarness.TestLog("* RunConcurrentQueueTest0_Empty()");

            ConcurrentQueue<int> q = new ConcurrentQueue<int>();
            for (int i = 0; i < count; i++)
                q.Enqueue(i);

            bool isEmpty = q.IsEmpty;
            int sawCount = q.Count;

            TestHarness.TestLog("  > IsEmpty={0} (expect {1}), Count={2} (expect {3})", isEmpty, count == 0, sawCount, count);

            return isEmpty == (count == 0) && sawCount == count;
        }

        // Pushes and pops a certain number of times, and validates the resulting count.
        // These operations happen sequentially in a somewhat-interleaved fashion. We use
        // a BCL queue on the side to validate contents are correctly maintained.
        private static bool RunConcurrentQueueTest1_EnqAndDeq(int pushes, int pops)
        {
            TestHarness.TestLog("* RunConcurrentQueueTest1_EnqAndDeq(pushes={0}, pops={1})", pushes, pops);

            Random r = new Random(33);
            ConcurrentQueue<int> s = new ConcurrentQueue<int>();
            Queue<int> s2 = new Queue<int>();

            int donePushes = 0, donePops = 0;
            while (donePushes < pushes || donePops < pops)
            {
                for (int i = 0; i < r.Next(1, 10); i++)
                {
                    if (donePushes == pushes)
                        break;

                    int val = r.Next();
                    s.Enqueue(val);
                    s2.Enqueue(val);
                    donePushes++;

                    int sc = s.Count, s2c = s2.Count;
                    if (sc != s2c)
                    {
                        TestHarness.TestLog("  > test failed - stack counts differ: s = {0}, s2 = {1}", sc, s2c);
                        return false;
                    }
                }
                for (int i = 0; i < r.Next(1, 10); i++)
                {
                    if (donePops == pops)
                        break;
                    if ((donePushes - donePops) <= 0)
                        break;

                    int e1, e2;
                    bool b1 = s.TryDequeue(out e1);
                    e2 = s2.Dequeue();
                    donePops++;

                    if (!b1)
                    {
                        TestHarness.TestLog("  > queue was unexpectedly empty, wanted #{0}  (pop={1})", e2, b1);
                        return false;
                    }

                    if (e1 != e2)
                    {
                        TestHarness.TestLog("  > queue contents differ, got #{0} but expected #{1}", e1, e2);
                        return false;
                    }

                    int sc = s.Count, s2c = s2.Count;
                    if (sc != s2c)
                    {
                        TestHarness.TestLog("  > test failed - stack counts differ: s = {0}, s2 = {1}", sc, s2c);
                        return false;
                    }
                }
            }

            int expected = pushes - pops;
            int endCount = s.Count;
            TestHarness.TestLog("  > expected = {0}, real = {1}: passed? {2}", expected, endCount, expected == endCount);

            return expected == endCount;
        }

        // Just pushes and pops, ensuring trypeek is always accurate.
        private static bool RunConcurrentQueueTest1b_TryPeek(int pushes)
        {
            TestHarness.TestLog("* RunConcurrentQueueTest1b_TryPeek(pushes={0})", pushes);

            Random r = new Random(33);
            ConcurrentQueue<int> s = new ConcurrentQueue<int>();
            int[] arr = new int[pushes];
            for (int i = 0; i < pushes; i++)
                arr[i] = r.Next();

            // should be empty.
            int y;
            if (s.TryPeek(out y))
            {
                TestHarness.TestLog("    > queue should be empty!  TryPeek returned true {0}", y);
                return false;
            }

            for (int i = 0; i < arr.Length; i++)
            {
                s.Enqueue(arr[i]);

                // Validate the front is still returned.
                int x;
                for (int j = 0; j < 5; j++)
                {
                    if (!s.TryPeek(out x) || x != arr[0])
                    {
                        TestHarness.TestLog("    > peek after enqueue didn't return expected element: {0} instead of {1}",
                            x, arr[0]);
                    }
                }
            }

            for (int i = 0; i < arr.Length; i++)
            {
                // Validate the element about to be returned is correct.
                int x;
                for (int j = 0; j < 5; j++)
                {
                    if (!s.TryPeek(out x) || x != arr[i])
                    {
                        TestHarness.TestLog("    > peek after enqueue didn't return expected element: {0} instead of {1}",
                            x, arr[i]);
                    }
                }

                s.TryDequeue(out x);
            }

            // should be empty.
            int z;
            if (s.TryPeek(out z))
            {
                TestHarness.TestLog("    > queue should be empty!  TryPeek returned true {0}", y);
                return false;
            }

            return true;
        }

        // Pushes and pops a certain number of times, and validates the resulting count.
        // These operations happen concurrently.
        private static bool RunConcurrentQueueTest2_ConcEnqAndDeq(int threads, int pushes, int pops)
        {
            TestHarness.TestLog("* RunConcurrentQueueTest2_ConcEnqAndDeq(threads={0}, pushes={1}, pops={2})", threads, pushes, pops);

            ConcurrentQueue<int> s = new ConcurrentQueue<int>();
            ManualResetEvent mre = new ManualResetEvent(false);
            Thread[] tt = new Thread[threads];

            // Create all threads.
            for (int k = 0; k < tt.Length; k++)
            {
                tt[k] = new Thread(delegate()
                {
                    Random r = new Random(33);
                    mre.WaitOne();

                    int donePushes = 0, donePops = 0;
                    while (donePushes < pushes || donePops < pops)
                    {
                        for (int i = 0; i < r.Next(1, 10); i++)
                        {
                            if (donePushes == pushes)
                                break;

                            s.Enqueue(r.Next());
                            donePushes++;
                        }
                        for (int i = 0; i < r.Next(1, 10); i++)
                        {
                            if (donePops == pops)
                                break;
                            if ((donePushes - donePops) <= 0)
                                break;

                            int e;
                            if (s.TryDequeue(out e))
                                donePops++;
                        }
                    }
                });
                tt[k].Start();
            }

            // Kick 'em off and wait for them to finish.
            mre.Set();
            foreach (Thread t in tt)
                t.Join();

            // Validate the count.
            int expected = threads * (pushes - pops);
            int endCount = s.Count;
            TestHarness.TestLog("  > expected = {0}, real = {1}: passed? {2}", expected, endCount, expected == endCount);

            return expected == endCount;
        }

        // Just validates enumerating the stack.
        private static bool RunConcurrentQueueTest4_Enumerator(int count)
        {
            TestHarness.TestLog("* RunConcurrentQueueTest4_Enumerator()");

            ConcurrentQueue<int> s = new ConcurrentQueue<int>();
            for (int i = 0; i < count; i++)
                s.Enqueue(i);

            // Test enumerator.
            int j = 0;
            foreach (int x in s)
            {
                // Clear the stack to ensure concurrent modifications are dealt w/.
                if (x == count - 1)
                {
                    int e;
                    while (s.TryDequeue(out e)) ;
                }
                if (x != j)
                {
                    TestHarness.TestLog("  > expected #{0}, but saw #{1}", j, x);
                    return false;
                }
                j++;
            }

            if (j != count)
            {
                TestHarness.TestLog("  > did not enumerate all elements in the stack");
                return false;
            }

            return true;
        }

        // Instantiates the queue w/ the enumerator ctor and validates the resulting copyto & toarray.
        private static bool RunConcurrentQueueTest5_CtorAndCopyToAndToArray(int count)
        {
            TestHarness.TestLog("* RunConcurrentQueueTest5_CtorAndCopyToAndToArray()");

            int[] arr = new int[count];
            for (int i = 0; i < count; i++) arr[i] = i;
            ConcurrentQueue<int> s = new ConcurrentQueue<int>(arr);

            // try toarray.
            int[] sa1 = s.ToArray();
            if (sa1.Length != arr.Length)
            {
                TestHarness.TestLog("  > ToArray resulting array is diff length: got {0}, wanted {1}",
                    sa1.Length, arr.Length);
                return false;
            }
            for (int i = 0; i < sa1.Length; i++)
            {
                if (sa1[i] != arr[i])
                {
                    TestHarness.TestLog("  > ToArray returned an array w/ diff contents: got {0}, wanted {1}",
                        sa1[i], arr[i]);
                    return false;
                }
            }

            int[] sa2 = new int[count];
            s.CopyTo(sa2, 0);
            if (sa2.Length != arr.Length)
            {
                TestHarness.TestLog("  > CopyTo(int[]) resulting array is diff length: got {0}, wanted {1}",
                    sa2.Length, arr.Length);
                return false;
            }
            for (int i = 0; i < sa2.Length; i++)
            {
                if (sa2[i] != arr[i])
                {
                    TestHarness.TestLog("  > CopyTo(int[]) returned an array w/ diff contents: got {0}, wanted {1}",
                        sa2[i], arr[i]);
                    return false;
                }
            }

            object[] sa3 = new object[count]; // test array variance.
            ((System.Collections.ICollection)s).CopyTo(sa3, 0);
            if (sa3.Length != arr.Length)
            {
                TestHarness.TestLog("  > CopyTo(object[]) resulting array is diff length: got {0}, wanted {1}",
                    sa3.Length, arr.Length);
                return false;
            }
            for (int i = 0; i < sa3.Length; i++)
            {
                if ((int)sa3[i] != arr[i])
                {
                    TestHarness.TestLog("  > CopyTo(object[]) returned an array w/ diff contents: got {0}, wanted {1}",
                        sa3[i], arr[i]);
                    return false;
                }
            }

            return true;
        }

        //
        // ManualResetEventSlim
        //

        internal static bool RunManualResetEventSlimTests()
        {
            bool passed = true;


            passed &= RunManualResetEventSlimTest0_StateTrans(false);
            passed &= RunManualResetEventSlimTest0_StateTrans(true);
            passed &= RunManualResetEventSlimTest1_SimpleWait();
            passed &= RunManualResetEventSlimTest2_TimeoutWait();
            passed &= RunManualResetEventSlimTest3_ConstructorTests();
            passed &= RunManualResetEventSlimTest4_CombinedStateTests();
            passed &= RunManualResetEventSlimTest5_Dispose();

            return passed;
        }

        // Validates init, set, reset state transitions.
        private static bool RunManualResetEventSlimTest0_StateTrans(bool init)
        {
            TestHarness.TestLog("* RunManualResetEventSlimTest0_StateTrans(init={0})", init);

            ManualResetEventSlim ev = new ManualResetEventSlim(init);
            if (ev.IsSet != init)
            {
                TestHarness.TestLog("  > expected IsSet=={0}, but it's {1}", init, ev.IsSet);
                return false;
            }

            for (int i = 0; i < 50; i++)
            {
                ev.Set();
                if (!ev.IsSet)
                {
                    TestHarness.TestLog("  > expected IsSet, but it's false");
                    return false;
                }

                ev.Reset();
                if (ev.IsSet)
                {
                    TestHarness.TestLog("  > expected !IsSet, but it's true");
                    return false;
                }
            }

            return true;
        }

        // Uses 3 events to coordinate between two threads. Very little validation.
        private static bool RunManualResetEventSlimTest1_SimpleWait()
        {
            TestHarness.TestLog("* RunManualResetEventSlimTest1_SimpleWait()");

            ManualResetEventSlim ev1 = new ManualResetEventSlim(false);
            ManualResetEventSlim ev2 = new ManualResetEventSlim(false);
            ManualResetEventSlim ev3 = new ManualResetEventSlim(false);

            ThreadPool.QueueUserWorkItem(delegate
            {
                ev2.Set();
                ev1.Wait();
                ev3.Set();
            });

            ev2.Wait();
            Thread.Sleep(100);
            ev1.Set();
            ev3.Wait();

            return true;
        }

        // Tests timeout on an event that is never set.
        private static bool RunManualResetEventSlimTest2_TimeoutWait()
        {
            TestHarness.TestLog("* RunManualResetEventSlimTest2_TimeoutWait()");

            ManualResetEventSlim ev = new ManualResetEventSlim(false);

            if (ev.Wait(0))
            {
                TestHarness.TestLog("  > ev.Wait(0) returned true -- event isn't set  ({0})", ev.IsSet);
                return false;
            }

            if (ev.Wait(100))
            {
                TestHarness.TestLog("  > ev.Wait(100) returned true -- event isn't set  ({0})", ev.IsSet);
                return false;
            }

            if (ev.Wait(TimeSpan.FromMilliseconds(100)))
            {
                TestHarness.TestLog("  > ev.Wait(0) returned true -- event isn't set  ({0})", ev.IsSet);
                return false;
            }

            ev.Dispose();

            return true;
        }

        // Tests timeout on an event that is never set.
        private static bool RunManualResetEventSlimTest3_ConstructorTests()
        {
            bool passed = true;
            TestHarness.TestLog("* RunManualResetEventSlimTest3_ConstructorTests()");
            passed &= TestHarnessAssert.EnsureExceptionThrown(
                          () => new ManualResetEventSlim(false, 2048), //max value is 2047.
                          typeof(ArgumentOutOfRangeException),
                          "The max value for spin count is 2047. An ArgumentException should be thrown.");

            passed &= TestHarnessAssert.EnsureExceptionThrown(
                () => new ManualResetEventSlim(false, -1),
                typeof(ArgumentOutOfRangeException),
                "The min value for spin count is 0. An ArgumentException should be thrown.");

            return passed;
        }

        // Tests that the shared state variable seems to be working correctly.
        private static bool RunManualResetEventSlimTest4_CombinedStateTests()
        {
            bool passed = true;
            TestHarness.TestLog("* RunManualResetEventSlimTest4_CombinedStateTests()");

            ManualResetEventSlim mres = new ManualResetEventSlim(false, 100);
            int expectedCount = Environment.ProcessorCount == 1 ? 1 : 100;
            passed &= TestHarnessAssert.AreEqual(expectedCount, mres.SpinCount, "Spin count did not write/read correctly, expected " + expectedCount + ", actual " + mres.SpinCount);
            passed &= TestHarnessAssert.IsFalse(mres.IsSet, "Set did not read correctly.");
            mres.Set();
            passed &= TestHarnessAssert.IsTrue(mres.IsSet, "Set did not write/read correctly.");

            return passed;
        }

        private static bool RunManualResetEventSlimTest5_Dispose()
        {
            bool passed = true;
            TestHarness.TestLog("* RunManualResetEventSlimTest5_Dispose()");
            ManualResetEventSlim mres = new ManualResetEventSlim(false);
            mres.Dispose();

            passed &= TestHarnessAssert.EnsureExceptionThrown(
               () => mres.Reset(),
               typeof(ObjectDisposedException),
               "The object has been disposed, should throw ObjectDisposedException.");

            passed &= TestHarnessAssert.EnsureExceptionThrown(
              () => mres.Wait(0),
              typeof(ObjectDisposedException),
              "The object has been disposed, should throw ObjectDisposedException.");


            passed &= TestHarnessAssert.EnsureExceptionThrown(
              () => 
              {
                  WaitHandle handle = mres.WaitHandle;
              },
              typeof(ObjectDisposedException),
              "The object has been disposed, should throw ObjectDisposedException.");

            mres = new ManualResetEventSlim(false); ;
            ManualResetEvent mre = (ManualResetEvent)mres.WaitHandle;
            mres.Dispose();

            passed &= TestHarnessAssert.EnsureExceptionThrown(
             () => mre.WaitOne(0, false),
             typeof(ObjectDisposedException),
             "The underlying event object has been disposed, should throw ObjectDisposedException.");

            return passed;
        }

        //
        // CountdownEvent
        //

        internal static bool RunCountdownEventTests()
        {
            bool passed = true;

            passed &= RunCountdownEventTest0_StateTrans(0, 0, false);
            passed &= RunCountdownEventTest0_StateTrans(1, 0, false);
            passed &= RunCountdownEventTest0_StateTrans(128, 0, false);
            passed &= RunCountdownEventTest0_StateTrans(1024 * 1024, 0, false);
            passed &= RunCountdownEventTest0_StateTrans(1, 1024, false);
            passed &= RunCountdownEventTest0_StateTrans(128, 1024, false);
            passed &= RunCountdownEventTest0_StateTrans(1024 * 1024, 1024, false);
            passed &= RunCountdownEventTest0_StateTrans(1, 0, true);
            passed &= RunCountdownEventTest0_StateTrans(128, 0, true);
            passed &= RunCountdownEventTest0_StateTrans(1024 * 1024, 0, true);
            passed &= RunCountdownEventTest0_StateTrans(1, 1024, true);
            passed &= RunCountdownEventTest0_StateTrans(128, 1024, true);
            passed &= RunCountdownEventTest0_StateTrans(1024 * 1024, 1024, true);
            passed &= RunCountdownEventTest1_SimpleTimeout(0);
            passed &= RunCountdownEventTest1_SimpleTimeout(100);

            return passed;
        }

        // Validates init, set, reset state transitions.
        private static bool RunCountdownEventTest0_StateTrans(int initCount, int increms, bool takeAllAtOnce)
        {
            TestHarness.TestLog("* RunCountdownEventTest0_StateTrans(initCount={0}, increms={1}, takeAllAtOnce={2})", initCount, increms, takeAllAtOnce);

            CountdownEvent ev = new CountdownEvent(initCount);

            // Check initial count.
            if (ev.InitialCount != initCount)
            {
                TestHarness.TestLog("  > error: initial count wrong, saw {0} expected {1}", ev.InitialCount, initCount);
                return false;
            }

            // Increment (optionally).
            for (int i = 0; i < increms; i++)
            {
                ev.AddCount();
                if (ev.CurrentCount != initCount + i + 1)
                {
                    TestHarness.TestLog("  > error: after incrementing, count is wrong, saw {0}, expect {1}", ev.CurrentCount, initCount + i + 1);
                    return false;
                }
            }

            // Decrement until it hits 0.
            if (takeAllAtOnce)
            {
                ev.Signal(initCount + increms);
            }
            else
            {
                for (int i = 0; i < initCount + increms; i++)
                {
                    if (ev.IsSet)
                    {
                        TestHarness.TestLog("  > error: latch is set after {0} signals", i);
                        return false;
                    }
                    ev.Signal();
                }
            }

            // Check the status.
            if (!ev.IsSet)
            {
                TestHarness.TestLog("  > error: latch was not set after all signals received");
                return false;
            }
            if (ev.CurrentCount != 0)
            {
                TestHarness.TestLog("  > error: latch count wasn't 0 after all signals received");
                return false;
            }

            // Now reset the event and check its count.
            ev.Reset();
            if (ev.CurrentCount != ev.InitialCount)
            {
                TestHarness.TestLog("  > error: latch count wasn't correctly reset");
                return false;
            }

            return true;
        }

        // Tries some simple timeout cases.
        private static bool RunCountdownEventTest1_SimpleTimeout(int ms)
        {
            TestHarness.TestLog("* RunCountdownEventTest1_SimpleTimeout(ms={0})", ms);

            // Wait on the event.
            CountdownEvent ev = new CountdownEvent(999);
            if (ev.Wait(ms))
            {
                TestHarness.TestLog("  > error: wait returned true, yet it was supposed to timeout");
                return false;
            }

            if (ev.IsSet)
            {
                TestHarness.TestLog("  > error: event says it was set...  shouldn't be");
                return false;
            }

            if (ev.WaitHandle.WaitOne(ms, false))
            {
                TestHarness.TestLog("  > error: WaitHandle.Wait returned true, yet it was supposed to timeout");
                return false;
            }

            return true;
        }



        //
        // AggregateException
        //

        internal static bool RunAggregateExceptionTests()
        {
            bool passed = true;

            passed &= RunAggregateException_Flatten();

            return passed;
        }

        // Validates that flattening (incl recursive) works.
        private static bool RunAggregateException_Flatten()
        {
            TestHarness.TestLog("* RunAggregateException_Flatten()");

            Exception exceptionA = new Exception("A");
            Exception exceptionB = new Exception("B");
            Exception exceptionC = new Exception("C");

            AggregateException aggExceptionBase = new AggregateException(exceptionA, exceptionB, exceptionC);

            // Verify flattening one with another.
            TestHarness.TestLog("  > Flattening (no recursion)...");

            AggregateException flattened1 = aggExceptionBase.Flatten();
            Exception[] expected1 = new Exception[] {
                exceptionA, exceptionB, exceptionC
            };

            if (expected1.Length != flattened1.InnerExceptions.Count)
            {
                TestHarness.TestLog("  > error: expected count {0} differs from actual {1}",
                    expected1.Length, flattened1.InnerExceptions.Count);
                return false;
            }

            for (int i = 0; i < flattened1.InnerExceptions.Count; i++)
            {
                if (expected1[i] != flattened1.InnerExceptions[i])
                {
                    TestHarness.TestLog("  > error: inner exception #{0} isn't right:", i);
                    TestHarness.TestLog("        expected: {0}", expected1[i]);
                    TestHarness.TestLog("        found   : {0}", flattened1.InnerExceptions[i]);
                    return false;
                }
            }

            // Verify flattening one with another, accounting for recursion.
            TestHarness.TestLog("  > Flattening (with recursion)...");

            AggregateException aggExceptionRecurse = new AggregateException(aggExceptionBase, aggExceptionBase);
            AggregateException flattened2 = aggExceptionRecurse.Flatten();
            Exception[] expected2 = new Exception[] {
                exceptionA, exceptionB, exceptionC, exceptionA, exceptionB, exceptionC,
            };

            if (expected2.Length != flattened2.InnerExceptions.Count)
            {
                TestHarness.TestLog("  > error: expected count {0} differs from actual {1}",
                    expected2.Length, flattened2.InnerExceptions.Count);
                return false;
            }

            for (int i = 0; i < flattened2.InnerExceptions.Count; i++)
            {
                if (expected2[i] != flattened2.InnerExceptions[i])
                {
                    TestHarness.TestLog("  > error: inner exception #{0} isn't right:", i);
                    TestHarness.TestLog("        expected: {0}", expected2[i]);
                    TestHarness.TestLog("        found   : {0}", flattened2.InnerExceptions[i]);
                    return false;
                }
            }

            return true;
        }


    }

}