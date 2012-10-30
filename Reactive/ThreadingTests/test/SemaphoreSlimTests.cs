using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace plinq_devtests
{
    /// <summary>
    /// SemaphoreSlim unit tests
    /// </summary>
    internal class SemaphoreSlimTests
    {
        /// <summary>
        /// SemaphoreSlim public methds and properties to be tested
        /// </summary>
        private enum SemaphoreSlimActions
        {
            Constructor,
            Wait,
            Release,
            Dispose,
            CurrentCount,
            AvailableWaitHandle
        }

        /// <summary>
        /// Run all the ubit tests
        /// </summary>
        /// <returns>True if all tests succeeded, false if one or more tests failed</returns>
        internal static bool RunSemaphoreSlimTests()
        {
            bool passed = true;

            #region Constructor Tests
            TestHarness.TestLog("Testing SemaphoreSlim Constructor");
            passed &= RunSemaphoreSlimTest0_Ctor(0, 10, null);
            passed &= RunSemaphoreSlimTest0_Ctor(5, 10, null);
            passed &= RunSemaphoreSlimTest0_Ctor(10, 10, null);
            passed &= RunSemaphoreSlimTest0_Ctor(10, 0, typeof(ArgumentOutOfRangeException));
            passed &= RunSemaphoreSlimTest0_Ctor(10, -1, typeof(ArgumentOutOfRangeException));
            passed &= RunSemaphoreSlimTest0_Ctor(-1, 10, typeof(ArgumentOutOfRangeException));
            #endregion

            #region Wait Tests
            TestHarness.TestLog("Testing SemaphoreSlim Wait");
            // Infinite timeout
            passed &= RunSemaphoreSlimTest1_Wait(10, 10, -1, true, null);
            passed &= RunSemaphoreSlimTest1_Wait(1, 10, -1, true, null);

            // Zero timeout
            passed &= RunSemaphoreSlimTest1_Wait(10, 10, 0, true, null);
            passed &= RunSemaphoreSlimTest1_Wait(1, 10, 0, true, null);
            passed &= RunSemaphoreSlimTest1_Wait(0, 10, 0, false, null);

            // Positive timeout
            passed &= RunSemaphoreSlimTest1_Wait(10, 10, 10, true, null);
            passed &= RunSemaphoreSlimTest1_Wait(1, 10, 10, true, null);
            passed &= RunSemaphoreSlimTest1_Wait(0, 10, 10, false, null);

            // Invalid timeout
            passed &= RunSemaphoreSlimTest1_Wait(10, 10, -10, true, typeof(ArgumentOutOfRangeException));
            passed &= RunSemaphoreSlimTest1_Wait
                (10, 10, new TimeSpan(0, 0, Int32.MaxValue), true, typeof(ArgumentOutOfRangeException));
            #endregion

            #region Release Tests
            TestHarness.TestLog("Testing SemaphoreSlim Release");
            // Valid release count
            passed &= RunSemaphoreSlimTest2_Release(5, 10, 1, null);
            passed &= RunSemaphoreSlimTest2_Release(0, 10, 1, null);
            passed &= RunSemaphoreSlimTest2_Release(5, 10, 5, null);

            // Invalid release count
            passed &= RunSemaphoreSlimTest2_Release(5, 10, 0, typeof(ArgumentOutOfRangeException));
            passed &= RunSemaphoreSlimTest2_Release(5, 10, -1, typeof(ArgumentOutOfRangeException));

            // Semaphore Full
            passed &= RunSemaphoreSlimTest2_Release(10, 10, 1, typeof(SemaphoreFullException));
            passed &= RunSemaphoreSlimTest2_Release(5, 10, 6, typeof(SemaphoreFullException));
            passed &= RunSemaphoreSlimTest2_Release
                (int.MaxValue - 1, int.MaxValue, 10, typeof(SemaphoreFullException));
            #endregion

            #region Dispose Tests
            TestHarness.TestLog("Testing SemaphoreSlim Dispose");
            passed &= RunSemaphoreSlimTest4_Dispose(5, 10, null, null);
            passed &= RunSemaphoreSlimTest4_Dispose(5, 10, SemaphoreSlimActions.CurrentCount, null);
            passed &= RunSemaphoreSlimTest4_Dispose
                (5, 10, SemaphoreSlimActions.Wait, typeof(ObjectDisposedException));
            passed &= RunSemaphoreSlimTest4_Dispose
               (5, 10, SemaphoreSlimActions.Release, typeof(ObjectDisposedException));
            passed &= RunSemaphoreSlimTest4_Dispose
               (5, 10, SemaphoreSlimActions.AvailableWaitHandle, typeof(ObjectDisposedException));
            #endregion

            #region CurrentCount Tests
            TestHarness.TestLog("Testing SemaphoreSlim CurrentCount");
            passed &= RunSemaphoreSlimTest5_CurrentCount(5, 10, null);
            passed &= RunSemaphoreSlimTest5_CurrentCount(5, 10, SemaphoreSlimActions.Wait);
            passed &= RunSemaphoreSlimTest5_CurrentCount(5, 10, SemaphoreSlimActions.Release);
            #endregion


            #region AvailableWaitHandle Tests
            TestHarness.TestLog("Testing SemaphoreSlim AvailableWaitHandle");
            passed &= RunSemaphoreSlimTest7_AvailableWaitHandle(5, 10, null, true);
            passed &= RunSemaphoreSlimTest7_AvailableWaitHandle(0, 10, null, false);

            passed &= RunSemaphoreSlimTest7_AvailableWaitHandle(5, 10, SemaphoreSlimActions.Wait, true);
            passed &= RunSemaphoreSlimTest7_AvailableWaitHandle(1, 10, SemaphoreSlimActions.Wait, false);
            passed &= RunSemaphoreSlimTest7_AvailableWaitHandle(5, 10, SemaphoreSlimActions.Wait, true);
            passed &= RunSemaphoreSlimTest7_AvailableWaitHandle(0, 10, SemaphoreSlimActions.Release, true);
            #endregion

            #region Concurrent Tests

            TestHarness.TestLog("Testing SemaphoreSlim concurrent Wait and Release");
            passed &= RunSemaphoreSlimTest8_ConcWaitAndRelease
                (5, 1000, 50, 50, 50, 0, 5, 1000);

            passed &= RunSemaphoreSlimTest8_ConcWaitAndRelease
                (0, 1000, 50, 25, 25, 25, 0, 5000);

            passed &= RunSemaphoreSlimTest8_ConcWaitAndRelease
               (0, 1000, 50, 0, 0, 50, 0, 100);

            #endregion

            return passed;
        }

       
        /// <summary>
        /// Test SemaphoreSlim constructor
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="exceptionType">The type of the thrown exception in case of invalid cases,
        /// null for valid cases</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static bool RunSemaphoreSlimTest0_Ctor(int initial, int maximum, Type exceptionType)
        {
            TestHarness.TestLog("SemaphoreSlim(" + initial +"," + maximum +")");
            Exception exception = null;
            try
            {
                SemaphoreSlim semaphore = new SemaphoreSlim(initial, maximum);
                if (semaphore.CurrentCount != initial)
                {
                    TestHarness.TestLog
                        ("Constructor test failed, expected " + initial +" actual "+ semaphore.CurrentCount);
                    return false;
                }
            }
            catch (Exception ex)
            {
                exception = ex;  
            }
            // The code threw excption and it is not expected because the excyptionType param is null
            if (exceptionType == null && exception != null)
            {
                TestHarness.TestLog("Constructor failed, the code threw an exception, and it is not supposed to.");
                return false;
            }

            // Compare both exception types in case of the code threw exception
            if (exception != null && !Type.Equals(exception.GetType(), exceptionType))
            {
                TestHarness.TestLog("Constructor failed, Excption types do not match");
                return false;
            }
            TestHarness.TestLog("Constructor succeeded.");

            return true;
        }

        /// <summary>
        /// Test SemaphoreSlim Wait
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="timeout">The timeout parameter for the wait method, it must be either int or TimeSpan</param>
        /// <param name="returnValue">The expected wait return value</param>
        /// <param name="exceptionType">The type of the thrown exception in case of invalid cases,
        /// null for valid cases</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static bool RunSemaphoreSlimTest1_Wait
            (int initial, int maximum,object timeout,bool returnValue, Type exceptionType)
        {
            TestHarness.TestLog("Wait(" + initial + "," + maximum + ","+ timeout+")");
            Exception exception = null;
            SemaphoreSlim semaphore = new SemaphoreSlim(initial, maximum);
            try
            {
                bool result = false;
                if (timeout is TimeSpan)
                {
                    result = semaphore.Wait((TimeSpan)timeout);
                }
                else
                {
                    result = semaphore.Wait((int)timeout);
                }

                if (result != returnValue ||
                (result && semaphore.CurrentCount != initial - 1))
                {
                    TestHarness.TestLog("Wait failed, the method returned " + result + " and expected "+ returnValue);
                    return false;
                }
                
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            // The code threw excption and it is not expected because the excyptionType param is null
            if (exceptionType == null && exception != null)
            {
                TestHarness.TestLog("Wait failed, the code threw an exception, and it is not supposed to.");
                return false;
            }

            // Compare both exception types in case of the code threw exception
            if (exception != null && !Type.Equals(exception.GetType(), exceptionType))
            {
                TestHarness.TestLog("Wait failed, Excption types do not match");
                return false;
            }
            TestHarness.TestLog("Wait succeeded");
            return true;
        }
        
        /// <summary>
        /// Test SemaphoreSlim Release
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="releaseCount">The release count for the release method</param>
        /// <param name="exceptionType">The type of the thrown exception in case of invalid cases,
        /// null for valid cases</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static bool RunSemaphoreSlimTest2_Release
           (int initial, int maximum, int releaseCount, Type exceptionType)
        {
            TestHarness.TestLog("Relese(" + initial + "," + maximum + "," + releaseCount + ")");
            Exception exception = null;
            SemaphoreSlim semaphore = new SemaphoreSlim(initial, maximum);
            try
            {
                int oldCount = semaphore.Release(releaseCount);
                if (semaphore.CurrentCount != initial + releaseCount || oldCount != initial)
                {
                    TestHarness.TestLog("Release failed, the method returned "+ oldCount + " and expected " + initial);
                    return false;
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            // The code threw excption and it is not expected because the excyptionType param is null
            if (exceptionType == null && exception != null)
            {
                TestHarness.TestLog("Release failed, the code threw an exception, and it is not supposed to.");
                return false;
            }

            // Compare both exception types in case of the code threw exception
            if (exception != null && !Type.Equals(exception.GetType(), exceptionType))
            {
                TestHarness.TestLog("Release failed, Excption types do not match");
                return false;
            }
            TestHarness.TestLog("Release succeeded");
            return true;
        }

        /// <summary>
        /// Call specific SemaphoreSlim method or property 
        /// </summary>
        /// <param name="semaphore">The SemaphoreSlim instance</param>
        /// <param name="action">The action name</param>
        /// <param name="param">The action parameter, null if it takes no parameters</param>
        /// <returns>The action return value, null if the action returns void</returns>
        private static object CallSemaphoreAction
            (SemaphoreSlim semaphore, SemaphoreSlimActions? action, object param)
        {
            
            if (action == SemaphoreSlimActions.Wait)
            {
                if (param is TimeSpan)
                {
                    return semaphore.Wait((TimeSpan)param);
                }
                else if (param is int)
                {
                    return semaphore.Wait((int)param);
                }
                semaphore.Wait();
                return null;
            }
            else if (action == SemaphoreSlimActions.Release)
            {
                if (param != null)
                {
                    return semaphore.Release((int)param);
                }
                return semaphore.Release();
            }
            else if (action == SemaphoreSlimActions.Dispose)
            {
                semaphore.Dispose();
                return null;
            }
            else if (action == SemaphoreSlimActions.CurrentCount)
            {
                return semaphore.CurrentCount;
            }
            else if (action == SemaphoreSlimActions.AvailableWaitHandle)
            {
                return semaphore.AvailableWaitHandle;
            }

            return null;

        }

        /// <summary>
        /// Test SemaphoreSlim Dispose
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="action">SemaphoreSlim action to be called after Dispose</param>
        /// <param name="exceptionType">The type of the thrown exception in case of invalid cases,
        /// null for valid cases</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static bool RunSemaphoreSlimTest4_Dispose
          (int initial, int maximum, SemaphoreSlimActions? action, Type exceptionType)
        {
            TestHarness.TestLog("Dispose(" + initial + "," + maximum + "," + action + ")");
            Exception exception = null;
            SemaphoreSlim semaphore = new SemaphoreSlim(initial, maximum);
            try
            {
                semaphore.Dispose();
                CallSemaphoreAction(semaphore,action,null);
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            // The code threw excption and it is not expected because the excyptionType param is null
            if (exceptionType == null && exception != null)
            {
                TestHarness.TestLog("Dispose failed, the code threw an exception, and it is not supposed to.");
                return false;
            }

            // Compare both exception types in case of the code threw exception
            if (exception != null && !Type.Equals(exception.GetType(), exceptionType))
            {
                TestHarness.TestLog("Dispose failed, Excption types do not match");
                return false;
            }
            TestHarness.TestLog("Dispose succeeded");
            return true;
        }

        /// <summary>
        /// Test SemaphoreSlim CurrentCount property
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="action">SemaphoreSlim action to be called before CurentCount</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static bool RunSemaphoreSlimTest5_CurrentCount
          (int initial, int maximum, SemaphoreSlimActions? action)
        {
            TestHarness.TestLog("CurrentCount(" + initial + "," + maximum + "," + action + ")");
            SemaphoreSlim semaphore = new SemaphoreSlim(initial, maximum);
            try
            {
                CallSemaphoreAction(semaphore, action, null);
                if ((action == SemaphoreSlimActions.Wait && semaphore.CurrentCount != initial - 1)
                || (action == SemaphoreSlimActions.Release && semaphore.CurrentCount != initial + 1))
                {
                    TestHarness.TestLog("CurrentCount failed");
                    return false;
                }
            }
            catch(Exception ex)
            {
                TestHarness.TestLog("CurrentCount failed, the code threw exception " + ex);
                return false;
            }
            TestHarness.TestLog("CurrentCount succeeded");
            return true;
        }

        /// <summary>
        /// Test SemaphoreSlim AvailableWaitHandle property
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="action">SemaphoreSlim action to be called before WaitHandle</param>
        /// <param name="state">The expected wait handle state</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static bool RunSemaphoreSlimTest7_AvailableWaitHandle
          (int initial, int maximum, SemaphoreSlimActions? action, bool state)
        {
            TestHarness.TestLog("AvailableWaitHandle(" + initial + "," + maximum + "," + action + ")");
            SemaphoreSlim semaphore = new SemaphoreSlim(initial, maximum);
            try
            {
                CallSemaphoreAction(semaphore, action, null);
                if (semaphore.AvailableWaitHandle == null)
                {
                    TestHarness.TestLog("AvailableWaitHandle failed, handle is null.");
                    return false;
                }
                if (semaphore.AvailableWaitHandle.WaitOne(0, false) != state)
                {
                    TestHarness.TestLog("AvailableWaitHandle failed, expected " + state + " actual " + !state); 
                    return false;
                }
            }
            catch(Exception ex)
            {
                TestHarness.TestLog("AvailableWaitHandle failed, the code threw exception " + ex);
                return false;
            }

            TestHarness.TestLog("AvailableWaitHandle succeeded.");
            return true;
        }

        /// <summary>
        /// Test SemaphoreSlim Wait and Release methods concurrently
        /// </summary>
        /// <param name="initial">The initial semaphore count</param>
        /// <param name="maximum">The maximum semaphore count</param>
        /// <param name="waitThreads">Number of the threads that call Wait method</param>
        /// <param name="releaseThreads">Number of the threads that call Release method</param>
        /// <param name="succeededWait">Number of succeeded wait threads</param>
        /// <param name="failedWait">Number of failed wait threads</param>
        /// <param name="finalCount">The final semaphore count</param>
        /// <returns>True if the test succeeded, false otherwise</returns>
        private static bool RunSemaphoreSlimTest8_ConcWaitAndRelease(int initial, int maximum,
            int waitThreads, int releaseThreads, int succeededWait, int failedWait,int finalCount, int timeout)
        {
            TestHarness.TestLog
                ("ConcurrentWaitRelease(" + initial + "," + maximum + "," + waitThreads + ", "+ releaseThreads + ")");
            try
            {
                SemaphoreSlim semaphore = new SemaphoreSlim(initial, maximum);
                Thread[] threads = new Thread[waitThreads + releaseThreads];
                int succeeded = 0;
                int failed = 0;
                ManualResetEvent mre = new ManualResetEvent(false);
                // launch threads
                for (int i = 0; i < threads.Length; i++)
                {
                    if (i < waitThreads)
                    {
                        threads[i] = new Thread(delegate()
                            {
                                mre.WaitOne();
                                if (semaphore.Wait(timeout))
                                {
                                    Interlocked.Increment(ref succeeded);
                                }
                                else
                                {
                                    Interlocked.Increment(ref failed);
                                }
                            });
                    }
                    else
                    {
                       
                        threads[i] = new Thread(delegate()
                           {
                               mre.WaitOne();
                               semaphore.Release();
                           });
                    }
                    threads[i].Start();
                }

                mre.Set();
                //wait work to be done;
                for (int i = 0; i < threads.Length; i++)
                {
                    threads[i].Join();
                }

                //check the number of succeeded and failed wait
                if (succeeded != succeededWait || failed != failedWait || semaphore.CurrentCount != finalCount)
                {
                    TestHarness.TestLog("ConcurrentWaitRelease failed. This might not be a bug, if the system was unstable during the test.");
                    return false;
                }

                
            }
            catch(Exception ex)
            {
                TestHarness.TestLog("ConcurrentWaitRelease failed, the code threw exception " + ex);
                return false;
            }
            TestHarness.TestLog("ConcurrentWaitRelease succeeded.");
            return true;
        }
        
    }
}
