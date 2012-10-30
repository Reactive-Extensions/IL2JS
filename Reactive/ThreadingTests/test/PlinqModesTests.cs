using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;

namespace plinq_devtests
{
    internal static class PlinqModesTests
    {
        internal static bool RunPlinqModesTests()
        {
            if (System.Linq.Parallel.Scheduling.GetDefaultDegreeOfParallelism() == 1)
            {
                Console.WriteLine("   - Test does not apply to the DOP=1 case.");
                return true;
            }

            Action<ParallelExecutionMode, Verifier>[] hardQueries = {
                (mode,verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .Select(x => verifier.Verify(x)).TakeWhile(x => true).ToArray(),

                (mode,verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .Select(x => verifier.Verify(x)).TakeWhile(x => true).Iterate(),

                (mode,verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .Where(x => true).Select(x => verifier.Verify(x)).Take(100).ToArray(),

                (mode,verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .Where(x => true).Select(x => verifier.Verify(x)).Take(100).Iterate(),

                (mode,verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .Select(x => verifier.Verify(x)).OrderBy(x => x).Select(x => verifier.Verify(x)).ElementAt(5),

                (mode,verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .Where(x=>true).Select(x => verifier.Verify(x)).ElementAt(5),

                (mode,verifier) => ParallelEnumerable.Range(0, 1000)
                    .OrderBy(x=>x).Select(x => verifier.Verify(x)).WithExecutionMode(mode).ElementAt(5),
            };

            Action<ParallelExecutionMode, Verifier>[] easyQueries = {

                (mode,verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .TakeWhile(x => true).Select(x => verifier.Verify(x)).ToArray(),

                (mode,verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .TakeWhile(x => true).Select(x => verifier.Verify(x)).Iterate(),

                (mode,verifier) => Enumerable.Range(0, 1000).ToArray().AsParallel()
                    .Select(x => verifier.Verify(x)).Take(100).WithExecutionMode(mode).ToArray(),

                (mode,verifier) => Enumerable.Range(0, 1000).ToArray().AsParallel().WithExecutionMode(mode)
                    .Take(100).Select(x => verifier.Verify(x)).Iterate(),

                (mode,verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .Select(x => verifier.Verify(x)).ElementAt(5),

                (mode, verifier) => ParallelEnumerable.Range(0, 1000).WithExecutionMode(mode)
                    .Select(x => verifier.Verify(x)).SelectMany((x,i) => Enumerable.Repeat(1, 2)).Iterate(),

                (mode, verifier) => Enumerable.Range(0, 1000).AsParallel().WithExecutionMode(mode)
                    .Select(x => verifier.Verify(x)).SelectMany((x,i) => Enumerable.Repeat(1, 2)).Iterate(),

                (mode, verifier) => Enumerable.Range(0, 1000).AsParallel().WithExecutionMode(mode).AsUnordered()
                    .Select(x => verifier.Verify(x)).Select((x,i) => x).Iterate(),

                (mode, verifier) => Enumerable.Range(0, 1000).AsParallel().WithExecutionMode(mode).AsUnordered().Where(x => true).Select(x => verifier.Verify(x)).First(),

                (mode, verifier) => Enumerable.Range(0, 1000).AsParallel().WithExecutionMode(mode)
                    .Select(x => verifier.Verify(x)).OrderBy(x => x).ToArray(),

                (mode, verifier) => Enumerable.Range(0, 1000).AsParallel().WithExecutionMode(mode)
                    .Select(x => verifier.Verify(x)).OrderBy(x => x).Iterate(),

                (mode, verifier) => Enumerable.Range(0, 1000).AsParallel().AsOrdered().WithExecutionMode(mode)
                    .Where(x => true).Select(x => verifier.Verify(x))
                    .Concat(Enumerable.Range(0, 1000).AsParallel().AsOrdered().Where(x => true))
                    .ToList(),
            };


            // Verify that all queries in 'easyQueries' run in parallel in default mode
            bool passed = true;

            for(int i = 0; i < easyQueries.Length; i++)
            {
                Verifier verifier = new ParVerifier();
                easyQueries[i].Invoke(ParallelExecutionMode.Default, verifier);
                if (!verifier.Passed)
                {
                    passed = false;
                    Console.WriteLine("Easy query {0} expected to run in parallel in default mode", i);
                }
            }


            // Verify that all queries in 'easyQueries' always run in forced mode
            for (int i = 0; i < easyQueries.Length; i++)
            {
                Verifier verifier = new ParVerifier();
                easyQueries[i].Invoke(ParallelExecutionMode.ForceParallelism, verifier);
                if (!verifier.Passed)
                {
                    passed = false;
                    Console.WriteLine("Easy query {0} expected to run in parallel in force-parallelism mode", i);
                }
            }

            // Verify that all queries in 'easyQueries' run sequentially in default mode
            for (int i = 0; i < hardQueries.Length; i++)
            {
                Verifier verifier = new SeqVerifier();
                hardQueries[i].Invoke(ParallelExecutionMode.Default, verifier);
                if (!verifier.Passed)
                {
                    passed = false;
                    Console.WriteLine("Hard query {0} expected to run sequentially in default mode", i);
                }
            }

            // Verify that all queries in 'easyQueries' always run in forced mode
            for (int i = 0; i < hardQueries.Length; i++)
            {
                Verifier verifier = new ParVerifier();
                hardQueries[i].Invoke(ParallelExecutionMode.ForceParallelism, verifier);
                if (!verifier.Passed)
                {
                    passed = false;
                    Console.WriteLine("Hard query {0} expected to run in parallel in force-parallelism mode", i);
                }
            }

            return passed;
        }

        private static void Iterate<T>(this IEnumerable<T> e)
        {
            foreach (var x in e) { }
        }

        // A class that checks whether Verify has been called from one or multiple threads.
        private abstract class Verifier
        {
            internal abstract int Verify(int x);
            internal abstract bool Passed { get; }
        }


        // A class that checks whether the Verify method got called from one thread only.
        private class SeqVerifier : Verifier
        {
            private int m_threadId = -1;
            private bool m_sequentialAccess = true;

            internal override int Verify(int x)
            {
                int myThreadId = Thread.CurrentThread.ManagedThreadId;
                int oldThreadId = Interlocked.Exchange(ref m_threadId, myThreadId);

                if (oldThreadId != myThreadId && oldThreadId != -1)
                {
                    m_sequentialAccess = false;
                }

                return x;
            }

            internal override bool Passed
            {
                get { return m_sequentialAccess; }
            }
        }

        // A class that checks whether the Verify method got called from at least two threads.
        // The first call to Verify() blocks. If another call to Verify() occurs prior to the timeout
        // then we know that Verify() is getting called from multiple threads.
        private class ParVerifier : Verifier
        {
            private int m_counter = 0;
            private bool m_passed = false;
            private const int TIMEOUT_LIMIT = 5000;

            internal override int Verify(int x)
            {
                lock(this) {
                    m_counter++;
                    if (m_counter == 1)
                    {
                        if (Monitor.Wait(this, TIMEOUT_LIMIT))
                        {
                            m_passed = true;
                        }
                    }
                    else if (m_counter == 2)
                    {
                        Monitor.Pulse(this);
                    }
                }

                return x;
            }

            internal override bool Passed
            {
                get { return m_passed; }
            }
        }
    }
}
