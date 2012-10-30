using System;
using System.Text;
using Microsoft.Build.Framework;

namespace Microsoft.LiveLabs.JavaScript.IL2JS.Tasks
{
    public class IL2JSCompiler : IL2JSTask
    {
        //
        // Options
        //

        public string Options { get; set; }
        public string Configuration { get; set; }
        public string Platform { get; set; }
        public ITaskItem[] CompileFileNames { get; set; }
        public ITaskItem[] ReferenceFileNames { get; set; }
        public string InputDirectory { get; set; }
        public string OutputDirectory { get; set; }

        protected override string ToolName { get { return "il2jsc.exe"; } }

        protected override string GenerateResponseFileCommands()
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(Configuration))
            {
                var config = Configuration.ToLowerInvariant();
                sb.AppendLine(@"@$IL2JSROOT\MSBuild\il2jsc.common." + config + ".opts");
            }

            if (!string.IsNullOrEmpty(Platform))
            {
                var mode = Platform.ToLowerInvariant();
                AppendStringOpt(sb, "mode", mode);
            }

            if (!string.IsNullOrEmpty(Options))
                sb.AppendLine(Options);

            if (ReferenceFileNames != null)
            {
                // Compile, even if listed as reference
                foreach (var item in ReferenceFileNames)
                    AppendStringOpt(sb, "compile", item.ItemSpec);
            }

            if (CompileFileNames != null)
            {
                foreach (var item in CompileFileNames)
                    AppendStringOpt(sb, "compile", item.ItemSpec);
            }

            if (!string.IsNullOrEmpty(InputDirectory))
                AppendStringOpt(sb, "inDir", InputDirectory);

            if (!string.IsNullOrEmpty(OutputDirectory))
                AppendStringOpt(sb, "outDir", OutputDirectory);

            return sb.ToString();
        }
    }
}
