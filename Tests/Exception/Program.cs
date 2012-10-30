using System;

namespace Microsoft.LiveLabs.JavaScript.Tests
{
    class MyException : Exception { }

    class MyOtherException : Exception { }

    static class TestException
    {

        static void Main()
        {
            TestMultipleCatches();
            TestMultipleFinalies();
            TestEscapes();
            TestNotSupported();
        }

        private static void TestNotSupported()
        {
            TestLogger.Log("Testing NotSupported...");
            try
            {
                throw new NotSupportedException();
            }
            catch (NotSupportedException)
            {
                TestLogger.Log("Caught not supported exception");
            }
        }

        static void TestMultipleFinalies()
        {
            TestLogger.Log("Testing finaly...");
            while (true)
            {
                try
                {
                    TestLogger.Log(1);
                    try
                    {
                        TestLogger.Log(2);
                        try
                        {
                            TestLogger.Log(3);
                            if (Int32.Parse("1") == 1)
                                break;
                        }
                        finally
                        {
                            TestLogger.Log(4);
                        }
                        TestLogger.Log(4.5);
                    }
                    finally
                    {
                        TestLogger.Log(5);
                    }
                    TestLogger.Log(5.5);
                }
                finally
                {
                    TestLogger.Log(6);
                }
                TestLogger.Log(6.5);
            }
        }

        static void TestMultipleCatches()
        {
            TestLogger.Log("Testing catch...");
            try
            {
                TestLogger.Log("1");
                TestThrow();
                TestLogger.Log("2");
            }
            catch (MyException)
            {
                TestLogger.Log("3");
            }
            catch
            {
                TestLogger.Log("4");
            }
        }

        static void TestThrow()
        {
            throw new MyException();
        }


        static void TestEscapes()
        {
            TestLogger.Log("Testing escaping...");
            try
            {
                TestThrowEscapeCatch();
            }
            catch (MyException)
            {
                TestLogger.Log("Caught MyException (expected).");
            }

            try
            {
                TestThrowEscapeFinally();
            }
            catch (MyException)
            {
                TestLogger.Log("Caught MyException (expected).");
            }
        }

        static void TestThrowEscapeCatch()
        {
            try
            {
                throw new MyException();
            }
            catch (MyOtherException)
            {
                TestLogger.Log("Caught MyOtherException (unexpected)");
            }
        }

        static void TestThrowEscapeFinally()
        {
            try
            {
                throw new MyException();
            }
            finally
            {
                TestLogger.Log("finally");
            }
        }

    }
}