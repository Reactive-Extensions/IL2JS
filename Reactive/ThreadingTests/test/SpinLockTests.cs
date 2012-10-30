using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
namespace plinq_devtests
{
    /// <summary>
    /// SpinLock unit tests
    /// </summary>
    class SpinLockTests
    {
        /// <summary>
        /// Run all SpinLock tests
        /// </summary>
        /// <returns>True if all tests passed, false if at least one test failed</returns>
        internal static bool RunSpinLockTests()
        {
            // boolean variable that represent the rest result, it is anded with each unit test
            // result, must be true after calling all tests
            bool passed = true;

            for (int i = 0; i < 2; i++)
            {
                bool b;
                if (i == 0)
                {
                    TestHarness.TestLog("NO THREAD IDS -- new SpinLock(true)");
                    b = true;
                }
                else
                {
                    TestHarness.TestLog("WITH THREAD IDS -- new SpinLock(false)");
                    b = false;
                }

                TestHarness.TestLog("Testing Enter()");
                passed &= RunSpinLockTest0_Enter(2, b);
                passed &= RunSpinLockTest0_Enter(128, b);
                passed &= RunSpinLockTest0_Enter(256, b);

                TestHarness.TestLog("Testing TryEnter()");
                passed &= RunSpinLockTest1_TryEnter(2, b);
                passed &= RunSpinLockTest1_TryEnter(128, b);
                passed &= RunSpinLockTest1_TryEnter(256, b);

                TestHarness.TestLog("Testing TryEnter(TimeSpan)");
                passed &= RunSpinLockTest2_TryEnter(2, b);
                passed &= RunSpinLockTest2_TryEnter(128, b);
                passed &= RunSpinLockTest2_TryEnter(256, b);

                TestHarness.TestLog("Testing Invalid cases for TryEnter()");
                passed &= RunSpinLockTest3_TryEnter(b);

                TestHarness.TestLog("Testing Exit()");
                passed &= RunSpinLockTest4_Exit(b);
            }

            return passed;
        }

        /// <summary>
        /// Test SpinLock.Enter by launching n threads that increment a variable inside a critical section
        /// the final count variable must be equal to n
        /// </summary>
        /// <param name="threadsCount">Number of threads that call enter/exit</param>
        /// <returns>True if succeeded, false otherwise</returns>
        private static bool RunSpinLockTest0_Enter(int threadsCount, bool enableThreadIDs)
        {
            TestHarness.TestLog("SpinLock.Enter(" + threadsCount + " threads)");

            // threads array
            Thread[] threads = new Thread[threadsCount];
            //spinlock object
            SpinLock slock = new SpinLock(enableThreadIDs);
            // scceeded threads counter
            int succeeded = 0;
            // Semaphore used to make sure that there is no other threads in the critical section
            Semaphore semaphore = new Semaphore(1, 1);

            for (int i = 0; i < threadsCount; i++)
            {
                threads[i] = new Thread(delegate()
                {
                    bool lockTaken = false;
                    try
                    {
                        slock.Enter(ref lockTaken);
                        //use semaphore to make sure that no other thread inside the critical section
                        if (!semaphore.WaitOne(0, false))
                        {
                            // This mean that there is another thread in the critical section
                            return;
                        }
                        succeeded++;
                        if (slock.IsThreadOwnerTrackingEnabled && !slock.IsHeldByCurrentThread)
                        {
                            // lock is obtained successfully
                            succeeded--;
                        }

                    }
                    catch
                    {
                        // decrement the count in case of exception
                        succeeded--;
                    }
                    finally
                    {
                        semaphore.Release();
                        if (lockTaken)
                        {
                            slock.Exit();
                        }
                    }
                });
                threads[i].Start();

            }
            // wait all threads
            for (int i = 0; i < threadsCount; i++)
            {
                threads[i].Join();
            }
            // count must be equal to the threads count
            if (succeeded != threadsCount)
            {
                TestHarness.TestLog("SpinLock.Enter() failed, actual count: " + succeeded + " expected: " + threadsCount);
                return false;
            }
            TestHarness.TestLog("SpinLock.Enter() passed.");
            return true;
        }

        /// <summary>
        /// Test SpinLock.TryEnter() by launching n threads, each one calls TryEnter, the succeeded threads increment
        /// a counter variable and failed threads increment failed variable, count + failed must be equal to n
        /// </summary>
        /// <param name="threadsCount">Number of threads that call enter/exit</param>
        /// <returns>True if succeeded, false otherwise</returns>
        private static bool RunSpinLockTest1_TryEnter(int threadsCount, bool enableThreadIDs)
        {
            TestHarness.TestLog("SpinLock.TryEnter(" + threadsCount + " threads)");

            Thread[] threads = new Thread[threadsCount];
            SpinLock slock = new SpinLock(enableThreadIDs);
            int succeeded = 0;
            int failed = 0;


            // Run threads
            for (int i = 0; i < threadsCount; i++)
            {
                threads[i] = new Thread(delegate()
                {
                    bool lockTaken = false;
                    slock.TryEnter(ref lockTaken);
                    if (lockTaken)
                    {
                        // Increment succeeded counter 
                        Interlocked.Increment(ref succeeded);
                        slock.Exit();
                    }
                    else
                    {
                        // Increment failed counter
                        Interlocked.Increment(ref failed);
                    }
                });
                threads[i].Start();
            }
            // Wait all threads
            for (int i = 0; i < threadsCount; i++)
            {
                threads[i].Join();
            }
            // succeeded + failed must be equal to the threads count.
            if (succeeded + failed != threadsCount)
            {
                TestHarness.TestLog("SpinLock.TryEnter() failed, actual count: " + (succeeded + failed) +
                    " expected :" + threadsCount);
                return false;
            }
            TestHarness.TestLog("SpinLock.TryEnter() passed.");
            return true;
        }

