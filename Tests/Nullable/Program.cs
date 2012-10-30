using System;

namespace Microsoft.LiveLabs.JavaScript.Tests
{
    static class TestNullable
    {
        static void Main()
        {
            {
                TestLogger.Log("Testing non-null");
                var n = (int?)42;
                LogNullable(n);
            }

            {
                TestLogger.Log("Testing null");
                var n = default(int?);
                LogNullable(n);
            }

            {
                TestLogger.Log("Testing lifting with non-null args");
                var n1 = (int?)1;
                var n2 = (int?)2;
                LogNullable(n1 + n2);
            }

            {
                TestLogger.Log("Testing lifting with null args");
                var n1 = (int?)1;
                var n2 = default(int?);
                LogNullable(n1 + n2);
            }

        }

        static void LogNullable<T>(Nullable<T> nt) where T : struct
        {
            if (nt.HasValue)
                TestLogger.Log("Nullable has value " + nt.Value.ToString());
            else
                TestLogger.Log("Nullable has no value");
            if (nt == null)
                TestLogger.Log("Nullable equals null");
            var o = (object)nt;
            if (o == null)
                TestLogger.Log("Nullable casts to null object");
            else
                TestLogger.Log("Nullable casts to object " + o.ToString());
            var nt2 = (Nullable<T>)o;
            if (nt.HasValue)
                TestLogger.Log("Recast nullable has value " + nt.Value.ToString());
            else
                TestLogger.Log("Recast nullable has no value");
            try
            {
                var t1 = (T)nt;
                TestLogger.Log("Nullable casts to underlying value " + t1.ToString());
            }
            catch (Exception e)
            {
                TestLogger.Log("Nullable fails casting to underlying value");
                TestLogger.LogException(e);
            }
            var t2 = nt ?? default(T);
            TestLogger.Log("Value or default is " + t2.ToString());
        }
    }
}