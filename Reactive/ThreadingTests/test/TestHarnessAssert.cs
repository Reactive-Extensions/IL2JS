using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

namespace plinq_devtests
{

    public static class OCEHelper
    {
        static MethodInfo s_tokenAccessor;
        static void InitializeTokenAccessor()
        {
            if (s_tokenAccessor != null) return;

            Assembly asm = Task.Factory.GetType().Assembly;
            Type type = null;
            foreach (Type t in asm.GetTypes())
                if (t.Name == "OperationCanceledException2")
                {
                    type = t;
                    break;
                }

            foreach (PropertyInfo pi in type.GetProperties())
                if (pi.Name == "CancellationToken")
                {
                    s_tokenAccessor = pi.GetAccessors()[0];
                    break;
                }
        }
#if PFX_LEGACY_3_5
        public static CancellationToken ExtractCT(OperationCanceledException oce)
        {
            InitializeTokenAccessor();
            Object result = s_tokenAccessor.Invoke(oce, null);
            return (CancellationToken)result;
        }
#else
        public static CancellationToken ExtractCT(OperationCanceledException oce)
        {
            return oce.CancellationToken;
        }
#endif

    }


    public static class TestHarnessAssert
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="comparand"></param>
        /// <param name="item"></param>
        /// <param name="message"></param>
        /// <returns>true if the assertion was true.</returns>
        public static bool AreEqual<T>(T comparand, T item, string message) 
        {
            if( ! item.Equals(comparand) )
            {
                TestHarness.TestLog("  > " + message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="comparand"></param>
        /// <param name="item"></param>
        /// <param name="message"></param>
        /// <returns>true if the assertion was true.</returns>
        public static bool AreNotEqual<T>(T comparand, T item, string message)
        {
            if (item.Equals(comparand))
            {
                TestHarness.TestLog("  > " + message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="message"></param>
        /// <returns>true if the assertion was true.</returns>
        public static bool IsNotNull<T>(T item, string message) where T:class {
            if (item == null)
            {
                TestHarness.TestLog("  > " + message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="message"></param>
        /// <returns>true if the assertion was true.</returns>
        public static bool IsNull<T>(T item, string message) where T : class
        {
            if (item != null)
            {
                TestHarness.TestLog("  > " + message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="message"></param>
        /// <returns>true if the assertion was true.</returns>
        public static bool IsTrue(bool predicate, string message)
        {
            if( !predicate )
            {
                TestHarness.TestLog("  > " + message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="message"></param>
        /// <returns>true if the assertion was true.</returns>
        public static bool IsFalse(bool predicate, string message)
        {
            
            if (predicate)
            {
                TestHarness.TestLog("  > " + message);
                return false;
            }

            return true;
        }

        public delegate void UntypedAction();


        public static bool EnsureOperationCanceledExceptionThrown(UntypedAction action, CancellationToken expectedCancellationTokenInException, string message)
        {
            OperationCanceledException exception = null;
            try
            {
                action();
            }
            catch (OperationCanceledException ex)
            {
                exception = ex;
            }

            if (exception == null)
            {
                TestHarness.TestLog("  > " + "OperationCanceledException was not thrown.  " + message);
                return false;
            }

#if !PFX_LEGACY_3_5

            if (exception.CancellationToken != expectedCancellationTokenInException)
            {
                TestHarness.TestLog("  > " + "CancellationToken does not match.  " + message);
                return false;
            }
#endif
            return true;

        }

        public static bool EnsureExceptionThrown(UntypedAction action, Type expectedExceptionType, string message)
        {
            Exception exception = null;
            try
            {
                action();
            }
            catch(Exception ex)
            {
                exception = ex;
            }

            if (exception == null || exception.GetType() != expectedExceptionType)
            {
                TestHarness.TestLog("  > " + message);
                return false;
            }
            else
            {
                return true;
            }
        }

        public static bool Fail(string message)
        {
            TestHarness.TestLog("  > " + message);
            return false;
        }
    }
}
