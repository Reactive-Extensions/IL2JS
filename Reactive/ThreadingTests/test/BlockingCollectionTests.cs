using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Concurrent;

namespace plinq_devtests
{
    /// <summary>The class that contains the unit tests of the BlockingCollection.</summary>
    internal static class BlockingCollectionTests
    {
        internal static bool RunBlockingCollectionTests()
        {
            bool passed = true;

            //bugfix 543683 test is an infinite loop
            //we comment it out for checked in version, to not block DevUnitTests
            //passed &= RunBlockingCollectionTest_BugFix543683();

            passed &= RunBlockingCollectionTest_BugFix544259();
            passed &= RunBlockingCollectionTest_Bug626345();

            passed &= RunBlockingCollectionTest0_Construction(-1);
            passed &= RunBlockingCollectionTest0_Construction(10);
            passed &= RunBlockingCollectionTest1_AddTake(1, 1, -1);
            passed &= RunBlockingCollectionTest1_AddTake(10, 9, -1);
            passed &= RunBlockingCollectionTest1_AddTake(10, 10, 10);
            passed &= RunBlockingCollectionTest1_AddTake(10, 10, 9);
            passed &= RunBlockingCollectionTest2_ConcurrentAdd(2, 10240);
            passed &= RunBlockingCollectionTest2_ConcurrentAdd(16, 1024);
            passed &= RunBlockingCollectionTest3_ConcurrentAddTake(16, 1024);
            passed &= RunBlockingCollectionTest4_Dispose();
            passed &= RunBlockingCollectionTest5_GetEnumerator();
            passed &= RunBlockingCollectionTest6_GetConsumingEnumerable();
            passed &= RunBlockingCollectionTest7_CompleteAdding();
            passed &= RunBlockingCollectionTest7_ConcurrentAdd_CompleteAdding();
            passed &= RunBlockingCollectionTest8_ToArray();
            passed &= RunBlockingCollectionTest9_CopyTo(0);
            passed &= RunBlockingCollectionTest9_CopyTo(8);
            passed &= RunBlockingCollectionTest10_Count();
            passed &= RunBlockingCollectionTest11_BoundedCapacity();
            passed &= RunBlockingCollectionTest12_IsCompleted_AddingIsCompleted();
            passed &= RunBlockingCollectionTest13_IsSynchronized_SyncRoot();
            passed &= RunBlockingCollectionTest14_AddAnyTakeAny(1, 1, 16, 0, -1);
            passed &= RunBlockingCollectionTest14_AddAnyTakeAny(10, 9, 16, 15, -1);
            passed &= RunBlockingCollectionTest14_AddAnyTakeAny(10, 10, 16, 14, 10);
            passed &= RunBlockingCollectionTest14_AddAnyTakeAny(10, 10, 16, 1, 9);
            passed &= RunBlockingCollectionTest15_ConcurrentAddAnyTakeAny(8, 10240, 2, 64);
            passed &= RunBlockingCollectionTest16_Ctor();
            passed &= RunBlockingCollectionTest17_AddExceptions();
            passed &= RunBlockingCollectionTest18_TakeExceptions();
            passed &= RunBlockingCollectionTest19_AddAnyExceptions();
            passed &= RunBlockingCollectionTest20_TakeAnyExceptions();
            passed &= RunBlockingCollectionTest21_CopyToExceptions();
            return passed;
        }

        


        private static bool RunBlockingCollectionTest_BugFix543683()
        {
            TestHarness.TestLog("* RunBlockingCollectionTest_BugFix543683: THIS TEST IS AN INFINITE LOOP, comment it out before running DevUnitTest");
            for (int i = 0; true; i++)
            {
                TestHarness.TestLog("RunBlockingCollectionTest_BugFix543683: THIS TEST IS AN INFINITE LOOP, comment it out before running DevUnitTest");
                Console.WriteLine("Run {0}", i);

                BlockingCollection<int> bc = new BlockingCollection<int>();
                Action consumerAction = delegate
                {
                    int myCount = 0;
                    foreach (int c in bc.GetConsumingEnumerable())
                    {
                        myCount += 1;
                    }
                };

                // Launch the consumers
                Console.WriteLine("Launching consumers...");

                Task[] consumers = new Task[4];

                for (int taskNum = 0; taskNum < 4; taskNum++)
                {
                    consumers[taskNum] = Task.Factory.StartNew(consumerAction);
                }

                // Now start producing 
                Console.WriteLine("Producing...");
                for (int j = 0; j < 1000; j++) bc.Add(j);


                // Release the consumers

                Console.WriteLine("Terminating blocking collection...");
                bc.CompleteAdding();

                // Wait for consumers to complete

                Console.WriteLine("Waiting on consumers...");
                Task.WaitAll(consumers);
            }
        }

        /// <summary>
        /// Bug description: BlockingCollection throws  InvalidOperationException when calling CompleteAdding even after adding and taking all elements
        /// </summary>
        /// <returns></returns>
        private static bool RunBlockingCollectionTest_BugFix544259()
        {
            TestHarness.TestLog("* RunBlockingCollectionTest_BugFix544259");
            int count = 8;
            CountdownEvent cde = new CountdownEvent(count);
            BlockingCollection<object> bc = new BlockingCollection<object>();

            //creates 8 consumers, each calling take to block itself
            for (int i = 0; i < count; i++)
            {
                int myi = i;
                Thread t = new Thread(() =>
                {
                    bc.Take();
                    cde.Signal();
                });
                t.Start();
            }
            //create 8 producers, each calling add to unblock a consumer
            for (int i = 0; i < count; i++)
            {
                int myi = i;
                Thread t = new Thread(() =>
                {
                    bc.Add(new object());
                });
                t.Start();
            }

            //CountdownEvent waits till all consumers are unblocked
            cde.Wait();
            bc.CompleteAdding();
            return true;
        }

        // as part of the bugfix for 626345, this code was suffering occassional ObjectDisposedExceptions due
        // to the expected race between cts.Dispose and the cts.Cancel coming from the linking sources.
        // ML: update - since the change to wait as part of CTS.Dispose, the ODE no longer occurs
        // but we keep the test as a good example of how cleanup of linkedCTS must be carefully handled to prevent 
        // users of the source CTS mistakenly calling methods on disposed targets.
        private static bool RunBlockingCollectionTest_Bug626345()
        {
            TestHarness.TestLog("* RunBlockingCollectionTest_Bug626345.");
            const int noOfProducers = 1;
            const int noOfConsumers = 50;
            const int noOfItemsToProduce = 2;
            TestHarness.TestLog("Producer: {0}, Consumer: {1}, Items: {2}", noOfProducers, noOfConsumers, noOfItemsToProduce);

            BlockingCollection<long> m_BlockingQueueUnderTest = new BlockingCollection<long>(new ConcurrentQueue<long>());

            Thread[] producers = new Thread[noOfProducers];
            for (int prodIndex = 0; prodIndex < noOfProducers; prodIndex++)
            {
                producers[prodIndex] = new Thread(() =>
                {
                    for (int dummyItem = 0;
                         dummyItem < noOfItemsToProduce;
                         dummyItem++)
                    {
                        for (int j = 0; j < 5; j++)
                        {
                            Math.Min(0, j);
                        }
                        m_BlockingQueueUnderTest.Add(dummyItem);

                    }
                }
                    );
                producers[prodIndex].Start();
            }

            //consumers
            Thread[] consumers = new Thread[noOfConsumers];
            for (int consumerIndex = 0; consumerIndex < noOfConsumers; consumerIndex++)
            {
                consumers[consumerIndex] = new Thread(() =>
                {
                    while (!m_BlockingQueueUnderTest.IsCompleted)
                    {
                        long item;
                        if (m_BlockingQueueUnderTest.TryTake(out item, 1))
                        {
                            for (int j = 0; j < 5; j++)
                            {
                                Math.Min(0, j);
                            }
                        }
                        Thread.Sleep(1);
                    }
                }
                    );
                consumers[consumerIndex].Start();
            }

            //Wait for the producers to finish.
            //It is possible for some of the tasks in the array to be null, because the
            //test was cancelled before all the tasks were creates, so we filter out the null values
            foreach (Thread t in producers)
            {
                if (t != null)
                {
                    t.Join();
                }
            }

            m_BlockingQueueUnderTest.CompleteAdding(); //signal all producers are done adding items

            //Wait for the consumers to finish.
            foreach (Thread t in consumers)
            {
                if (t != null)
                {
                    t.Join();
                }
            }

            return true; // success is not suffering exceptions.
        }

