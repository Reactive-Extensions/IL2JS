using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Runtime.Serialization;
using System.Security;
using System.Collections;

namespace plinq_devtests
{
    /// <summary>The class that contains the unit tests of the ThreadLocal.</summary>
    internal static class ThreadLocalTests
    {
        /// <summary>Run the ThreadLocal unit tests.</summary>
        /// <returns>True if all tests succeeded, false otherwise.</returns>
        internal static bool RunThreadLocalTests()
        {
            bool passed = true;

            passed &= RunThreadLocalTest1_Ctor();
            passed &= RunThreadLocalTest2_ToString();
            passed &= RunThreadLocalTest3_IsValueCreated();
            passed &= RunThreadLocalTest4_Value();
            passed &= RunThreadLocalTest5_Dispose();
            passed &= RunThreadLocalTest6_SlowPath();
          


            return passed;
        }

        /// <summary>Tests for the Ctor.</summary>
        /// <returns>True if the tests succeeds, false otherwise.</returns>
        private static bool RunThreadLocalTest1_Ctor()
        {
            TestHarness.TestLog("* RunThreadLocalTest1_Ctor()");
            try
            {
                new ThreadLocal<object>();
            }
            catch
            {
                TestHarness.TestLog(" > test failed - un expected exception has been thrown.");
                return false;
            }

            try
            {
                new ThreadLocal<object>(() => new object());
              
            }
            catch 
            {
                TestHarness.TestLog(" > test failed - un expected exception has been thrown.");
                return false;
            }

            return true;
        }

        /// <summary>Tests for the ToString.</summary>
        /// <returns>True if the tests succeeds, false otherwise.</returns>
        private static bool RunThreadLocalTest2_ToString()
        {
            TestHarness.TestLog("* RunThreadLocalTest2_ToString()");
            ThreadLocal<object> tlocal = new ThreadLocal<object>(() => (object)1);
            if (tlocal.ToString() != 1.ToString())
            {
                TestHarness.TestLog(" > test failed - Unexpected return value from ToString(); Actual={0}, Expected={1}.", tlocal.ToString(), 1.ToString());
                return false;
            }

            return true;
        }

        /// <summary>Tests for the Initialized property.</summary>
        /// <returns>True if the tests succeeds, false otherwise.</returns>
        private static bool RunThreadLocalTest3_IsValueCreated()
        {
            TestHarness.TestLog("* RunThreadLocalTest6_Initialized()");
            ThreadLocal<string> tlocal = new ThreadLocal<string>(() => "Test");
            if (tlocal.IsValueCreated)
            {
                TestHarness.TestLog(" > test failed - expected ThreadLocal to be uninitialized.");
                return false;
            }
            string temp = tlocal.Value;
            if (!tlocal.IsValueCreated)
            {
                TestHarness.TestLog(" > test failed - expected ThreadLocal to be initialized.");
                return false;
            }
            return true;
        }

        private static bool RunThreadLocalTest4_Value()
        {
            TestHarness.TestLog("* RunThreadLocalTest4_Value()");
            ThreadLocal<string> tlocal = null;
            try
            {
                int x = 0;
                tlocal = new ThreadLocal<string>(delegate
                {
                    if (x++ < 5)
                        return tlocal.Value;
                    else
                        return "Test";
                });
                string str  = tlocal.Value;
                TestHarness.TestLog(" > test failed - expected exception InvalidOperationException");
                return false;
            }
            catch (InvalidOperationException)
            {
            }

            
            // different threads call Value
            int numOfThreads = 10;
            Thread[] threads = new Thread[numOfThreads];
            ArrayList seenValuesFromAllThreads = new ArrayList();
            int counter = 0;
            tlocal = new ThreadLocal<string>(() => (++counter).ToString());
            for (int i = 0; i < threads.Length; ++i)
            {
                threads[i] = new Thread(()=>
                {
                    string value = tlocal.Value;
                    seenValuesFromAllThreads.Add(value);
                });
                threads[i].Start();
                threads[i].Join();
            }
            bool successful = true;
            string values = "";
            for (int i = 1; i <= threads.Length; ++i)
            {
                string seenValue = (string)seenValuesFromAllThreads[i - 1];
                values += seenValue + ",";
                if (seenValue != i.ToString())
                {
                    successful = false;
                }
            }

            if (!successful)
            {
                TestHarness.TestLog(" > test failed - ThreadLocal test failed. Seen values are: " + values.Substring(0, values.Length - 1));
            }

            return successful;
        }


