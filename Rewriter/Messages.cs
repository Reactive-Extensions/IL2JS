//
// Something to tell the user about
//

using System.Text;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.Rewriter
{
    public class UsageMessage : Message
    {
        public string Opt;

        public UsageMessage(string opt) : base(null, Severity.Error, "3001") {
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
            sb.AppendLine("Rewrite assembly to implement JavaScript interop. Version 0.8.");
            sb.AppendLine("Usage:");
            sb.AppendLine("  il2jsr <option>...");
            sb.AppendLine("where <option> includes:");
            sb.AppendLine("  -allWarns");
            sb.AppendLine("     Enable all warnings.");
            sb.AppendLine("  +/-delaySign (DEFAULT +)");
            sb.AppendLine("     If +, delay sign rewritten assembly");
            sb.AppendLine("  -help");
            sb.AppendLine("     Show this help");
            sb.AppendLine("  -inDir <dirname> (DEFAULT current directory)");
            sb.AppendLine("     Where to find input assemblies");
            sb.AppendLine("  -keyFile <filename>");
            sb.AppendLine("     Sign output assembly using key in file");
            sb.AppendLine("  -out <filename> (REQUIRED, cannot be same as rewrite filename)");
            sb.AppendLine("     Where to save rewitten assembly");
            sb.AppendLine("  -reference <filename>");
            sb.AppendLine("     Location of assemblies which are referenced from assembly being rewritten");
            sb.AppendLine("  -rewrite <filename> (REQUIRED)");
            sb.AppendLine("     Location of assembly to rewrite");
            sb.AppendLine("  -root <identifier> (DEFAULT 'IL2JSR')");
            sb.AppendLine("     Name of global identifier to hold interop runtime structure");
            sb.AppendLine("  -noWarn <id>");
            sb.AppendLine("     Supress warning with <id>");
            sb.AppendLine("  -warn <id>");
            sb.AppendLine("     Enable warning with <id>");
            sb.AppendLine("  @<filename>");
            sb.AppendLine("     Include options from file with <filename>");
        }
    }

    // ----------------------------------------------------------------------
    //  Assembly loading
    // ----------------------------------------------------------------------

    public class DuplicateSpecialAssemblyMessage : Message
    {
        public readonly string SimpleAssemblyName;
        public readonly string ExistingFileName;
        public readonly string NewFileName;

        public DuplicateSpecialAssemblyMessage(string simpleAssemblyName, string existingFileName, string thisFileName)
            : base(null, Severity.Error, "3002")
        {
            SimpleAssemblyName = simpleAssemblyName;
            ExistingFileName = existingFileName;
            NewFileName = thisFileName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("The special '");
            sb.Append(SimpleAssemblyName);
            sb.Append("' assembly has already been loaded from file '");
            sb.Append(ExistingFileName);
            sb.Append("' and cannot be reloaded from flie '");
            sb.Append(NewFileName);
            sb.Append("'");
        }
    }

    public class MissingSpecialAssemblyMessage : Message
    {
        public readonly string SimpleAssemblyName;

        public MissingSpecialAssemblyMessage(string simpleAssemblyName)
            : base(null, Severity.Error, "3003")
        {
            SimpleAssemblyName = simpleAssemblyName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("No assembly name resembled the special '");
            sb.Append(SimpleAssemblyName);
            sb.Append("' assembly");
        }
    }

    public class FoundSpecialAssemblyMessage : Message
    {
        public readonly string SimpleAssemblyName;
        public readonly string AssemblyName;

        public FoundSpecialAssemblyMessage(string simpleAssemblyName, string assemblyName)
            : base(null, Severity.Warning, "3004")
        {
            SimpleAssemblyName = simpleAssemblyName;
            AssemblyName = assemblyName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Using assembly '");
            sb.Append(AssemblyName);
            sb.Append("' for the special '");
            sb.Append(SimpleAssemblyName);
            sb.Append("' assembly");
        }
    }

    public class UnloadableAssemblyMessage : Message
    {
        public readonly string FileName;
        public readonly string Message;

        public UnloadableAssemblyMessage(string fileName, string message)
            : base(null, Severity.Error, "3005")
        {
            FileName = fileName;
            Message = message;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Unable to load assembly from file '");
            sb.Append(FileName);
            sb.Append("'");
            if (!string.IsNullOrEmpty(Message))
            {
                sb.Append(": ");
                sb.Append(Message);
            }
        }
    }

    public class UnresolvableReferenceMessage : Message
    {
        public readonly string SourceAssemblyName;
        public readonly string SourceFileName;
        public readonly string TargetAssemblyName;

        public UnresolvableReferenceMessage(string sourceAssemblyName, string sourceFileName, string targetAssemblyName)
            : base(null, Severity.Error, "3006")
        {
            SourceAssemblyName = sourceAssemblyName;
            SourceFileName = sourceFileName;
            TargetAssemblyName = targetAssemblyName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Unable to resolve assembly reference to '");
            sb.Append(TargetAssemblyName.ToString());
            sb.Append("' from assembly '");
            sb.Append(SourceAssemblyName.ToString());
            sb.Append("' loaded from file '");
            sb.Append(SourceFileName);
            sb.Append("'");
        }
    }

    public class DuplicateAssemblyStrongNameMessage : Message
    {
        public readonly string NewFileName;
        public readonly string AssemblyName;
        public readonly string ExistingFileName;

        public DuplicateAssemblyStrongNameMessage(string newFileName, string assemblyName, string existingFileName)
            : base(null, Severity.Error, "3007")
        {
            NewFileName = newFileName;
            AssemblyName = assemblyName;
            ExistingFileName = existingFileName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Assembly at file '");
            sb.Append(NewFileName);
            sb.Append("' has strong name '");
            sb.Append(AssemblyName.ToString());
            sb.Append("' which has already been loaded from file '");
            sb.Append(ExistingFileName);
            sb.Append("'");
        }
    }

    public class DuplicateAssemblyFileNameMessage : Message
    {
        public readonly string OriginalFileName;
        public readonly string CanonicalFileName;

        public DuplicateAssemblyFileNameMessage(string originalFileName, string canonicalFileName)
            : base(null, Severity.Error, "3008")
        {
            OriginalFileName = originalFileName;
            CanonicalFileName = canonicalFileName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Assembly at file '");
            sb.Append(OriginalFileName);
            sb.Append("' has already been loaded from canonical path '");
            sb.Append(CanonicalFileName);
            sb.Append("'");
        }
    }

    public class LoadedAssemblyMessage : Message
    {
        public readonly string AssemblyName;
        public readonly string FileName;

        public LoadedAssemblyMessage(string assemblyName, string fileName)
            : base(null, Severity.Warning, "3009")
        {
            AssemblyName = assemblyName;
            FileName = fileName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Successfully loaded assembly '");
            sb.Append(AssemblyName);
            sb.Append("' from file '");
            sb.Append(FileName);
            sb.Append("'");
        }
    }

    public class SavedAssemblyMessage : Message
    {
        public readonly string AssemblyName;
        public readonly string FileName;

        public SavedAssemblyMessage(string assemblyName, string fileName)
            : base(null, Severity.Warning, "3010")
        {
            AssemblyName = assemblyName;
            FileName = fileName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Successfully saved assembly '");
            sb.Append(AssemblyName);
            sb.Append("' to file '");
            sb.Append(FileName);
            sb.Append("'");
        }
    }

    public class AssemblySigningErrorMessage : Message
    {
        public readonly string AssemblyName;
        public readonly string Message;

        public AssemblySigningErrorMessage(string assemblyName, string message)
            : base(null, Severity.Warning, "3011")
        {
            AssemblyName = assemblyName;
            Message = message;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Unable to sign assembly '");
            sb.Append(AssemblyName);
            sb.Append("': ");
            sb.Append(Message);
        }
    }

    // ----------------------------------------------------------------------
    // Interop
    // ----------------------------------------------------------------------

    public class InvalidInteropMessage : Message
    {
        public readonly string Reason;

        public InvalidInteropMessage(MessageContext ctxt, string reason)
            : base(ctxt, Severity.Error, "3012")
        {
            Reason = reason;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Invalid JavaScript interop specification");
            if (!string.IsNullOrEmpty(Reason))
            {
                sb.Append(": ");
                sb.Append(Reason);
            }
        }
    }

    public class InteropInfoMessage : Message
    {
        public readonly string Message;

        public InteropInfoMessage(MessageContext ctxt, string message)
            : base(ctxt, Severity.Warning, "3013")
        {
            Message = message;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Interop");
            if (!string.IsNullOrEmpty(Message))
            {
                sb.Append(": ");
                sb.Append(Message);
            }
        }
    }

    public class CopiedFileMessage : Message
    {
        public readonly string Source;
        public readonly string Target;

        public CopiedFileMessage(string source, string target)
            : base(null, Severity.Warning, "3010")
        {
            Source = source;
            Target = target;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Successfully copied file '");
            sb.Append(Source);
            sb.Append("' to file '");
            sb.Append(Target);
            sb.Append("'");
        }
    }


}

