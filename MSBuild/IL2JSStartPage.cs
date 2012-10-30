using System.Text;

namespace Microsoft.LiveLabs.JavaScript.IL2JS.Tasks
{
    public class IL2JSStartPage : IL2JSTask
    {
        //
        // Options
        //

        public string InputDirectory { get; set; }
        public string OutputDirectory { get; set; }
        public string TemplatePage { get; set; }
        public string RedirectorName { get; set; }
        public string ManifestName { get; set; }

        protected override string ToolName { get { return "il2jsp.exe"; } }

        protected override string GenerateResponseFileCommands()
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(InputDirectory))
                AppendStringOpt(sb, "inDir", InputDirectory);

            if (!string.IsNullOrEmpty(OutputDirectory))
                AppendStringOpt(sb, "outDir", OutputDirectory);

            if (!string.IsNullOrEmpty(TemplatePage))
                AppendStringOpt(sb, "template", TemplatePage);

            if (!string.IsNullOrEmpty(RedirectorName))
                AppendStringOpt(sb, "redirector", RedirectorName);

            if (!string.IsNullOrEmpty(ManifestName))
                AppendStringOpt(sb, "manifest", ManifestName);

            return sb.ToString();
        }
    }
}
