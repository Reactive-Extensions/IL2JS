using System;
using System.IO;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Win32;

namespace Microsoft.LiveLabs.JavaScript.IL2JS.Tasks
{
    public abstract class IL2JSTask : ToolTask
    {
        private const string regPath = @"Microsoft\IL2JS";

        protected static string RootPath()
        {
            var rootPath = Environment.GetEnvironmentVariable("IL2JSROOT");
            if (string.IsNullOrEmpty(rootPath))
            {
                using (var win32key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\" + regPath))
                {
                    using (var win64key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\" + regPath))
                    {
                        var key = win32key ?? win64key;
                        if (key != null)
                            rootPath = (string)key.GetValue("Root");
                    }
                }
            }
            if (String.IsNullOrEmpty(rootPath))
                throw new InvalidOperationException(String.Format("unable to determine location of IL2JS binaries"));
            return rootPath;
        }

        protected override string GenerateFullPathToTool()
        {
            return Path.Combine(Path.Combine(RootPath(), "bin"), ToolName);
        }

        protected static void AppendStringOpt(StringBuilder sb, string opt, string val)
        {
            sb.Append('-');
            sb.Append(opt);
            sb.Append(" \"");
            sb.Append(val.Replace("\"", "\"\""));
            sb.AppendLine("\"");
        }
    }
}
