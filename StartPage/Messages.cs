using System.Text;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.StartPage
{
    public class UsageMessage : Message
    {
        public string Opt;

        public UsageMessage(string opt)
            : base(null, Severity.Error, "4001")
        {
            Opt = opt;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            if (!string.IsNullOrEmpty(Opt))
            {
                base.AppendMessage(sb);
                sb.Append("Bad option '");
                sb.Append(Opt);
                sb.AppendLine("'");
            }
            sb.AppendLine("Create a redirecting HTML start page.");
            sb.AppendLine("Usage:");
            sb.AppendLine("  il2jsp <option>...");
            sb.AppendLine("where <option> includes:");
            sb.AppendLine("  -inDir <dirname>");
            sb.AppendLine("     Directory containing HTML template (DEFAULT '.')");
            sb.AppendLine("  -outDir <dirname>");
            sb.AppendLine("     Directory containing compiled JavaScript (DEFAULT '.')");
            sb.AppendLine("  -template <filename>");
            sb.AppendLine("     Name of template HTML file (DEFAULT 'main.html')");
            sb.AppendLine("  -redirector <filename>");
            sb.AppendLine("     Base part of redirectory page name (DEFAULT 'Start')");
            sb.AppendLine("  -manifest <filename>");
            sb.AppendLine("     Name of manifest file (DEFAULT 'manifest.txt')");
            sb.AppendLine("  -help");
            sb.AppendLine("     Show this help.");
            sb.AppendLine("  @<filename>");
            sb.AppendLine("     Include options from file with <filename>.");
        }
    }

    public class InvalidTemplatePageMessage : Message
    {
        public InvalidTemplatePageMessage(string file, int line)
            : base(new MessageContext(null, new Location(file, 0, line, 0, 0, line, 0), null), Severity.Error, "4002")
        {
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.AppendLine("Ill-formed html template page");
        }
    }
}