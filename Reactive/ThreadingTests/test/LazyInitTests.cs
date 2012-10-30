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
    /// <summary>The class that contains the unit tests of the Lazy.</summary>
    internal static class LazyTests
    {
        /// <summary>Run the Lazy unit tests.</summary>
        /// <returns>True if all tests succeeded, false otherwise.</returns>
        internal static bool RunLazyTests()
        {
            bool passed = true;

            passed &= RunLazyTest1_Ctor();
            passed &= RunLazyTest2_Serialization();
            passed &= RunLazyTest5_ToString();
            passed &= RunLazyTest6_IsValueCreated();
            passed &= RunLazyTest8_Value();
            passed &= RunLazyTest9_Exceptions();

            return passed;
        }

        /// <summary>Tests for the Ctor.</summary>
        /// <returns>True if the tests succeeds, false otherwise.</returns>
        private static bool RunLazyTest1_Ctor()
        {
            TestHarness.TestLog("* RunLazyTest1_Ctor()");
            try
            {
                new Lazy<object>();
            }
            catch
            {
                TestHarness.TestLog(" > test failed - un expected exception has been thrown.");
                return false;
            }

            try
            {
                new Lazy<object>(null);
                TestHarness.TestLog(" > test failed - expected exception ArgumentOutOfRangeException");
                return false;
            }
            catch (ArgumentNullException)
            {
            }

            return true;
        }

        /// <summary>Tests for the serialization functionality.</summary>
        /// <returns>True if the tests succeeds, false otherwise.</returns>
        private static bool RunLazyTest2_Serialization()
        {
            TestHarness.TestLog("* RunLazyTest2_Serialization()");

            BinaryFormatter binaryFmt = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();

            

            ms.Dispose();
            ms = null;

            Lazy<string> lazy;
            Lazy<string> deserializedLazy;

            TestHarness.TestLog("  - Trying to serialize a good Lazy<string>");
            ms = new MemoryStream();
            lazy = new Lazy<string>(() => "Lazy<T>", true);
            binaryFmt.Serialize(ms, lazy);
            ms.Position = 0;
            deserializedLazy = (Lazy<string>)binaryFmt.Deserialize(ms);

            ms.Dispose();
            ms = null;

            if (!deserializedLazy.IsValueCreated
                || deserializedLazy.Value != "Lazy<T>")
            {
                TestHarness.TestLog(" > test failed - the Lazy<T> object was deserialized incorrectly.");
                return false;
            }

            TestHarness.TestLog("  - Trying to serialize a null Lazy<string>");
            ms = new MemoryStream();
            lazy = new Lazy<string>(() => null, true);
            binaryFmt.Serialize(ms, lazy);
            ms.Position = 0;
            deserializedLazy = (Lazy<string>)binaryFmt.Deserialize(ms);

            ms.Dispose();
            ms = null;

            if (!deserializedLazy.IsValueCreated
                || deserializedLazy.Value != null)
            {
                TestHarness.TestLog(" > test failed - the Lazy<T> object was deserialized incorrectly.");
                return false;
            }

            TestHarness.TestLog("  - Trying to serialize a good Lazy<int>");
            ms = new MemoryStream();
            Lazy<int> lazyInt = new Lazy<int>(() => 33, true);
            binaryFmt.Serialize(ms, lazyInt);
            ms.Position = 0;
            Lazy<int> deserializedLazyInt = (Lazy<int>)binaryFmt.Deserialize(ms);

            ms.Dispose();
            ms = null;

            if (!deserializedLazyInt.IsValueCreated
                || deserializedLazyInt.Value != 33)
            {
                TestHarness.TestLog(" > test failed - the Lazy<T> object was deserialized incorrectly.");
                return false;
            }

            return true;
        }

        /// <summary>Tests for the ToString.</summary>
        /// <returns>True if the tests succeeds, false otherwise.</returns>
        private static bool RunLazyTest5_ToString()
        {
            TestHarness.TestLog("* RunLazyTest5_ToString()");
            Lazy<object> lazy = new Lazy<object>(() => (object)1);
            if (lazy.ToString() == 1.ToString())
            {
                TestHarness.TestLog(" > test failed - Unexpected return value from ToString(); Actual={0}, Expected={1}.", lazy.ToString(), 1.ToString());
                return false;
            }
            if (lazy.IsValueCreated)
            {
                TestHarness.TestLog(" > test failed - ToString shouldn't force allocation");
                return false;
            }

            object tmp = lazy.Value;
            if (lazy.ToString() != 1.ToString())
            {
                TestHarness.TestLog(" > test failed - Unexpected return value from ToString(); Actual={0}, Expected={1}.", lazy.ToString(), 1.ToString());
                return false;
            }

            return true;
        }

        /// <summary>Tests for the Initialized property.</summary>
        /// <returns>True if the tests succeeds, false otherwise.</returns>
        private static bool RunLazyTest6_IsValueCreated()
        {
            TestHarness.TestLog("* RunLazyTest6_Initialized()");
            Lazy<string> lazy = new Lazy<string>(() => "Test");
            if (lazy.IsValueCreated)
            {
                TestHarness.TestLog(" > test failed - expected lazy to be uninitialized.");
                return false;
            }
            string temp = lazy.Value;
            if (!lazy.IsValueCreated)
            {
                TestHarness.TestLog(" > test failed - expected lazy to be initialized.");
                return false;
            }

            return true;
        }


        /// <summary>Tests for the Value property.</summary>
        /// <returns>True if the tests succeeds, false otherwise.</returns>
        private static bool RunLazyTest8_Value()
        {
            TestHarness.TestLog("* RunLazyTest8_Value()");

            string value = "Test";
            Lazy<string> lazy = new Lazy<string>(() => value);
            string lazilyAllocatedValue = lazy.Value;
            if (lazilyAllocatedValue != value)
            {
                TestHarness.TestLog(" > test failed - unexpected lazy.Value;Actual={0}, Expected={1}", lazilyAllocatedValue, value);
                return false;
            }

            int valueInt = 99;
            Lazy<int> LazyInt = new Lazy<int>(() => valueInt);
            int lazilyAllocatedValueInt = LazyInt.Value;
            if (lazilyAllocatedValueInt != valueInt)
            {
                TestHarness.TestLog(" > test failed - unexpected lazy.Value;Actual={0}, Expected={1}", lazilyAllocatedValueInt, valueInt);
                return false;
            }

            lazy = new Lazy<string>(() => value, true);
            lazilyAllocatedValue = lazy.Value;
            if (lazilyAllocatedValue != value)
            {
                TestHarness.TestLog(" > test failed - unexpected lazy.Value;Actual={0}, Expected={1}", lazilyAllocatedValue, value);
                return false;
            }

           

            lazy = new Lazy<string>(() => null, false);
            lazilyAllocatedValue = lazy.Value;
            if (lazilyAllocatedValue != null)
            {
                TestHarness.TestLog(" > test failed - unexpected lazy.Value;Actual={0}, Expected=null", lazilyAllocatedValue);
                return false;
            }

            try
            {
                int x = 0;
                lazy = new Lazy<string>(delegate
                {
                    if (x++ < 5)
                        return lazy.Value;
                    else
                        return "Test";
                }, true);
                lazilyAllocatedValue = lazy.Value;
                TestHarness.TestLog(" > test failed - expected exception InvalidOperationException");
                return false;
            }
            catch (InvalidOperationException)
            {
            }

            try
            {
                lazy = new Lazy<string>();
                lazilyAllocatedValue = lazy.Value;
                TestHarness.TestLog(" > test failed - expected exception MissingMemberException");
                return false;
            }
            catch (MissingMemberException)
            {
            }


            return true;
        }

        static int m_exceptionCounter = 0;
         /// <summary>Tests for the Value factory throws an exception.</summary>
        /// <returns>True if the tests succeeds, false otherwise.</returns>
        private static bool RunLazyTest9_Exceptions()
        {
            TestHarness.TestLog("* RunLazyTest9_Exceptions()");
            int counter = m_exceptionCounter;
            Lazy<string> l = new Lazy<string>(() =>
                {
                    m_exceptionCounter++;
                    int zero = 0;
                    int x = 1 / zero;
                    return "";
                }, true);
            string s;
            if (!TestHarnessAssert.EnsureExceptionThrown(() => s = l.Value, typeof(DivideByZeroException), "Expected DivideByZeroException"))
            {
                TestHarness.TestLog("failed");
                return false;
            }
            if (!TestHarnessAssert.EnsureExceptionThrown(() => s = l.Value, typeof(DivideByZeroException), "Expected DivideByZeroException"))
            {
                TestHarness.TestLog("failed");
                return false;
            }
            if (!TestHarnessAssert.AreEqual<int>(counter + 1, m_exceptionCounter, "value factory has been called twise and it should be called only once."))
            {
                TestHarness.TestLog("failed");
                return false;
            }

            if (l.IsValueCreated)
            {
                TestHarness.TestLog("failed* IsValueCreated should return false.");
                return false;
            }

            return true;

        }


       
       
        class HasDefaultCtor { }

        class NoDefaultCtor
        {
            public NoDefaultCtor(int x) { }
        }

        private static bool RunLazyInitializer_SimpleRef()
        {
            TestHarness.TestLog("* RunLazyInitializer_SimpleRef()");

            HasDefaultCtor hdcTemplate = new HasDefaultCtor();
            string strTemplate = "foo";

            //
            // Simple overloads (ref types).
            //

            HasDefaultCtor a = null;
            HasDefaultCtor b = hdcTemplate;
            string c = null;
            string d = strTemplate;
            string e = null;

            // Activator.CreateInstance (uninitialized).
            if (LazyInitializer.EnsureInitialized<HasDefaultCtor>(ref a) == null)
            {
                TestHarness.TestLog(" > EnsureInitialized(ref a) == null");
                return false;
            }
            else if (a == null)
            {
                TestHarness.TestLog(" > the value of a == null after a call to EnsureInitialized(ref a)");
                return false;
            }

            // Activator.CreateInstance (already initialized).
            if (LazyInitializer.EnsureInitialized<HasDefaultCtor>(ref b) != hdcTemplate)
            {
                TestHarness.TestLog(" > EnsureInitialized(ref b) != hdcTemplate -- already initialized, should have been unchanged");
                return false;
            }
            else if (b != hdcTemplate)
            {
                TestHarness.TestLog(" > the value of b != hdcTemplate (" + b + ") after a call to EnsureInitialized(ref b)");
                return false;
            }

            // Func based initialization (uninitialized).
            if (LazyInitializer.EnsureInitialized<string>(ref c, () => strTemplate) != strTemplate)
            {
                TestHarness.TestLog(" > EnsureInitialized(ref c, ...) != strTemplate");
                return false;
            }
            else if (c != strTemplate)
            {
                TestHarness.TestLog(" > the value of c != strTemplate (" + c + ") after a call to EnsureInitialized(ref c, ..)");
                return false;
            }

            // Func based initialization (already initialized).
            if (LazyInitializer.EnsureInitialized<string>(ref d, () => strTemplate + "bar") != strTemplate)
            {
                TestHarness.TestLog(" > EnsureInitialized(ref c, ...) != strTemplate -- already initialized, should have been unchanged");
                return false;
            }
            else if (d != strTemplate)
            {
                TestHarness.TestLog(" > the value of c != strTemplate (" + d + ") after a call to EnsureInitialized(ref d, ..)");
                return false;
            }

            // Func based initialization (nulls not permitted).
            try
            {
                LazyInitializer.EnsureInitialized<string>(ref e, () => null);
                TestHarness.TestLog(" > EnsureInitialized(ref e, () => null) should have thrown an exception");
                return false;
            }
            catch (InvalidOperationException)
            {
            }

            // Activator.CreateInstance (for a type w/out a default ctor).
            NoDefaultCtor ndc = null;
            try
            {
                LazyInitializer.EnsureInitialized<NoDefaultCtor>(ref ndc);
                TestHarness.TestLog(" > EnsureInitialized(ref ndc) should have thrown an exception - no default ctor");
                return false;
            }
            catch (MissingMemberException)
            {
            }

            return true;
        }

        private static bool RunLazyInitializer_ComplexRef()
        {
            TestHarness.TestLog("* RunLazyInitializer_ComplexRef()");

            string strTemplate = "foo";
            HasDefaultCtor hdcTemplate = new HasDefaultCtor();

            //
            // Complicated overloads (ref types).
            //

            HasDefaultCtor a = null; bool ainit = false; object alock = null;
            HasDefaultCtor b = hdcTemplate; bool binit = true; object block = null;
            string c = null; bool cinit = false; object clock = null;
            string d = strTemplate; bool dinit = true; object dlock = null;
            string e = null; bool einit = false; object elock = null;

            // Activator.CreateInstance (uninitialized).
            if (LazyInitializer.EnsureInitialized<HasDefaultCtor>(ref a, ref ainit, ref alock) == null)
            {
                TestHarness.TestLog(" > EnsureInitialized(ref a) == null");
                return false;
            }
            else if (a == null)
            {
                TestHarness.TestLog(" > the value of a == null after a call to EnsureInitialized(ref a)");
                return false;
            }

            // Activator.CreateInstance (already initialized).
            if (LazyInitializer.EnsureInitialized<HasDefaultCtor>(ref b, ref binit, ref block) != hdcTemplate)
            {
                TestHarness.TestLog(" > EnsureInitialized(ref b) != hdcTemplate -- already initialized, should have been unchanged");
                return false;
            }
            else if (b != hdcTemplate)
            {
                TestHarness.TestLog(" > the value of b != hdcTemplate (" + b + ") after a call to EnsureInitialized(ref b)");
                return false;
            }

            // Func based initialization (uninitialized).
            if (LazyInitializer.EnsureInitialized<string>(ref c, ref cinit, ref clock, () => strTemplate) != strTemplate)
            {
                TestHarness.TestLog(" > EnsureInitialized(ref c, ...) != strTemplate");
                return false;
            }
            else if (c != strTemplate)
            {
                TestHarness.TestLog(" > the value of c != strTemplate (" + c + ") after a call to EnsureInitialized(ref c, ..)");
                return false;
            }

            // Func based initialization (already initialized).
            if (LazyInitializer.EnsureInitialized<string>(ref d, ref dinit, ref dlock, () => strTemplate + "bar") != strTemplate)
            {
                TestHarness.TestLog(" > EnsureInitialized(ref c, ...) != strTemplate -- already initialized, should have been unchanged");
                return false;
            }
            else if (d != strTemplate)
            {
                TestHarness.TestLog(" > the value of c != strTemplate (" + d + ") after a call to EnsureInitialized(ref d, ..)");
                return false;
            }

            // Func based initialization (nulls *ARE* permitted).
            int runs = 0;
            if (LazyInitializer.EnsureInitialized<string>(ref e, ref einit, ref elock, () => { runs++; return null; }) != null)
            {
                TestHarness.TestLog(" > EnsureInitialized(ref e, ...) != null");
                return false;
            }
            else if (e != null)
            {
                TestHarness.TestLog(" > the value of e != null (" + d + ") after a call to EnsureInitialized(ref d, ..)");
                return false;
            }
            else if (LazyInitializer.EnsureInitialized<string>(ref e, ref einit, ref elock, () => { runs++; return null; }) != null || runs > 1)
            {
                TestHarness.TestLog(" > erroneously ran the initialization routine twice... " + runs);
                return false;
            }

            // Activator.CreateInstance (for a type w/out a default ctor).
            NoDefaultCtor ndc = null; bool ndcinit = false; object ndclock = null;
            try
            {
                LazyInitializer.EnsureInitialized<NoDefaultCtor>(ref ndc, ref ndcinit, ref ndclock);
                TestHarness.TestLog(" > EnsureInitialized(ref ndc) should have thrown an exception - no default ctor");
                return false;
            }
            catch (MissingMemberException)
            {
            }

            return true;
        }

        struct LIX
        {
            internal int f;
            public LIX(int f) { this.f = f; }
            public override bool Equals(object other) { return other is LIX && ((LIX)other).f == f; }
            public override int GetHashCode() { return f.GetHashCode(); }
            public override string ToString() { return "LIX<" + f + ">"; }
        }

        private static bool RunLazyInitializer_ComplexVal()
        {
            TestHarness.TestLog("* RunLazyInitializer_ComplexVal()");

            LIX empty = new LIX();
            LIX template = new LIX(33);

            //
            // Complicated overloads (value types).
            //

            LIX a = default(LIX); bool ainit = false; object alock = null;
            LIX b = template; bool binit = true; object block = null;
            LIX c = default(LIX); bool cinit = false; object clock = null;
            LIX d = template; bool dinit = true; object dlock = null;

            // Activator.CreateInstance (uninitialized).
            if (!LazyInitializer.EnsureInitialized<LIX>(ref a, ref ainit, ref alock).Equals(empty))
            {
                TestHarness.TestLog(" > EnsureInitialized(ref a) != empty");
                return false;
            }
            else if (!a.Equals(empty))
            {
                TestHarness.TestLog(" > the value of a != empty (" + a + ") after a call to EnsureInitialized(ref a)");
                return false;
            }

            // Activator.CreateInstance (already initialized).
            if (!LazyInitializer.EnsureInitialized<LIX>(ref b, ref binit, ref block).Equals(template))
            {
                TestHarness.TestLog(" > EnsureInitialized(ref b) != template -- already initialized, should have been unchanged");
                return false;
            }
            else if (!b.Equals(template))
            {
                TestHarness.TestLog(" > the value of b != template (" + b + ") after a call to EnsureInitialized(ref b)");
                return false;
            }

            // Func based initialization (uninitialized).
            if (!LazyInitializer.EnsureInitialized<LIX>(ref c, ref cinit, ref clock, () => template).Equals(template))
            {
                TestHarness.TestLog(" > EnsureInitialized(ref c, ...) != template");
                return false;
            }
            else if (!c.Equals(template))
            {
                TestHarness.TestLog(" > the value of c != template (" + c + ") after a call to EnsureInitialized(ref c, ..)");
                return false;
            }

            // Func based initialization (already initialized).
            LIX template2 = new LIX(template.f*2);
            if (!LazyInitializer.EnsureInitialized<LIX>(ref d, ref dinit, ref dlock, () => template2).Equals(template))
            {
                TestHarness.TestLog(" > EnsureInitialized(ref c, ...) != template -- already initialized, should have been unchanged");
                return false;
            }
            else if (!d.Equals(template))
            {
                TestHarness.TestLog(" > the value of d != template (" + d + ") after a call to EnsureInitialized(ref d, ..)");
                return false;
            }

            return true;
        }



    }
}
