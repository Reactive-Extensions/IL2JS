using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
namespace plinq_devtests
{
    /// <summary>
    /// Barrier unit tests
    /// </summary>
    internal class BarrierTests
    {
        /// <summary>
        /// Runs all the unit tests
        /// </summary>
        /// <returns>True if all tests succeeded, false if one or more tests failed</returns>
        internal static bool RunBarrierTests()
        {
            bool passed = true;

            TestHarness.TestLog("Testing Barrier Constructor");
            passed &= RunBarrierTest1_ctor(10, null);
            passed &= RunBarrierTest1_ctor(0, null);
            passed &= RunBarrierTest1_ctor(-1, typeof(ArgumentOutOfRangeException));
            passed &= RunBarrierTest1_ctor(int.MaxValue, typeof(ArgumentOutOfRangeException));

            TestHarness.TestLog("Testing Barrier SignalAndWait");
            passed &= RunBarrierTest2_SignalAndWait(1, new TimeSpan(0, 0, 0, 0, -1), true, null);
            passed &= RunBarrierTest2_SignalAndWait(1, new TimeSpan(0, 0, 0, 0, -2), false, typeof(ArgumentOutOfRangeException));
            passed &= RunBarrierTest2_SignalAndWait(5, new TimeSpan(0, 0, 0, 0, 100), false, null);
            passed &= RunBarrierTest2_SignalAndWait(5, new TimeSpan(0), false, null);

            TestHarness.TestLog("Testing Barrier parallel SignalAndWait");
            passed &= RunBarrierTest3_SignalAndWait(3);

            TestHarness.TestLog("Testing Barrier AddParticipants");
            passed &= RunBarrierTest4_AddParticipants(0, 1, null);
            passed &= RunBarrierTest4_AddParticipants(5, 3, null);
            passed &= RunBarrierTest4_AddParticipants(0, 0, typeof(ArgumentOutOfRangeException));
            passed &= RunBarrierTest4_AddParticipants(2, -1, typeof(ArgumentOutOfRangeException));
            passed &= RunBarrierTest4_AddParticipants(0x00007FFF, 1, typeof(ArgumentOutOfRangeException));
            passed &= RunBarrierTest4_AddParticipants(100, int.MaxValue, typeof(ArgumentOutOfRangeException)); // bug#714977

            TestHarness.TestLog("Testing Barrier RemoveParticipants");
            passed &= RunBarrierTest5_RemoveParticipants(1, 1, null);
            passed &= RunBarrierTest5_RemoveParticipants(10, 7, null);
            passed &= RunBarrierTest5_RemoveParticipants(10, 0, typeof(ArgumentOutOfRangeException));
            passed &= RunBarrierTest5_RemoveParticipants(1, -1, typeof(ArgumentOutOfRangeException));
            passed &= RunBarrierTest5_RemoveParticipants(5, 6, typeof(ArgumentOutOfRangeException));

            TestHarness.TestLog("Testing Barrier Dispose");
            passed &= RunBarrierTest6_Dispose();
            passed &= RunBarrierTest7_Bug603035();
            passed &= RunBarrierTest8_PostPhaseException();
            passed &= RunBarrierTest9_PostPhaseException();
            return passed;
        }

