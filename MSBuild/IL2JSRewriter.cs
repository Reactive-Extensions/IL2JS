using System.Text;
using Microsoft.Build.Framework;

namespace Microsoft.LiveLabs.JavaScript.IL2JS.Tasks
{
    public class IL2JSRewriter : IL2JSTask
    {
        //
        // Options
        //

        public string RewriteFileName { get; set; }
        public ITaskItem[] ReferenceFileNames { get; set; }
        public string InputDirectory { get; set; }
        public string OutputFileName { get; set; }
        public string KeyFile { get; set; }
        public bool DelaySign { get; set; }

        protected override string ToolName { get { return "il2jsr.exe"; } }       

        protected override string GenerateResponseFileCommands()
        {
            var sb = new StringBuilder();

            sb.AppendLine(@"@$IL2JSROOT\MSBuild\il2jsr.common.opts");

            if (!string.IsNullOrEmpty(RewriteFileName))
                AppendStringOpt(sb, "rewrite", RewriteFileName);

            if (ReferenceFileNames != null)
            {
                foreach (var item in ReferenceFileNames)
                    AppendStringOpt(sb, "reference", item.ItemSpec);
            }

            if (!string.IsNullOrEmpty(InputDirectory))
                AppendStringOpt(sb, "inDir", InputDirectory);

            if (!string.IsNullOrEmpty(OutputFileName))
                AppendStringOpt(sb, "out", OutputFileName);

            if (!string.IsNullOrEmpty(KeyFile))
                AppendStringOpt(sb, "keyFile", KeyFile);

            sb.AppendLine(DelaySign ? "+delaySign" : "-delaySign");

            return sb.ToString();
        }
    }
}