        /// <summary>
        /// Tests the default BlockingCollection constructor which initializes a BlockingQueue
        /// </summary>
        /// <param name="boundedCapacity"></param>
        /// <returns></returns>
        private static bool RunBlockingCollectionTest0_Construction(int boundedCapacity)
        {
            TestHarness.TestLog("* RunBlockingCollectionTest0_Constructor(boundedCapacity={0}"
                , boundedCapacity);
            BlockingCollection<int> blockingQueue;
            if (boundedCapacity != -1)
            {
                blockingQueue = new BlockingCollection<int>(boundedCapacity);
            }
            else
            {
                blockingQueue = new BlockingCollection<int>();
            }

            if (blockingQueue.BoundedCapacity != boundedCapacity)
            {
                TestHarness.TestLog(" > test failed - Bounded cpacitities do not match");
                return false;
            }

            // Test for queue properties, Taked item should be i nthe same order of the insertion
            int count = boundedCapacity != -1 ? boundedCapacity : 10;

            for (int i = 0; i < count; i++)
            {
                blockingQueue.Add(i);
            }
            for (int i = 0; i < count; i++)
            {
                if (blockingQueue.Take() != i)
                {
                    TestHarness.TestLog(" > test failed - the default underlying collection is not a queue");
                    return false;
                }
            }

            return true;

        }
        /// <summary>Adds "numOfAdds" elements to the BlockingCollection and then Takes "numOfTakes" elements and 
        /// checks that the count is as expected, the elements removed matched those added and verifies the return 
        /// values of TryAdd() and TryTake().</summary>
        /// <param name="numOfAdds">The number of elements to add to the BlockingCollection.</param>
        /// <param name="numOfTakes">The number of elements to Take from the BlockingCollection.</param>
        /// <param name="boundedCapacity">The bounded capacity of the BlockingCollection, -1 is unbounded.</param>
        /// <returns>True if test succeeded, false otherwise.</returns>
        private static bool RunBlockingCollectionTest1_AddTake(int numOfAdds, int numOfTakes, int boundedCapacity)
        {
            TestHarness.TestLog("* RunBlockingCollectionTest1_AddTake(numOfAdds={0}, numOfTakes={1}, boundedCapacity={2})", numOfAdds, numOfTakes, boundedCapacity);
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>(boundedCapacity);
            return AddAnyTakeAny(numOfAdds, numOfTakes, boundedCapacity, blockingCollection, null, -1);
        }

        /// <summary> Launch some threads performing Add operation and makes sure that all items added are 
        /// present in the collection.</summary>
        /// <param name="numOfThreads">Number of producer threads.</param>
        /// <param name="numOfElementsPerThread">Number of elements added per thread.</param>
        /// <returns>True if test succeeded, false otherwise.</returns>
        private static bool RunBlockingCollectionTest2_ConcurrentAdd(int numOfThreads, int numOfElementsPerThread)
        {
            TestHarness.TestLog("* RunBlockingCollectionTest2_ConcurrentAdd(numOfThreads={0}, numOfElementsPerThread={1})", numOfThreads, numOfElementsPerThread);
            ManualResetEvent mre = new ManualResetEvent(false);
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();
            Thread[] threads = new Thread[numOfThreads];

            for (int i = 0; i < threads.Length; ++i)
            {
                threads[i] = new Thread(delegate(object index)
                {
                    int startOfSequence = ((int)index) * numOfElementsPerThread;
                    int endOfSequence = startOfSequence + numOfElementsPerThread;

                    mre.WaitOne();

                    for (int j = startOfSequence; j < endOfSequence; ++j)
                    {
                        if (!blockingCollection.TryAdd(j))
                        {
                            TestHarness.TestLog(" > test failed - TryAdd returned false unexpectedly");
                        }
                    }
                });
                threads[i].Start(i);

            }

            mre.Set();
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
            int expectedCount = numOfThreads * numOfElementsPerThread;
            if (blockingCollection.Count != expectedCount)
            {
                TestHarness.TestLog(" > test failed - expected count = {0}, actual = {1}", expectedCount, blockingCollection.Count);
                return false;
            }
            var sortedElementsInCollection = blockingCollection.OrderBy(n => n);
            return VerifyElementsAreMembersOfSequence(sortedElementsInCollection, 0, expectedCount - 1);
        }

        /// <summary>Launch threads/2 producers and threads/2 consumers then make sure that all elements produced
        /// are consumed by consumers with no element lost nor consumed more than once.</summary>
        /// <param name="threads">Total number of producer and consumer threads.</param>
        /// <param name="numOfElementsPerThread">Number of elements to Add/Take per thread.</param>
        /// <returns>True if test succeeded, false otherwise.</returns>
        private static bool RunBlockingCollectionTest3_ConcurrentAddTake(int numOfThreads, int numOfElementsPerThread)
        {
            //If numOfThreads is not an even number, make it even.
            if ((numOfThreads % 2) != 0)
            {
                numOfThreads++;
            }
            TestHarness.TestLog("* RunBlockingCollectionTest3_ConcurrentAddTake(numOfThreads={0}, numOfElementsPerThread={1})", numOfThreads, numOfElementsPerThread);
            ManualResetEvent mre = new ManualResetEvent(false);
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();
            Thread[] threads = new Thread[numOfThreads];
            ArrayList removedElementsFromAllThreads = ArrayList.Synchronized(new ArrayList());

            for (int i = 0; i < threads.Length; ++i)
            {
                if (i < (threads.Length / 2))
                {
                    threads[i] = new Thread(delegate(object index)
                    {
                        int startOfSequence = ((int)index) * numOfElementsPerThread;
                        int endOfSequence = startOfSequence + numOfElementsPerThread;

                        mre.WaitOne();
                        for (int j = startOfSequence; j < endOfSequence; ++j)
                        {
                            if (!blockingCollection.TryAdd(j))
                            {
                                TestHarness.TestLog(" > test failed - TryAdd returned false unexpectedly");
                            }
                        }
                    });
                    threads[i].Start(i);
                }
                else
                {
                    threads[i] = new Thread(delegate()
                    {
                        ArrayList removedElements = new ArrayList();
                        mre.WaitOne();
                        for (int j = 0; j < numOfElementsPerThread; ++j)
                        {
                            removedElements.Add(blockingCollection.Take());
                        }

                        //The elements are added later in this loop to removedElementsFromAllThreads ArrayList and not in 
                        //the loop above so that the synchronization mechanisms of removedElementsFromAllThreads do not 
                        //interfere in coordinating the threads and only blockingCollection is coordinating the threads.
                        for (int j = 0; j < numOfElementsPerThread; ++j)
                        {
                            removedElementsFromAllThreads.Add(removedElements[j]);
                        }
                    });
                    threads[i].Start();
                }


            }

            mre.Set();
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
            int expectedCount = 0;
            if (blockingCollection.Count != expectedCount)
            {
                TestHarness.TestLog(" > test failed - expected count = {0}, actual = {1}", expectedCount, blockingCollection.Count);
                return false;
            }
            int[] arrayOfRemovedElementsFromAllThreads = (int[])(removedElementsFromAllThreads.ToArray(typeof(int)));
            var sortedElementsInCollection = arrayOfRemovedElementsFromAllThreads.OrderBy(n => n);
            return VerifyElementsAreMembersOfSequence(sortedElementsInCollection, 0, (numOfThreads / 2 * numOfElementsPerThread) - 1);
        }

