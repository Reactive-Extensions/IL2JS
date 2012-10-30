using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace plinq_devtests
{
    public static class PfxPerfBVT
    {
        //constants for PerfBVT DevUnitTest 
        static readonly string DEV_UNIT_TESTS_PATH = Environment.GetEnvironmentVariable("_NTDRIVE")
            + Environment.GetEnvironmentVariable("_NTROOT")
            + @"\qa\pfx\DevUnitTests";
        const string OUTPUT_FILE_NAME = "TempBvtResult.csv";
        const string BEFORE_FILE_NAME = "PfxPerfBvt_Before.csv";
        const string AFTER_FILE_NAME = "PfxPerfBvt_After.csv";
        static readonly string OUTPUT_FILE_PATH = DEV_UNIT_TESTS_PATH + "\\" + OUTPUT_FILE_NAME;

        static readonly string TF_PATH = Environment.GetEnvironmentVariable("_NTDRIVE")
            + Environment.GetEnvironmentVariable("_NTROOT")
            + @"\tools\x86\managed\v4.0\tf.cmd";

        //constants for test executables. Executables are all stored in one folder : EXE_BIN_PATH
        static readonly string EXE_BIN_PATH = Environment.GetEnvironmentVariable("VSPATH") + "\\PCP\\pfx\\tests";

        static readonly string CDS_EXE_PATH = EXE_BIN_PATH + @"\CDSperf.exe";
        const string CDS_EXE_ARG = "spawn /yetiMsgOff /csvResultView /pri:0";

        static readonly string PLINQ_EXE_PATH = EXE_BIN_PATH + @"\PFXDevPerfTests.exe";
        const string PLINQ_EXE_ARG = "/plinqbvt";

        static readonly string TPL_EXE_PATH = EXE_BIN_PATH + @"\PFXDevPerfTests.exe";
        const string TPL_EXE_ARG = "/tpl";

        //constants for output file formating
        const string CDS_HEADER = "======== CDS BVTs ========";
        const string CDS_FOOTER = "======== END OF CDS BVTs ========";
        const string PLINQ_HEADER = "======== PLINQ BVTs ========";
        const string PLINQ_FOOTER = "======== END OF PLINQ BVTs ========";
        const string TPL_HEADER = "======== TPL BVTs ========";
        const string TPL_FOOTER = "======== END OF TPL BVTs ========";

        public static bool RunPerfBVT()
        {
            TestHarness.TestLog(@"* Run PfxPerfBvts. This test always returns true even if perf runs are not successful or result file has invalid format
     * This test will generate a result file named PfxPerfBvt.csv, please check in this file with your changes to the product code
     * Also at the end of PfxPerfBvt.csv, please specify your changes that you think will affect performance");

            //validating build info
            if (Environment.GetEnvironmentVariable("_BuildType") != "ret")
            {
                TestHarness.TestLog("Error: Perf BVTs must run under retail build");
                return true;
            }

            //we need to use this boolean variable, rather than return directly in the #IF #ENDIF block
            //otherwise in the CHK build, the compiler will complain unreachable code after the return statement.
            bool isCHKBuild = false;
#if DEBUG
            isCHKBuild=true;
#endif
            if (isCHKBuild)
            {
                TestHarness.TestLog("Error: DevUnitTest must be compiled under retail build to run perf BVTs");
                return true;
            }
            using (Process p = new Process())
            {
                p.StartInfo.RedirectStandardError = false;
                p.StartInfo.RedirectStandardOutput = false;
                p.StartInfo.UseShellExecute = false;

                TestHarness.TestLog("tf edit " + AFTER_FILE_NAME);
                p.StartInfo.WorkingDirectory = DEV_UNIT_TESTS_PATH;
                p.StartInfo.FileName = TF_PATH;
                p.StartInfo.Arguments = "edit " + AFTER_FILE_NAME;
                p.Start();
                p.WaitForExit();

                TestHarness.TestLog("tf edit " + BEFORE_FILE_NAME);
                p.StartInfo.WorkingDirectory = DEV_UNIT_TESTS_PATH;
                p.StartInfo.FileName = TF_PATH;
                p.StartInfo.Arguments = "edit " + BEFORE_FILE_NAME;
                p.Start();
                p.WaitForExit();

                // Create (if not existing) or overwrite PfxBvtResults.csv.
                using (StreamWriter sw = File.CreateText(OUTPUT_FILE_PATH))
                { }

                p.Close();
            }

            //Print header to the output file
            using (StreamWriter sw = File.AppendText(OUTPUT_FILE_PATH))
            {
                sw.WriteLine("Time: {0}, Machine: {1}, Build: {2}{3}, User: {4}",
                    DateTime.Now.ToString(),
                    Environment.GetEnvironmentVariable("COMPUTERNAME"),
                    Environment.GetEnvironmentVariable("_BuildArch"),
                    Environment.GetEnvironmentVariable("_BuildType"),
                    Environment.GetEnvironmentVariable("USERNAME"));
                sw.WriteLine();
                sw.Flush();


                //----- run CDS BVT -----
                TestHarness.TestLog("Running CDS Perf BVT......");
                TestHarness.TestLog("{0} {1}", CDS_EXE_PATH, CDS_EXE_ARG);
                sw.WriteLine(CDS_HEADER);
                if (!RunTest(EXE_BIN_PATH, CDS_EXE_PATH, CDS_EXE_ARG, sw))
                {
                    TestHarness.TestLog("CDS Perf BVT tests failed, but we ignore the failure and proceed to Plinq BVT");
                }
                sw.WriteLine(CDS_FOOTER);
                sw.WriteLine();
                sw.Flush();

                //----- run PLINQ BVT -----
                TestHarness.TestLog("Running PLINQ Perf BVT......");
                TestHarness.TestLog("{0} {1}", PLINQ_EXE_PATH, PLINQ_EXE_ARG);
                sw.WriteLine(PLINQ_HEADER);
                if (!RunTest(EXE_BIN_PATH, PLINQ_EXE_PATH, PLINQ_EXE_ARG, sw))
                {
                    TestHarness.TestLog("Plinq Perf BVT tests failed, but we ignore the failure and proceed to TPL BVT");
                }
                sw.WriteLine(PLINQ_FOOTER);
                sw.WriteLine();
                sw.Flush();

                //----- run TPL BVT -----
                TestHarness.TestLog("Running TPL Perf BVT......");
                TestHarness.TestLog("{0} {1}", TPL_EXE_PATH, TPL_EXE_ARG);
                sw.WriteLine(TPL_HEADER);
                if (!RunTest(EXE_BIN_PATH, TPL_EXE_PATH, TPL_EXE_ARG, sw))
                {
                    TestHarness.TestLog("TPL Perf BVT failed");
                }
                sw.WriteLine(TPL_FOOTER);
                sw.WriteLine();


                sw.WriteLine("*** Below please specify the major changes you have made that would have affect performance ");
            }
            return true;
        }

        internal static bool RunTest(string workDir, string exeFileName, string arg, StreamWriter sw)
        {
            if (!File.Exists(exeFileName))
            {
                TestHarness.TestLog("Error: " + exeFileName + " not found! Make sure you have built all required tests");
                return false;
            }
            int exitcode;
            using (Process p = new Process())
            {
                p.StartInfo.RedirectStandardError = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.WorkingDirectory = workDir;
                p.StartInfo.FileName = exeFileName;
                p.StartInfo.Arguments = arg;
                p.Start();
                ProcessOutput(p.StandardOutput, sw, true);
                p.WaitForExit();
                exitcode = p.ExitCode;
                p.Close();
            }
            if (exitcode != 0)
            {
                TestHarness.TestLog("Error: test process failed with exit code {0}", exitcode);
                return false;
            }
            else
                return true;
        }

        static void ProcessOutput(StreamReader sr, StreamWriter sw, bool echoToConsole)
        {
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine().Trim();
                if (echoToConsole)
                {
                    Console.WriteLine(line);
                }
                sw.WriteLine(line);
                sw.Flush();
            }
        }
    }
}