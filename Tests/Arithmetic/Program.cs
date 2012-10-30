using System;

namespace Microsoft.LiveLabs.JavaScript.Tests
{
    internal static class TestArithmetic
    {
        public static void Main()
        {
            TestLogger.Log("Testing integer arithmetic...");
            var a = ReadInt("1");
            var b = ReadInt("2");
            var c = ReadInt("3");
            TestLogger.Log((a + (b * c)));
            TestLogger.Log((a - (b * c)));
            TestLogger.Log((((a + b) * (c + b)) * c));
            TestLogger.Log((-c * 4));
            TestLogger.Log(((c * c) / b));
            TestLogger.Log(((-c * c) / b));
            TestLogger.Log(((c * c) % b));
            TestLogger.Log(((-c * c) % b));
            TestLogger.Log(a < b);
            TestLogger.Log(a + b <= c);
            var i = 0;
            Unary(++i, ++i);
            Unary(i++, i++);
            TestLogger.Log(i);

            TestLogger.Log("Testing for loop...");
            for (var l = 3; l < 7; l++)
            {
                TestLogger.Log(l.ToString());
            }

            TestLogger.Log("Testing floating-point arithmetic...");
            var x = ReadDouble("1.5");
            var y = ReadDouble("2.0");
            var z = ReadDouble("3.0");
            TestLogger.Log((x * y));
            TestLogger.Log((z / x));
            TestLogger.Log((z / y));
            TestLogger.Log(x < y);
            TestLogger.Log(x + y <= z);

            TestLogger.Log("Calculating primes...");
            var primes = new Primes(10001);

            TestLogger.Log("Calculating pi...");
            DigitsOfPi.Emit(primes);
        }

        private static double ReadDouble(string number)
        {
            var val = double.Parse(number);
            TestLogger.Log(number + " => " + val);
            return val;
        }

        private static int ReadInt(string number)
        {
            var val = int.Parse(number);
            TestLogger.Log(number + " => " + val);
            return val;
        }

        private static void Unary(int a, int b)
        {
            TestLogger.Log(a + ", " + b);
        }
    }

    internal class Primes
    {
        private readonly int[] primes;

        public Primes(int n)
        {
            primes = new int[n];
            enumPrimes();
        }

        private void enumPrimes()
        {
            found(0, 1);
            found(1, 2);
            var candidate = 1;
            for (var i = 2; i < primes.Length; i++)
            {
                do
                    candidate += 2;
                while (!isPrime(candidate));
                found(i, candidate);
            }
        }

        private void found(int i, int prime)
        {
            primes[i] = prime;
            if (i > 0 && i % 1000 == 0)
                TestLogger.Log(prime + " is prime...");
        }

        private bool isPrime(int candidate)
        {
            var i = 1;
            while (true)
            {
                var p = primes[i];
                if (candidate < p * p)
                    return true;
                if (candidate % p == 0)
                    return false;
                i++;
            }
        }

        public int Count { get { return primes.Length; } }
        public int this[int i] { get { return primes[i]; } }
    }

    // Based on
    //   Simon Plouffe, "On the Computation of the n'th decimal digit of various transcendental numbers" (Nov 1996).
    // Written in C by by Fabrice Bellard (Jan 1997).

    internal static class DigitsOfPi
    {
        public static string knownDigits = "3.141592653589793238";

        public static void Emit(Primes primes)
        {
            TestLogger.Log("3.");
            var i = 2;
            while (true)
            {
                if (i + 9 > knownDigits.Length)
                {
                    TestLogger.Log("Ran out of known digits to check against");
                    return;
                }
                var digits = nineDigitsAt(primes, i - 1);
                if (digits == null)
                {
                    TestLogger.Log("Ran out of primes");
                    return;
                }
                if (digits != knownDigits.Substring(i, 9))
                    throw new InvalidOperationException("calculated digits do not agree with actual digits");
                i += 9;
                TestLogger.Log(digits);
            }
        }

        // a mod m
        private static double fmod(double a, double m)
        {
            var d = a / m;
            return (d - Math.Floor(d));
        }

        // a s.t. (a * x) mod m == 1 by extended Euclid
        private static int inv_mod(int x, int m)
        {
            var u = x;
            var v = m;
            var c = 1;
            var a = 0;
            do
            {
                var q = v / u;
                var t = c;
                c = a - (q * c);
                a = t;
                t = u;
                u = v - (q * u);
                v = t;
            }
            while (u != 0);
            a = a % m;
            if (a < 0)
                a = m + a;
            return a;
        }

        // (a * b) mod m
        private static int mul_mod(int a, int b, int m)
        {
            return (int)((a * b) % ((long)m));
        }

        // (a ^ b) mod m
        private static int pow_mod(int a, int b, int m)
        {
            var r = 1;
            var aa = a;
            while (true)
            {
                if ((b & 1) != 0)
                    r = mul_mod(r, aa, m);
                b = b >> 1;
                if (b == 0)
                    return r;
                aa = mul_mod(aa, aa, m);
            }
        }

        private static string nineDigitsAt(Primes primes, int n)
        {
            var N = (int)(((n + 20) * Math.Log(10.0)) / Math.Log(2.0));
            var sum = 0.0;
            var primeIndex = 2;

            while (true)
            {
                if (primeIndex >= primes.Count)
                    return null;
                var a = primes[primeIndex++];
                if (a > 2 * N)
                {
                    var digits = (int)(sum * 1e9);
                    var str = "000000000" + digits;
                    return str.Substring(str.Length - 9);
                }

                var vmax = (int)(Math.Log(2 * N) / Math.Log(a));
                var av = 1;
                for (var i = 0; i < vmax; i++)
                    av *= a;

                var s = 0;
                var num = 1;
                var den = 1;
                var v = 0;
                var kq = 1;
                var kq2 = 1;
                for (var k = 1; k <= N; k++)
                {
                    var t = k;
                    if (kq >= a)
                    {
                        do
                        {
                            t /= a;
                            v--;
                        }
                        while ((t % a) == 0);
                        kq = 0;
                    }
                    num = mul_mod(num, t, av);
                    kq++;

                    t = (2 * k) - 1;
                    if (kq2 >= a)
                    {
                        if (kq2 == a)
                        {
                            do
                            {
                                t /= a;
                                v++;
                            }
                            while ((t % a) == 0);
                        }
                        kq2 -= a;
                    }
                    den = mul_mod(den, t, av);
                    kq2 += 2;

                    if (v > 0)
                    {
                        t = mul_mod(mul_mod(inv_mod(den, av), num, av), k, av);
                        for (var i = v; i < vmax; i++)
                            t = mul_mod(t, a, av);
                        s += t;
                        if (s >= av)
                            s -= av;
                    }
                }
                s = mul_mod(s, pow_mod(10, n - 1, av), av);
                sum = fmod(sum + ((double)s / (double)av), 1.0);
            }
        }
    }
}