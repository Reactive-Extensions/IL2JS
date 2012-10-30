//
// Load an assembly and all of its referenced assemblies into CCI at most once.
//

using System;
using System.IO;
using Microsoft.LiveLabs.Extras;
using CCI = Microsoft.Cci;

namespace Microsoft.LiveLabs.JavaScript.ManagedInterop.Rewriter
{

    public class AssemblyLoader
    {
        private class Info { public CCI.AssemblyNode Assembly; public string FileName; };

        private bool loadFailed;

        private readonly RewriterEnvironment env;
        private readonly Map<string, Info> strongNameToInfo;
        private readonly Map<string, CCI.AssemblyNode> fileNameToAssembly;
        // Entry for (referencor strong name, referencee strong name) for references know to be unresolvable
        private readonly Set<string> knownBad;

        public AssemblyLoader(RewriterEnvironment env)
        {
            this.env = env;
            loadFailed = false;
            strongNameToInfo = new Map<string, Info>();
            fileNameToAssembly = new Map<string, CCI.AssemblyNode>();
            knownBad = new Set<string>();
        }

        private string CanonicalFileName(string fileName)
        {
            return Path.GetFullPath(Path.Combine(env.InputDirectory, fileName));
        }

        private void CheckTypeDefn(CCI.AssemblyNode expectedContainingAssembly, CCI.TypeNode typeDefn)
        {
            if (typeDefn.DeclaringModule == null || typeDefn.DeclaringModule.ContainingAssembly == null ||
                typeDefn.DeclaringModule.ContainingAssembly != expectedContainingAssembly || typeDefn.Name == null)
            {
                var refName = expectedContainingAssembly.StrongName;
                var refInfo = default(Info);
                var refFilename = "<unknown>";
                if (strongNameToInfo.TryGetValue(refName, out refInfo))
                    refFilename = refInfo.FileName;
                env.Log(new UnresolvableReferenceMessage(refName, refFilename, "<unknown>"));
                throw new ExitException();
            }
            foreach (var member in typeDefn.Members)
            {
                var nestedTypeDefn = member as CCI.TypeNode;
                if (nestedTypeDefn != null)
                    CheckTypeDefn(expectedContainingAssembly, nestedTypeDefn);
            }
        }

