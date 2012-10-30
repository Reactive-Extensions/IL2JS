//
// Entry point for IL -> JavaScript compiler
//

using System;
using System.IO;
using System.Linq;
using Microsoft.LiveLabs.Extras;
using CST = Microsoft.LiveLabs.CST;

namespace Microsoft.LiveLabs.JavaScript.IL2JS
{
    public static class Program
    {
        private static bool IsUpToDate(CompilerEnvironment env, CST.AssemblyDef assembly, bool complain)
        {
            var annot = assembly.Annotations.OfType<CST.AssemblyFileAnnotation>().FirstOrDefault();
            if (annot == null)
                throw new InvalidOperationException("assembly is missing file annotation");
            var fn = default(string);
            var lastCompTime = AssemblyCompiler.LastCompilationTime(env, assembly.Name, out fn);
            if (lastCompTime.HasValue)
            {
                if (annot.LastWriteTime <= lastCompTime.Value)
                    return true;
                else
                {
                    if (complain)
                        env.Log
                            (new OutOfDateAssemblyMessage(assembly.Name, annot.CanonicalFileName, fn));
                    return false;
                }
            }
            else
            {
                if (complain)
                    env.Log(new AssemblyNotCompiledMessage(assembly.Name, fn));
                return false;
            }
        }

        public static int Main(string[] args)
        {
            var env = new CompilerEnvironment();
            try
            {
                // Collect command line args
                var cl = new CompilerCommandLine(env.Log, env);
                cl.MergeFromArgs(args);

                if (env.CompilationMode == CompilationMode.Traced && env.TraceFileNames.Count == 0 && string.IsNullOrEmpty(env.FinalTraceName))
                {
                    env.Log(new UsageMessage("'-mode traced' must be accompanied by at least on of '-trace' or '-finalTraceName' options."));
                    throw new ExitException();
                }

                env.SetupPrimTracer();

                // ----------------------------------------
                // Load the assemblies
                // ----------------------------------------

                // Renames
                var subst = new CST.AssemblyNameSubstitution(env.Log, env.PrimTracer, env.AssemblyNameResolution);
                foreach (var r in env.RenameRules)
                    subst.Add(r);

                // Augmentations
                var augmentations = new CST.AugmentationDatabase(subst);
                foreach (var fn in env.AugmentationFileNames)
                    augmentations.AddFile(Path.Combine(env.InputDirectory, fn));
                foreach (var sn in env.OriginalStrongNames)
                    augmentations.AddOriginal(sn);

                // Final references
                var loadedAssemblies = new CST.LoadedAssemblyDatabase(augmentations);
                foreach (var fileName in env.ReferenceFileNames)
                    loadedAssemblies.Add(fileName, false);
                foreach (var fileName in env.CompileFileNames)
                    loadedAssemblies.Add(fileName, true);

                var loader = new CST.PELoader(loadedAssemblies);
                var global = loader.Load();

                // Done with the loader
                loader = null;

                env.SetupMetadata(global);

                try
                {
                    env.Validity.Check();

                    foreach (var assmInfo in loadedAssemblies)
                        env.Log(new CST.LoadedAssemblyMessage(assmInfo.TargetAssemblyName));

                    if (env.Tracer != null)
                        env.Tracer.Trace
                            ("Loaded assemblies",
                             w =>
                                 {
                                     foreach (var assemblyDef in global.Assemblies)
                                     {
                                         assemblyDef.Append(w);
                                         w.EndLine();
                                     }
                                 });

                    // ----------------------------------------
                    // Setup Interop
                    // ----------------------------------------
                    env.InteropManager.Setup();

                    // ----------------------------------------
                    // Collect assemblies
                    // ----------------------------------------

                    var allAssemblies = new Set<CST.AssemblyDef>();
                    var compilingAssemblies = new Set<CST.AssemblyDef>();
                    var referencedAssemblies = new Set<CST.AssemblyDef>();
                    var ok = true;
                    foreach (var assmInfo in loadedAssemblies)
                    {
                        var assembly = assmInfo.AssemblyDef;
                        allAssemblies.Add(assembly);
                        if (assmInfo.ForCompilation)
                        {
                            if (string.IsNullOrEmpty(env.OutputDirectory))
                            {
                                env.Log
                                    (new SkippingJavaScriptComplilation
                                         (assembly.Name, "No output directory was given"));
                                referencedAssemblies.Add(assembly);
                            }
                            else if (env.SkipUpToDate && IsUpToDate(env, assembly, false))
                            {
                                env.Log
                                    (new SkippingJavaScriptComplilation
                                         (assembly.Name, "Previously generated JavaScript is up to date"));
                                referencedAssemblies.Add(assembly);
                            }
                            else
                                compilingAssemblies.Add(assembly);
                        }
                        else
                        {
                            if (IsUpToDate(env, assembly, true))
                                referencedAssemblies.Add(assembly);
                            else
                                ok = false;
                        }
                    }

                    // Done with compilations
                    loadedAssemblies = null;                    

                    if (!ok)
                        throw new ExitException();

                    if (compilingAssemblies.Count > 0)
                    {
                        // ----------------------------------------
                        // Compile
                        // ----------------------------------------

                        switch (env.CompilationMode)
                        {
                        case CompilationMode.Plain:
                        case CompilationMode.Collecting:
                            // Compile all assemblies
                            foreach (var assemblyDef in compilingAssemblies)
                            {
                                var ac = new AssemblyCompiler(env, assemblyDef);
                                ac.Emit(null);
                            }
                            break;
                        case CompilationMode.Traced:
                            // Collect the trace file info
                            if (!string.IsNullOrEmpty(env.InitialTraceFileName))
                                env.Traces.Add(env.InitialTraceFileName, true);
                            foreach (var fn in env.TraceFileNames)
                                env.Traces.Add(fn, false);
                            env.Traces.AddFinalOrRemainder(env.FinalTraceName);
                            // Compile all traces and all remaining definitions
                            foreach (var kv in env.Traces.TraceMap)
                            {
                                var compiler = new TraceCompiler(env, kv.Value);
                                compiler.Emit();
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                        }

                        // ----------------------------------------
                        // Runtime
                        // ----------------------------------------

                        if (env.Global.Assemblies.Any(a => a.EntryPoint != null))
                        {
                            // Emit the runtime itself
                            var compiler = new RuntimeCompiler(env);
                            compiler.Emit();
                        }
                    }
                }
                finally
                {
                    env.Teardown();
                }
            }
            catch (ExitException)
            {
                // No-op
            }
            catch (Exception e)
            {
                Console.WriteLine("Internal error: {0}", e.Message);
                Console.WriteLine("Please report this error to the developers");
                env.NumErrors++;
            }
            finally
            {
                Console.WriteLine("{0} errors, {1} warnings", env.NumErrors, env.NumWarnings);
            }
            return env.NumErrors == 0 ? 0 : 1;
        }
    }
}