//
// Something to tell the user about
//

using System.Text;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    public class UsageMessage : Message
    {
        public string Msg;

        public UsageMessage(string msg)
            : base(null, Severity.Error, "2001")
        {
            Msg = msg;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            if (!string.IsNullOrEmpty(Msg))
            {
                base.AppendMessage(sb);
                sb.AppendLine(Msg);
            }
            sb.AppendLine("Compile CLR assemblies to JavaScript. Version 0.8.");
            sb.AppendLine("Usage:");
            sb.AppendLine("  il2jsc <option>...");
            sb.AppendLine("where <option> includes:");
            sb.AppendLine("  -assemblyNameResolution (name | nameVersion | full) (DEFAULT name)");
            sb.AppendLine("     Set the resoultion of assembly strong names.");
            sb.AppendLine("  -allWarns");
            sb.AppendLine("     Enable all warnings.");
            sb.AppendLine("  -augment <filename>");
            sb.AppendLine("     Assembly in <filename> contributes to an augmented assembly.");
            sb.AppendLine("  +/-breakOnBreak (DEFAULT -)");
            sb.AppendLine("     If +, break in compiler when see 'Break' custom attribute.");
            sb.AppendLine("     For debugging of compiler only.");
            sb.AppendLine("  +/-clrArraySemantics (DEFAULT +)");
            sb.AppendLine("     If +, perform null, index bounds, and assignability checks on array access.");
            sb.AppendLine("  +/-clrInteropExceptions (DEFAULT +)");
            sb.AppendLine("     If +, JavaScript exceptions throw from imported methods are raised as");
            sb.AppendLine("     CLR exceptions, and vise-versa for exported methods.");
            sb.AppendLine("  +/-clrNullVirtcallSemantics (DEFAULT +)");
            sb.AppendLine("     If +, mimic the semantics of the CLR when checking whether the target");
            sb.AppendLine("     of a virtual call is non-null.");
            sb.AppendLine("  -compile <filename>");
            sb.AppendLine("     Compile assembly <filename> to JavaScript.");
            sb.AppendLine("  +/-debug (DEFAULT -)");
            sb.AppendLine("     If +, generate annotated and readable JavaScript suitable for debugging.");
            sb.AppendLine("  -debugLevel <int> (DEFAULT 0)");
            sb.AppendLine("     In debug mode, control when to break into debugger on an exception:");
            sb.AppendLine("       0: never");
            sb.AppendLine("       1: on JavaScript exceptions only");
            sb.AppendLine("       2: on JavaScript and .Net exceptions");
            sb.AppendLine("  -debugTrace <filename>");
            sb.AppendLine("     Trace operation of complier to given file, or stdout if '-'.");
            sb.AppendLine("  -finalTraceName <name>");
            sb.AppendLine("     Name for final trace, containing all definitions not yet included in");
            sb.AppendLine("     other traces.");
            sb.AppendLine("  -help");
            sb.AppendLine("     Print this usage.");
            sb.AppendLine("  -inDir <pathname> (DEFAULT current directory)");
            sb.AppendLine("     Set all assembly file names to be relative to this input directory.");
            sb.AppendLine("  -initialTrace <filename>");
            sb.AppendLine("     Definitions in this trace are placed in the loader JavaScript file.");
            sb.AppendLine("  -importInlineThreshold <int>");
            sb.AppendLine("     Maximum size of inlinable imported methods, or -1 to disable inlining.");
            sb.AppendLine("  -inlineThreshold <int>");
            sb.AppendLine("     Maximum size of inlinable methods, or -1 to disable inlining.");
            sb.AppendLine("  -loadPath <pathname>");
            sb.AppendLine("     Include <pathname> in search for JavaScript files.");
            sb.AppendLine("  -mode <modename>");
            sb.AppendLine("     Set compilation mode, one of:");
            sb.AppendLine("       collecting");
            sb.AppendLine("       traced");
            sb.AppendLine("       plain (DEFAULT)");
            sb.AppendLine("  -noWarn <id>");
            sb.AppendLine("     Supress warnings with <id>");
            sb.AppendLine("  -original <name>");
            sb.AppendLine("     Include definitions of original assembly with <name> strong name in");
            sb.AppendLine("     tail of augmentations");
            sb.AppendLine("  -outDir <dirname>");
            sb.AppendLine("     If given, the directory to hold all output JavaScript directories.");
            sb.AppendLine("     Otherwise, no JavaScript is output.");
            sb.AppendLine("  +/-prettyPrint (DEFAULT same as debug)");
            sb.AppendLine("     If +, pretty-print JavaScript output");
            sb.AppendLine("  -reference <filename>");
            sb.AppendLine("     Use assembly <filename> when resolving references, and check");
            sb.AppendLine("     it's compilation is up-to-date.");
            sb.AppendLine("  -rename <srcname> <tgtname>");
            sb.AppendLine("     Assemblies with <srcname> strong name are renamed to <tgtname> strong name.");
            sb.AppendLine("     Renames are applied to both augmentation assemblies and reference/compile");
            sb.AppendLine("     assemblies. If <tgtname> is empty, assembly with <srcname> is considered");
            sb.AppendLine("     unavailable.");
            sb.AppendLine("  -root <identifier> (DEFAULT 'IL2JSC')");
            sb.AppendLine("     Name of global identifier to hold IL2JS root runtime structure.");
            sb.AppendLine("  +/-safeInterop (DEFAULT +)");
            sb.AppendLine("     If +, check the types of all imported JavaScript objects");
            sb.AppendLine("  +/-skipUpToDate (DEFAULT -)");
            sb.AppendLine("     If +, only compile assemblies which are newer than their existing JavaScript.");
            sb.AppendLine("  -target <target>");
            sb.AppendLine("     Set target of compilation, one of:");
            sb.AppendLine("       browser (DEFAULT)");
            sb.AppendLine("       cscript");
            sb.AppendLine("  -trace <filename>");
            sb.AppendLine("     Definitions in this trace are placed in their own JavaScript file");
            sb.AppendLine("  -warn <id>");
            sb.AppendLine("     Enable warnings with <id>");
            sb.AppendLine("  @<filename>");
            sb.AppendLine("     Include options from file with <filename>.");
        }
    }

    // ----------------------------------------------------------------------
    // Assemblies
    // ----------------------------------------------------------------------

    public class OutOfDateAssemblyMessage : Message
    {
        public readonly CST.AssemblyName AssemblyName;
        public readonly string AssemblyFileName;
        public readonly string JavaScriptFileName;
        
        public OutOfDateAssemblyMessage(CST.AssemblyName assemblyName, string assemblyFileName, string javaScriptFileName)
            : base(null, Severity.Error, "2002")
        {
            AssemblyName = assemblyName;
            AssemblyFileName = assemblyFileName;
            JavaScriptFileName = javaScriptFileName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Assembly '");
            sb.Append(AssemblyName);
            sb.Append("' at '");
            sb.Append(AssemblyFileName);
            sb.Append("' is newer than its compiled form at '");
            sb.Append(JavaScriptFileName);
            sb.Append("'");
        }
    }

    public class AssemblyNotCompiledMessage : Message
    {
        public readonly CST.AssemblyName AssemblyName;
        public readonly string JavaScriptFileName;

        public AssemblyNotCompiledMessage(CST.AssemblyName assemblyName, string javaScriptFileName)
            : base(null, Severity.Error, "2003")
        {
            AssemblyName = assemblyName;
            JavaScriptFileName = javaScriptFileName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Could not find complied form for assembly '");
            sb.Append(AssemblyName);
            sb.Append("' at '");
            sb.Append(JavaScriptFileName);
            sb.Append("'");
        }
    }


    // ----------------------------------------------------------------------
    // Interop
    // ----------------------------------------------------------------------

    public class InvalidInteropMessage : Message
    {
        public readonly string Reason;

        public InvalidInteropMessage(MessageContext ctxt, string reason)
            : base(ctxt, Severity.Error, "2004")
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
            : base(ctxt, Severity.Warning, "2005")
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

    public class UnimplementableFeatureMessage : Message
    {
        public readonly string Feature;
        public readonly string Reason;

        public UnimplementableFeatureMessage(MessageContext ctxt, string feature, string reason)
            : base(ctxt, Severity.Warning, "2006")
        {
            Feature = feature;
            Reason = reason;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Unimplementable ");
            sb.Append(Feature);
            sb.Append(": ");
            sb.Append(Reason);
        }
    }

    // ----------------------------------------------------------------------
    // Skipping definitions
    // ----------------------------------------------------------------------

    public class UnusedDefinitionMessage : Message
    {
        public UnusedDefinitionMessage(MessageContext ctxt)
            : base(ctxt, Severity.Warning, "2007")
        {
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Skipping compilation of unused definition");
        }
    }

    public class InlinedDefinitionMessage : Message
    {
        public InlinedDefinitionMessage(MessageContext ctxt)
            : base(ctxt, Severity.Warning, "2008")
        {
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Skipping compilation of inlined definition");
        }
    }

    // ----------------------------------------------------------------------
    // Compilation 
    // ----------------------------------------------------------------------

    public class SkippingJavaScriptComplilation : Message
    {
        public readonly CST.AssemblyName AssemblyName;
        public readonly string Reason;

        public SkippingJavaScriptComplilation(CST.AssemblyName assemblyName, string reason)
            : base(null, Severity.Warning, "2009")
        {
            AssemblyName = assemblyName;
            Reason = reason;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Skipping compilation of assembly '");
            sb.Append(AssemblyName);
            sb.Append("': ");
            sb.Append(Reason);
        }
    }

    public class GeneratedJavaScriptFile : Message
    {
        public readonly string What;
        public readonly string FileName;

        public GeneratedJavaScriptFile(string what, string fileName)
            : base(null, Severity.Warning, "2010")
        {
            What = what;
            FileName = fileName;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Compiled ");
            sb.Append(What);
            sb.Append(" to file '");
            sb.Append(FileName);
            sb.Append("'");
        }
    }

    public class LossOfPrecisionMessage : Message
    {
        public readonly string Message;

        public LossOfPrecisionMessage(MessageContext ctxt, string message)
            : base(ctxt, Severity.Warning, "2011")
        {
            Message = message;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("JavaScript cannot faithfully represent result of arithmetic expression");
            if (!string.IsNullOrEmpty(Message))
            {
                sb.Append(": ");
                sb.Append(Message);
            }
        }
    }

    // ----------------------------------------------------------------------
    // Traces
    // ----------------------------------------------------------------------

    public class InvalidTraceMessage : Message
    {
        public readonly string Message;

        public InvalidTraceMessage(MessageContext ctxt, string message)
            : base(ctxt, Severity.Error, "2012")
        {
            Message = message;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Invalid trace file: ");
            sb.Append(Message);
        }
    }

    public class DuplicateTraceEntryMessage : Message {
        public readonly string PreviousTrace;
        public readonly string CurrentTrace;

        public DuplicateTraceEntryMessage(MessageContext ctxt, string previousTrace, string currentTrace) :
            base(ctxt, Severity.Warning, "2013")
        {
            PreviousTrace = previousTrace;
            CurrentTrace = currentTrace;
        }

        public override void AppendMessage(StringBuilder sb)
        {
            base.AppendMessage(sb);
            sb.Append("Ignoring definition in trace '");
            sb.Append(CurrentTrace);
            sb.Append("' since it already appears in trace '");
            sb.Append(PreviousTrace);
            sb.Append("'");
        }
    }
}