        /// <summary>
        /// Test SpinLock.TryEnter(Timespan) by generating random timespan milliseconds
        /// </summary>
        /// <param name="threadsCount">Number of threads that call enter/exit</param>
        /// <returns>True if succeeded, false otherwise</returns>
        private static bool RunSpinLockTest2_TryEnter(int threadsCount, bool enableThreadIDs)
        {
            TestHarness.TestLog("SpinLock.TryEnter(" + threadsCount + " threads)");

            Thread[] threads = new Thread[threadsCount];
            SpinLock slock = new SpinLock(enableThreadIDs);
            int succeeded = 0;
            int failed = 0;

            // Run threads
            for (int i = 0; i < threadsCount; i++)
            {
                threads[i] = new Thread(delegate(object x)
                {
                    // Generate random timespan
                    Random rand = new Random(33);
                    bool lockTaken = false;
                    TimeSpan time = TimeSpan.FromMilliseconds(rand.Next(-1, 20));
                    slock.TryEnter(time, ref lockTaken);
                    if (lockTaken)
                    {
                        // add some delay in the critical section
                        Thread.Sleep(15);
                        Interlocked.Increment(ref succeeded);
                        slock.Exit();
                    }
                    else
                    {
                        // Failed to get the lock within the timeout
                        Interlocked.Increment(ref failed);
                    }
                });
                threads[i].Start(i);
            }
            // Wait all threads
            for (int i = 0; i < threadsCount; i++)
            {
                threads[i].Join();
            }
            // succeeded + failed must be equal to the threads count.
            if (succeeded + failed != threadsCount)
            {
                TestHarness.TestLog("SpinLock.TryEnter() failed, actual count: " + (succeeded + failed) +
                    " expected :" + threadsCount);
                return false;
            }
            TestHarness.TestLog("SpinLock.TryEnter() passed.");
            return true;
        }

        /// <summary>
        /// Test TryEnter invalid cases
        /// </summary>
        /// <returns>True if succeeded, false otherwise</returns>
        private static bool RunSpinLockTest3_TryEnter(bool enableThreadIDs)
        {
            TestHarness.TestLog("SpinLock.TryEnter(invalid cases)");
            Exception exception = null;
            SpinLock slock = new SpinLock(enableThreadIDs);
            bool lockTaken = false;
            #region Recursive lock
            if (enableThreadIDs) // only valid if thread IDs are on
            {

                // Test recursive locks
                slock.Enter(ref lockTaken);
                try
                {
                    if (lockTaken)
                    {

                        bool dummy = false;
                        // reacquire the lock
                        slock.Enter(ref dummy);
                    }
                }
                catch (Exception ex)
                {
                    // LockRecursionException must be thrown
                    exception = ex;
                }
                if (lockTaken)
                {
                    slock.Exit();
                    //TODO: uncomment after finishing type forwarding in clr integration
                    if (exception == null /*|| exception.GetType() != typeof(LockRecursionException)*/)
                    {
                        TestHarness.TestLog("SpinLock.TryEnter() failed, recursive locks without exception");
                        return false;
                    }
                    if (slock.IsHeldByCurrentThread)
                    {
                        TestHarness.TestLog("SpinLock.TryEnter() failed, IsHeld is true after calling Exit");
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            #endregion
            #region timeout > int.max
            // Test invalid argument handling, too long timeout
            exception = null;
            try
            {
                lockTaken = false;
                slock.TryEnter(TimeSpan.MaxValue, ref lockTaken);

            }
            catch (Exception ex)
            {
                exception = ex;
            }
            if (exception == null || exception.GetType() != typeof(ArgumentOutOfRangeException))
            {
                TestHarness.TestLog(@"SpinLock.TryEnter() failed, timeout.Totalmilliseconds > int.maxValue
                 without throwing ArgumentOutOfRangeException " + exception);
                return false;
            }
            #endregion

            #region Timeout > int.max
            // Test invalid argument handling, timeout < -1
            exception = null;
            try
            {
                lockTaken = false;
                slock.TryEnter(-2, ref lockTaken);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            if (exception == null || exception.GetType() != typeof(ArgumentOutOfRangeException))
            {
                TestHarness.TestLog(@"SpinLock.TryEnter() failed, timeout < -1
                 without throwing ArgumentOutOfRangeException");
                return false;
            }
            #endregion

            TestHarness.TestLog("SpinLock.TryEnter() passed.");
            return true;
        }

        /// <summary>
        /// Test Exit
        /// </summary>
        /// <returns>True if succeeded, false otherwise</returns>
        private static bool RunSpinLockTest4_Exit(bool enableThreadIDs)
        {
            TestHarness.TestLog("SpinLock.Exit()");
            Exception exception = null;
            SpinLock slock = new SpinLock(enableThreadIDs);
            bool lockTaken = false;
            slock.Enter(ref lockTaken);
            slock.Exit();
            if (enableThreadIDs && slock.IsHeldByCurrentThread)
            {
                TestHarness.TestLog("SpinLock.Exit() failed, IsHeld is true after calling Exit");
                return false;
            }

            // Calling Exit without owning the lock
            try
            {
                slock.Exit();
            }
            catch (Exception ex)
            {
                // SynchronizationLockException must be thrown
                exception = ex;
            }
            if (enableThreadIDs)
            {
                if (exception == null || exception.GetType() != typeof(SynchronizationLockException))
                {
                    TestHarness.TestLog(@"SpinLock.Exit() failed, calling Exit without owning the lock");
                    return false;
                }
            }

            TestHarness.TestLog("SpinLock.Exit() passed.");
            return true;

        }

    }
}