        public void Load(IImSeq<string> fileNames, out CCI.AssemblyNode mscorlib, out CCI.AssemblyNode jsTypes)
        {
            foreach (var fileName in fileNames)
            {
                var canonicalFileName = CanonicalFileName(fileName);
                if (fileNameToAssembly.ContainsKey(canonicalFileName))
                {
                    env.Log(new DuplicateAssemblyFileNameMessage(fileName, canonicalFileName));
                    throw new ExitException();
                }
                else
                    fileNameToAssembly.Add(canonicalFileName, null);
            }

            // ----------------------------------------
            // Which assembly should we use for mscorlib and JSTypes?
            // ----------------------------------------
            var mscorlibCanonicalName = default(string);
            var jsTypesCanonicalName = default(string);
            foreach (var kv in fileNameToAssembly)
            {
                var baseName = Path.GetFileNameWithoutExtension(kv.Key);
                if (baseName.ToLower().Contains(Constants.MsCorLibSimpleName.ToLower()))
                {
                    if (mscorlibCanonicalName != null)
                    {
                        env.Log(new DuplicateSpecialAssemblyMessage(Constants.MsCorLibSimpleName, mscorlibCanonicalName, kv.Key));
                        throw new ExitException();
                    }
                    mscorlibCanonicalName = kv.Key;
                }
                else if (baseName.ToLower().Contains(Constants.JSTypesSimpleName.ToLower()))
                {
                    if (jsTypesCanonicalName != null)
                    {
                        env.Log(new DuplicateSpecialAssemblyMessage(Constants.JSTypesSimpleName, jsTypesCanonicalName, kv.Key));
                        throw new ExitException();
                    }
                    jsTypesCanonicalName = kv.Key;
                }
            }
            if (mscorlibCanonicalName == null)
            {
                env.Log(new MissingSpecialAssemblyMessage(Constants.MsCorLibSimpleName));
                throw new ExitException();
            }
            if (jsTypesCanonicalName == null)
            {
                env.Log(new MissingSpecialAssemblyMessage(Constants.JSTypesSimpleName));
                throw new ExitException();
            }

            // ----------------------------------------
            // Initialize CCI, which will implicitly load mscorlib
            // ----------------------------------------
            var frameworkDir = Path.GetDirectoryName(mscorlibCanonicalName);
            if (!Directory.Exists(frameworkDir))
            {
                env.Log(new UnloadableAssemblyMessage(frameworkDir, "directory does not exist"));
                throw new ExitException();
            }

            // These special CCI assemblies, and mscorlib, will be picked up from the framework directory
            CCI.SystemDataAssemblyLocation.Location = null;
            CCI.SystemXmlAssemblyLocation.Location = null;

            CCI.TargetPlatform.SetToV2(frameworkDir);

            // At this point we could "fixup" CCI's hard-wired system assembly references:
            //
            //     foreach (var asmRefs in CCI.TargetPlatform.AssemblyReferenceFor.GetEnumerator())
            //     {
            //         var asmRef = (CCI.AssemblyReference)asmRefs.Value;
            //         asmRef.Location = <the right place>;
            //     }
            //     SystemAssemblyLocation.Location = <the right place>;
            //     SystemXmlAssemblyLocation.Location = <the right place>;
            // 
            // But so far that doesn't seem necessary

            CCI.SystemTypes.Initialize(false, true, ResolveReference);

            // ----------------------------------------
            // Account for mscorlib being loaded
            // ----------------------------------------
            mscorlib = CCI.SystemTypes.SystemAssembly;
            if (mscorlib == null || mscorlib.Directory == null)
            {
                env.Log(new UnloadableAssemblyMessage(frameworkDir, "cannot load mscorlib"));
                throw new ExitException();
            }

            env.Log(new FoundSpecialAssemblyMessage(Constants.MsCorLibSimpleName, mscorlib.StrongName));

            fileNameToAssembly[mscorlibCanonicalName] = mscorlib;
            strongNameToInfo.Add
                (mscorlib.StrongName, new Info { Assembly = mscorlib, FileName = mscorlibCanonicalName });

            // ----------------------------------------
            // Load the remaining registered assemblies
            // ----------------------------------------
            var pending = new Seq<string>();
            foreach (var kv in fileNameToAssembly)
            {
                if (kv.Value == null)
                    pending.Add(kv.Key);
                // else: must have been mscorlib, which we loaded above
            }
            jsTypes = null;
            foreach (var canonicalFileName in pending)
            {
                var assembly = CCI.AssemblyNode.GetAssembly(canonicalFileName, null, false, true, true);
                if (assembly == null)
                {
                    env.Log(new UnloadableAssemblyMessage(canonicalFileName, "CCI cannot load assembly"));
                    throw new ExitException();
                }
                var info = default(Info);
                if (strongNameToInfo.TryGetValue(assembly.StrongName, out info))
                {
                    env.Log(new DuplicateAssemblyStrongNameMessage(canonicalFileName, assembly.StrongName, info.FileName));
                    throw new ExitException();
                }
                fileNameToAssembly[canonicalFileName] = assembly;
                strongNameToInfo.Add(assembly.StrongName, new Info { Assembly = assembly, FileName = canonicalFileName });
                assembly.AssemblyReferenceResolution += ResolveReference;

                if (canonicalFileName.Equals(jsTypesCanonicalName))
                {
                    jsTypes = assembly;
                    env.Log(new FoundSpecialAssemblyMessage(Constants.JSTypesSimpleName, jsTypes.StrongName));
                }
            }

#if false
            // ----------------------------------------
            // Check all references resolve to known definitions
            // ----------------------------------------
            foreach (var kv in strongNameToInfo)
            {
                new CCI.StandardVisitor().Visit(kv.Value.Assembly);
                foreach (var reference in kv.Value.Assembly.AssemblyReferences)
                {
                    if (reference.Assembly == null ||
                        !reference.Assembly.StrongName.Equals(reference.StrongName, StringComparison.OrdinalIgnoreCase) ||
                        reference.Assembly.Location.Equals("unknown:location", StringComparison.OrdinalIgnoreCase))
                    {
                        env.Log(new UnresolvableReferenceMessage(kv.Key, kv.Value.FileName, reference.StrongName));
                        throw new ExitException();
                    }
                }
                foreach (var typeDefn in kv.Value.Assembly.Types)
                    CheckTypeDefn(kv.Value.Assembly, typeDefn);

                env.Log(new LoadedAssemblyMessage(kv.Key, kv.Value.FileName));
            }
#endif

            if (loadFailed)
                throw new ExitException();
        }

        private CCI.AssemblyNode ResolveReference(CCI.AssemblyReference reference, CCI.Module referencingModule)
        {
            var info = default(Info);
            if (strongNameToInfo.TryGetValue(reference.StrongName, out info))
                return info.Assembly;
            else
            {
                var sourceInfo = default(Info);
                var sourceFileName = "<unknown>";
                if (strongNameToInfo.TryGetValue(referencingModule.ContainingAssembly.StrongName, out sourceInfo))
                    sourceFileName = sourceInfo.FileName;
                var key = "(" + referencingModule.ContainingAssembly.StrongName + "," + reference.StrongName + ")";
                if (!knownBad.Contains(key))
                {
                    knownBad.Add(key);
                    env.Log
                        (new UnresolvableReferenceMessage
                             (referencingModule.ContainingAssembly.StrongName, sourceFileName, reference.StrongName));
                }
                // CCI will swallow this exception
                loadFailed = true;
                throw new ExitException();
            }
        }

        public CCI.AssemblyNode Find(string fileName)
        {
            var canonicalFileName = CanonicalFileName(fileName);
            var assembly = default(CCI.AssemblyNode);
            if (!fileNameToAssembly.TryGetValue(canonicalFileName, out assembly))
                throw new InvalidOperationException("no such assembly");
            return assembly;
        }
    }
}
