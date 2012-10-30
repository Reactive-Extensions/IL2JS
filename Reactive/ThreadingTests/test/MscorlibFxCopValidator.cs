using System;
using System.Diagnostics;
using System.IO;
namespace plinq_devtests
{
    internal static class FxCopValidator
    {
        internal static bool RunFxCopValidator()
        {
            TestHarness.TestLog("Running FxCop validation for mscorlib.dll, this might take several minutes.");

            string binPath = Environment.GetEnvironmentVariable("_NTTREE");
            if (binPath == null || binPath.Length == 0)
            {
                TestHarness.TestLog(" *This test must be run from x86chk clrenv window");
                return true; // return true to avoid failure in the lap run since they are not using clrenv enlistment
            }

            if (Environment.GetEnvironmentVariable("_BuildArch").ToLower() != "x86")
            {
                TestHarness.TestLog("Only x86chk is supported to run this test");
                return true; // return true to avoid blocking checkin if the dev knows what he is doing
            }
            if (Environment.GetEnvironmentVariable("_BuildType").ToLower() != "chk")
            {
                TestHarness.TestLog("Only x86chk build is supported to run this test");
                return true; // return true to avoid blocking checkin if the dev knows what he is doing
            }

            if (!File.Exists(binPath + "\\mscorlib.dll"))
            {
                TestHarness.TestLog(" *Test failed, Can't find the compiled mscorlib.dll in the bin directory");
                return false;
            }

            File.Copy(binPath + "\\mscorlib.dll", "mscorlib.dll", true);
            Process p = new Process();

            string ddSuitesPath = Environment.GetEnvironmentVariable("DD_SuiteRoot");
            p.StartInfo.FileName = ddSuitesPath + @"\src\FxCop\Tests\Common\FxCopTester";
            p.StartInfo.Arguments = @"/p:mscorlib.fxcop /d:" + ddSuitesPath + @"\src\FxCop\Excludes\Triaged /d:" + ddSuitesPath + @"\src\FxCop\Excludes\EverettBreaking " +
                "/t:mscorlib.dll.lst /b:" + ddSuitesPath + @"\src\clr\x86\devunit\FxCop\Mscorlib\BaselineExcludes.lst " +
                "/e:mscorlib.dll-breaking.xml /c:" + ddSuitesPath + @"\src\FxCop\Dictionaries\DevDivCustomDictionary.xml /ret:100 /verbose";

            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.Start();
            string output = p.StandardOutput.ReadToEnd();

            using (StreamWriter writer = new StreamWriter("mscorlibFxCopViolationResult.txt"))
            {
                writer.Write(output);
            }
            if (output.IndexOf("FxCop Verification Failed") > -1)
            {
                TestHarness.TestLog("FxCop failed. Check mscorlibFxCopViolationResult.txt");
                return false;
            }
            try
            {
                File.Delete("mscorlib.dll");
            }
            catch (IOException)
            {
            }
            TestHarness.TestLog("FxCop passed");
            return true;

        }
    }
}


