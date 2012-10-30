//#define STRESS_ON

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace plinq_devtests
{
    internal static class PerfHelpers
    {
        internal delegate void Routine();

        internal static void DrivePerfComparison(Routine seq, Routine par, int loops)
        {
            // Capture the perf #s.

            long[] measuresSeq = new long[loops];
            Stopwatch sw = new Stopwatch();
            for (int i = 0; i < measuresSeq.Length; i++)
            {
                sw.Start();

                seq();

                sw.Stop();
                measuresSeq[i] = sw.ElapsedTicks;
                TestHarness.TestLog("  > SEQ: {0}", sw.Elapsed);
                sw.Reset();
            }

            long[] measuresPar = new long[loops];
            for (int i = 0; i < measuresPar.Length; i++)
            {
                sw.Start();

                par();

                sw.Stop();
                measuresPar[i] = sw.ElapsedTicks;
                TestHarness.TestLog("  > PAR: {0}", sw.Elapsed);
                sw.Reset();
            }

            // We'll remove the slowest and fastest XX% of the test.
            float deviancePercent = 0.2F;
            int remDeviance = (int)(loops * deviancePercent);

            // Sort from fastest to slowest.
            Array.Sort(measuresPar);
            Array.Sort(measuresSeq);

            if (remDeviance > 0) {
                TestHarness.TestLog("  > (Deviance: Removing slowest/fastest {0}%: {1})", deviancePercent, remDeviance);
            } else {
                TestHarness.TestLog("  > (Insufficient test set to remove {0}% deviance)", deviancePercent);
            }

            // Accumulate a total, ignoring the fast/slow XX%.
            long totalPar = 0;
            TestHarness.TestLog("  > S / P ratios = [ ");
            for (int k = 0; k < measuresPar.Length; k++) {
                if (k >= remDeviance && k < (measuresSeq.Length - remDeviance)) {
                    totalPar += measuresPar[k];
                    TestHarness.TestLog("        {0} ", measuresSeq[k] / (float)measuresPar[k]);
                } else {
                    TestHarness.TestLog("        [!{0}] ", measuresSeq[k] / (float)measuresPar[k]);
                }
            }
            TestHarness.TestLog("  > ]");

            long totalSeq = 0;
            for (int k = 0; k < measuresSeq.Length; k++) {
                if (k >= remDeviance && k < (measuresSeq.Length - remDeviance)) {
                    totalSeq += measuresSeq[k];
                }
            }

            TestHarness.TestLog("  > S = {0} / P = {1}", totalSeq, totalPar);
            TestHarness.TestLog("  > **FINAL S/P: {0}", (double)totalSeq / totalPar);
        }

    }


    internal static class QueryOperatorPerfTests
    {

        internal static bool RunForAllTests()
        {
            bool passed = true;

            RunForAllTest1(1024 * 1024, 0, 10);
            RunForAllTest1(1024 * 1024 * 8, 0, 10);
            RunForAllTest1(1024 * 1024, 100, 10);
            RunForAllTest1(1024 * 1024, 1000, 10);
            RunForAllTest1(1024 * 1024, 10000, 5);

            //RunJoinTest_ForProfile(1024 * 1024 * 4, 1024 * 8, 10);

            return passed;
        }

        private static int SimulateCycles(int x, int cycleCount)
        {
            for (int i = 0; i < cycleCount; i++)
            {
                x = x + i;
            }
            return x;
        }

        private static bool RunForAllTest1(int dataSize, int cycleCount, int loops)
        {
            TestHarness.TestLog("RunForAllTest1({0}, {1}, {2})", dataSize, cycleCount, loops);

            IEnumerable<int> data;
            {
                int[] arr = new int[dataSize];
                for (int i = 0; i < dataSize; i++) arr[i] = i;
                data = arr;
            }

            PerfHelpers.DrivePerfComparison(
                delegate {
                    foreach (int x in data)
                    {
                        // Simulate N cycles of work:
                        SimulateCycles(x, cycleCount);
                    }
                },
                delegate {
                    data.AsParallel().ForAll(
                        delegate(int x)
                        {
                            // Simulate N cycles of work:
                            SimulateCycles(x, cycleCount);
                        });
                },
                loops);

            return true;
        }

        internal static bool RunJoinTests()
        {
            bool passed = true;

            // @TODO: perf quotas: i.e. avoid regressions?

            RunJoinTest1(1024 * 1024, 1024, 5);
            RunJoinTest1_ForAll(1024 * 1024 * 16, 1024 * 1024, 5);
            RunJoinPlusWhereTest1(1024 * 1024, 1024, 5);
            RunJoinTest2(1024 * 1024, 1024, 5);
            RunJoinTest3(1024 * 1024, 1024, 5);

            //RunJoinTest_ForProfile(1024 * 1024 * 4, 1024 * 8, 10);

            return passed;
        }


        struct Pair
        {
            internal int x;
            internal int y;
            internal Pair(int x, int y) { this.x = x; this.y = y; }
        }

        private static bool RunJoinTest_ForProfile(int leftSize, int rightSize, int loops)
        {
            int[] left = new int[leftSize];
            int[] right = new int[rightSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i*right.Length;

            Func<int, int> identityKeySelector = delegate(int x) { return x; };

            ParallelQuery<Pair> par = left.AsParallel().Join<int, int, int, Pair>(
                right.AsParallel(), identityKeySelector, identityKeySelector, delegate(int x, int y) { return new Pair(x, y); });

            for (int j = 0; j < loops; j++)
            {
                Console.WriteLine(j);
                List<Pair> pp = par.ToList<Pair>();
            }

            return true;
        }

        private static bool RunJoinTest1(int leftSize, int rightSize, int loops)
        {
            TestHarness.TestLog("RunJoinTest1({0}, {1}, {2})", leftSize, rightSize, loops);

            int[] left = new int[leftSize];
            int[] right = new int[rightSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i*right.Length;

            Func<int, int> identityKeySelector = delegate(int x) { return x; };

            IEnumerable<Pair> seqQuery = Enumerable.Join<int, int, int, Pair>(
                left, right, identityKeySelector, identityKeySelector, delegate(int x, int y) { return new Pair(x, y); });
            ParallelQuery<Pair> parQuery = left.AsParallel().Join<int, int, int, Pair>(
                right.AsParallel(), identityKeySelector, identityKeySelector, delegate(int x, int y) { return new Pair(x, y); });

            PerfHelpers.DrivePerfComparison(
                delegate {
                    Enumerable.ToList(seqQuery);
                },
                delegate {
                    parQuery.ToList();
                },
                loops);

            return true;
        }

        private static bool RunJoinTest1_ForAll(int leftSize, int rightSize, int loops)
        {
            TestHarness.TestLog("RunJoinTest1_ForAll({0}, {1}, {2})", leftSize, rightSize, loops);

            int[] left = new int[leftSize];
            int[] right = new int[rightSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i*right.Length;

            Func<int, int> identityKeySelector = delegate(int x) { return x; };

            IEnumerable<Pair> seqQuery = Enumerable.Join<int, int, int, Pair>(
                left, right, identityKeySelector, identityKeySelector, delegate(int x, int y) { return new Pair(x, y); });
            ParallelQuery<Pair> parQuery = left.AsParallel().Join<int, int, int, Pair>(
                right.AsParallel(), identityKeySelector, identityKeySelector, delegate(int x, int y) { return new Pair(x, y); });

            PerfHelpers.DrivePerfComparison(
                delegate {
                    foreach (Pair p in seqQuery)
                    {
                        // Calc the log (to simulate some work):
                        Math.Log(p.x, p.y);
                    }
                },
                delegate {
                    parQuery.ForAll<Pair>(delegate(Pair p)
                    {
                        // Calc the log (to simulate some work):
                        Math.Log(p.x, p.y);
                    });
                },
                loops);

            return true;
        }

        private static bool RunJoinPlusWhereTest1(int leftSize, int rightSize, int loops)
        {
            TestHarness.TestLog("RunJoinPlusWhereTest1({0}, {1}, {2})", leftSize, rightSize, loops);

            int[] left = new int[leftSize];
            int[] right = new int[rightSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i*right.Length;

            Func<int, int> identityKeySelector = delegate(int x) { return x; };

            IEnumerable<Pair> seqQuery = Enumerable.Join<int, int, int, Pair>(
                Enumerable.Where<int>(left, delegate(int x) { return (x % 2) == 0; }),
                right, identityKeySelector, identityKeySelector, delegate(int x, int y) { return new Pair(x, y); });
            IEnumerable<Pair> parQuery = left.AsParallel().Where<int>(delegate(int x) { return (x % 2) == 0; })
                .Join<int, int, int, Pair>(right.AsParallel(), identityKeySelector, identityKeySelector, 
                    delegate(int x, int y) { return new Pair(x, y); });

            PerfHelpers.DrivePerfComparison(
                delegate {
                    foreach (Pair p in seqQuery)
                    {
                        // Calc the log (to simulate some work):
                        Math.Log(p.x, p.y);
                    }
                },
                delegate {
                    foreach (Pair p in parQuery)
                    {
                        // Calc the log (to simulate some work):
                        Math.Log(p.x, p.y);
                    }
                },
                loops);

            return true;
        }

        private static bool RunJoinTest2(int leftSize, int rightSize, int loops)
        {
            TestHarness.TestLog("RunJoinTest2({0}, {1}, {2})", leftSize, rightSize, loops);

            int[] left = new int[leftSize];
            int[] right = new int[rightSize];

            for (int i = 0; i < left.Length; i++) left[i] = i;
            for (int i = 0; i < right.Length; i++) right[i] = i*right.Length;

            Func<int, int> identityKeySelector = delegate(int x) { return x; };

            IEnumerable<Pair> seqQuery = Enumerable.Join<int, int, int, Pair>(
                left, right, identityKeySelector, identityKeySelector, delegate(int x, int y) { return new Pair(x, y); });
            IEnumerable<Pair> parQuery = left.AsParallel().Join<int, int, int, Pair>(
                right.AsParallel(), identityKeySelector, identityKeySelector, delegate(int x, int y) { return new Pair(x, y); });

            PerfHelpers.DrivePerfComparison(
                delegate {
                    foreach (Pair p in seqQuery)
                    {
                        // Simulate work:
                        int z = 0;
                        for (int i = 0; i < 5; i++)
                        {
                            z *= p.x;
                            z *= p.y;
                        }
                    }
                },
                delegate {
                    foreach (Pair p in parQuery)
                    {
                        // Simulate work:
                        int z = 0;
                        for (int i = 0; i < 5; i++)
                        {
                            z *= p.x;
                            z *= p.y;
                        }
                    }
                },
                loops);

            return true;
        }

        class Wrapped<T>
        {
            internal T val;
            internal Wrapped(T val) { this.val = val; }
        }

        private static bool RunJoinTest3(int leftSize, int rightSize, int loops)
        {
            TestHarness.TestLog("RunJoinTest3({0}, {1}, {2})", leftSize, rightSize, loops);

            Wrapped<int>[] left = new Wrapped<int>[leftSize];
            Wrapped<int>[] right = new Wrapped<int>[rightSize];

            for (int i = 0; i < left.Length; i++) left[i] = new Wrapped<int>(i);
            for (int i = 0; i < right.Length; i++) right[i] = new Wrapped<int>(i*right.Length);

            Func<Wrapped<int>, int> identityKeySelector = delegate(Wrapped<int> x) { return x.val; };

            IEnumerable<Pair> seqQuery = Enumerable.Join<Wrapped<int>, Wrapped<int>, int, Pair>(
                left, right, identityKeySelector, identityKeySelector, delegate(Wrapped<int> x, Wrapped<int> y) { return new Pair(x.val, y.val); });
            IEnumerable<Pair> parQuery = left.AsParallel().Join<Wrapped<int>, Wrapped<int>, int, Pair>(
                right.AsParallel(), identityKeySelector, identityKeySelector, delegate(Wrapped<int> x, Wrapped<int> y) { return new Pair(x.val, y.val); });

            PerfHelpers.DrivePerfComparison(
                delegate {
                    foreach (Pair p in seqQuery)
                    {
                        // Simulate work:
                        int z = 0;
                        for (int i = 0; i < 5; i++)
                        {
                            z *= p.x;
                            z *= p.y;
                        }
                    }
                },
                delegate {
                    foreach (Pair p in parQuery)
                    {
                        // Simulate work:
                        int z = 0;
                        for (int i = 0; i < 5; i++)
                        {
                            z *= p.x;
                            z *= p.y;
                        }
                    }
                },
                loops);

            return true;
        }    

        internal static bool RunWhereTests()
        {
            bool passed = true;

            // @TODO: perf quotas: i.e. avoid regressions?

            RunWhereTest1(1024 * 1024 * 8, 100, 50, 5);
            RunWhereTest1(1024 * 1024, 100, 50, 10);
            RunWhereTest1(1024 * 1024, 125, 25, 10);
            RunWhereTest1(1024 * 1024, 75, 75, 10);

            RunWhereTest2(1024 * 1024, 10);

            return passed;
        }

        private static bool RunWhereTest1(int size, int predSimulation, int workSimulation, int loops)
        {
            int[] data = new int[size];

            for (int i = 0; i < data.Length; i++) data[i] = i;

            Func<int, int> identityKeySelector = delegate(int x) { return x; };

            IEnumerable<int> seqQuery = Enumerable.Where<int>(data, delegate(int x) { int xx = 1; for (int i = 0; i < predSimulation; i++) xx *= (i + 1); return (x % 2) == 0; });
            IEnumerable<int> parQuery = data.AsParallel().Where<int>(delegate(int x) { int xx = 1; for (int i = 0; i < predSimulation; i++) xx *= (i + 1); return (x % 2) == 0; });

            PerfHelpers.DrivePerfComparison(
                delegate {
                    foreach (int p in seqQuery) {
                        // Simulate some work:
                        int z = 1;
                        for (int i = 0; i < workSimulation; i++) z *= p;
                    }
                },
                delegate {
                    foreach (int p in parQuery) {
                        // Simulate some work:
                        int z = 1;
                        for (int i = 0; i < workSimulation; i++) z *= p;
                    }
                },
                loops);

            return true;
        }

        private static bool RunWhereTest2(int size, int loops)
        {
            int[] data = new int[size];

            for (int i = 0; i < data.Length; i++) data[i] = i;

            Func<int, int> identityKeySelector = delegate(int x) { return x; };

            IEnumerable<int> seqQuery = Enumerable.Where<int>(data, delegate(int x) { object o = new object(); int xx = o.GetHashCode(); return (x % 2) == 0; });
            IEnumerable<int> parQuery = data.AsParallel().Where<int>(delegate(int x) { object o = new object(); int xx = o.GetHashCode(); return (x % 2) == 0; });

            PerfHelpers.DrivePerfComparison(
                delegate {
                    foreach (int p in seqQuery) {
                        // Simulate some work:
                        object o1 = new object(); int xx1 = o1.GetHashCode();
                        object o2 = new object(); int xx2 = o1.GetHashCode();
                    }
                },
                delegate {
                    foreach (int p in parQuery) {
                        // Simulate some work:
                        object o1 = new object(); int xx1 = o1.GetHashCode();
                        object o2 = new object(); int xx2 = o1.GetHashCode();
                    }
                },
                loops);

            return true;
        }

        internal static bool RunAggregateTests()
        {
            bool passed = true;

            // @TODO: perf quotas: i.e. avoid regressions?

            RunSumTest1(1024, 10);
            RunSumTest1(1024 * 8, 10);
            RunSumTest1(1024 * 1024, 10);
            RunSumTest1(1024 * 1024 * 8, 5);
            RunSumTest1(1024 * 1024 * 32, 3);

            return passed;
        }

        private static bool RunSumTest1(int size, int loops)
        {
            TestHarness.TestLog("RunSumTest1({0}, {1})", size, loops);

            int[] data = new int[size];
            for (int i = 0; i < data.Length; i++) data[i] = i+1;

            PerfHelpers.DrivePerfComparison(
                delegate {
                    double xx = Enumerable.Average(data);
                    Debug.Assert(xx == (double)(size+1) / 2);
                },
                delegate {
                    double xx = data.AsParallel<int>().Average();
                    Debug.Assert(xx == (double)(size+1) / 2);
                },
                loops);

            return true;
        }

        internal static bool RunWeightedAverageTests()
        {
            bool passed = true;

            RunWeightedAverage1(1024, 10);
            RunWeightedAverage1(1024 * 8, 10);
            RunWeightedAverage1(1024 * 1024, 10);
            RunWeightedAverage1(1024 * 1024 * 8, 5);
            RunWeightedAverage1(1024 * 1024 * 32, 3);

            return passed;
        }

        private static bool RunWeightedAverage1(int size, int loops)
        {
            TestHarness.TestLog("RunWeightedAverage1({0}, {1})", size, loops);

            long[] data = new long[size];
            for (int i = 0; i < size; i++) data[i] = i+1;
            long[] weights = new long[size];
            for (int i = 0; i < size; i++) weights[i] = 1 + (i % 20);

            long presum = Enumerable.Sum(weights);

            PerfHelpers.DrivePerfComparison(
                delegate {
                    double total = Enumerable.Sum(Enumerable.Select<long, long>(data, delegate(long x, int idx) { return x * weights[idx]; }));
                    double avg = total / presum;//Enumerable.Sum(weights);
                },
                delegate {
                    double total = data.AsParallel().Zip<long, long, long>(weights.AsParallel(), (f, s) => f * s).Sum();
                    double avg = total / presum;//;ParallelEnumerable.Sum(weights.AsParallel());
                },
                loops);

            return true;
        }

        internal static bool RunGroupWordsTests()
        {
            bool passed = true;

            RunGroupWords1Test(1024, 10);
            RunGroupWords1Test(1024 * 8, 10);
            RunGroupWords1Test(-1, 5);
            RunGroupWords1Test(1024 * 1024, 3);
            RunGroupWords1Test(1024 * 1024 * 4, 3);
            RunGroupWords1Test(1024 * 1024 * 32, 3);

            return passed;
        }

        private static bool RunGroupWords1Test(int targetWords, int loops)
        {
            TestHarness.TestLog("RunGroupWords1Test(targetWords={0}, loops={1})", targetWords, loops);

            //@TODO: bundle data into a resource pack.
            string text = System.IO.File.ReadAllText("dat\\litpe10.txt");
            string[] split = text.Split(' ');
            string[] words = split;

            // If specific # of words desired, fill the buffer.
            if (targetWords != -1)
            {
                words = new string[targetWords];
                int last = 0;
                while (last < targetWords)
                {                    
                    Array.Copy(split, 0, words, last, Math.Min(split.Length, words.Length - last));
                    last += split.Length;
                }
            }

            TestHarness.TestLog("  (text length == {0}, words == {1})", text.Length, words.Length);

            PerfHelpers.DrivePerfComparison(
                delegate {
                    var counts = from w in words group w by w;
                    var force = counts.ToList();
                },
                delegate {
                    var counts = from w in words.AsParallel() group w by w;
                    var force = counts.ToList();
                },
                loops);

            return true;
        }

        internal static bool RunMatrixMultTests()
        {
            bool passed = true;

            for (int i = 1; i <= 512; i*=2)
            {
                RunMatrixMultVsHandCodedTest(i, i, i, 5);
            }

            for (int i = 1; i <= 512; i*=2)
            {
                RunMatrixMultTest(i, i, i, 5);
            }

            return passed;
        }

        private static bool RunMatrixMultTest(int m, int n, int o, int loops)
        {
            TestHarness.TestLog("RunMatrixMultTest(m={0}, n={1}, o={2}, loops={3})", m, n, o, loops);

            Random r = new Random(33); // same seed for predictable test results.

            // Generate our two matrixes out of random #s.

            int[,] m1 = new int[m,n];
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    m1[i,j] = r.Next(100);

            int[,] m2 = new int[n,o];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < o; j++)
                    m2[i,j] = r.Next(100);

            PerfHelpers.DrivePerfComparison(
                delegate {
                    int oldDop = System.Linq.Parallel.Scheduling.DefaultDegreeOfParallelism;
                    System.Linq.Parallel.Scheduling.DefaultDegreeOfParallelism = 1;
                    try {
                        var inner = ParallelEnumerable.Range(0, o);
                        int[] m3 = ParallelEnumerable.Range(0, m).
                            SelectMany(i => inner, (i,j) => 
                                Enumerable.Range(0, n).Sum((k) => m1[i,k] * m2[k,j])).
                            ToArray();
                    } finally {
                        System.Linq.Parallel.Scheduling.DefaultDegreeOfParallelism = oldDop;
                    }
                },
                delegate {
                    var inner = ParallelEnumerable.Range(0, o);
                    int[] m3 = ParallelEnumerable.Range(0, m).
                        SelectMany(i => inner, (i,j) => 
                            Enumerable.Range(0, n).Sum((k) => m1[i,k] * m2[k,j])).
                        ToArray();
                },
                loops);

            return true;
        }

        private static bool RunMatrixMultVsHandCodedTest(int m, int n, int o, int loops)
        {
            TestHarness.TestLog("RunMatrixMultVsHandCodedTest(m={0}, n={1}, o={2}, loops={3}) -- vs. hand coded parallel", m, n, o, loops);

            Random r = new Random(33); // same seed for predictable test results.

            // Generate our two matrixes out of random #s.

            int[,] m1 = new int[m,n];
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    m1[i,j] = r.Next(100);

            int[,] m2 = new int[n,o];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < o; j++)
                    m2[i,j] = r.Next(100);

            PerfHelpers.DrivePerfComparison(
                delegate {
                    int[,] m3 = new int[m, o];

                    using (CountdownEvent latch = new CountdownEvent(Environment.ProcessorCount))
                    {
                        int stride = m / Environment.ProcessorCount;
                        for (int _p = 0; _p < Environment.ProcessorCount; _p++)
                        {
                            int p = _p;
                            ThreadPool.QueueUserWorkItem(delegate {
                                int start = p * stride;
                                int end = Math.Min((p+1) * stride, m);
                                for (int i = start; i < end; i++) {
                                    for (int j = 0; j < o; j++) {
                                        int s = 0;
                                        for (int k = 0; k < n; k++) {
                                           s += m1[i, k] * m2[k, j];
                                        }
                                        m3[i, j] = s;
                                    }
                                }
                                latch.Signal();
                            });
                        }

                        latch.Wait();
                    }
                },
                delegate {
                    var inner = ParallelEnumerable.Range(0, o);
                    int[] m3 = ParallelEnumerable.Range(0, m).
                        SelectMany(i => inner, (i,j) => 
                            Enumerable.Range(0, n).Sum((k) => m1[i,k] * m2[k,j])).
                        ToArray();
                },
                loops);

            return true;
        }

        internal static bool RunBicubicInterpolationTests()
        {
            bool passed = true;

            for (int i = 1; i <= 512; i*=2)
            {
                RunBicubicInterpolationTest(i, i, 5);
            }

            return passed;
        }

        private static bool RunBicubicInterpolationTest(int m, int n, int loops)
        {
            TestHarness.TestLog("RunBicubicInterpolationTest(m={0}, n={1}, loops={2})", m, n, loops);

            Random r = new Random(33); // same seed for predictable test results.

            // Generate our image out of random #s.

            int[,] img = new int[m,n];
            for (int i = 0; i < m; i++)
                for (int j = 0; j < n; j++)
                    img[i,j] = r.Next(100);

            // Just pick some random weights.

            double[] bicubic_weights = new double[] {
                0.15, 0.25, 0.25, 0.15, 0.25, 0.5,  0.5,  0.25, 0.8,
                0.25, 0.5,  0.5,  0.25, 0.15, 0.25, 0.25, 0.15
            };
            ParallelQuery<double> W = bicubic_weights.AsParallel();

            PerfHelpers.DrivePerfComparison(
                delegate {
                    // HACK: current version of LINQ doesn't have the right SelectMany
                    // overload yet.  So we just compare to PLINQ w/ a DOP of 1 instead.
                    int oldDop = System.Linq.Parallel.Scheduling.DefaultDegreeOfParallelism;
                    System.Linq.Parallel.Scheduling.DefaultDegreeOfParallelism = 1;
                    try {
                        double[] img2 = (
                            from x in ParallelEnumerable.Range(0, m)
		                    from y in ParallelEnumerable.Range(0, n)
		                    select
			                    (from x2 in Enumerable.Range(m - 3, 7)
			                    from y2 in Enumerable.Range(n - 3, 7)
			                    where x2 >= 0 && x2 < m &&
	     	                         y2 >= 0 && y2 < n &&
		                              (x2 == x || (x2-(m-3) % 2) == 0) &&
		                              (y2 == y || (y2-(n-3) % 2) == 0)
                                select img[x2,y2]
                                ).AsParallel().Zip(W,(i,j)=>new Pair<int,double>(i,j)).Sum((p)=>p.First*p.Second) / W.Sum()).ToArray();
                    } finally {
                        System.Linq.Parallel.Scheduling.DefaultDegreeOfParallelism = oldDop;
                    }
                },
                delegate {
                    double[] img2 = (
                        from x in ParallelEnumerable.Range(0, m)
		                from y in ParallelEnumerable.Range(0, n)
		                select
			                (from x2 in Enumerable.Range(m - 3, 7)
			                from y2 in Enumerable.Range(n - 3, 7)
			                where x2 >= 0 && x2 < m &&
	     	                     y2 >= 0 && y2 < n &&
		                          (x2 == x || (x2-(m-3) % 2) == 0) &&
		                          (y2 == y || (y2-(n-3) % 2) == 0)
                            select img[x2,y2]
                            ).AsParallel().Zip(W, (i,j) => new Pair<int,double>(i,j)).Sum((p)=>p.First*p.Second) / W.Sum()).ToArray();
                },
                loops);

            return true;
        }

        internal static bool RunOrderByTestsSmall()
        {
            bool passed = true;

            // Small sizes (0.5MB).
            int intsPer500KB = (1024 * 512)/sizeof(int);
            passed &= RunOrderByTest1(intsPer500KB, false, DataDistributionType.AlreadyAscending, 5);
            passed &= RunOrderByTest1(intsPer500KB, false, DataDistributionType.AlreadyDescending, 5);
            passed &= RunOrderByTest1(intsPer500KB, false, DataDistributionType.Random, 5);
            passed &= RunOrderByTest1(intsPer500KB, true, DataDistributionType.AlreadyAscending, 5);
            passed &= RunOrderByTest1(intsPer500KB, true, DataDistributionType.AlreadyDescending, 5);
            passed &= RunOrderByTest1(intsPer500KB, true, DataDistributionType.Random, 5);

            return passed;
        }

        internal static bool RunOrderByTestsMedium()
        {
            bool passed = true;

            // Medium sizes (1MB).
            int intsPer1MB = (1024 * 1024)/sizeof(int);
            passed &= RunOrderByTest1(intsPer1MB, false, DataDistributionType.AlreadyAscending, 5);
            passed &= RunOrderByTest1(intsPer1MB, false, DataDistributionType.AlreadyDescending, 5);
            passed &= RunOrderByTest1(intsPer1MB, false, DataDistributionType.Random, 5);
            passed &= RunOrderByTest1(intsPer1MB, true, DataDistributionType.AlreadyAscending, 5);
            passed &= RunOrderByTest1(intsPer1MB, true, DataDistributionType.AlreadyDescending, 5);
            passed &= RunOrderByTest1(intsPer1MB, true, DataDistributionType.Random, 5);

            // Med/lg sizes (10MB).
            int intsPer10MB = (1024 * 1024 * 10)/sizeof(int);
            passed &= RunOrderByTest1(intsPer10MB, false, DataDistributionType.AlreadyAscending, 2);
            passed &= RunOrderByTest1(intsPer10MB, false, DataDistributionType.AlreadyDescending, 2);
            passed &= RunOrderByTest1(intsPer10MB, false, DataDistributionType.Random, 2);
            passed &= RunOrderByTest1(intsPer10MB, true, DataDistributionType.AlreadyAscending, 2);
            passed &= RunOrderByTest1(intsPer10MB, true, DataDistributionType.AlreadyDescending, 2);
            passed &= RunOrderByTest1(intsPer10MB, true, DataDistributionType.Random, 2);

            return passed;
        }

        internal static bool RunOrderByTestsLarge()
        {
            bool passed = true;

            // Large sizes (250MB).
            int intsPer250MB = (1024 * 1024 * 250)/sizeof(int);
            //passed &= RunOrderByTest1(intsPer250MB, false, DataDistributionType.AlreadyAscending, 1);
            //passed &= RunOrderByTest1(intsPer250MB, false, DataDistributionType.AlreadyDescending, 1);
            passed &= RunOrderByTest1(intsPer250MB, false, DataDistributionType.Random, 1);
            //passed &= RunOrderByTest1(intsPer250MB, true, DataDistributionType.AlreadyAscending, 1);
            //passed &= RunOrderByTest1(intsPer250MB, true, DataDistributionType.AlreadyDescending, 1);
            passed &= RunOrderByTest1(intsPer250MB, true, DataDistributionType.Random, 1);

            return passed;
        }

        internal static bool RunOrderByTests_progression()
        {
#if STRESS_ON
            int[] data = CreateOrderByInput(1024 * 1024, DataDistributionType.Random);
            RunOrderByTest_Stress(data, false, 5);
#else
            for (int i = 8; i <= (1024*128)/4; i *= 2)
            {
                TestHarness.TestLog("TEST {0} MB of ints: {1}", ((float)i/1024)*4, i);
                RunOrderByTest1(i*1024, false, DataDistributionType.Random, 3);
            }
#endif

            return true;
        }

        enum DataDistributionType
        {
            AlreadyAscending,
            AlreadyDescending,
            Random
        }

        private static int[] CreateOrderByInput(int dataSize, DataDistributionType type)
        {
            int[] data = new int[dataSize];
            switch (type)
            {
                case DataDistributionType.AlreadyAscending:
                    for (int i = 0; i < data.Length; i++) data[i] = i;
                    break;
                case DataDistributionType.AlreadyDescending:
                    for (int i = 0; i < data.Length; i++) data[i] = dataSize - i;
                    break;
                case DataDistributionType.Random:
                    Random rand = new Random(33);
                    for (int i = 0; i < data.Length; i++) data[i] = rand.Next();
                    break;
            }
            return data;
        }

        //-----------------------------------------------------------------------------------
        // Exercises basic OrderBy behavior by sorting a fixed set of integers. They are fed
        // in already ascending, already descending, and random order for 3 variants.
        //

        private static bool RunOrderByTest1(int dataSize, bool descending, DataDistributionType type, int loops)
        {
            TestHarness.TestLog("RunOrderByTest1(dataSize = {0}, descending = {1}, type = {2})", dataSize, descending, type);

            int[] data = CreateOrderByInput(dataSize, type);

            IEnumerable<int> seqQuery;
            ParallelQuery<int> parQuery;
            if (descending)
            {
                seqQuery = Enumerable.OrderByDescending<int, int>(data, delegate(int x) { return x; }, System.Linq.Parallel.Util.GetDefaultComparer<int>());
                parQuery = data.AsParallel<int>().OrderByDescending<int, int>(
                    delegate(int x) { return x; }, System.Linq.Parallel.Util.GetDefaultComparer<int>());
            }
            else
            {
                seqQuery = Enumerable.OrderBy<int, int>(data, delegate(int x) { return x; }, System.Linq.Parallel.Util.GetDefaultComparer<int>());
                parQuery = data.AsParallel<int>().OrderBy<int, int>(
                    delegate(int x) { return x; }, System.Linq.Parallel.Util.GetDefaultComparer<int>());
            }

            PerfHelpers.DrivePerfComparison(
                delegate { List<int> r = Enumerable.ToList<int>(seqQuery); },
                delegate { List<int> r = parQuery.ToList<int>(); },
                loops);

            return true;
        }

        private static bool RunOrderByTest_Stress(int[] data, bool descending, int loops)
        {
            TestHarness.TestLog("RunOrderByTest_Stress(data={0}, descending = {1}, loops = {2})", data.Length, descending, loops);

            ParallelQuery<int> parQuery;
            if (descending)
            {
                parQuery = data.AsParallel<int>().OrderByDescending<int, int>(
                    delegate(int x) { return x; });
            }
            else
            {
                parQuery = data.AsParallel<int>().OrderBy<int, int>(
                    delegate(int x) { return x; });
            }

            for (int i = 0; i < loops; i++) {
                Console.Write("{0}  ", i);
                List<int> r = parQuery.ToList<int>();
            }
            Console.WriteLine();

            return true;
        }
    }
}