        /// <summary>Validates the Dispose() method.</summary>
        /// <returns>True if test succeeded, false otherwise.</returns>
        private static bool RunBlockingCollectionTest4_Dispose()
        {
            TestHarness.TestLog("* RunBlockingCollectionTest4_Dispose()");
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();

            blockingCollection.Dispose();
            bool testSuceeded = false;
            int numOfExceptionsThrown = 0;
            int numOfTests = 0;

            try
            {
                numOfTests++;
                blockingCollection.Add(default(int));
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                blockingCollection.TryAdd(default(int));
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                blockingCollection.TryAdd(default(int), 1);
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                blockingCollection.TryAdd(default(int), new TimeSpan(1));
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            int item;
            try
            {
                numOfTests++;
                blockingCollection.Take();
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                blockingCollection.TryTake(out item);
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                blockingCollection.TryTake(out item, 1);
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                blockingCollection.TryTake(out item, new TimeSpan(1));
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            const int NUM_OF_COLLECTIONS = 10;
            BlockingCollection<int>[] blockingCollections = new BlockingCollection<int>[NUM_OF_COLLECTIONS];
            for (int i = 0; i < NUM_OF_COLLECTIONS - 1; ++i)
            {
                blockingCollections[i] = ConstructBlockingCollection<int>(-1);
            }

            blockingCollections[NUM_OF_COLLECTIONS - 1] = blockingCollection;
            try
            {
                numOfTests++;
                BlockingCollection<int>.AddToAny(blockingCollections, default(int));
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                BlockingCollection<int>.TryAddToAny(blockingCollections, default(int));
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                BlockingCollection<int>.TryAddToAny(blockingCollections, default(int), 1);
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                BlockingCollection<int>.TryAddToAny(blockingCollections, default(int), new TimeSpan(1));
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                BlockingCollection<int>.TakeFromAny(blockingCollections, out item);
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                BlockingCollection<int>.TryTakeFromAny(blockingCollections, out item);
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                BlockingCollection<int>.TryTakeFromAny(blockingCollections, out item, 1);
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                BlockingCollection<int>.TryTakeFromAny(blockingCollections, out item, new TimeSpan(1));
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                blockingCollection.CompleteAdding();
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                blockingCollection.ToArray();
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                blockingCollection.CopyTo(new int[1], 0);
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            int? boundedCapacity = 0;
            try
            {
                numOfTests++;
                boundedCapacity = blockingCollection.BoundedCapacity;
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            bool isCompleted = false;
            try
            {
                numOfTests++;
                isCompleted = blockingCollection.IsCompleted;
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            bool addingIsCompleted = false;
            try
            {
                numOfTests++;
                addingIsCompleted = blockingCollection.IsAddingCompleted;
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            int count = 0;
            try
            {
                numOfTests++;
                count = blockingCollection.Count;
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            object syncRoot = null;
            try
            {
                numOfTests++;
                syncRoot = ((ICollection)blockingCollection).SyncRoot;
            }
            catch (NotSupportedException)
            {
                numOfExceptionsThrown++;
            }

            bool isSynchronized = false;
            try
            {
                numOfTests++;
                isSynchronized = ((ICollection)blockingCollection).IsSynchronized;
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                blockingCollection.Dispose();
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                foreach (int element in blockingCollection)
                {
                    int temp = element;
                }
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }

            try
            {
                numOfTests++;
                foreach (int element in blockingCollection.GetConsumingEnumerable())
                {
                    int temp = element;
                }
            }
            catch (ObjectDisposedException)
            {
                numOfExceptionsThrown++;
            }


            testSuceeded = (numOfExceptionsThrown == numOfTests);

            if (!testSuceeded)
            {
                TestHarness.TestLog(" > test failed - Not all methods threw ObjectDisposedExpection");
            }

            return testSuceeded;
        }

        /// <summary>Validates GetEnumerator and makes sure that BlockingCollection.GetEnumerator() produces the 
        /// same results as IConcurrentCollection.GetEnumerator().</summary>
        /// <returns>True if test succeeded, false otherwise.</returns>
        private static bool RunBlockingCollectionTest5_GetEnumerator()
        {
            TestHarness.TestLog("* RunBlockingCollectionTest5_GetEnumerator()");
            ConcurrentStackCollection<int> concurrentCollection = new ConcurrentStackCollection<int>();
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();

            const int MAX_NUM_TO_ADD = 100;
            for (int i = 0; i < MAX_NUM_TO_ADD; ++i)
            {
                blockingCollection.Add(i);
                concurrentCollection.TryAdd(i);
            }

            ArrayList resultOfEnumOfBlockingCollection = new ArrayList();
            foreach (int i in blockingCollection)
            {
                resultOfEnumOfBlockingCollection.Add(i);
            }

            ArrayList resultOfEnumOfConcurrentCollection = new ArrayList();
            foreach (int i in concurrentCollection)
            {
                resultOfEnumOfConcurrentCollection.Add(i);
            }

            if (resultOfEnumOfBlockingCollection.Count != resultOfEnumOfConcurrentCollection.Count)
            {
                TestHarness.TestLog(" > test failed - number of elements returned from enumerators mismatch: ConcurrentCollection={0}, BlockingCollection={1}",
                                    resultOfEnumOfConcurrentCollection.Count,
                                    resultOfEnumOfBlockingCollection.Count);

                return false;
            }

            for (int i = 0; i < resultOfEnumOfBlockingCollection.Count; ++i)
            {
                if ((int)resultOfEnumOfBlockingCollection[i] != (int)resultOfEnumOfConcurrentCollection[i])
                {
                    TestHarness.TestLog(" > test failed - elements returned from enumerators mismatch: ConcurrentCollection={0}, BlockingCollection={1}",
                                    (int)resultOfEnumOfConcurrentCollection[i],
                                    (int)resultOfEnumOfBlockingCollection[i]);

                    return false;
                }
            }
            return true;
        }

        /// <summary>Validates GetConsumingEnumerator and makes sure that BlockingCollection.GetConsumingEnumerator() 
        /// produces the same results as if call Take in a loop.</summary>
        /// <returns>True if test succeeded, false otherwise.</returns>
        private static bool RunBlockingCollectionTest6_GetConsumingEnumerable()
        {
            TestHarness.TestLog("* RunBlockingCollectionTest6_GetConsumingEnumerable()");
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();
            BlockingCollection<int> blockingCollectionMirror = ConstructBlockingCollection<int>();

            const int MAX_NUM_TO_ADD = 100;
            for (int i = 0; i < MAX_NUM_TO_ADD; ++i)
            {
                blockingCollection.Add(i);
                blockingCollectionMirror.Add(i);
            }

            if (blockingCollection.Count != MAX_NUM_TO_ADD)
            {
                TestHarness.TestLog(" > test failed - unexpcted count: actual={0}, expected={1}", blockingCollection.Count, MAX_NUM_TO_ADD);
            }

            ArrayList resultOfEnumOfBlockingCollection = new ArrayList();

            //CompleteAdding() is called so that the MoveNext() on the Enumerable resulting from 
            //GetConsumingEnumerable return false after the collection is empty.
            blockingCollection.CompleteAdding();
            foreach (int i in blockingCollection.GetConsumingEnumerable())
            {
                resultOfEnumOfBlockingCollection.Add(i);
            }

            if (blockingCollection.Count != 0)
            {
                TestHarness.TestLog(" > test failed - unexpcted count: actual={0}, expected=0", blockingCollection.Count);
            }

            ArrayList resultOfEnumOfBlockingCollectionMirror = new ArrayList();
            while (blockingCollectionMirror.Count != 0)
            {
                resultOfEnumOfBlockingCollectionMirror.Add(blockingCollectionMirror.Take());
            }

            if (resultOfEnumOfBlockingCollection.Count != resultOfEnumOfBlockingCollectionMirror.Count)
            {
                TestHarness.TestLog(" > test failed - number of elements mismatch: BlockingCollectionMirror={0}, BlockingCollection={1}",
                                    resultOfEnumOfBlockingCollectionMirror.Count,
                                    resultOfEnumOfBlockingCollection.Count);

                return false;
            }

            for (int i = 0; i < resultOfEnumOfBlockingCollection.Count; ++i)
            {
                if ((int)resultOfEnumOfBlockingCollection[i] != (int)resultOfEnumOfBlockingCollectionMirror[i])
                {
                    TestHarness.TestLog(" > test failed - elements mismatch: BlockingCollectionMirror={0}, BlockingCollection={1}",
                                    (int)resultOfEnumOfBlockingCollectionMirror[i],
                                    (int)resultOfEnumOfBlockingCollection[i]);

                    return false;
                }
            }

            return true;

        }

        /// <summary>Validates that after CompleteAdding() is called, future calls to Add will throw exceptions, calls
        /// to Take will not block waiting for more input, and calls to MoveNext on the enumerator returned from GetEnumerator 
        /// on the enumerable returned from GetConsumingEnumerable will return false when the collection’s count reaches 0.</summary>
        /// <returns>True if test succeeded, false otherwise.</returns>
        private static bool RunBlockingCollectionTest7_CompleteAdding()
        {
            TestHarness.TestLog("* RunBlockingCollectionTest7_CompleteAdding()");

            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();

            blockingCollection.Add(0);

            blockingCollection.CompleteAdding();

            try
            {
                blockingCollection.Add(1);
                TestHarness.TestLog(" > test failed - Add should have thrown InvalidOperationException");
                return false;
            }
            catch (InvalidOperationException)
            {
            }

            if (blockingCollection.Count != 1)
            {
                TestHarness.TestLog(" > test failed - Unexpected count: Actual={0}, Expected=1", blockingCollection.Count);
                return false;
            }

            blockingCollection.Take();

            try
            {
                blockingCollection.Take();
                TestHarness.TestLog(" > test failed - Take should have thrown OperationCanceledException");
                return false;
            }
            catch (InvalidOperationException)
            {
            }

            int item = 0;


            if (blockingCollection.TryTake(out item))
            {
                TestHarness.TestLog(" > test failed - TryTake should have return false");
                return false;
            }

            int counter = 0;
            foreach (int i in blockingCollection.GetConsumingEnumerable())
            {
                counter++;
            }

            if (counter > 0)
            {
                TestHarness.TestLog(" > test failed - the enumerable returned from GetConsumingEnumerable() should not have enumerated through the collection");
                return false;
            }


            return true;
        }

        private static bool RunBlockingCollectionTest7_ConcurrentAdd_CompleteAdding()
        {
            TestHarness.TestLog("* RunBlockingCollectionTest7_CompleteAdding()");

            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();
            Thread[] threads = new Thread[4];
            int succeededAdd = 0;
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(() =>
                {
                    for (int j = 0; j < 1000; j++)
                    {
                        try
                        {
                            blockingCollection.Add(j);
                            Interlocked.Increment(ref succeededAdd);
                        }
                        catch (InvalidOperationException)
                        {
                            break;
                        }

                    }
                });

                threads[i].Start();
            }

            blockingCollection.CompleteAdding();
            int count1 = blockingCollection.Count;
            Thread.Sleep(100);
            int count2 = blockingCollection.Count;

            if (count1 != count2)
            {
                TestHarness.TestLog(" > test failed - The count has been changed after returning from CompleteAdding");
                return false;
            }

            if (count1 != succeededAdd)
            {
                TestHarness.TestLog(" > test failed - The collection count doesn't match the read count succeededCount = " + succeededAdd + " read count = " + count1);
                return false;
            }
            TestHarness.TestLog(" > test succeeded " + count1);
            return true;

        }

        /// <summary>Validates that BlockingCollection.ToArray() produces same results as 
        /// IConcurrentCollection.ToArray().</summary>
        /// <returns>True if test succeeded, false otherwise.</returns>
        private static bool RunBlockingCollectionTest8_ToArray()
        {
            TestHarness.TestLog("* RunBlockingCollectionTest8_ToArray()");
            ConcurrentStackCollection<int> concurrentCollection = new ConcurrentStackCollection<int>();
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();

            const int MAX_NUM_TO_ADD = 100;
            for (int i = 0; i < MAX_NUM_TO_ADD; ++i)
            {
                blockingCollection.Add(i);
                concurrentCollection.TryAdd(i);
            }

            int[] arrBlockingCollection = blockingCollection.ToArray();
            int[] arrConcurrentCollection = concurrentCollection.ToArray();

            if (arrBlockingCollection.Length != arrConcurrentCollection.Length)
            {
                TestHarness.TestLog(" > test failed - Arrays length mismatch: arrBlockingCollection={0}, arrConcurrentCollection={1}",
                                    arrBlockingCollection.Length,
                                    arrConcurrentCollection.Length);

                return false;
            }

            for (int i = 0; i < arrBlockingCollection.Length; ++i)
            {
                if (arrBlockingCollection[i] != arrConcurrentCollection[i])
                {
                    TestHarness.TestLog(" > test failed - Array elements mismatch: arrBlockingCollection[{2}]={0}, arrConcurrentCollection[{2}]={1}",
                                        arrBlockingCollection[i],
                                        arrConcurrentCollection[i],
                                        i);
                    return false;
                }
            }

            return true;
        }

        /// <summary>Validates that BlockingCollection.CopyTo() produces same results as IConcurrentCollection.CopyTo().</summary>        
        /// <param name="indexOfInsertion">The zero-based index in the array at which copying begins.</param>
        /// <returns>True if test succeeded, false otherwise.</returns>    
        private static bool RunBlockingCollectionTest9_CopyTo(int indexOfInsertion)
        {
            TestHarness.TestLog("* RunBlockingCollectionTest9_CopyTo(indexOfInsertion={0})", indexOfInsertion);
            ConcurrentStackCollection<int> concurrentCollection = new ConcurrentStackCollection<int>();
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();

            const int MAX_NUM_TO_ADD = 100;
            for (int i = 0; i < MAX_NUM_TO_ADD; ++i)
            {
                blockingCollection.Add(i);
                concurrentCollection.TryAdd(i);
            }

            //Array is automatically initialized to default(int).
            int[] arrBlockingCollection = new int[MAX_NUM_TO_ADD + indexOfInsertion];
            int[] arrConcurrentCollection = new int[MAX_NUM_TO_ADD + indexOfInsertion];

            blockingCollection.CopyTo(arrBlockingCollection, indexOfInsertion);
            concurrentCollection.CopyTo(arrConcurrentCollection, indexOfInsertion);

            for (int i = 0; i < arrBlockingCollection.Length; ++i)
            {
                if (arrBlockingCollection[i] != arrConcurrentCollection[i])
                {
                    TestHarness.TestLog(" > test failed - Array elements mismatch: arrBlockingCollection[{2}]={0}, arrConcurrentCollection[{2}]={1}",
                                        arrBlockingCollection[i],
                                        arrConcurrentCollection[i],
                                        i);
                    return false;
                }
            }

            return true;
        }

        /// <summary>Validates BlockingCollection.Count.</summary>        
        /// <returns>True if test succeeded, false otherwise.</returns>    
        private static bool RunBlockingCollectionTest10_Count()
        {
            TestHarness.TestLog("* RunBlockingCollectionTest10_Count()");
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>(1);

            if (blockingCollection.Count != 0)
            {
                TestHarness.TestLog(" > test failed - Unexpected count: Actual={0}, Expected=0", blockingCollection.Count);
                return false;
            }

            blockingCollection.Add(1);

            if (blockingCollection.Count != 1)
            {
                TestHarness.TestLog(" > test failed - Unexpected count: Actual={0}, Expected=1", blockingCollection.Count);
                return false;
            }

            blockingCollection.TryAdd(1);

            if (blockingCollection.Count != 1)
            {
                TestHarness.TestLog(" > test failed - Unexpected count: Actual={0}, Expected=1", blockingCollection.Count);
                return false;
            }

            blockingCollection.Take();

            if (blockingCollection.Count != 0)
            {
                TestHarness.TestLog(" > test failed - Unexpected count: Actual={0}, Expected=0", blockingCollection.Count);
                return false;
            }

            return true;
        }

        /// <summary>Validates BlockingCollection.BoundedCapacity.</summary>        
        /// <returns>True if test succeeded, false otherwise.</returns>    
        private static bool RunBlockingCollectionTest11_BoundedCapacity()
        {
            TestHarness.TestLog("* RunBlockingCollectionTest11_BoundedCapacity()");
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>(1);

            if (blockingCollection.BoundedCapacity != 1)
            {
                TestHarness.TestLog(" > test failed - Unexpected boundedCapacity: Actual={0}, Expected=1", blockingCollection.BoundedCapacity);
                return false;
            }

            blockingCollection = ConstructBlockingCollection<int>();

            if (blockingCollection.BoundedCapacity != -1)
            {
                TestHarness.TestLog(" > test failed - Unexpected boundedCapacity: Actual={0}, Expected=-1", blockingCollection.BoundedCapacity);
                return false;
            }

            return true;
        }

        /// <summary>Validates BlockingCollection.IsCompleted and BlockingCollection.AddingIsCompleted.</summary>        
        /// <returns>True if test succeeded, false otherwise.</returns>    
        private static bool RunBlockingCollectionTest12_IsCompleted_AddingIsCompleted()
        {
            TestHarness.TestLog("* RunBlockingCollectionTest12_IsCompleted_AddingIsCompleted()");
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();

            if (blockingCollection.IsAddingCompleted)
            {
                TestHarness.TestLog(" > test failed (Empty Collection) - AddingIsCompleted should be false");
                return false;
            }

            if (blockingCollection.IsCompleted)
            {
                TestHarness.TestLog(" > test failed (Empty Collection) - IsCompleted should be false");
                return false;
            }

            blockingCollection.CompleteAdding();

            if (!blockingCollection.IsAddingCompleted)
            {
                TestHarness.TestLog(" > test failed (Empty Collection) - AddingIsCompleted should be true");
                return false;
            }

            if (!blockingCollection.IsCompleted)
            {
                TestHarness.TestLog(" > test failed (Empty Collection) - IsCompleted should be true");
                return false;
            }

            blockingCollection = ConstructBlockingCollection<int>();
            blockingCollection.Add(0);
            blockingCollection.CompleteAdding();

            if (!blockingCollection.IsAddingCompleted)
            {
                TestHarness.TestLog(" > test failed (NonEmpty Collection) - AddingIsCompleted should be true");
                return false;
            }

            if (blockingCollection.IsCompleted)
            {
                TestHarness.TestLog(" > test failed (NonEmpty Collection) - IsCompleted should be false");
                return false;
            }

            blockingCollection.Take();

            if (!blockingCollection.IsCompleted)
            {
                TestHarness.TestLog(" > test failed (NonEmpty Collection) - IsCompleted should be true");
                return false;
            }

            return true;
        }

        /// <summary>Validates BlockingCollection.IsSynchronized and BlockingCollection.SyncRoot.</summary>        
        /// <returns>True if test succeeded, false otherwise.</returns>    
        private static bool RunBlockingCollectionTest13_IsSynchronized_SyncRoot()
        {
            TestHarness.TestLog("* RunBlockingCollectionTest13_IsSynchronized_SyncRoot()");
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();

            bool exceptionThrown =false;
            try
            {
                var dummy = ((ICollection)blockingCollection).SyncRoot; 

            }
            catch(NotSupportedException)
            {
                exceptionThrown = true;
            }
            if (!exceptionThrown)
            {
                TestHarness.TestLog(" > test failed - SyncRoot should throw NotSupportException");
            }

            if (((ICollection)blockingCollection).IsSynchronized)
            {
                TestHarness.TestLog(" > test failed - IsSynchronized should be false");
                return false;
            }

            return true;
        }

        /// <summary>Initializes an array of blocking collections such that all are full except one in case of adds and
        /// all are empty except one (the same blocking collection) in case of Takes.
        /// Adds "numOfAdds" elements to the BlockingCollection and then Takes "numOfTakes" elements and checks
        /// that the count is as expected, the elements Taked matched those added and verifies the return values of 
        /// TryAdd() and TryTake().</summary>
        /// <param name="numOfAdds">Number of elements to Add.</param>
        /// <param name="numOfTakes">Number of elements to Take.</param>
        /// <param name="numOfBlockingCollections">Length of BlockingCollections array.</param>
        /// <param name="indexOfBlockingCollectionUnderTest">Index of the BlockingCollection that will accept the operations.</param>
        /// <param name="boundedCapacity">The bounded capacity of the BlockingCollection under test.</param>
        /// <returns>True if test succeeds, false otherwise.</returns>
        private static bool RunBlockingCollectionTest14_AddAnyTakeAny(int numOfAdds,
                                                                        int numOfTakes,
                                                                        int numOfBlockingCollections,
                                                                        int indexOfBlockingCollectionUnderTest,
                                                                        int boundedCapacity)
        {
            TestHarness.TestLog("* RunBlockingCollectionTest14_AddAnyTakeAny(numOfAdds={0}, numOfTakes={1}, numOfBlockingCollections={2}," +
                                " indexOfBlockingCollectionUnderTest={3}, boundedCapacity={4})",
                                numOfAdds,
                                numOfTakes,
                                numOfBlockingCollections,
                                indexOfBlockingCollectionUnderTest,
                                boundedCapacity);

            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>(boundedCapacity);
            BlockingCollection<int>[] blockingCollections = new BlockingCollection<int>[numOfBlockingCollections];

            return AddAnyTakeAny(numOfAdds, numOfTakes, boundedCapacity, blockingCollection, blockingCollections, indexOfBlockingCollectionUnderTest);
        }

        /// <summary>Launch threads/2 producers and threads/2 consumers then makes sure that all elements produced
        /// are consumed by consumers with no element lost nor consumed more than once.</summary>
        /// <param name="threads">Total number of producer and consumer threads.</param>
        /// <param name="numOfElementsPerThread">Number of elements to Add/Take per thread.</param>
        /// <returns>True if test succeeded, false otherwise.</returns>
        private static bool RunBlockingCollectionTest15_ConcurrentAddAnyTakeAny(int numOfThreads, int numOfElementsPerThread, int numOfCollections, int boundOfCollections)
        {
            //If numOfThreads is not an even number, make it even.
            if ((numOfThreads % 2) != 0)
            {
                numOfThreads++;
            }
            TestHarness.TestLog("* RunBlockingCollectionTest15_ConcurrentAddAnyTakeAny(numOfThreads={0}, numOfElementsPerThread={1},numOfCollections={2},boundOfCollections={3})",
                                numOfThreads,
                                numOfElementsPerThread,
                                numOfCollections,
                                boundOfCollections);
            ManualResetEvent mre = new ManualResetEvent(false);

            BlockingCollection<int>[] blockingCollections = new BlockingCollection<int>[numOfCollections];
            for (int i = 0; i < numOfCollections; ++i)
            {
                blockingCollections[i] = ConstructBlockingCollection<int>(boundOfCollections);
            }

            Thread[] threads = new Thread[numOfThreads];
            ArrayList removedElementsFromAllThreads = ArrayList.Synchronized(new ArrayList());

            for (int i = 0; i < threads.Length; ++i)
            {
                if (i < (threads.Length / 2))
                {
                    threads[i] = new Thread(delegate(object index)
                    {
                        int startOfSequence = ((int)index) * numOfElementsPerThread;
                        int endOfSequence = startOfSequence + numOfElementsPerThread;

                        mre.WaitOne();
                        for (int j = startOfSequence; j < endOfSequence; ++j)
                        {
                            int indexOfCollection = BlockingCollection<int>.AddToAny(blockingCollections, j);
                            if (indexOfCollection < 0)
                            {
                                TestHarness.TestLog(" > test failed - AddToAny returned {0} unexpectedly", indexOfCollection);
                            }
                        }
                    });
                    threads[i].Start(i);
                }
                else
                {
                    threads[i] = new Thread(delegate()
                    {
                        ArrayList removedElements = new ArrayList();
                        mre.WaitOne();
                        for (int j = 0; j < numOfElementsPerThread; ++j)
                        {
                            int item = -1;
                            int indexOfCollection = BlockingCollection<int>.TakeFromAny(blockingCollections, out item);
                            if (indexOfCollection < 0)
                            {
                                TestHarness.TestLog(" > test failed - TakeFromAny returned {0} unexpectedly", indexOfCollection);
                            }
                            else
                            {
                                removedElements.Add(item);
                            }
                        }

                        //The elements are added later in this loop to removedElementsFromAllThreads ArrayList and not in 
                        //the loop above so that the synchronization mechanisms of removedElementsFromAllThreads do not 
                        //interfere in coordinating the threads and only blockingCollection is coordinating the threads.
                        for (int j = 0; j < numOfElementsPerThread; ++j)
                        {
                            removedElementsFromAllThreads.Add(removedElements[j]);
                        }
                    });
                    threads[i].Start();
                }

            }

            mre.Set();
            foreach (Thread thread in threads)
            {
                thread.Join();
            }
            int expectedCount = 0;
            int blockingCollectionIndex = 0;
            foreach (BlockingCollection<int> blockingCollection in blockingCollections)
            {
                if (blockingCollection.Count != expectedCount)
                {
                    TestHarness.TestLog(" > test failed - expected count = {0}, actual = {1}, blockingCollectionIndex = {2}", expectedCount, blockingCollection.Count, blockingCollectionIndex);
                    return false;
                }
                blockingCollectionIndex++;
            }
            int[] arrayOfRemovedElementsFromAllThreads = (int[])(removedElementsFromAllThreads.ToArray(typeof(int)));
            var sortedElementsInCollection = arrayOfRemovedElementsFromAllThreads.OrderBy(n => n);
            return VerifyElementsAreMembersOfSequence(sortedElementsInCollection, 0, (numOfThreads / 2 * numOfElementsPerThread) - 1);
        }

        /// <summary>Validates the constructor of BlockingCollection.</summary>
        /// <returns>True if test succeeded, false otherwise.</returns>
        private static bool RunBlockingCollectionTest16_Ctor()
        {
            TestHarness.TestLog("* RunBlockingCollectionTest16_Ctor()");

            BlockingCollection<int> blockingCollection = null;

            try
            {
                blockingCollection = new BlockingCollection<int>(null);
                TestHarness.TestLog(" > test failed - expected ArgumentNullException");
                return false;

            }
            catch (ArgumentNullException)
            {
            }

            try
            {
                blockingCollection = new BlockingCollection<int>(null, 1);
                TestHarness.TestLog(" > test failed - expected ArgumentNullException");
                return false;

            }
            catch (ArgumentNullException)
            {
            }

            try
            {
                blockingCollection = new BlockingCollection<int>(new ConcurrentStackCollection<int>(), 0);
                TestHarness.TestLog(" > test failed - expected ArgumentOutOfRangeException");
                return false;

            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                blockingCollection = new BlockingCollection<int>(new ConcurrentStackCollection<int>(), -1);
                TestHarness.TestLog(" > test failed - expected ArgumentOutOfRangeException");
                return false;

            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                ConcurrentStackCollection<int> concurrentStack = new ConcurrentStackCollection<int>();
                concurrentStack.TryAdd(1);
                concurrentStack.TryAdd(2);
                blockingCollection = new BlockingCollection<int>(concurrentStack, 1);
                TestHarness.TestLog(" > test failed - expected ArgumentException");
                return false;

            }
            catch (ArgumentException)
            {
            }

            return true;
        }

        /// <summary>Verfies that the correct exceptions are thrown for invalid inputs.</summary>
        /// <returns>True if test succeeds and false otherwise.</returns>
        private static bool RunBlockingCollectionTest17_AddExceptions()
        {
            TestHarness.TestLog("* RunBlockingCollectionTest17_AddExceptions()");
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();

            try
            {
                blockingCollection.TryAdd(0, new TimeSpan(0, 0, 0, 1, 2147483647));
                TestHarness.TestLog(" > test failed - expected exception ArgumentOutOfRangeException");
                return false;
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                blockingCollection.TryAdd(0, -2);
                TestHarness.TestLog(" > test failed - expected exception ArgumentOutOfRangeException");
                return false;
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                blockingCollection.CompleteAdding();
                blockingCollection.TryAdd(0);
                TestHarness.TestLog(" > test failed - expected exception InvalidOperationException");
                return false;
            }
            catch (InvalidOperationException)
            {
            }

            // test if the underlyingcollection.TryAdd returned flse
            BlockingCollection<int> bc = new BlockingCollection<int>(new QueueProxy1<int>());
            try
            {
                bc.Add(1);
                TestHarness.TestLog(" > test failed - expected exception InvalidOperationException");
                return false;
            }
            catch (InvalidOperationException)
            {
            }

            return true;
        }

        /// <summary>
        /// Internal IPCC implementer that its TryAdd returns false
        /// </summary>
        internal class QueueProxy1<T> : ConcurrentQueue<T>, IProducerConsumerCollection<T>
        {
            bool IProducerConsumerCollection<T>.TryAdd(T item)
            {
                return false;
            }
        }


        /// <summary>Verfies that the correct exceptions are thrown for invalid inputs.</summary>
        /// <returns>True if test succeeds and false otherwise.</returns>
        private static bool RunBlockingCollectionTest18_TakeExceptions()
        {
            TestHarness.TestLog("* RunBlockingCollectionTest18_TakeExceptions()");
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();

            int item;
            try
            {
                blockingCollection.TryTake(out item, new TimeSpan(0, 0, 0, 1, 2147483647));
                TestHarness.TestLog(" > test failed - expected exception ArgumentOutOfRangeException");
                return false;
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                blockingCollection.TryTake(out item, -2);
                TestHarness.TestLog(" > test failed - expected exception ArgumentOutOfRangeException");
                return false;
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                blockingCollection.CompleteAdding();
                blockingCollection.Take();
                TestHarness.TestLog(" > test failed - expected exception OperationCanceledException");
                return false;
            }
            catch (InvalidOperationException)
            {
            }
            return true;
        }

        /// <summary>Verfies that the correct exceptions are thrown for invalid inputs.</summary>
        /// <returns>True if test succeeds and false otherwise.</returns>
        private static bool RunBlockingCollectionTest19_AddAnyExceptions()
        {
            TestHarness.TestLog("* RunBlockingCollectionTest19_AddAnyExceptions()");
            const int NUM_OF_COLLECTIONS = 2;
            BlockingCollection<int>[] blockingCollections = new BlockingCollection<int>[NUM_OF_COLLECTIONS];
            for (int i = 0; i < NUM_OF_COLLECTIONS; ++i)
            {
                blockingCollections[i] = ConstructBlockingCollection<int>();
            }

            try
            {
                BlockingCollection<int>.TryAddToAny(blockingCollections, 0, new TimeSpan(0, 0, 0, 1, 2147483647));
                TestHarness.TestLog(" > test failed - expected exception ArgumentOutOfRangeException");
                return false;
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                BlockingCollection<int>.TryAddToAny(blockingCollections, 0, -2);
                TestHarness.TestLog(" > test failed - expected exception ArgumentOutOfRangeException");
                return false;
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                BlockingCollection<int>.TryAddToAny(new BlockingCollection<int>[NUM_OF_COLLECTIONS], 0);
                TestHarness.TestLog(" > test failed - expected exception ArgumentException");
                return false;
            }
            catch (ArgumentException)
            {
            }

            try
            {
                BlockingCollection<int>.TryAddToAny(new BlockingCollection<int>[0], 0);
                TestHarness.TestLog(" > test failed - expected exception ArgumentException");
                return false;
            }
            catch (ArgumentException)
            {
            }

            try
            {
                blockingCollections[NUM_OF_COLLECTIONS - 1].CompleteAdding();
                BlockingCollection<int>.TryAddToAny(blockingCollections, 0);
                TestHarness.TestLog(" > test failed - expected exception ArgumentException");
                return false;
            }
            catch (ArgumentException)
            {
            }

            try
            {
                BlockingCollection<int>.TryAddToAny(null, 0);
                TestHarness.TestLog(" > test failed - expected exception ArgumentNullException");
                return false;
            }
            catch (ArgumentNullException)
            {
            }

            // test if the underlyingcollection.TryAdd returned flse
            BlockingCollection<int> collection = new BlockingCollection<int>(new QueueProxy1<int>());
            try
            {
                BlockingCollection<int>.AddToAny(new BlockingCollection<int>[] { collection }, 1);
                TestHarness.TestLog(" > test failed - expected exception InvalidOperationException");
                return false;
            }
            catch (InvalidOperationException)
            {
            }

            // Test if the collections range > supported range
            for (int i = 0; i < 2; i++)
            {
                int len = 64;
                ApartmentState state = ApartmentState.MTA;
                bool failed = false;
                if (i == 1)
                {
                    len = 63;
                    state = ApartmentState.STA;
                }
                BlockingCollection<int>[] collections = new BlockingCollection<int>[len];
                for (int j = 0; j < len; j++)
                {
                    collections[j] = new BlockingCollection<int>(1);
                    collections[j].Add(j);
                }
                Thread t = new Thread(() =>
                {
                    try
                    {
                        BlockingCollection<int>.TryAddToAny(collections, -1, 100);
                        failed = true;
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                    }
                });
                t.SetApartmentState(state);

                t.Start();
                t.Join();
                if (failed)
                {
                    TestHarness.TestLog(" > test failed - expected exception ArgumentOutOfRangeException");
                    return false;
                }
            }
            return true;
        }

        /// <summary>Verfies that the correct exceptions are thrown for invalid inputs.</summary>
        /// <returns>True if test succeeds and false otherwise.</returns>
        private static bool RunBlockingCollectionTest20_TakeAnyExceptions()
        {
            TestHarness.TestLog("* RunBlockingCollectionTest20_TakeAnyExceptions()");
            const int NUM_OF_COLLECTIONS = 2;
            BlockingCollection<int>[] blockingCollections = new BlockingCollection<int>[NUM_OF_COLLECTIONS];
            for (int i = 0; i < NUM_OF_COLLECTIONS; ++i)
            {
                blockingCollections[i] = ConstructBlockingCollection<int>();
            }

            int item;
            try
            {
                BlockingCollection<int>.TryTakeFromAny(blockingCollections, out item, new TimeSpan(0, 0, 0, 1, 2147483647));
                TestHarness.TestLog(" > test failed - expected exception ArgumentOutOfRangeException");
                return false;
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                BlockingCollection<int>.TryTakeFromAny(blockingCollections, out item, -2);
                TestHarness.TestLog(" > test failed - expected exception ArgumentOutOfRangeException");
                return false;
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                BlockingCollection<int>.TryTakeFromAny(new BlockingCollection<int>[NUM_OF_COLLECTIONS], out item);
                TestHarness.TestLog(" > test failed - expected exception ArgumentException");
                return false;
            }
            catch (ArgumentException)
            {
            }

            try
            {
                BlockingCollection<int>.TryTakeFromAny(new BlockingCollection<int>[0], out item);
                TestHarness.TestLog(" > test failed - expected exception ArgumentException");
                return false;
            }
            catch (ArgumentException)
            {
            }

            try
            {
                BlockingCollection<int>.TryTakeFromAny(null, out item);
                TestHarness.TestLog(" > test failed - expected exception ArgumentNullException");
                return false;
            }
            catch (ArgumentNullException)
            {
            }

            return true;
        }


        /// <summary>Verfies that the correct exceptions are thrown for invalid inputs.</summary>
        /// <returns>True if test succeeds and false otherwise.</returns>
        private static bool RunBlockingCollectionTest21_CopyToExceptions()
        {
            TestHarness.TestLog("* RunBlockingCollectionTest22_CopyToExceptions()");
            BlockingCollection<int> blockingCollection = ConstructBlockingCollection<int>();
            blockingCollection.Add(0);
            blockingCollection.Add(0);
            int[] arr = new int[2];
            try
            {
                blockingCollection.CopyTo(null, 0);
                TestHarness.TestLog(" > test failed - expected exception ArgumentNullException");
                return false;
            }
            catch (ArgumentNullException)
            {
            }

            try
            {
                blockingCollection.CopyTo(arr, -1);
                TestHarness.TestLog(" > test failed - expected exception ArgumentOutOfRangeException");
                return false;
            }
            catch (ArgumentOutOfRangeException)
            {
            }

            try
            {
                blockingCollection.CopyTo(arr, 2);
                TestHarness.TestLog(" > test failed - expected exception ArgumentException");
                return false;
            }
            catch (ArgumentException)
            {
            }

            try
            {
                int[,] twoDArray = new int[2, 2];
                ((ICollection)blockingCollection).CopyTo(twoDArray, 0);
                TestHarness.TestLog(" > test failed - expected exception ArgumentException");
                return false;
            }
            catch (ArgumentException)
            {
            }

            try
            {
                float[,] twoDArray = new float[2, 2];
                ((ICollection)blockingCollection).CopyTo(twoDArray, 0);
                TestHarness.TestLog(" > test failed - expected exception ArgumentException");
                return false;
            }
            catch (ArgumentException)
            {
            }

            return true;
        }

      


        /// <summary>Initializes an array of blocking collections (if its not null) such that all are full except one in case 
        /// of adds and all are empty except one (the same blocking collection) in case of Takes.
        /// Adds "numOfAdds" elements to the BlockingCollection and then Takes "numOfTakes" elements and checks
        /// that the count is as expected, the elements Taked matched those added and verifies the return values of 
        /// TryAdd() and TryTake().</summary>        
        /// <param name="numOfAdds">Number of elements to Add.</param>
        /// <param name="numOfTakes">Number of elements to Take.</param>
        /// <param name="boundedCapacity">The bounded capacity of the BlockingCollection under test.</param>
        /// <param name="blockingCollection">The blocking collection under test.</param>
        /// <param name="blockingCollections">The array of blocking collections under test. Null if this method should use TryAdd/Take
        /// and not AddToAny/TakeFromAny.</param>
        /// <param name="indexOfBlockingCollectionUnderTest">Index of the BlockingCollection that will accept the operations.</param>
        /// <returns>True if test succeeds, false otherwise.</returns>
        private static bool AddAnyTakeAny(int numOfAdds,
                                            int numOfTakes,
                                            int boundedCapacity,
                                            BlockingCollection<int> blockingCollection,
                                            BlockingCollection<int>[] blockingCollections,
                                            int indexOfBlockingCollectionUnderTest
                                            )
        {

            if (blockingCollections != null)
            {
                //Initialize all other blocking collections to be full so that Adds are done on blockingCollection.
                for (int i = 0; i < blockingCollections.Length; ++i)
                {
                    if (i == indexOfBlockingCollectionUnderTest)
                    {
                        blockingCollections[i] = blockingCollection;
                    }
                    else
                    {
                        blockingCollections[i] = ConstructFullBlockingCollection<int>();
                    }
                }
            }

            ConcurrentStackCollection<int> concurrentCollection = new ConcurrentStackCollection<int>();
            Random random = new Random();
            int numberToAdd = 0;
            int numOfTrueTryAdds = 0;
            int expectedNumOfSuccessfulTryAdds = Math.Min(numOfAdds, (boundedCapacity == -1) ? Int32.MaxValue : boundedCapacity);
            for (int i = 0; i < numOfAdds; ++i)
            {
                numberToAdd = random.Next();
                if (blockingCollections == null)
                {
                    if (blockingCollection.TryAdd(numberToAdd))
                    {
                        numOfTrueTryAdds++;
                    }
                }
                else
                {
                    int indexOfCollectionThatAcceptedTheOperation = BlockingCollection<int>.TryAddToAny(blockingCollections, numberToAdd);
                    if (indexOfCollectionThatAcceptedTheOperation == indexOfBlockingCollectionUnderTest)
                    {
                        numOfTrueTryAdds++;
                    }
                    else if (i < expectedNumOfSuccessfulTryAdds)
                    {
                        TestHarness.TestLog(" > test failed - TryAddToAny returned #{0} while it should return #{1}", indexOfCollectionThatAcceptedTheOperation, indexOfBlockingCollectionUnderTest);
                        return false;
                    }
                }
                if (i < expectedNumOfSuccessfulTryAdds)
                {
                    concurrentCollection.TryAdd(numberToAdd);
                }
            }
            if (numOfTrueTryAdds != expectedNumOfSuccessfulTryAdds)
            {
                TestHarness.TestLog(" > test failed - expected #{0} calls to TryAdd will return true while actual is #{1}", expectedNumOfSuccessfulTryAdds, numOfTrueTryAdds);
                return false;
            }
            if (concurrentCollection.Count != blockingCollection.Count)
            {
                TestHarness.TestLog(" > test failed - collections count differs: blockingCollection = {0}, concurrentCollection = {1}",
                                    blockingCollection.Count,
                                    concurrentCollection.Count);
                return false;
            }
            int itemFromBlockingCollection;
            int itemFromConcurrentCollection;
            int numOfTrueTryTakes = 0;
            int expectedNumOfSuccessfulTryTakes = Math.Min(expectedNumOfSuccessfulTryAdds, numOfTakes);

            if (blockingCollections != null)
            {
                //Initialize all other blocking collections to be empty so that Takes are done on blockingCollection
                for (int i = 0; i < blockingCollections.Length; ++i)
                {
                    if (i != indexOfBlockingCollectionUnderTest)
                    {
                        blockingCollections[i] = ConstructBlockingCollection<int>();
                    }
                }
            }

            for (int i = 0; i < numOfTakes; ++i)
            {
                if (blockingCollections == null)
                {
                    if (blockingCollection.TryTake(out itemFromBlockingCollection))
                    {
                        numOfTrueTryTakes++;
                    }
                }
                else
                {
                    int indexOfCollectionThatAcceptedTheOperation = BlockingCollection<int>.TryTakeFromAny(blockingCollections, out itemFromBlockingCollection);
                    if (indexOfCollectionThatAcceptedTheOperation == indexOfBlockingCollectionUnderTest)
                    {
                        numOfTrueTryTakes++;
                    }
                    else if (i < expectedNumOfSuccessfulTryTakes)
                    {
                        TestHarness.TestLog(" > test failed - TryTakeFromAny returned #{0} while it should return #{1}", indexOfCollectionThatAcceptedTheOperation, indexOfBlockingCollectionUnderTest);
                        return false;
                    }

                }
                if (i < expectedNumOfSuccessfulTryTakes)
                {
                    concurrentCollection.TryTake(out itemFromConcurrentCollection);
                    if (itemFromBlockingCollection != itemFromConcurrentCollection)
                    {
                        TestHarness.TestLog(" > test failed - Taked elements differ : itemFromBlockingCollection = {0}, itemFromConcurrentCollection = {1}",
                                            itemFromBlockingCollection,
                                            itemFromConcurrentCollection);
                        return false;
                    }
                }

            }
            if (numOfTrueTryTakes != expectedNumOfSuccessfulTryTakes)
            {
                TestHarness.TestLog(" > test failed - expected #{0} calls to TryTake will return true while actual is #{1}", expectedNumOfSuccessfulTryTakes, numOfTrueTryTakes);
                return false;
            }
            int expectedCount = expectedNumOfSuccessfulTryAdds - expectedNumOfSuccessfulTryTakes;
            expectedCount = (expectedCount < 0) ? 0 : expectedCount;

            if (blockingCollection.Count != expectedCount)
            {
                TestHarness.TestLog(" > test failed - count is not as expected: expected = {0}, actual = {1}",
                                    expectedCount,
                                    blockingCollection.Count);

                return false;
            }

            return true;
        }

        /// <summary>Constructs and returns an unbounded blocking collection.</summary>
        /// <typeparam name="T">The type of the elements in the blocking collection.</typeparam>
        /// <returns>An unbounded blocking collection.</returns>
        private static BlockingCollection<T> ConstructBlockingCollection<T>()
        {
            return ConstructBlockingCollection<T>(-1);
        }

        /// <summary>Constructs and returns a full bounded blocking collection.</summary>
        /// <typeparam name="T">The type of the elements in the blocking collection.</typeparam>
        /// <returns>An full bounded blocking collection.</returns>
        private static BlockingCollection<T> ConstructFullBlockingCollection<T>()
        {
            BlockingCollection<T> blockingCollection = ConstructBlockingCollection<T>(1);
            blockingCollection.Add(default(T));
            return blockingCollection;
        }

        /// <summary>Constructs and returns a blocking collection.</summary>
        /// <typeparam name="T">The type of the elements in the blocking collection.</typeparam>
        /// <param name="boundedCapacity">The bounded capacity of the collection.</param>
        /// <returns>A blocking collection.</returns>
        private static BlockingCollection<T> ConstructBlockingCollection<T>(int boundedCapacity)
        {
            ConcurrentStackCollection<T> concurrentCollection = new ConcurrentStackCollection<T>();
            BlockingCollection<T> blockingCollection = null;

            if (boundedCapacity == -1)
            {
                blockingCollection = new BlockingCollection<T>(concurrentCollection);
            }
            else
            {
                blockingCollection = new BlockingCollection<T>(concurrentCollection, boundedCapacity);
            }
            return blockingCollection;
        }

        /// <summary>Verifies that the elements in sortedElementsInCollection are a sequence from start to end.</summary>
        /// <param name="sortedElementsInCollection">The enumerable containing the elements.</param>
        /// <param name="start">The start of the sequence.</param>
        /// <param name="end">The end of the sequence.</param>
        /// <returns></returns>
        private static bool VerifyElementsAreMembersOfSequence(IEnumerable sortedElementsInCollection, int start, int end)
        {
            int current = start;
            bool elementsAreMembersOfSequence = true;

            foreach (int element in sortedElementsInCollection)
            {
                if (element != current)
                {
                    elementsAreMembersOfSequence = false;
                }
                current++;
            }
            if ((current - 1) != end)
            {
                if ((current - 1) < end)
                {
                    TestHarness.TestLog(" > test failed - the collection contains less elements than expected: actual={0}, expected{1}",
                                        current - start,
                                        end - start + 1);
                }
            }
            if (!elementsAreMembersOfSequence)
            {
                TestHarness.TestLog(" > test failed - elements are not properly added");
                foreach (int element in sortedElementsInCollection)
                {
                    TestHarness.TestLog(" > {0}", element);
                }
                return false;
            }

            return true;
        }

        /// <summary>This is a Stack implementing IConcurrentCollection to be used in the tests of BlockingCollection.</summary>
        /// <typeparam name="T">The type of elements stored in the stack.</typeparam>
        private class ConcurrentStackCollection<T> : IProducerConsumerCollection<T>
        {
            ConcurrentStack<T> concurrentStack;

            public ConcurrentStackCollection()
            {
                concurrentStack = new ConcurrentStack<T>();
            }
            #region IProducerConsumerCollection<T> Members

            public void CopyTo(T[] dest, int idx)
            {
                concurrentStack.CopyTo(dest, idx);
            }

            public T[] ToArray()
            {
                return concurrentStack.ToArray();
            }

            public bool TryAdd(T item)
            {
                concurrentStack.Push(item);
                return true;
            }

            public bool TryTake(out T item)
            {
                return concurrentStack.TryPop(out item);
            }

            #endregion

            #region IEnumerable<T> Members

            public IEnumerator<T> GetEnumerator()
            {
                return concurrentStack.GetEnumerator();
            }

            #endregion

            #region IEnumerable Members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return concurrentStack.GetEnumerator();
            }

            #endregion

            #region ICollection Members

            public void CopyTo(Array array, int index)
            {
                ((ICollection)concurrentStack).CopyTo(array, index);
            }

            public int Count
            {
                get { return concurrentStack.Count; }
            }

            public bool IsSynchronized
            {
                get { throw new NotImplementedException(); }
            }

            public object SyncRoot
            {
                get { throw new NotImplementedException(); }
            }

            #endregion
        }
    }
}