        /// <summary>
        /// Testing Barrier constructor
        /// </summary>
        /// <param name="initialCount">The intial barrier count</param>
        /// <param name="exceptionType">Type of the exception in case of invalid input, null for valid cases</param>
        /// <returns>Tru if the test succeeded, false otherwise</returns>
        private static bool RunBarrierTest1_ctor(int initialCount, Type exceptionType)
        {
            TestHarness.TestLog("Barrier(" + initialCount + ")");
            Exception exception = null;
            try
            {
                Barrier b = new Barrier(initialCount);
                if (b.ParticipantCount != initialCount)
                {
                    TestHarness.TestLog("Constructor failed, ParticipantCount doesn't match the initialCount.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception != null && exceptionType == null)
            {
                TestHarness.TestLog("Constructor failed, unexpected exception has been thrown.");
                return false;
            }
            if (exception != null && !Type.Equals(exceptionType, exception.GetType()))
            {
                TestHarness.TestLog("Constructor failed, exceptions types do not match.");
                return false;
            }
            TestHarness.TestLog("Constructor succeeded");
            return true;
        }

        /// <summary>
        /// Test SignalAndWait sequential
        /// </summary>
        /// <param name="initialCount">The initial barrier participants</param>
        /// <param name="timeout">SignalAndWait timeout</param>
        /// <param name="result">Expected return value</param>
        /// <param name="exceptionType"></param>
        /// <param name="exceptionType">Type of the exception in case of invalid input, null for valid cases</param>
        /// <returns>Tru if the test succeeded, false otherwise</returns>
        private static bool RunBarrierTest2_SignalAndWait(int initialCount, TimeSpan timeout, bool result, Type exceptionType)
        {
            TestHarness.TestLog("SignalAndWait(" + initialCount + "," + timeout + ")");
            Barrier b = new Barrier(initialCount);
            Exception exception = null;
            try
            {
                if (b.SignalAndWait(timeout) != result)
                {
                    TestHarness.TestLog("SignalAndWait failed, return value doesn't match the expected value");
                    return false;
                }
                if (result && b.CurrentPhaseNumber != 1)
                {
                    TestHarness.TestLog("SignalAndWait failed, CurrentPhaseNumber has not been incremented after SignalAndWait");
                    return false;
                }
                if (result && b.ParticipantsRemaining != b.ParticipantCount)
                {
                    TestHarness.TestLog("SignalAndWait failed, ParticipantsRemaming does not equal the total participants");
                    return false;
                }

            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception != null && exceptionType == null)
            {
                TestHarness.TestLog("SignalAndWait failed, unexpected exception has been thrown.");
                return false;
            }
            if (exception != null && !Type.Equals(exceptionType, exception.GetType()))
            {
                TestHarness.TestLog("SignalAndWait failed, exceptions types do not match.");
                return false;
            }
            TestHarness.TestLog("SignalAndWait succeeded");
            return true;

        }

        /// <summary>
        /// Test SignalANdWait parallel
        /// </summary>
        /// <param name="initialCount">Initial barrier count</param>
        /// <returns>Tru if the test succeeded, false otherwise</returns>
        private static bool RunBarrierTest3_SignalAndWait(int initialCount)
        {
            Barrier b = new Barrier(initialCount);
            for (int i = 0; i < initialCount - 1; i++)
            {
                new Thread(() => b.SignalAndWait()).Start();
            }
            b.SignalAndWait();

            if (b.CurrentPhaseNumber != 1)
            {
                TestHarness.TestLog("SignalAndWait failed, CurrentPhaseNumber has not been incremented after SignalAndWait");
                return false;
            }
            if (b.ParticipantsRemaining != b.ParticipantCount)
            {
                TestHarness.TestLog("SignalAndWait failed, ParticipantsRemaming does not equal the total participants");
                return false;
            }
            TestHarness.TestLog("SignalAndWait succeeded");
            return true;

        }

        /// <summary>
        /// Test AddParticipants
        /// </summary>
        /// <param name="initialCount">The initial barrier participants count</param>
        /// <param name="participantsToAdd">The aprticipants that will be added</param>
        /// <param name="exceptionType">Type of the exception in case of invalid input, null for valid cases</param>
        /// <returns>Tru if the test succeeded, false otherwise</returns>
        private static bool RunBarrierTest4_AddParticipants(int initialCount, int participantsToAdd, Type exceptionType)
        {
            TestHarness.TestLog("AddParticipants(" + initialCount + "," + participantsToAdd + ")");
            Barrier b = new Barrier(initialCount);
            Exception exception = null;
            try
            {
                if (b.AddParticipants(participantsToAdd) != 0)
                {
                    TestHarness.TestLog("AddParticipants failed, the return phase count is not correct");
                    return false;
                }
                if (b.ParticipantCount != initialCount + participantsToAdd)
                {
                    TestHarness.TestLog("AddParticipants failed, total participant was not increased");
                    return false;
                }

            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception != null && exceptionType == null)
            {
                TestHarness.TestLog("AddParticipants failed, unexpected exception has been thrown.");
                return false;
            }
            if (exception != null && !Type.Equals(exceptionType, exception.GetType()))
            {
                TestHarness.TestLog("AddParticipants failed, exceptions types do not match.");
                return false;
            }
            TestHarness.TestLog("AddParticipants succeeded");
            return true;
        }

        /// <summary>
        /// Test RemoveParticipants
        /// </summary>
        /// <param name="initialCount">The initial barrier participants count</param>
        /// <param name="participantsToRemove">The aprticipants that will be added</param>
        /// <param name="exceptionType">Type of the exception in case of invalid input, null for valid cases</param>
        /// <returns>Tru if the test succeeded, false otherwise</returns>
        private static bool RunBarrierTest5_RemoveParticipants(int initialCount, int participantsToRemove, Type exceptionType)
        {
            TestHarness.TestLog("RemoveParticipants(" + initialCount + "," + participantsToRemove + ")");
            Barrier b = new Barrier(initialCount);
            Exception exception = null;
            try
            {
                b.RemoveParticipants(participantsToRemove);
                if (b.ParticipantCount != initialCount - participantsToRemove)
                {
                    TestHarness.TestLog("RemoveParticipants failed, total participant was not decreased");
                    return false;
                }

            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception != null && exceptionType == null)
            {
                TestHarness.TestLog("RemoveParticipants failed, unexpected exception has been thrown.");
                return false;
            }
            if (exception != null && !Type.Equals(exceptionType, exception.GetType()))
            {
                TestHarness.TestLog("RemoveParticipants failed, exceptions types do not match.");
                return false;
            }
            TestHarness.TestLog("RemoveParticipants succeeded");
            return true;
        }

        /// <summary>
        /// Test Dispose
        /// </summary>
        /// <returns>Tru if the test succeeded, false otherwise</returns>
        private static bool RunBarrierTest6_Dispose()
        {
            TestHarness.TestLog("*RunBarrierTest6_Dispose");
            Barrier b = new Barrier(1);
            b.Dispose();
            try
            {
                b.SignalAndWait();
            }
            catch (ObjectDisposedException)
            {
                TestHarness.TestLog("Dispose succeeded");
                return true;
            }
            TestHarness.TestLog("Cancel failed, SignalAndWait didn't throw exceptions after Dispose.");
            return false;
        }

       
        private static bool RunBarrierTest7_Bug603035()
        {
            TestHarness.TestLog("*RunBarrierTest7_Bug603035");
            bool failed = false;
            for (int j = 0; j < 100 && !failed; j++)
            {
                Barrier b = new Barrier(0);
               
                Action[] actions = Enumerable.Repeat((Action)(() =>
                {
                    for (int i = 0; i < 400; i++)
                    {
                        try
                        {
                            b.AddParticipant();
                            b.RemoveParticipant();
                        }
                        catch
                        {
                            failed = true;
                            break;
                        }
                    }

                }), 4).ToArray();
                Parallel.Invoke(actions);
                if (b.ParticipantCount != 0)
                    failed = true;
            }

            if (failed)
            {
                TestHarness.TestLog("Bug603035 failed");
                return false;
            }
            TestHarness.TestLog("Bug603035 succeeded");
            return true;

        }

        /// <summary>
        /// Test ithe case when the post phase action throws an exception
        /// </summary>
        /// <returns>Tru if the test succeeded, false otherwise</returns>
        private static bool RunBarrierTest8_PostPhaseException()
        {
            TestHarness.TestLog("*RunBarrierTest8_PostPhaseException");
            bool shouldThrow = true;
            int participants = 4;
            Barrier barrier = new Barrier(participants,(b) =>
                {
                    if (shouldThrow)
                        throw new InvalidOperationException();
                });
            int succeededCount = 0;


            // Run threads that will expect BarrierPostPhaseException when they call SignalAndWait, and increment the count in the catch block
            // The BarrierPostPhaseException inner exception should be the real excption thrown by the post phase action
            Thread[] threads = new Thread[participants];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(() =>
                    {
                        try
                        {
                            barrier.SignalAndWait();
                        }
                        catch (BarrierPostPhaseException ex)
                        {
                            if (ex.InnerException.GetType().Equals(typeof(InvalidOperationException)))
                                Interlocked.Increment(ref succeededCount);
                        }

                    });
                threads[i].Start();
            }

            foreach (Thread t in threads)
                t.Join();
            if (succeededCount != participants)
            {
                TestHarness.TestLog(">Failed, Not all participants threw the exception");
                return false;
            }
            // make sure the phase count is incremented
            if (barrier.CurrentPhaseNumber != 1)
            {
                TestHarness.TestLog(">Failed, Expected phase count = 1");
                return false;
            }

            // turn off the exception
            shouldThrow = false;

            // Now run the threads again and they shouldn't got the exception, decrement the count if it got the exception
            threads = new Thread[participants];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        barrier.SignalAndWait();
                    }
                    catch (BarrierPostPhaseException)
                    {
                        Interlocked.Decrement(ref succeededCount);
                    }

                   

                });