        private static bool RunThreadLocalTest5_Dispose()
        {
            TestHarness.TestLog("* RunThreadLocalTest5_Dispose()");

            ThreadLocal<string> tl = new ThreadLocal<string>(() => "dispose test");
            string value = tl.Value;

            tl.Dispose();

            if (!TestHarnessAssert.EnsureExceptionThrown(() => { string tmp = tl.Value; }, typeof(ObjectDisposedException), "The Value property of the disposed ThreadLocal object should throw ODE"))
                return false;

            if (!TestHarnessAssert.EnsureExceptionThrown(() => { bool tmp = tl.IsValueCreated; }, typeof(ObjectDisposedException), "The IsValueCreated property of the disposed ThreadLocal object should throw ODE"))
                return false;

            if (!TestHarnessAssert.EnsureExceptionThrown(() => { string tmp = tl.ToString(); }, typeof(ObjectDisposedException), "The ToString method of the disposed ThreadLocal object should throw ODE"))
                return false;


            // test recycling the combination index;

            tl = new ThreadLocal<string>(() => null);
            if(tl.IsValueCreated)
            {
                TestHarness.TestLog("* Filed, reusing the same index kept the old value and didn't use the new value.");
                return false;
            }
            if (tl.Value != null)
            {
                TestHarness.TestLog("* Filed, reusing the same index kept the old value and didn't use the new value.");
                return false;
            }


            return true;
        }

        private static bool RunThreadLocalTest6_SlowPath()
        {
            TestHarness.TestLog("* RunThreadLocalTest6_SlowPath()");

            TestHarness.TestLog("* Testing SlowPath per type");
            // the maximum fast path instances for each type is 16 ^ 3 = 4096, when this number changes in the product code, it should be changed here as well
            int MaximumFastPathPerInstance = 4096;
            ThreadLocal<int>[] locals_int = new ThreadLocal<int>[MaximumFastPathPerInstance + 10 ];
            for (int i = 0; i < locals_int.Length; i++)
            {
                locals_int[i] = new ThreadLocal<int>(() => i);
                var val = locals_int[i].Value;
            }

            for (int i = 0; i < locals_int.Length; i++)
            {
                if (locals_int[i].Value != i)
                {
                    TestHarness.TestLog("* Filed, Slowpath value failed, expected {0}, actual {1}.",i, locals_int[i].Value);
                    return false;
                }
            }

            TestHarness.TestLog("* Testing SlowPath for all types");
            // The maximum slowpath for all Ts is MaximumFastPathPerInstance * 4;
            locals_int = new ThreadLocal<int>[4096];
            ThreadLocal<long>[] locals_long = new ThreadLocal<long>[4096];
            ThreadLocal<float>[] locals_float = new ThreadLocal<float>[4096];
            ThreadLocal<double>[] locals_double = new ThreadLocal<double>[4096];
            for (int i = 0; i < locals_int.Length; i++)
            {
                locals_int[i] = new ThreadLocal<int>(() => i);
                locals_long[i] = new ThreadLocal<long>(() => i);
                locals_float[i] = new ThreadLocal<float>(() => i);
                locals_double[i] = new ThreadLocal<double>(() => i);
            }

            ThreadLocal<string> local = new ThreadLocal<string>(() => "slow path");
            if (local.Value != "slow path")
            {
                TestHarness.TestLog("* Filed, Slowpath value failed, expected slow path, actual {0}.", local.Value);
                return false;
            }

            return true;


        }



    }
}