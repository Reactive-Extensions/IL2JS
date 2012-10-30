namespace Microsoft.LiveLabs.JavaScript.Tests
{
    static class TestRecursion
    {
        static void Main()
        {
            TestLogger.Log(F(9).ToString());
            TestLogger.Log(A(9).ToString());
        }

        static int F(int n)
        {
            return n == 0 ? 1 : n * F(n - 1);
        }

        static int A(int n)
        {
            return n == 0 ? 1 : B(n);
        }

        static int B(int n)
        {
            return n * A(n - 1);
        }
    }
}