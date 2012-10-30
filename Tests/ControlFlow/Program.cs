namespace Microsoft.LiveLabs.JavaScript.Tests
{
    static public class TestControlFlow
    {
        static void Main()
        {
            TestIfThen(3);
            TestIfThen(7);
            TestIfThenElse(3);
            TestIfThenElse(7);
            TestSwitch(3);
            TestSwitch(5);
            TestSwitch(7);
            TestFor(3);
            TestWhile(3);
            TestDoWhile(3);
            TestRefs(3, 7);
            TestTernaryPure(3, 7);
            TestTernaryPure(7, 3);
            TestTernarySideEffects(3, 7);
            TestTernarySideEffects(7, 3);
            TestIs(new B());
            TestIs(new C());
            TestIs(new D());
        }

        static void TestIfThen(int arg)
        {
            TestLogger.Log("Testing if-then...");
            if (arg > 5)
            {
                TestLogger.Log("then");
            }
            TestLogger.Log("after");
        }


        static void TestIfThenElse(int arg)
        {
            TestLogger.Log("Testing if-then-else...");
            if (arg > 5)
            {
                TestLogger.Log("then");
            }
            else
            {
                TestLogger.Log("else");
            }
            TestLogger.Log("after");
        }


        static void TestSwitch(int arg)
        {
            TestLogger.Log("Testing switch...");
            switch (arg)
            {
                case 2:
                    TestLogger.Log("2");
                    break;
                case 3:
                    TestLogger.Log("3");
                    return;
                case 4:
                case 5:
                    TestLogger.Log("4 or 5");
                    break;
                default:
                    TestLogger.Log("default");
                    break;
            }
            TestLogger.Log("after");
        }

        static void TestFor(int arg)
        {
            TestLogger.Log("Testing for...");
            for (var i = 0; i < arg; i++)
                TestLogger.Log(i);
            TestLogger.Log("after");
        }

        static void TestWhile(int arg)
        {
            TestLogger.Log("Testing while...");
            var i = 0;
            while (i < arg)
            {
                TestLogger.Log(i++);
            }
            TestLogger.Log("after");
        }

        static void TestDoWhile(int arg)
        {
            TestLogger.Log("Testing do-while...");
            var i = 0;
            do
            {
                TestLogger.Log(i++);
            } while (i < arg);
            TestLogger.Log("after");
        }

        static void TestRefs(int a, int b)
        {
            TestLogger.Log("Testing refs...");

            TestLogger.Log("a = " + a);
            TestLogger.Log("b = " + b);

            Swap(ref a, ref b);

            TestLogger.Log("a = " + a);
            TestLogger.Log("b = " + b);
        }

        static void Swap(ref int a, ref int b)
        {
            var t = a;
            a = b;
            b = t;
        }

        static void TestTernaryPure(int x, int y)
        {
            TestLogger.Log("Testing ternary ?: operator without side-effects...");
            TestLogger.Log("cond1: " + (x > 7 ? ">" : "<=") +
                ", cond2: " + (y > 3 ? ">" : "<=") +
                ", cond3: " + (x > 3 ? (y > 7 ? "> >" : "> <=") : (y > 7 ? "<= >" : "<= <=")));
        }

        static void TestTernarySideEffects(int x, int y)
        {
            TestLogger.Log("Testing ternary ?: operator with side-effects...");
            TestLogger.Log("x = " + x + ", y = " + y);
            TestLogger.Log("cond1: " + (x++ > 7 ? ">" : "<=") +
                ", cond2: " + (y-- > 3 ? ">" : "<=") +
                ", cond3: " + (++x > 3 ? (--y > 7 ? "> >" : "> <=") : (--y > 7 ? "<= >" : "<= <=")));
            TestLogger.Log("x = " + x + ", y = " + y);
        }

        static void TestIs(A a)
        {
            TestLogger.Log("Testing is in conditionals...");
            if (!(a is B) && !(a is C))
                TestLogger.Log("is a D");
            else
                TestLogger.Log("is not a D");
            if (a is B || a is C)
                TestLogger.Log("is not a D");
            else
                TestLogger.Log("is a D");
        }
    }

    public class A {}
    public class B : A {}
    public class C : A {}
    public class D : A {}

}