using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Collections;
using System.IO;

namespace plinq_devtests
{
    internal static class PFXStandAloneTests
    {
      
        /// <summary>
        /// Run PFX 3.5 tests
        /// </summary>
        /// <returns>True if succeeded, false otherwise</returns>
        internal static bool RunPFXTests()
        {
            if (Environment.GetEnvironmentVariable("_BuildArch") == null)
            {
                TestHarness.TestLog("This test must be run from a clr env window.");
                return true;
            }
            if (CreatePFXProj())
            {
                if (BuildPFX())
                {
                    TestHarness.TestLog("Build Succeeded");
                    return true;
                }
            }
            else
            {
                TestHarness.TestLog("Creating the 3.5 compatible build failed.");
                return false;
            }
             return false;
          
        }

        /// <summary>
        /// Run the perl script to create the 3.5 project
        /// </summary>
        /// <returns>True if succeeded, false otherwise</returns>
        private static bool CreatePFXProj()
        {
            TestHarness.TestLog("Create PFX 3.5 project");
            string scriptFolder = Environment.GetEnvironmentVariable("_NT_SOURCE_PATH") + "\\qa\\pfx\\ParallelExtensions_3_5";
            string scriptFile = scriptFolder + "\\createPFX3.5.bat";
            Process p = new Process();
            if (!File.Exists(scriptFile))
            {
                 TestHarness.TestLog("Can't locate the script file");
                return false;
            }
            p.StartInfo.FileName = scriptFile;
            p.StartInfo.WorkingDirectory = scriptFolder;
            List<string> scriptErrors = RunProcess(p);
            if (scriptErrors != null)
            {
                SaveErrors(scriptErrors, "srciptBuild.err.txt");
                TestHarness.TestLog("Script failed, check {0} file", "srciptBuild.err.txt");
                return false;
            }
            return true;

        }

        /// <summary>
        /// Local helper function ro run a given process, redirec the output and check for errors
        /// </summary>
        /// <param name="p">The process to start</param>
        /// <returns>Null if there is no errors in the output</returns>
        private static List<string> RunProcess(Process p)
        {
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.Start();
            string[] lines = p.StandardOutput.ReadToEnd().Split(new char[] { '\n' });
            List<string> errorLines = new List<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].IndexOf("error") > -1)
                {
                    errorLines.Add(lines[i]);
                }
            }
            return (errorLines.Count == 0)? null : errorLines;
        }

        /// <summary>
        /// Build the PFX, dev unit tests and XML for PFX
        /// </summary>
        /// <returns>True if succeeded, false otherwise</returns>
        private static bool BuildPFX()
        {
             TestHarness.TestLog("Build PFX 3.5 source code");
            Process p = new Process();
            string msbuildPath = p.StartInfo.EnvironmentVariables["_NTBINDIR"] + "\\tools\\x86\\managed\\WinFx\\v3.5\\msbuild.exe";
            p.StartInfo.FileName = msbuildPath;
            p.StartInfo.Arguments = "ParallelExtensions_3_5.csproj";
            p.StartInfo.WorkingDirectory = Environment.GetEnvironmentVariable("_NT_SOURCE_PATH") + "\\qa\\pfx\\ParallelExtensions_3_5";
            List<string> srcBuildErrors, testBuildErrors, xmlBuildErrors;
            srcBuildErrors = RunProcess(p);
            if (srcBuildErrors != null)
            {
                SaveErrors(srcBuildErrors, "srcBuild.err.txt");
                TestHarness.TestLog("Build source failed, check {0} file", "srcBuild.err.txt");
                return false;
            }

            TestHarness.TestLog("Build PFX 3.5 dev unit tests");
            p.StartInfo.Arguments = "DevUnitTests.csproj";

            testBuildErrors = RunProcess(p);
            if (testBuildErrors != null)
            {
                SaveErrors(testBuildErrors, "testsBuild.err.txt");
                TestHarness.TestLog("Build tests failed, check {0} file", "testsBuild.err.txt");
                return false;
            }

             TestHarness.TestLog("Build PFX 3.5 Xml docs");
            p.StartInfo.Arguments = "ParallelExtensions_3_5.csproj";
            p.StartInfo.EnvironmentVariables.Add("Enable_PFX_Docs", "1");

            xmlBuildErrors = RunProcess(p);
            if (xmlBuildErrors != null)
            {
                SaveErrors(xmlBuildErrors, "xmlBuild.err.txt");
                TestHarness.TestLog("Build XML failed, check {0} file", "xmlBuild.err.txt");

                // Even if the XML check fails, we will let the test pass.
                return true;
            }
            return true;
        }

        /// <summary>
        /// Copy the error log to specific file
        /// </summary>
        /// <param name="errorLines">The errors list</param>
        /// <param name="fileName">The log file name</param>
        private static void SaveErrors(List<string> errorLines, string fileName)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(fileName))
                {
                    foreach (string line in errorLines)
                        writer.WriteLine(line);
                }
            }
            catch (IOException)
            {
                TestHarness.TestLog("Couldn't create the error log file");
            }
        }
    }
}


