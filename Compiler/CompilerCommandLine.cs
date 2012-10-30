using System;
using System.Linq;
using Microsoft.LiveLabs.Extras;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    public class CompilerCommandLine : CommandLine<CompilerEnvironment>
    {
        public CompilerCommandLine(Log log, CompilerEnvironment env)
            : base(log, env)
        {
        }

        protected override void Usage(string opt)
        {
            log(new UsageMessage("Bad option: " + opt));
        }

        protected override void Process(string opt, Func<int, string[]> getArgs)
        {
            var c = StringComparer.OrdinalIgnoreCase;
            if (c.Equals(opt, "-allWarns"))
                env.NoWarns = new Set<string>();
            else if (c.Equals(opt, "-assemblyNameResolution"))
            {
                var f = getArgs(1)[0];
                if (c.Equals(f, "full"))
                    env.AssemblyNameResolution = CST.AssemblyNameResolution.Full;
                else if (c.Equals(f, "nameVersion"))
                    env.AssemblyNameResolution = CST.AssemblyNameResolution.NameVersion;
                else if (c.Equals(f, "name"))
                    env.AssemblyNameResolution = CST.AssemblyNameResolution.Name;
                else
                {
                    log(new UsageMessage("Unrecognised -assemblyNameResolution value"));
                    throw new ExitException();
                }
            }
            else if (c.Equals(opt, "-augment"))
                env.AugmentationFileNames.Add(ExpandVars(getArgs(1)[0]));
            else if (c.Equals(opt, "-breakOnBreak"))
                env.BreakOnBreak = false;
            else if (c.Equals(opt, "+breakOnBreak"))
                env.BreakOnBreak = true;
            else if (c.Equals(opt, "-clrArraySemantics"))
                env.CLRArraySemantics = false;
            else if (c.Equals(opt, "+clrArraySemantics"))
                env.CLRArraySemantics = true;
            else if (c.Equals(opt, "-clrInteropExceptions"))
                env.CLRInteropExceptions = false;
            else if (c.Equals(opt, "+clrInteropExceptions"))
                env.CLRInteropExceptions = true;
            else if (c.Equals(opt, "-clrNullVirtcallSemantics"))
                env.CLRNullVirtcallSemantics = false;
            else if (c.Equals(opt, "+clrNullVirtcallSemantics"))
                env.CLRNullVirtcallSemantics = true;
            else if (c.Equals(opt, "-compile"))
                env.CompileFileNames.Add(ExpandVars(getArgs(1)[0]));
            else if (c.Equals(opt, "-debug"))
            {
                env.DebugMode = false;
                env.PrettyPrint = false;
                env.DebugLevel = 0;
            }
            else if (c.Equals(opt, "+debug"))
            {
                env.DebugMode = true;
                env.PrettyPrint = true;
                env.DebugLevel = 1;
            }
            else if (c.Equals(opt, "-debugLevel"))
            {
                var n = default(int);
                if (!int.TryParse(getArgs(1)[0], out n) || n < 0 || n > 2)
                {
                    log(new UsageMessage("Invalid -debugLevel value"));
                    throw new ExitException();
                }
                env.DebugLevel = n;
            }
            else if (c.Equals(opt, "-debugTrace"))
                env.DebugTraceFileName = getArgs(1)[0];
            else if (c.Equals(opt, "-finalTraceName"))
                env.FinalTraceName = getArgs(1)[0];
            else if (c.Equals(opt, "-help"))
                Usage(null);
            else if (c.Equals(opt, "-inDir"))
                env.InputDirectory = ExpandVars(getArgs(1)[0]);
            else if (c.Equals(opt, "-initialTrace"))
                env.InitialTraceFileName = getArgs(1)[0];
            else if (c.Equals(opt, "-importInlineThreshold"))
            {
                var i = default(int);
                if (!int.TryParse(getArgs(1)[0], out i))
                {
                    log(new UsageMessage("Invalid -importInlineThreshold value"));
                    throw new ExitException();
                }
                env.ImportInlineThreshold = Math.Max(-1, i);
            }
            else if (c.Equals(opt, "-inlineThreshold"))
            {
                var i = default(int);
                if (!int.TryParse(getArgs(1)[0], out i))
                {
                    log(new UsageMessage("Invalid -inlineThreshold value"));
                    throw new ExitException();
                }
                env.InlineThreshold = Math.Max(-1, i);
            }
            else if (c.Equals(opt, "-loadPath"))
                env.LoadPaths.Add(getArgs(1)[0]);
            else if (c.Equals(opt, "-mode"))
            {
                var m = getArgs(1)[0];
                if (c.Equals(m, "plain"))
                    env.CompilationMode = CompilationMode.Plain;
                else if (c.Equals(m, "collecting"))
                    env.CompilationMode = CompilationMode.Collecting;
                else if (c.Equals(m, "traced"))
                    env.CompilationMode = CompilationMode.Traced;
                else
                {
                    log(new UsageMessage("Invalid -mode value"));
                    throw new ExitException();
                }
            }
            else if (c.Equals(opt, "-noWarn"))
            {
                var id = getArgs(1)[0].Trim().ToLowerInvariant();
                env.NoWarns.Add(id);
            }
            else if (c.Equals(opt, "-original"))
                env.OriginalStrongNames.Add(getArgs(1)[0]);
            else if (c.Equals(opt, "-outDir"))
                env.OutputDirectory = ExpandVars(getArgs(1)[0]);
            else if (c.Equals(opt, "+prettyPrint"))
                env.PrettyPrint = true;
            else if (c.Equals(opt, "-prettyPrint"))
                env.PrettyPrint = false;
            else if (c.Equals(opt, "-reference"))
                env.ReferenceFileNames.Add(ExpandVars(getArgs(1)[0]));
            else if (c.Equals(opt, "-rename"))
            {
                var args = getArgs(2);
                env.RenameRules.Add(new CST.RenameRule(args[0], args[1]));
            }
            else if (c.Equals(opt, "-root"))
                env.Root = getArgs(1)[0];
            else if (c.Equals(opt, "-safeInterop"))
                env.SafeInterop = false;
            else if (c.Equals(opt, "+safeInterop"))
                env.SafeInterop = true;
            else if (c.Equals(opt, "-skipUpToDate"))
                env.SkipUpToDate = false;
            else if (c.Equals(opt, "+skipUpToDate"))
                env.SkipUpToDate = true;
            else if (c.Equals(opt, "-target"))
            {
                var str = getArgs(1)[0];
                if (c.Equals(str, "browser"))
                    env.Target = Target.Browser;
                else if (c.Equals(str, "cscript"))
                    env.Target = Target.CScript;
                else
                {
                    log(new UsageMessage("Invalid -target value"));
                    throw new ExitException();
                }
            }
            else if (c.Equals(opt, "-trace"))
                env.TraceFileNames.Add(getArgs(1)[0]);
            else if (c.Equals(opt, "+warn"))
            {
                var id = getArgs(1)[0].Trim().ToLowerInvariant();
                env.NoWarns = env.NoWarns.Where(id2 => !id2.Equals(id, StringComparison.Ordinal)).ToSet();
            }
            else
            {
                log(new UsageMessage("Unrecognised option '" + opt + "'"));
                throw new ExitException();
            }
        }
    }
}