                threads[i].Start();
            }
            foreach (Thread t in threads)
                t.Join();
            if (succeededCount != participants)
            {
                TestHarness.TestLog(">Failed, At least one participant threw BarrierPostPhaseException.");
                return false;
            }

            TestHarness.TestLog(">Succeeded");
            return true;


        }


        /// <summary>
        /// Test ithe case when the post phase action throws an exception
        /// </summary>
        /// <returns>Tru if the test succeeded, false otherwise</returns>
        private static bool RunBarrierTest9_PostPhaseException()
        {
            TestHarness.TestLog("*RunBarrierTest9_PostPhaseException");
            Barrier barrier = new Barrier(1, (b) => b.SignalAndWait());
            if (!EnsurePostPhaseThrew(barrier))
                return false;

            barrier = new Barrier(1, (b) => b.Dispose());
            if (!EnsurePostPhaseThrew(barrier))
                return false;

            barrier = new Barrier(1, (b) => b.AddParticipant());
            if (!EnsurePostPhaseThrew(barrier))
                return false;

            barrier = new Barrier(1, (b) => b.RemoveParticipant());
            if (!EnsurePostPhaseThrew(barrier))
                return false;

            return true;
           
        }

        /// <summary>
        /// Ensures the post phase action throws if Dispose,SignalAndWait and Add/Remove participants called from it.
        /// </summary>
        private static bool EnsurePostPhaseThrew(Barrier barrier)
        {
            try
            {
                barrier.SignalAndWait();
                TestHarness.TestLog("PostPhaseException failed, Postphase action didn't throw.");
                return false;
            }
            catch (BarrierPostPhaseException ex)
            {
                if (!ex.InnerException.GetType().Equals(typeof(InvalidOperationException)))
                {
                    TestHarness.TestLog("PostPhaseException failed, Postphase action didn't throw the correct exception.");
                    return false;
                }
            }

            return true;
        }

    }
}
