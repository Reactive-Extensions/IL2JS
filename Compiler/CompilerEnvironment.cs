//
// Global environment used across the compiler
//

using System;
using System.IO;
using Microsoft.LiveLabs.Extras;
using CST = Microsoft.LiveLabs.CST;
using Microsoft.LiveLabs.JavaScript.IL2JS.Interop;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    public enum CompilationMode
    {
        Plain,
        // Record a trace of every assembly/type/method required
        Collecting,
        // Use traces to group definitions
        Traced
    }

    public enum Target
    {
        Browser,
        CScript
    }
    public class CompilerEnvironment
    {
        //
        // Options
        //

        public CST.AssemblyNameResolution AssemblyNameResolution;
        public bool BreakOnBreak;
        public string DebugTraceFileName;
        public bool DebugMode;
        public CompilationMode CompilationMode;
        public int DebugLevel;
        public bool PrettyPrint;
        public bool CLRArraySemantics;
        public bool CLRNullVirtcallSemantics;
        public bool CLRInteropExceptions;
        public bool SafeInterop;
        public string Root;
        public readonly ISeq<string> ReferenceFileNames;
        public readonly ISeq<string> CompileFileNames;
        public string InputDirectory;
        public string OutputDirectory;
        public readonly ISeq<string> LoadPaths;
        public bool SkipUpToDate;
        public Target Target;
        public readonly Seq<CST.RenameRule> RenameRules;
        public readonly Seq<string> AugmentationFileNames;
        public readonly Seq<string> OriginalStrongNames;
        public string InitialTraceFileName;
        public readonly ISeq<string> TraceFileNames;
        public string FinalTraceName;
        public int ImportInlineThreshold;
        public int InlineThreshold;
        public Set<string> NoWarns;

        //
        // Globals
        //

        public int NumWarnings;
        public int NumErrors;

        public CST.Global Global;
        private StreamWriter tracerStream;
        public Writer PrimTracer;
        public CST.CSTWriter Tracer;
        public ValidityContext Validity;

        // Special type for enumerators over built-in arrays
        public CST.TypeRef GenericEnumeratorTypeConstructorRef;

        // Special types used by interop
        public CST.TypeRef JSContextRef;
        public CST.TypeRef JSObjectRef;
        public CST.TypeRef JSPropertyRef;
        public CST.TypeRef JSExceptionRef;

        // Name to slot mappings
        public GlobalMapping GlobalMapping;

        public AttributeHelper AttributeHelper;

        public InteropManager InteropManager;

        public JSTHelpers JSTHelpers;

        public InlinedMethodCache InlinedMethods;
        public Traces Traces;

        public CompilerEnvironment()
        {
            AssemblyNameResolution = CST.AssemblyNameResolution.Name;
            BreakOnBreak = false;
            DebugTraceFileName = null;
            DebugMode = false;
            CompilationMode = CompilationMode.Plain;
            DebugLevel = 0;
            PrettyPrint = false;
            CLRArraySemantics = true;
            CLRNullVirtcallSemantics = true;
            CLRInteropExceptions = true;
            SafeInterop = true;
            Root = "IL2JSC";
            ReferenceFileNames = new Seq<string>();
            CompileFileNames = new Seq<string>();
            InputDirectory = ".";
            OutputDirectory = null;
            LoadPaths = new Seq<string>();
            SkipUpToDate = false;
            Target = Target.Browser;
            RenameRules = new Seq<CST.RenameRule>();
            AugmentationFileNames = new Seq<string>();
            OriginalStrongNames = new Seq<string>();
            InitialTraceFileName = null;
            TraceFileNames = new Seq<string>();
            FinalTraceName = null;
            ImportInlineThreshold = 15;
            InlineThreshold = 15;
            NoWarns = new Set<string>();
        }

        private CST.TypeRef MkRef(string name)
        {
            return new CST.NamedTypeRef(new CST.QualifiedTypeName(Global.MsCorLibName, CST.TypeName.FromReflectionName(name)));
        }

        public void SetupPrimTracer()
        {
            if (!string.IsNullOrEmpty(DebugTraceFileName))
            {
                var stream = default(Stream);
                if (DebugTraceFileName.Equals("-", StringComparison.Ordinal))
                    stream = Console.OpenStandardOutput();
                else
                    stream = new FileStream(DebugTraceFileName, FileMode.Create, FileAccess.Write);
                tracerStream = new StreamWriter(stream);
                PrimTracer = new Writer(tracerStream, true);
            }
        }

        public void SetupMetadata(CST.Global global)
        {
            NumWarnings = 0;
            NumErrors = 0;

            Global = global;

            if (tracerStream != null)
                Tracer = new CST.CSTWriter(global, CST.WriterStyle.Debug, tracerStream);

            Validity = new ValidityContext(this);

            GenericEnumeratorTypeConstructorRef = MkRef(Constants.GenericEnumeratorTypeConstructorName);
            JSContextRef = MkRef(Constants.JSContextName);
            JSObjectRef = MkRef(Constants.JSObjectName);
            JSPropertyRef = MkRef(Constants.JSPropertyName);
            JSExceptionRef = MkRef(Constants.JSExceptionName);

            GlobalMapping = new GlobalMapping(this);
            AttributeHelper = new AttributeHelper(this);
            InteropManager = new InteropManager(this);
            JSTHelpers = new JSTHelpers(this);
            InlinedMethods = new InlinedMethodCache(this);
            Traces = new Traces(this);
        }

        public void Teardown()
        {
            if (Tracer != null)
            {
                Tracer.Close();
                Tracer = null;
            }
        }

        public void Log(Message msg)
        {
            switch (msg.Severity)
            {
                case Severity.Warning:
                    NumWarnings++;
                    if (NoWarns.Contains(msg.Id))
                        return;
                    break;
                case Severity.Error:
                    NumErrors++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Console.WriteLine(msg.ToString());
        }
    }
}