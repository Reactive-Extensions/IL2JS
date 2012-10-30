// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// TestHarness.cs
//
// The PLINQ dev unit test harness.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Parallel;
using System.Text;

namespace plinq_devtests
{

    delegate bool TestFunction();

    [Flags]
    enum TestType
    {
        None = 0,
        Quick = 1,
        Normal = 2,
        Stress = 4,
        Performance = 8,
        Scheduler = 16,
        PLINQ = 32,
        TPL = 64,
        CDS = 128,
        BVT = 256,
        Disabled = 512,
        All = ~0,
        AllEnabled = ~Disabled,
    }

    struct TestDefinition
    {
        internal TestFunction m_func;
        internal TestType m_type;
        internal TestDefinition(TestFunction func, TestType type)
        {
            m_func = func;
            m_type = type;
        }
    }

    internal partial class TestHarness
    {
        private static bool stopOnFirst = true;

        internal static TestType RunFlags = TestType.Quick | TestType.Normal;
        private static StringBuilder errs = new StringBuilder();

        private static TestDefinition[] s_tests = new TestDefinition[] {
            
            // PLINQ TESTS

            new TestDefinition(PartitionedEnumeratorTests.RunContiguousRangePartitionedTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(PartitionedEnumeratorTests.RunStripePartitionedTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(PartitionedEnumeratorTests.RunContiguousRangePartitionedTests_Parallel, TestType.Normal | TestType.PLINQ),
            new TestDefinition(PartitionedEnumeratorTests.RunStripePartitionedTests_Parallel, TestType.Normal | TestType.PLINQ),

            new TestDefinition(ChannelTests.RunBoundedSingleChannelTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(ChannelTests.RunBoundedSingleChannelTests_Parallel, TestType.Normal | TestType.PLINQ),

            new TestDefinition(ExchangeTests.RunPartitionMergeTests, TestType.Quick | TestType.PLINQ),

            new TestDefinition(QueryOperatorTests.RunSelectTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunWhereTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunJoinTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunGroupJoinTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunSelectManyTests, TestType.Quick | TestType.PLINQ),

            new TestDefinition(QueryOperatorTests.RunOrderByTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunThenByTests, TestType.Quick | TestType.PLINQ),

            new TestDefinition(QueryOperatorTests.RunGroupByTests, TestType.Quick | TestType.PLINQ),

            new TestDefinition(QueryOperatorTests.RunAggregationTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunSumTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunAvgTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunMinTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunMaxTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunAnyAllTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunSequenceEqualTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunTakeSkipWhileTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunTakeSkipTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunContainsTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunFirstTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunLastTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunSingleTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunElementAtTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunCastAndOfTypeTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunDefaultIfEmptyTests, TestType.Quick | TestType.PLINQ),

            new TestDefinition(QueryOperatorTests.RunReverseTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunConcatTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunUnionTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunIntersectTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunExceptTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunDistinctTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunZipTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunExceptionTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunToArrayTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunToDictionaryTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunToLookupTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunRangeTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunRepeatTests, TestType.Quick | TestType.PLINQ),

            new TestDefinition(QueryOperatorTests.RunForAllTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunPartitionerTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorTests.RunDopTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(QueryOperatorPairTests.RunTests, TestType.Normal | TestType.PLINQ),

            new TestDefinition(PlinqDelegateExceptions.RunUserDelegateExceptionTests, TestType.Quick | TestType.PLINQ), 
            new TestDefinition(PlinqCancellationCoreHarness.RunPlinqCancellationTests, TestType.Normal | TestType.PLINQ), 
            new TestDefinition(PartitionerStaticTests.RunPartitionerStaticTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(PlinqModesTests.RunPlinqModesTests, TestType.Quick | TestType.PLINQ),
            new TestDefinition(PlinqMiscTests.RunFixedMaxHeapTests, TestType.Quick | TestType.PLINQ),

            // TPL TESTS

            new TestDefinition(TaskRtTests.RunBasicTaskTests, TestType.Quick | TestType.TPL),
            new TestDefinition(TaskRtTests.RunBreakTests, TestType.Quick | TestType.TPL),
            new TestDefinition(TaskRtTests.RunParallelLoopResultTests, TestType.Quick | TestType.TPL),
            new TestDefinition(TaskRtTests.RunParallelForTests, TestType.Quick | TestType.TPL),
            new TestDefinition(TaskRtTests.RunTaskSchedulerTests, TestType.Quick | TestType.TPL),
            new TestDefinition(TaskRtTests.RunContinueWithTests, TestType.Quick | TestType.TPL),
            new TestDefinition(TaskRtTests.RunAPMFactoryTests, TestType.Quick | TestType.TPL),
            new TestDefinition(TaskRtTests.RunTaskFactoryTests, TestType.Quick | TestType.TPL),
            new TestDefinition(TaskRtTests.RunValidationForDebuggerDependencies, TestType.Quick | TestType.TPL),
            new TestDefinition(TaskRtScenarios.RunTaskScenarioTests, TestType.Quick | TestType.TPL),            

           
            // CDS TESTS

            new TestDefinition(CdsTests.RunConcurrentStackTests, TestType.Quick | TestType.CDS),
            new TestDefinition(CdsTests.RunConcurrentQueueTests, TestType.Quick | TestType.CDS),
            new TestDefinition(CdsTests.RunManualResetEventSlimTests, TestType.Quick | TestType.CDS),
            new TestDefinition(CdsTests.RunCountdownEventTests, TestType.Quick | TestType.CDS),
            new TestDefinition(CdsTests.RunAggregateExceptionTests, TestType.Quick | TestType.CDS),
            new TestDefinition(SemaphoreSlimTests.RunSemaphoreSlimTests, TestType.Quick | TestType.CDS),
            new TestDefinition(BlockingCollectionTests.RunBlockingCollectionTests, TestType.Quick | TestType.CDS),
            new TestDefinition(LazyTests.RunLazyTests, TestType.Quick | TestType.CDS),
            new TestDefinition(ThreadLocalTests.RunThreadLocalTests, TestType.Quick | TestType.CDS),
            new TestDefinition(SpinLockTests.RunSpinLockTests,TestType.Quick | TestType.CDS),
            new TestDefinition(BarrierTests.RunBarrierTests,TestType.Quick | TestType.CDS),
            new TestDefinition(ConcurrentDictionaryTests.RunConcurrentDictionaryTests,TestType.Quick | TestType.CDS),
            new TestDefinition(ConcurrentBagTests.RunConcurrentBagTests,TestType.Quick | TestType.CDS),
            new TestDefinition(CdsCancellationCoreHarness.RunCancellationCoreTests, TestType.Quick | TestType.CDS), 

            // General PFX tests or cross-feature-team tests
            // FxCop validation of mscorlib.dll
            new TestDefinition(FxCopValidator.RunFxCopValidator, TestType.Quick | TestType.CDS | TestType.TPL), 

            // PFX 3.5 test
            // This test needs to rebuild the 3.5 legacy build. This would get messy if we are currently running the 3.5
            // build, so we just skip the test in that case.
#if !PFX_LEGACY_3_5
            new TestDefinition(PFXStandAloneTests.RunPFXTests, TestType.Quick | TestType.CDS | TestType.TPL | TestType.PLINQ), 
#endif

            // Pfx perf BVTs
            new TestDefinition(PfxPerfBVT.RunPerfBVT, TestType.Quick | TestType.BVT ),

            // Scheduler TESTS

#if EXPOSE_MANAGED_SCHEDULER
            new TestDefinition(SchedulerTests.TracingTests.RunTracingTests, TestType.Quick | TestType.Scheduler),
            new TestDefinition(SchedulerTests.WorkStealingTests.RunWorkStealingTests, TestType.Quick | TestType.Scheduler),
            new TestDefinition(SchedulerTests.SafeRWListTests.Test, TestType.Quick | TestType.Scheduler),
            new TestDefinition(SchedulerTests.ContextTests.RunContextTests, TestType.Quick | TestType.Scheduler),
            new TestDefinition(SchedulerTests.SchedulerAPITests.Run, TestType.Quick | TestType.Scheduler),
            new TestDefinition(SchedulerTests.EventTests.RunEventTests, TestType.Quick | TestType.Scheduler),
            new TestDefinition(SchedulerTests.FinalizationTests.RunFinalizationTests, TestType.Quick | TestType.Scheduler),
            new TestDefinition(SchedulerTests.EndToEndTests.PingPong, TestType.Quick | TestType.Scheduler),
            new TestDefinition(SchedulerTests.TaskTests.RunTaskTests, TestType.Quick | TestType.Scheduler)
             
#endif
                         
        };

        private static string CMD_LINE_USAGE = "Usage: PlinqTest.exe <options> <test #s>" + Environment.NewLine +
                                               "    <options> = -allerrs : keep running after first error" + Environment.NewLine +
                                               "                -dop <n> : run using a max DOP of n" + Environment.NewLine +
                                               "                -quick   : only run quick tests" + Environment.NewLine +
                                               "                -stress  : run stress tests" + Environment.NewLine +
                                               "                -stressX : only run stress tests" + Environment.NewLine +
                                               "                -perf    : run perf tests" + Environment.NewLine +
                                               "                -perfX   : only run perf tests" + Environment.NewLine +
                                               "                -sched   : run scheduler tests" + Environment.NewLine +
                                               "                -schedX  : only run scheduler tests" + Environment.NewLine +
                                               "                -plinq   : run PLINQ tests" + Environment.NewLine +
                                               "                -plinqX  : only run PLINQ tests" + Environment.NewLine +
                                               "                -tpl   : run TPL tests" + Environment.NewLine +
                                               "                -tplX  : only run TPL tests" + Environment.NewLine +
                                               "                -cds   : run CDS tests" + Environment.NewLine +
                                               "                -cdsX  : only run CDS tests" + Environment.NewLine +
                                               "                -quiet   : quiet mode" + Environment.NewLine +
                                               "                -trace   : trace to console" + Environment.NewLine +
                                               "                -trace_f : trace to file PLINQ.trace.out" + Environment.NewLine +
                                               "                -trace_v : trace verbosely (stack traces, etc.)" + Environment.NewLine +
                                               "                -help    : this screen";

        private static void PrintUsageAndExit(int exitCode)
        {
            string usage = CMD_LINE_USAGE;

            if (s_tests.Length == 0)
                usage += Environment.NewLine + "**error: no tests found**";

            for (int i = 0; i < s_tests.Length; i++)
            {
                if (i == 0)
                    usage += Environment.NewLine + "    <test #s> = ";
                else
                    usage += Environment.NewLine + "                ";
                usage += string.Format("{0} - {1}", i, GetTestName(i));
            }

            Console.WriteLine(usage);
            Environment.Exit(exitCode);
        }

        private static string GetTestName(int idx)
        {
            string testName = s_tests[idx].m_func.Method.ReflectedType.Name;
            testName += "//" + s_tests[idx].m_func.Method.Name;
            return testName;
        }

        //internal static void TestLog(string msg, params object[] args) {
        //    if (!quietMode)
        //        Console.WriteLine("   - " + msg, args);
        //}

        private static void RunTests(int[] testsToRun)
        {
            for (int i = 0; i < testsToRun.Length; i++)
            {
                int testIndex = testsToRun[i];
                if (testIndex >= 0 && testIndex < s_tests.Length)
                {
                    TestDefinition test = s_tests[testIndex];
                    string testName = GetTestName(testIndex);
                    if ((test.m_type & RunFlags) != TestType.None)
                    {
                        Console.WriteLine("RUN: {0} <#{1}, {2}>", testName, testIndex, test.m_type);
                        for (int j = 0; j < 80; j++) Console.Write("~");
                        Console.WriteLine();

                        if (!test.m_func())
                        {
                            errs.AppendLine(string.Format("---: FAILED ({0} <#{1}>)", testName, testIndex));
                            Console.Error.WriteLine("---: FAILED ({0} <#{1}>)", testName, testIndex);
                            Console.WriteLine("     {0}", DateTime.Now);
                            if (stopOnFirst)
                            {
                                Environment.Exit(1);
                            }
                        }
                        else
                        {
                            Console.Error.WriteLine("+++: PASSED ({0} <#{1}>)", testName, testIndex);
                            Console.WriteLine("     {0}", DateTime.Now);
                        }

                        Console.WriteLine();
                    }
                    else
                    {
                        Console.WriteLine("SKIP: {0} <#{1}, {2}>##", testName, testIndex, test.m_type);
                        Console.WriteLine(DateTime.Now);
                        for (int j = 0; j < 80; j++) Console.Write("~");
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.WriteLine("Unknown test: {0}.\n", i);
                    PrintUsageAndExit(1);
                }
            }
        }

        public static int Main(String[] args)
        {
            // We'll assume that we're going to do all the tests.
            int[] testsToRun = new int[s_tests.Length];
            for (int i = 0; i < testsToRun.Length; i++)
                testsToRun[i] = i;

            // Now check for cmd-line arguments.
            int arg = 0;
            while (arg < args.Length)
            {
                if (args[arg].Equals("-allerrs"))
                {
                    stopOnFirst = false;
                    arg++;
                }
                else if (args[arg].Equals("-dop"))
                {
                    arg++;
                    if (arg == args.Length)
                    {
                        Console.WriteLine("Need a DOP #");
                        PrintUsageAndExit(1);
                    }
                    Scheduling.DefaultDegreeOfParallelism = int.Parse(args[arg]);
                    arg++;
                }
                else if (args[arg].Equals("-quick"))
                {
                    RunFlags = TestType.Quick;
                    arg++;
                }
                else if (args[arg].Equals("-stress"))
                {
                    RunFlags |= TestType.Stress;
                    arg++;
                }
                else if (args[arg].Equals("-stressX"))
                {
                    RunFlags = TestType.Stress;
                    arg++;
                }
                else if (args[arg].Equals("-perf"))
                {
                    RunFlags |= TestType.Performance;
                    arg++;
                }
                else if (args[arg].Equals("-perfX"))
                {
                    RunFlags = TestType.Performance;
                    arg++;
                }
                else if (args[arg].Equals("-sched"))
                {
                    RunFlags |= TestType.Scheduler;
                    arg++;
                }
                else if (args[arg].Equals("-schedX"))
                {
                    RunFlags = TestType.Scheduler;
                    arg++;
                }
                else if (args[arg].Equals("-plinq"))
                {
                    RunFlags |= TestType.PLINQ;
                    arg++;
                }
                else if (args[arg].Equals("-plinqX"))
                {
                    RunFlags = TestType.PLINQ;
                    arg++;
                }
                else if (args[arg].Equals("-tpl"))
                {
                    RunFlags |= TestType.TPL;
                    arg++;
                }
                else if (args[arg].Equals("-tplX"))
                {
                    RunFlags = TestType.TPL;
                    arg++;
                }
                else if (args[arg].Equals("-cds"))
                {
                    RunFlags |= TestType.CDS;
                    arg++;
                }
                else if (args[arg].Equals("-cdsX"))
                {
                    RunFlags = TestType.CDS;
                    arg++;
                }
                else if (args[arg].Equals("-perfBvt"))
                {
                    RunFlags |= TestType.BVT;
                    arg++;
                }
                else if (args[arg].Equals("-perfBvtX"))
                {
                    RunFlags = TestType.BVT;
                    arg++;
                }
                else if (args[arg].Equals("-quiet"))
                {
                    quietMode = true;
                    arg++;
                }
                else if (args[arg].Equals("-not"))
                {
                    // Hack to disallow selected tests.
                    if (arg < args.Length - 1)
                    {
                        int testNo = Int32.Parse(args[arg + 1]);
                        if ((testNo >= 0) && (testNo < s_tests.Length))
                        {
                            s_tests[testNo].m_type = TestType.None;
                        }
                        arg += 2;
                    }
                    else arg++;
                }
                else if (args[arg].Equals("-help") || args[arg].Equals("-?"))
                {
                    PrintUsageAndExit(1);
                }
                else
                {
                    // Passing numbers on the command line can be used to limit the run to
                    // a specific set of tests.
                    testsToRun = new int[args.Length - arg];

                    if (arg < args.Length)
                        RunFlags = TestType.All;

                    for (int i = 0; arg < args.Length; arg++, i++)
                    {
                        int testNum = Int32.Parse(args[arg]);
                        if (testNum < 0 || testNum >= s_tests.Length)
                        {
                            Console.WriteLine("Bad test #: {0}.", testNum);
                            PrintUsageAndExit(10);
                        }
                        testsToRun[i] = testNum;
                    }
                }
            }

            for (int i = 0; i < 80; i++) Console.Write("=");
            Console.WriteLine();
            Console.WriteLine(System.Reflection.Assembly.GetExecutingAssembly().FullName);
            Console.WriteLine(typeof(string).Assembly.FullName);
            Console.WriteLine("    File {0}", new System.IO.FileInfo(typeof(string).Assembly.Location));
            Console.WriteLine("    Created {0})", new System.IO.FileInfo(typeof(string).Assembly.Location).CreationTime);
            Console.WriteLine(typeof(Uri).Assembly.FullName);
            Console.WriteLine("    File {0}", new System.IO.FileInfo(typeof(Uri).Assembly.Location));
            Console.WriteLine("    Created {0}", new System.IO.FileInfo(typeof(Uri).Assembly.Location).CreationTime);
            Console.WriteLine(typeof(ParallelEnumerable).Assembly.FullName);
            Console.WriteLine("    File {0}", new System.IO.FileInfo(typeof(ParallelEnumerable).Assembly.Location));
            Console.WriteLine("    Created {0}", new System.IO.FileInfo(typeof(ParallelEnumerable).Assembly.Location).CreationTime);
            Console.WriteLine("Scheduling.DefaultDegreeOfParallelism = {0}", Scheduling.DefaultDegreeOfParallelism);
            Console.WriteLine("RUNNING TESTS W/ FLAGS: {0}", RunFlags);
            Console.WriteLine(DateTime.Now);
            for (int i = 0; i < 80; i++) Console.Write("=");
            Console.WriteLine();
            Console.WriteLine();

            DateTime start = DateTime.Now;
            RunTests(testsToRun);
            TimeSpan duration = DateTime.Now - start;

            if (errs.Length == 0)
            {
                for (int i = 0; i < 80; i++) Console.Write(".");
                Console.WriteLine();

                Console.WriteLine("+++: PASSED all {0} tests.", testsToRun.Length);
                Console.WriteLine("     {0}", DateTime.Now);
                Console.WriteLine("     [completed in: {0}]", duration);
                return 0;
            }
            else
            {
                for (int i = 0; i < 80; i++) Console.Write("!");
                Console.WriteLine();

                Console.WriteLine("---: FAILED.");
                Console.WriteLine("     {0}", DateTime.Now);
                Console.WriteLine("     [completed in: {0}]", duration);
                Console.WriteLine(errs.ToString());
                return 100;
            }
        }

    }

    //-----------------------------------------------------------------------------------
    // A pair just wraps two bits of data into a single addressable unit. This is a
    // value type to ensure it remains very lightweight, since it is frequently used
    // with other primitive data types as well.
    //
    // @BUG#899:
    // Note: this class is another copy of the Pair<T, U> class defined in CommonDataTypes.cs.
    // For now, we have a copy of the class here, because we can't import the System.Linq.Parallel
    // namespace.
    //

    internal struct Pair<T, U>
    {

        // The first and second bits of data.
        internal T m_first;
        internal U m_second;

        //-----------------------------------------------------------------------------------
        // A simple constructor that initializes the first/second fields.
        //

        public Pair(T first, U second)
        {
            m_first = first;
            m_second = second;
        }

        //-----------------------------------------------------------------------------------
        // Accessors for the left and right data.
        //

        public T First
        {
            get { return m_first; }
            set { m_first = value; }
        }

        public U Second
        {
            get { return m_second; }
            set { m_second = value; }
        }

    }
}
