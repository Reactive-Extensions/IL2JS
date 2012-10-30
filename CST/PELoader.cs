//
// Load PE file metadata into CST representation
//
// Four glitches prevent us from going directly into the CST representation in one pass:
//  - Type parameter constraints are stored in GenericParamConstraintRow, but do not indicate if the constraint
//    is for a class or an interface. Thus we can't separate extends and implements clauses until type resolution
//    is working. Better would have been to include a flag in the row.
//  - Custom attribute property bindings store enumeration values using their underlying representation, which can be
//    between one and eight bytes. Again, type resolution must be working in order to determine an enumerations
//    underlying representation type and deserialize the custom attribute. Better would have been to store and
//    encoding of the type with the value.
//  - We wish to represent all overloading and interface method implementations in a single list, whilst the PE file
//    records only explicit interface implementations. To construct the full list we must be able to resolve
//    types and methods. Better would have been to separate extends and implements, as is done for type defs.
//  - Most method defs and method refs elide the 'this' argument. If the declaring type is a value type, the
//    'this' argument must be a pointer to the declaring type, but we can't determine this until types can
//    be resolve. Better would have been to distinguish value and non-value types in type ref rows, as is done
//    in type signature type-def-or-ref clauses.
//  - We replace IntPtr with a strongly typed code pointer for delegate constructors, at both the constructor
//    definition and its references.
// These are fixed by the validity checker.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using Microsoft.LiveLabs.Extras;
using PE = Microsoft.LiveLabs.PE;

namespace Microsoft.LiveLabs.CST
{
    public class RenameRule
    {
        [NotNull]
        public readonly string SourceStrongName;
        [CanBeNull] // null => unavailable
        public readonly string TargetStrongName;

        public RenameRule(string sourceStrongName, string targetStrongName)
        {
            SourceStrongName = sourceStrongName;
            TargetStrongName = targetStrongName;
        }
    }

    // Collect renaming rules to make a parallel substitution from assembly-names to assembly-names
    public class AssemblyNameSubstitution
    {
        [NotNull]
        public readonly Log Log;
        [CanBeNull]  // null => no tracing
        public readonly Writer Tracer;
        [NotNull]
        public readonly AssemblyNameResolution Resolution;
        [NotNull]
        private readonly Map<AssemblyName, AssemblyName> subst;

        public AssemblyNameSubstitution(Log log, Writer tracer, AssemblyNameResolution resolution)
        {
            Log = log;
            Tracer = tracer;
            Resolution = resolution;
            subst = new Map<AssemblyName, AssemblyName>();
        }

        public void Add(RenameRule rule)
        {
            var source = FromStrongName(rule.SourceStrongName);
            var target = string.IsNullOrEmpty(rule.TargetStrongName) ? null : FromStrongName(rule.TargetStrongName);
            var existTarget = default(AssemblyName);
            if (subst.TryGetValue(source, out existTarget))
            {
                if ((existTarget == null && target != null) || (existTarget != null && target == null) ||
                    (existTarget != null && !existTarget.Equals(target)))
                {
                    Log(new InvalidAssemblyRenameMessage(source, existTarget, target));
                    throw new ExitException();
                }
            }
            else
                subst.Add(source, target);
        }

        private AssemblyName FromStrongName(string strongName)
        {
            var res = AssemblyName.FromReflectionName(Resolution, strongName);
            if (res == null)
            {
                Log(new IllFormedStrongNameMessage(strongName));
                throw new ExitException();
            }
            return res;
        }

        public AssemblyName Rename(AssemblyName source)
        {
            var res = default(AssemblyName);
            if (subst.TryGetValue(source, out res))
                return res;
            else
                return source;
        }
    }

    // Info related to a single Dll, which may be part of a larger augmentation
    public class DllLoadContext
    {
        //
        // Determined when load file
        //
        [NotNull]
        public readonly string CanonicalFileName;
        [NotNull]
        public readonly PE.PEFile File;
        [NotNull]
        public readonly PE.ReaderContext ReaderContext;
        [NotNull]
        public readonly PE.Row AssemblyRow;
        [NotNull]
        public readonly AssemblyName AssemblyName; // original name, before substitution

        //
        // Cached during loading
        //
        [NotNull]
        public readonly Set<AssemblyName> WarnedRenames; // suppresses duplicate warnings
        [NotNull]
        public readonly Map<PE.Row, PE.TypeDefRow> MemberToDefiningType; // inverts child->parent pointer
        [NotNull]
        public readonly Map<PE.Row, AssemblyName> AssemblyRefCache;
        [NotNull]
        public readonly Map<PE.Row, FieldRef> FieldRefCache;
        [NotNull]
        public readonly Map<PE.Row, MethodRef> MethodRefCache;
        [NotNull]
        public readonly Map<PE.Row, PolymorphicMethodRef> PolymorphicMethodRefCache;
        [NotNull]
        public readonly Map<PE.Row, MethodSignature> MethodSignatureCache;
        [NotNull]
        public readonly Map<PE.Row, TypeRef> FKTypeRefCache;
        [NotNull]
        public readonly Map<PE.Row, TypeRef> HKTypeRefCache;
        [NotNull]
        public readonly Map<PE.Row, TypeDef> TypeDefs; // including type-bound and method-bound type parameters
        [NotNull]
        public readonly Map<PE.Row, MemberDef> MemberDefs;
        [NotNull]
        public readonly Map<PE.Row, ParameterOrLocalOrResult> ParametersAndLocals;

        public DllLoadContext(string canonicalFileName, PE.PEFile file, PE.ReaderContext readerContext, PE.Row assemblyRow, AssemblyName assemblyName)
        {
            CanonicalFileName = canonicalFileName;
            File = file;
            ReaderContext = readerContext;
            AssemblyRow = assemblyRow;
            AssemblyName = assemblyName;

            WarnedRenames = new Set<AssemblyName>();
            MemberToDefiningType = new Map<PE.Row, PE.TypeDefRow>();
            AssemblyRefCache = new Map<PE.Row, AssemblyName>();
            FieldRefCache = new Map<PE.Row, FieldRef>();
            MethodRefCache = new Map<PE.Row, MethodRef>();
            PolymorphicMethodRefCache = new Map<PE.Row, PolymorphicMethodRef>();
            MethodSignatureCache = new Map<PE.Row, MethodSignature>();
            FKTypeRefCache = new Map<PE.Row, TypeRef>();
            HKTypeRefCache = new Map<PE.Row, TypeRef>();
            TypeDefs = new Map<PE.Row, TypeDef>();
            MemberDefs = new Map<PE.Row, MemberDef>();
            ParametersAndLocals = new Map<PE.Row, ParameterOrLocalOrResult>();
        }
    }

    // Manage the loading of dlls
    public class DllLoader
    {
        [NotNull]
        private readonly Log log;
        [CanBeNull]  // null => no tracing
        private readonly Writer tracer;
        [NotNull]
        private readonly AssemblyNameResolution resolution;
        [NotNull]
        private readonly Map<string, DllLoadContext> loaded;

        public DllLoader(Log log, Writer tracer, AssemblyNameResolution resolution)
        {
            this.log = log;
            this.tracer = tracer;
            this.resolution = resolution;
            loaded = new Map<string, DllLoadContext>();
        }

        public DllLoadContext Load(string fileName, bool canBeShared)
        {
            var canonicalFileName = FromFileName(fileName);
            var info = default(DllLoadContext);
            if (loaded.TryGetValue(canonicalFileName, out info))
            {
                if (!canBeShared)
                {
                    log(new DuplicateAugmentationFileNameMessage(canonicalFileName));
                    throw new ExitException();
                }
            }
            else
            {
                info = LoadDll(canonicalFileName);
                loaded.Add(canonicalFileName, info);
            }
            return info;
        }

        private string FromFileName(string fileName)
        {
            try
            {
                return Path.GetFullPath(fileName);
            }
            catch (ArgumentException e)
            {
                log(new IllFormedFileNameMessage(fileName, e.Message));
                throw new ExitException();
            }
            catch (SecurityException e)
            {
                log(new IllFormedFileNameMessage(fileName, e.Message));
                throw new ExitException();
            }
            catch (NotSupportedException e)
            {
                log(new IllFormedFileNameMessage(fileName, e.Message));
                throw new ExitException();
            }
            catch (PathTooLongException e)
            {
                log(new IllFormedFileNameMessage(fileName, e.Message));
                throw new ExitException();
            }
        }

        private AssemblyName AssemblyNameFromAssemblyRow(PE.AssemblyRow row)
        {
            return new AssemblyName(resolution, row.Name.Value, row.MajorVersion, row.MinorVersion, row.BuildNumber, row.RevisionNumber, row.Culture.Value, row.PublicKey.Value);
        }

        private DllLoadContext LoadDll(string canonicalFileName)
        {
            try
            {
                var readerContext = new PE.ReaderContext(canonicalFileName, tracer);
                var file = new PE.PEFile();
                file.Read(readerContext);
                var assemblyRow = (from r in file.Tables.AssemblyTable select r).FirstOrDefault();
                if (assemblyRow == null)
                    throw new InvalidOperationException("dll does not contain an assembly");
                var assemblyName = AssemblyNameFromAssemblyRow(assemblyRow);

                return new DllLoadContext(canonicalFileName, file, readerContext, assemblyRow, assemblyName);
            }
            catch (PE.PEException e)
            {
                log(new UnloadableAssemblyMessage(canonicalFileName, e.Message));
                throw new ExitException();
            }
            catch (IOException e)
            {
                log(new UnloadableAssemblyMessage(canonicalFileName, e.Message));
                throw new ExitException();
            }
        }

    }

    // Collect renaming and augmentation rules to build a set of augmented assemblies from dlls
    public class AugmentationDatabase
    {
        [NotNull]
        public readonly Log Log;
        [NotNull]
        public readonly AssemblyNameSubstitution Subst;
        public readonly AssemblyNameResolution Resolution;
        [NotNull]
        public readonly DllLoader DllLoader;
        [NotNull]
        private readonly Map<AssemblyName, Seq<DllLoadContext>> augmentations;
        [NotNull]
        private readonly Set<AssemblyName> originals;

        public AugmentationDatabase(AssemblyNameSubstitution subst)
        {
            Log = subst.Log;
            Subst = subst;
            Resolution = subst.Resolution;
            DllLoader = new DllLoader(subst.Log, subst.Tracer, subst.Resolution);
            augmentations = new Map<AssemblyName, Seq<DllLoadContext>>();
            originals = new Set<AssemblyName>();
        }

        private void AddDll(DllLoadContext ctxt)
        {
            var target = Subst.Rename(ctxt.AssemblyName);
            if (target == null)
            {
                Log(new AugmentingIgnoredAssemblyMessage(ctxt.AssemblyName, ctxt.CanonicalFileName));
                throw new ExitException();
            }
            var dllInfos = default(Seq<DllLoadContext>);
            if (!augmentations.TryGetValue(target, out dllInfos))
            {
                dllInfos = new Seq<DllLoadContext>();
                augmentations.Add(target, dllInfos);
            }
            dllInfos.Add(ctxt);
        }

        public void AddFile(string fileName)
        {
            var dllInfo = DllLoader.Load(fileName, false);
            AddDll(dllInfo);
        }

        public void AddOriginal(string strongName)
        {
            var name = AssemblyName.FromReflectionName(Resolution, strongName);
            if (name == null)
            {
                Log(new IllFormedStrongNameMessage(strongName));
                throw new ExitException();
            }
            originals.Add(name);
        }

        public bool IsOriginal(AssemblyName name)
        {
            return originals.Contains(name);
        }

        public void AddOriginalDllInfo(DllLoadContext ctxt)
        {
            AddDll(ctxt);
        }

        public Seq<DllLoadContext> AugmentedAssembly(AssemblyName name)
        {
            var res = default(Seq<DllLoadContext>);
            if (augmentations.TryGetValue(name, out res))
                return res;
            else
                return null;
        }
    }

    // Collect the Dlls which make up a final augmented assembly
    public class AssemblyLoadContext
    {
        [NotNull]
        public readonly AssemblyName TargetAssemblyName; // after renaming
        public bool ForCompilation;
        [NotNull]
        public readonly IImSeq<DllLoadContext> Dlls;

        public AssemblyDef AssemblyDef;

        public AssemblyLoadContext(AssemblyName targetAssemblyName, bool forCompilation, IImSeq<DllLoadContext> dlls)
        {
            TargetAssemblyName = targetAssemblyName;
            ForCompilation = forCompilation;
            Dlls = dlls;
        }
    }

    // Manage loading sets of dlls as augmented assemblies
    public class LoadedAssemblyDatabase : IEnumerable<AssemblyLoadContext>
    {
        [NotNull]
        public readonly Log Log;
        [NotNull]
        public readonly AssemblyNameSubstitution Subst;
        public readonly AssemblyNameResolution Resolution;
        [NotNull]
        private readonly AugmentationDatabase augmentations;
        [NotNull]
        private readonly Map<AssemblyName, AssemblyLoadContext> compilations;

        public LoadedAssemblyDatabase(AugmentationDatabase augmentations)
        {
            Log = augmentations.Log;
            Subst = augmentations.Subst;
            Resolution = augmentations.Resolution;
            this.augmentations = augmentations;
            compilations = new Map<AssemblyName, AssemblyLoadContext>();
        }

        public void Add(string fileName, bool forCompilation)
        {
            var dllInfo = augmentations.DllLoader.Load(fileName, true);

            if (augmentations.IsOriginal(dllInfo.AssemblyName))
                augmentations.AddOriginalDllInfo(dllInfo);

            var target = Subst.Rename(dllInfo.AssemblyName);
            if (target == null)
                Log(new NotCompilingAssemblyMessage(dllInfo.AssemblyName, dllInfo.CanonicalFileName));
            else
            {
                var existAssemblyInfo = default(AssemblyLoadContext);
                if (compilations.TryGetValue(target, out existAssemblyInfo))
                {
                    if (forCompilation)
                        existAssemblyInfo.ForCompilation = true;
                }
                else
                {
                    var dllInfos = augmentations.AugmentedAssembly(target);
                    if (dllInfos == null)
                    {
                        if (target.Equals(dllInfo.AssemblyName))
                            Log(new CompilingAssemblyMessage(dllInfo.AssemblyName, dllInfo.CanonicalFileName));
                        else
                            Log
                                (new CompilingRenamedAssemblyMessage
                                     (dllInfo.AssemblyName, target, dllInfo.CanonicalFileName));
                        compilations.Add(target, new AssemblyLoadContext(target, forCompilation, new Seq<DllLoadContext> { dllInfo }));
                    }
                    else
                    {
                        Log
                            (new CompilingAugmentedAssemblyMessage
                                 (dllInfo.AssemblyName,
                                  dllInfo.CanonicalFileName,
                                  target,
                                  dllInfos.Select(i => i.CanonicalFileName).ToSeq()));
                        compilations.Add(target, new AssemblyLoadContext(target, forCompilation, dllInfos));
                    }
                }
            }
        }

        public bool Loaded(AssemblyName target)
        {
            return compilations.ContainsKey(target);
        }

        public AssemblyName FindSpecialAssembly(string specialName)
        {
            var matching = default(AssemblyName);
            foreach (var kv in compilations)
            {
                if (kv.Key.Name.Contains(specialName))
                {
                    if (matching == null)
                        matching = kv.Key;
                    else
                    {
                        Log(new DuplicateSpecialAssemblyMessage(specialName, matching, kv.Key));
                        throw new ExitException();
                    }
                }
            }
            if (matching == null)
            {
                Log(new MissingSpecialAssemblyMessage(specialName));
                throw new ExitException();
            }
            else
                Log(new FoundSpecialAssemblyMessage(specialName, matching));
            return matching;
        }

        public IEnumerator<AssemblyLoadContext> GetEnumerator()
        {
            foreach (var kv in compilations)
                yield return kv.Value;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class PELoader
    {
        [NotNull]
        private readonly Log log;
        [NotNull]
        private readonly LoadedAssemblyDatabase loadedAssemblies;

        [CanBeNull] // null => Load not yet called
        private Global global;
        [CanBeNull] // null => Load not yet called
        private Set<TypeRef> knownUnavailableCustomAttributeTypes;

        public PELoader(LoadedAssemblyDatabase loadedAssemblies)
        {
            log = loadedAssemblies.Log;
            this.loadedAssemblies = loadedAssemblies;
        }

        // ----------------------------------------------------------------------
        // Renaming
        // ----------------------------------------------------------------------

        private AssemblyName SubstAssemblyName(DllLoadContext ctxt, AssemblyName source)
        {
            var target = loadedAssemblies.Subst.Rename(source);
            if (target == null || !target.Equals(source))
            {
                if (!ctxt.WarnedRenames.Contains(source))
                {
                    ctxt.WarnedRenames.Add(source);
                    if (target == null)
                        log
                            (new IgnoringAssemblyReferenceMessage
                                 (ctxt.AssemblyName, ctxt.CanonicalFileName, source));
                    else
                        log
                            (new RenamingAssemblyReferenceMessage
                                 (ctxt.AssemblyName, ctxt.CanonicalFileName, source, target));
                }
            }

            if (target != null && !loadedAssemblies.Loaded(target))
            {
                log
                    (new UnresolvableReferenceMessage
                         (ctxt.AssemblyName, ctxt.CanonicalFileName, target));
                throw new ExitException();
            }

            if (target == null)
                // Not available            
                return new AssemblyName(loadedAssemblies.Resolution, source);
            else if (target.Equals(source))
                return source;
            else
                // Redirected
                return new AssemblyName(loadedAssemblies.Resolution, target, source);
        }

        // ----------------------------------------------------------------------
        // Names
        // ----------------------------------------------------------------------

        private AssemblyName AssemblyNameFromExportRow(PE.Row row)
        {
            switch (row.Tag)
            {
                case PE.TableTag.File:
                    throw new NotSupportedException("multi-module assemblies are not supported");
                case PE.TableTag.ExportedType:
                    {
                        var exportedTypeRow = (PE.ExportedTypeRow)row;
                        return AssemblyNameFromExportRow(exportedTypeRow.Implementation.Value);
                    }
                default:
                    throw new InvalidOperationException("unexpected export row");
            }
        }

        private AssemblyName AssemblyNameFromAssemblyRefRow(DllLoadContext ctxt, PE.AssemblyRefRow row)
        {
            var res = default(AssemblyName);
            if (!ctxt.AssemblyRefCache.TryGetValue(row, out res))
            {
                res = new AssemblyName
                    (loadedAssemblies.Resolution,
                     row.Name.Value,
                     row.MajorVersion,
                     row.MinorVersion,
                     row.BuildNumber,
                     row.RevisionNumber,
                     row.Culture.Value,
                     row.PublicKeyOrToken.Value);
                res = SubstAssemblyName(ctxt, res);
                ctxt.AssemblyRefCache.Add(row, res);
            }
            return res;
        }

        private AssemblyName AssemblyNameFromResolutionScopeRow(DllLoadContext ctxt, PE.Row row)
        {
            switch (row.Tag)
            {
                case PE.TableTag.Module:
                    {
                        var moduleRow = (PE.ModuleRow)row;
                        throw new NotImplementedException("multi-module assemblies are not supported");
                    }
                case PE.TableTag.ModuleRef:
                    {
                        var moduleRefRow = (PE.ModuleRefRow)row;
                        throw new NotImplementedException("multi-module assemblies are not supported");
                    }
                case PE.TableTag.AssemblyRef:
                    {
                        var assemblyRefRow = (PE.AssemblyRefRow)row;
                        return AssemblyNameFromAssemblyRefRow(ctxt, assemblyRefRow);
                    }
                case PE.TableTag.TypeRef:
                    {
                        // We've already extracted the outer type names from type row, now just
                        // need to extract the strong assembly name
                        var typeRefRow = (PE.TypeRefRow)row;
                        return AssemblyNameFromTypeRefRow(ctxt, typeRefRow);
                    }
                default:
                    throw new InvalidOperationException("unexpected resolution scope row");
            }
        }

        private AssemblyName AssemblyNameFromTypeRefRow(DllLoadContext ctxt, PE.TypeRefRow typeRefRow)
        {
            if (typeRefRow.ResolutionScope.IsNull)
            {
                var exportRows = from r in ctxt.File.Tables.ExportedTypeTable
                                 where
                                     r.TypeNamespace.Value == typeRefRow.TypeNamespace.Value &&
                                     r.TypeName.Value == typeRefRow.TypeName.Value
                                 select r.Implementation.Value;
                var exportRow = exportRows.FirstOrDefault();
                if (exportRow == null)
                    throw new InvalidOperationException("unable to determine referenced assembly for type ref");
                return AssemblyNameFromExportRow(exportRow);
            }
            else
                return AssemblyNameFromResolutionScopeRow(ctxt, typeRefRow.ResolutionScope.Value);
        }

        private TypeName TypeNameFromTypeDefRow(DllLoadContext ctxt, PE.TypeDefRow row)
        {
            if (string.IsNullOrEmpty(row.TypeName.Value))
                throw new InvalidOperationException("expecting type name");


            var outerRow =
                (from r in ctxt.File.Tables.NestedClassTable
                 where r.NestedClass.Value == row
                 select r.EnclosingClass.Value).FirstOrDefault();

            var thisTypeName = default(string);
            if (outerRow == null || string.IsNullOrEmpty(row.TypeNamespace.Value))
                thisTypeName = row.TypeName.Value;
            else
                // Ideally, row.TypeNamespace.Value would be null, but it appears some toolsz]
                // will split type names containing a '.' into a namespace and name part, even
                // if the type is nested. So undo this now.
                thisTypeName = row.TypeNamespace.Value + "." + row.TypeName.Value;

            // HACK HACK HACK: Nuke the guid from this type since it is not invariant on recompilation
            if (thisTypeName.StartsWith("<PrivateImplementationDetails>"))
                thisTypeName = "<PrivateImplementationDetails>";

            if (outerRow == null)
                return new TypeName(row.TypeNamespace.Value, thisTypeName);
            else
            {
                var outerName = TypeNameFromTypeDefRow(ctxt, outerRow);
                var nmList = outerName.Types.ToSeq();
                nmList.Add(thisTypeName);
                return new TypeName(outerName.Namespace, nmList);
            }
        }

        private TypeName TypeNameFromTypeRefRow(PE.TypeRefRow row)
        {
            if (string.IsNullOrEmpty(row.TypeName.Value))
                throw new InvalidOperationException("expecting type name");

            if (row.ResolutionScope.IsNull || row.ResolutionScope.Value.Tag != PE.TableTag.TypeRef)
                return new TypeName(row.TypeNamespace.Value, row.TypeName.Value);
            else
            {
                var outerName = TypeNameFromTypeRefRow((PE.TypeRefRow)row.ResolutionScope.Value);
                if (!string.IsNullOrEmpty(row.TypeNamespace.Value))
                    throw new InvalidOperationException("unexpected namespace for nested type");
                var nmList = new Seq<string>(outerName.Types);
                nmList.Add(row.TypeName.Value);
                return new TypeName(outerName.Namespace, nmList);
            }
        }

        // ----------------------------------------------------------------------
        // Type references
        // ----------------------------------------------------------------------

        private TypeRef TypeRefFromTypeSigWithCustomMods(DllLoadContext ctxt, PE.TypeWithCustomMods typecm)
        {
            var annotations = default(Seq<Annotation>);
            if (typecm.CustomMods.Count > 0)
            {
                annotations = new Seq<Annotation>();
                foreach (var cm in typecm.CustomMods)
                    annotations.Add
                        (new CustomModAnnotation
                             (cm.IsRequired, TypeRefFromRow(ctxt, null, cm.TypeDefOrRef.Value, true)));
            }
            return TypeRefFromTypeSig(ctxt, annotations, typecm.Type, true);
        }

        private TypeRef TypeRefFromPrimitiveTypeName(PE.PrimitiveType primType)
        {
            switch (primType)
            {
                case PE.PrimitiveType.Boolean:
                    return global.BooleanRef;
                case PE.PrimitiveType.Char:
                    return global.CharRef;
                case PE.PrimitiveType.Int8:
                    return global.Int8Ref;
                case PE.PrimitiveType.Int16:
                    return global.Int16Ref;
                case PE.PrimitiveType.Int32:
                    return global.Int32Ref;
                case PE.PrimitiveType.Int64:
                    return global.Int64Ref;
                case PE.PrimitiveType.IntNative:
                    return global.IntNativeRef;
                case PE.PrimitiveType.UInt8:
                    return global.UInt8Ref;
                case PE.PrimitiveType.UInt16:
                    return global.UInt16Ref;
                case PE.PrimitiveType.UInt32:
                    return global.UInt32Ref;
                case PE.PrimitiveType.UInt64:
                    return global.UInt64Ref;
                case PE.PrimitiveType.UIntNative:
                    return global.UIntNativeRef;
                case PE.PrimitiveType.Single:
                    return global.SingleRef;
                case PE.PrimitiveType.Double:
                    return global.DoubleRef;
                case PE.PrimitiveType.Object:
                    return global.ObjectRef;
                case PE.PrimitiveType.String:
                    return global.StringRef;
                case PE.PrimitiveType.TypedRef:
                    return global.TypedReferenceRef;
                case PE.PrimitiveType.Type:
                    return global.TypeRef;
                case PE.PrimitiveType.Void:
                    return global.VoidRef;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private TypeRef TypeRefFromTypeSig(DllLoadContext ctxt, IImSeq<Annotation> annotations, PE.TypeSig sig, bool mustBeFirstKinded)
        {
            switch (sig.Flavor)
            {
            case PE.TypeSigFlavor.PinnedPsuedo:
            case PE.TypeSigFlavor.SentinelPsuedo:
            case PE.TypeSigFlavor.CustomModPsuedo:
                throw new InvalidOperationException("pseudo type in type signature");
            case PE.TypeSigFlavor.Primitive:
                {
                    var primSig = (PE.PrimitiveTypeSig)sig;
                    var typeRef = TypeRefFromPrimitiveTypeName(primSig.PrimitiveType);
                    return annotations == null || annotations.Count == 0
                               ? typeRef
                               : typeRef.WithAnnotations(annotations);
                }
            case PE.TypeSigFlavor.UnmanagedPointer:
                {
                    var unmanPtrSig = (PE.UnmanagedPointerTypeSig)sig;
                    return global.UnmanagedPointerTypeConstructorRef.ApplyTo(TypeRefFromTypeSigWithCustomMods(ctxt, unmanPtrSig.ElementType));
                }
            case PE.TypeSigFlavor.ManagedPointer:
                {
                    var manPtrSig = (PE.ManagedPointerTypeSig)sig;
                    return global.ManagedPointerTypeConstructorRef.ApplyTo(annotations, TypeRefFromTypeSig(ctxt, null, manPtrSig.ElementType, true));
                }
            case PE.TypeSigFlavor.Array:
                {
                    var arrayTypeSig = (PE.ArrayTypeSig)sig;
                    return global.ArrayTypeConstructorRef.ApplyTo(annotations, TypeRefFromTypeSigWithCustomMods(ctxt, arrayTypeSig.ElementType));
                }
            case PE.TypeSigFlavor.MultiDimArray:
                {
                    var multiSig = (PE.MultiDimArrayTypeSig)sig;
                    var bounds = new Seq<MultiDimArrayBound>(multiSig.Rank);
                    for (var i = 0; i < multiSig.Rank; i++)
                    {
                        var l = i < multiSig.LoBounds.Count ? (int?)multiSig.LoBounds[i] : null;
                        var s = i < multiSig.Sizes.Count ? (int?)multiSig.Sizes[i] : null;
                        bounds.Add(new MultiDimArrayBound(l, s));
                    }
                    return TypeRef.MultiDimArrayFrom
                        (global, bounds, TypeRefFromTypeSig(ctxt, null, multiSig.ElementType, true));
                }
            case PE.TypeSigFlavor.TypeDefOrRef:
                {
                    var typeDefOrRefSig = (PE.TypeDefOrRefSig)sig;
                    var res = TypeRefFromRow
                        (ctxt, annotations, typeDefOrRefSig.TypeDefOrRef.Value, mustBeFirstKinded);
                    return res;
                }
            case PE.TypeSigFlavor.TypeParameter:
                {
                    var paramSig = (PE.TypeParameterTypeSig)sig;
                    return new ParameterTypeRef(annotations, ParameterFlavor.Type, paramSig.Index);
                }
            case PE.TypeSigFlavor.Application:
                {
                    var appTypeSig = (PE.ApplicationTypeSig)sig;
                    // NOTE: Don't turn applicand into self reference if it is higer-kinded
                    var applicand = TypeRefFromTypeSig(ctxt, null, appTypeSig.Applicand, false);
                    var args = new Seq<TypeRef>();
                    foreach (var s in appTypeSig.Arguments)
                        args.Add(TypeRefFromTypeSig(ctxt, null, s, true));
                    switch (applicand.Flavor)
                    {
                    case TypeRefFlavor.Parameter:
                    case TypeRefFlavor.Skolem:
                        throw new InvalidOperationException("no higher-kinded type parameters");
                    case TypeRefFlavor.Named:
                        {
                            var namedApplicand = (NamedTypeRef)applicand;
                            if (namedApplicand.Arguments.Count > 0)
                                throw new InvalidOperationException("no partial type application");
                            return new NamedTypeRef(annotations, namedApplicand.QualifiedTypeName, args);
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
            case PE.TypeSigFlavor.MethodParameter:
                {
                    var paramSig = (PE.MethodParameterTypeSig)sig;
                    return new ParameterTypeRef(annotations, ParameterFlavor.Method, paramSig.Index);
                }
            case PE.TypeSigFlavor.FunctionPointer:
                {
                    var funcPtrSig = (PE.FunctionPointerTypeSig)sig;
                    if (!funcPtrSig.Method.IsStatic && funcPtrSig.Method.IsExplicitThis)
                        throw new InvalidOperationException("cannot determine type of implicit 'this' argument");
                    var retType = TypeRefFromTypeSigWithCustomMods(ctxt, funcPtrSig.Method.ReturnType);
                    var paramTypes = default(Seq<TypeRef>);
                    if (funcPtrSig.Method.Parameters.Count > 0)
                    {
                        paramTypes = new Seq<TypeRef>();
                        foreach (var tcm in funcPtrSig.Method.Parameters)
                            paramTypes.Add(TypeRefFromTypeSigWithCustomMods(ctxt, tcm));
                    }
                    if (retType.Equals(global.VoidRef))
                        retType = null;
                    return TypeRef.CodePointerFrom(global, paramTypes, retType);
                }
            default:
                throw new ArgumentOutOfRangeException();
            }
        }

        private TypeRef TypeRefFromRow(DllLoadContext ctxt, IImSeq<Annotation> annotations, PE.Row row, bool mustBeFirstKinded)
        {
            var res = default(TypeRef);
            var cache = mustBeFirstKinded ? ctxt.FKTypeRefCache : ctxt.HKTypeRefCache;
            if (!cache.TryGetValue(row, out res)) {
                switch (row.Tag)
                {
                case PE.TableTag.TypeDef:
                    {
                        var typeDefRow = (PE.TypeDefRow)row;
                        var typeBoundArguments = default(Seq<TypeRef>);
                        if (mustBeFirstKinded)
                        {
                            // Members of higher-kinded types refer to their defining type using the higher-kinded
                            // type def itself rather than a type spec which instantiates it to it's own type
                            // parameters. Undo that space optimization here.
                            var typeParams = from r in ctxt.File.Tables.GenericParamTable
                                             where r.Owner.Value == typeDefRow
                                             select r;
                            var typeArity = typeParams.Count();
                            if (typeArity > 0)
                            {
                                typeBoundArguments = new Seq<TypeRef>();
                                for (var i = 0; i < typeArity; i++)
                                    typeBoundArguments.Add(new ParameterTypeRef(ParameterFlavor.Type, i));
                            }
                        }
                        var typeName = TypeNameFromTypeDefRow(ctxt, typeDefRow);
                        var strongName = SubstAssemblyName(ctxt, ctxt.AssemblyName);
                        res = new NamedTypeRef
                            (annotations, new QualifiedTypeName(strongName, typeName), typeBoundArguments);
                        break;
                    }
                case PE.TableTag.TypeRef:
                    {
                        var typeRefRow = (PE.TypeRefRow)row;
                        var typeName = TypeNameFromTypeRefRow(typeRefRow);
                        var strongName = default(AssemblyName);
                        if (typeRefRow.ResolutionScope.IsNull)
                        {
                            var exportRows = from r in ctxt.File.Tables.ExportedTypeTable
                                             where
                                                 r.TypeNamespace.Value == typeRefRow.TypeNamespace.Value &&
                                                 r.TypeName.Value == typeRefRow.TypeName.Value
                                             select r.Implementation.Value;
                            var exportRow = exportRows.FirstOrDefault();
                            if (exportRow == null)
                                throw new InvalidOperationException
                                    ("unable to determine referenced assembly for type ref");
                            strongName = AssemblyNameFromExportRow(exportRow);
                        }
                        else
                            strongName = AssemblyNameFromResolutionScopeRow(ctxt, typeRefRow.ResolutionScope.Value);
                        res = new NamedTypeRef(annotations, new QualifiedTypeName(strongName, typeName));
                        break;
                    }
                case PE.TableTag.TypeSpec:
                    {
                        var typeSpecRow = (PE.TypeSpecRow)row;
                        res = TypeRefFromTypeSig(ctxt, annotations, typeSpecRow.Signature.Value, mustBeFirstKinded);
                        break;
                    }
                default:
                    throw new InvalidOperationException("unexpected type row");
                }
                cache.Add(row, res);
            }
            return res;
        }

        private TypeRef DefiningTypeRefFromRow(DllLoadContext ctxt, PE.Row row)
        {
            switch (row.Tag)
            {
            case PE.TableTag.TypeDef:
            case PE.TableTag.TypeRef:
            case PE.TableTag.TypeSpec:
                return TypeRefFromRow(ctxt, null, row, true);
            case PE.TableTag.ModuleRef:
                {
                    var strongName = SubstAssemblyName(ctxt, ctxt.AssemblyName);
                    return new NamedTypeRef(new QualifiedTypeName(strongName, new TypeName("<Module>")));
                }
            case PE.TableTag.MethodDef:
                {
                    var methodDefRow = (PE.MethodDefRow)row;
                    var definingTypeDefRow = default(PE.TypeDefRow);
                    if (!ctxt.MemberToDefiningType.TryGetValue(methodDefRow, out definingTypeDefRow))
                        throw new InvalidOperationException("method def does not have a defining type row");
                    return TypeRefFromRow(ctxt, null, definingTypeDefRow, true);
                }
            default:
                throw new InvalidOperationException("unexpected defining type row");
            }
        }

        // ----------------------------------------------------------------------
        // Member references
        // ----------------------------------------------------------------------

        private FieldRef FieldRefFromRow(DllLoadContext ctxt, PE.Row row)
        {
            var res = default(FieldRef);
            if (!(ctxt.FieldRefCache.TryGetValue(row, out res)))
            {
                switch (row.Tag)
                {
                case PE.TableTag.MemberRef:
                    {
                        var memberRefRow = (PE.MemberRefRow)row;
                        var fieldSig = memberRefRow.Signature.Value as PE.FieldMemberSig;
                        if (fieldSig == null)
                            throw new InvalidOperationException("expecting a field reference");
                        var definingType = TypeRefFromRow(ctxt, null, memberRefRow.Class.Value, true);
                        var fieldType = TypeRefFromTypeSigWithCustomMods(ctxt, fieldSig.Type);
                        res = new FieldRef(definingType, memberRefRow.Name.Value, fieldType);
                        break;
                    }
                case PE.TableTag.Field:
                    {
                        var fieldRow = (PE.FieldRow)row;
                        var definingTypeRow = default(PE.TypeDefRow);
                        if (!ctxt.MemberToDefiningType.TryGetValue(fieldRow, out definingTypeRow))
                            throw new InvalidOperationException("no type def contains field def");
                        var definingType = TypeRefFromRow(ctxt, null, definingTypeRow, true);
                        var fieldType = TypeRefFromTypeSigWithCustomMods(ctxt, fieldRow.Signature.Value.Type);
                        res = new FieldRef(definingType, fieldRow.Name.Value, fieldType);
                        break;
                    }
                default:
                    throw new InvalidOperationException("unexpected row for field ref");
                }
                ctxt.FieldRefCache.Add(row, res);
            }
            return res;
        }

        // FOUR SPECIAL CASES:
        //  - We may need to replace the 'this' parameter with a managed pointer should the member
        //    turn out to be an instance member on a value type
        //  - We need to replace IntPtr's in delegate constructors with their code pointer equivalents
        //  - The defining type may be a particular instance of a higher-kinded type, but we
        //    must turn it back into an instance of that type at its own type parameters to
        //    represent the 'this' argument
        //  - The 'Get'/'Set' runtime  methods of multi-dimensional arrays refers to the 'value'
        //    result/parameter using the actual array element type, while we wish to refer to it as if
        //    it were a method on a higher-kinded type.
        private void ArgsAndResultFromMethodSig(DllLoadContext ctxt, TypeRef definingTypeRef, string name, bool isStatic, out IImSeq<TypeRef> valueParameters, out TypeRef result, PE.MethodMemberSig sig)
        {
            var implicitThis = !isStatic && !sig.IsExplicitThis;
            var arity = sig.Parameters.Count + (implicitThis ? 1 : 0);
            var valParams = arity == 0 ? default(Seq<TypeRef>) : new Seq<TypeRef>(arity);
            if (implicitThis)
            {
                var thisType = definingTypeRef;
                if (thisType.Arguments.Count > 0)
                {
                    var newArguments = new Seq<TypeRef>(thisType.Arguments.Count);
                    for (var i = 0; i < thisType.Arguments.Count; i++)
                        newArguments.Add(new ParameterTypeRef(ParameterFlavor.Type, i));
                    thisType = thisType.WithArguments(newArguments);
                }
                valParams.Add(thisType);
            }

            foreach (var p in sig.Parameters)
                valParams.Add(TypeRefFromTypeSigWithCustomMods(ctxt, p));

            result = TypeRefFromTypeSigWithCustomMods(ctxt, sig.ReturnType);
            if (result.Equals(global.VoidRef))
                result = null;

            if (definingTypeRef.Arguments.Count == 1 && !isStatic && (name.Equals("Set", StringComparison.Ordinal) || name.Equals("Get", StringComparison.Ordinal)) &&
                valParams != null)
            {
                var bounds = global.GetMultiDimArrayTypeConstructorDetails(definingTypeRef.QualifiedTypeName);
                if (bounds != null)
                {
                    if (name.Equals("Set", StringComparison.Ordinal) && valParams.Count == 2 + bounds.Rank &&
                        valParams[valParams.Count - 1].Equals(definingTypeRef.Arguments[0]))
                    {
                        var paramRef = new ParameterTypeRef(ParameterFlavor.Type, 0);
                        valParams[valParams.Count - 1] = paramRef;
                    }
                    else if (name.Equals("Get", StringComparison.Ordinal) && valParams.Count == 1 + bounds.Rank &&
                             result != null && result.Equals(definingTypeRef.Arguments[0]))
                    {
                        result = new ParameterTypeRef(ParameterFlavor.Type, 0);
                    }
                }
            }

            valueParameters = valParams;
        }

        private PolymorphicMethodRef MethodRefFromMethodSig(DllLoadContext ctxt, TypeRef definingTypeRef, string name, bool isStatic, IImSeq<TypeRef> methodBoundArguments, int arity, PE.MethodMemberSig sig)
        {
            var valueParameters = default(IImSeq<TypeRef>);
            var result = default(TypeRef);
            ArgsAndResultFromMethodSig(ctxt, definingTypeRef, name, isStatic, out valueParameters, out result, sig);
            if (arity >= 0)
                return new PolymorphicMethodRef(definingTypeRef, name, isStatic, arity, valueParameters, result);
            else
                return new MethodRef(definingTypeRef, name, isStatic, methodBoundArguments, valueParameters, result);
        }

        private MethodSignature SignatureFromMethodSig(DllLoadContext ctxt, TypeRef definingTypeRef, string name, bool isStatic, PE.MethodMemberSig sig)
        {
            var valueParameters = default(IImSeq<TypeRef>);
            var result = default(TypeRef);
            ArgsAndResultFromMethodSig(ctxt, definingTypeRef, name, isStatic, out valueParameters, out result, sig);
            return new MethodSignature(name, isStatic, sig.TypeArity, valueParameters, result);
        }

        private MethodRef MethodRefFromRowWithTypeArgs(DllLoadContext ctxt, IImSeq<TypeRef> methodBoundArguments, PE.Row row)
        {
            var res = default(MethodRef);
            switch (row.Tag)
            {
            case PE.TableTag.MethodDef:
                {
                    var methodDefRow = (PE.MethodDefRow)row;
                    var definingTypeRow = default(PE.TypeDefRow);
                    if (!ctxt.MemberToDefiningType.TryGetValue(methodDefRow, out definingTypeRow))
                        throw new InvalidOperationException("no type def contains method def");
                    var definingTypeRef = TypeRefFromRow(ctxt, null, definingTypeRow, true);
                    var isStatic = (methodDefRow.Flags & PE.MethodAttributes.Static) != 0;
                    var methodSig = methodDefRow.Signature.Value;
                    if (methodSig.TypeArity != (methodBoundArguments == null ? 0 : methodBoundArguments.Count))
                        throw new InvalidOperationException("mismatched method type arity");
                    res =
                        (MethodRef)
                        MethodRefFromMethodSig
                            (ctxt,
                             definingTypeRef,
                             methodDefRow.Name.Value,
                             isStatic,
                             methodBoundArguments,
                             -1,
                             methodSig);
                    break;
                }
            case PE.TableTag.MemberRef:
                {
                    var memberRefRow = (PE.MemberRefRow)row;
                    var definingTypeRef = DefiningTypeRefFromRow(ctxt, memberRefRow.Class.Value);
                    var methodSig = memberRefRow.Signature.Value as PE.MethodMemberSig;
                    if (methodSig == null)
                        throw new InvalidOperationException("expecting a method sig");
                    if (methodSig.TypeArity != (methodBoundArguments == null ? 0 : methodBoundArguments.Count))
                        throw new InvalidOperationException("mismatched method type arity");
                    var annotations = new Seq<Annotation>();
                    annotations.Add(AnnotationFromCallingConvention(methodSig.CallingConvention));
                    res =
                        (MethodRef)
                        MethodRefFromMethodSig
                            (ctxt,
                             definingTypeRef,
                             memberRefRow.Name.Value,
                             methodSig.IsStatic,
                             methodBoundArguments,
                             -1,
                             methodSig);
                    break;
                }
            case PE.TableTag.MethodSpec:
                throw new InvalidOperationException("partial type application for methods is not supported");
            default:
                throw new InvalidOperationException("unexpected row for method ref");
            }
            return res;
        }

        private MethodRef MethodRefFromRow(DllLoadContext ctxt, PE.Row row)
        {
            var res = default(MethodRef);
            if (!ctxt.MethodRefCache.TryGetValue(row, out res))
            {
                switch (row.Tag)
                {
                case PE.TableTag.MethodDef:
                case PE.TableTag.MemberRef:
                    res = MethodRefFromRowWithTypeArgs(ctxt, null, row);
                    break;
                case PE.TableTag.MethodSpec:
                    {
                        var methodSpecRow = (PE.MethodSpecRow)row;
                        var methodBoundArguments = default(Seq<TypeRef>);
                        if (methodSpecRow.Instantiation.Value.Arguments.Count > 0)
                        {
                            methodBoundArguments = new Seq<TypeRef>();
                            foreach (var s in methodSpecRow.Instantiation.Value.Arguments)
                                methodBoundArguments.Add(TypeRefFromTypeSig(ctxt, null, s, true));
                        }
                        res = MethodRefFromRowWithTypeArgs(ctxt, methodBoundArguments, methodSpecRow.Method.Value);
                        break;
                    }
                default:
                    throw new InvalidOperationException("unexpected row for method ref");
                }
                ctxt.MethodRefCache.Add(row, res);
            }
            return res;
        }

        private PolymorphicMethodRef PolymorphicMethodRefFromRow(DllLoadContext ctxt, PE.Row row)
        {
            var res = default(PolymorphicMethodRef);
            if (!ctxt.PolymorphicMethodRefCache.TryGetValue(row, out res))
            {
                switch (row.Tag)
                {
                case PE.TableTag.MethodDef:
                    {
                        var methodDefRow = (PE.MethodDefRow)row;
                        var definingTypeRow = default(PE.TypeDefRow);
                        if (!ctxt.MemberToDefiningType.TryGetValue(methodDefRow, out definingTypeRow))
                            throw new InvalidOperationException("no type def contains method def");
                        var definingTypeRef = TypeRefFromRow(ctxt, null, definingTypeRow, true);
                        var isStatic = (methodDefRow.Flags & PE.MethodAttributes.Static) != 0;
                        var methodSig = methodDefRow.Signature.Value;
                        res =
                            (PolymorphicMethodRef)
                            MethodRefFromMethodSig
                                (ctxt,
                                 definingTypeRef,
                                 methodDefRow.Name.Value,
                                 isStatic,
                                 null,
                                 methodSig.TypeArity,
                                 methodSig);
                        break;
                    }
                case PE.TableTag.MemberRef:
                    {
                        var memberRefRow = (PE.MemberRefRow)row;
                        var definingTypeRef = TypeRefFromRow(ctxt, null, memberRefRow.Class.Value, true);
                        var methodSig = memberRefRow.Signature.Value as PE.MethodMemberSig;
                        if (methodSig == null)
                            throw new InvalidOperationException("expecting a method sig");
                        var annotations = new Seq<Annotation>();
                        annotations.Add(AnnotationFromCallingConvention(methodSig.CallingConvention));
                        res =
                            (PolymorphicMethodRef)
                            MethodRefFromMethodSig
                                (ctxt,
                                 definingTypeRef,
                                 memberRefRow.Name.Value,
                                 methodSig.IsStatic,
                                 null,
                                 methodSig.TypeArity,
                                 methodSig);
                        break;
                    }
                case PE.TableTag.MethodSpec:
                    throw new InvalidOperationException("expecting polymorphic method");
                default:
                    throw new InvalidOperationException("unexpected row for method ref");
                }
                ctxt.PolymorphicMethodRefCache.Add(row, res);
            }
            return res;
        }

        private MethodSignature SignatureFromRow(DllLoadContext ctxt, PE.Row row)
        {
            var res = default(MethodSignature);
            if (!ctxt.MethodSignatureCache.TryGetValue(row, out res))
            {
                switch (row.Tag)
                {
                case PE.TableTag.MethodDef:
                    {
                        var methodDefRow = (PE.MethodDefRow)row;
                        var definingTypeRow = default(PE.TypeDefRow);
                        if (!ctxt.MemberToDefiningType.TryGetValue(methodDefRow, out definingTypeRow))
                            throw new InvalidOperationException("no type def contains method def");
                        var definingTypeRef = TypeRefFromRow(ctxt, null, definingTypeRow, true);
                        var isStatic = (methodDefRow.Flags & PE.MethodAttributes.Static) != 0;
                        var methodSig = methodDefRow.Signature.Value;
                        res = SignatureFromMethodSig
                            (ctxt, definingTypeRef, methodDefRow.Name.Value, isStatic, methodSig);
                        break;
                    }
                case PE.TableTag.MemberRef:
                    {
                        var memberRefRow = (PE.MemberRefRow)row;
                        var definingTypeRef = TypeRefFromRow(ctxt, null, memberRefRow.Class.Value, true);
                        var methodSig = memberRefRow.Signature.Value as PE.MethodMemberSig;
                        if (methodSig == null)
                            throw new InvalidOperationException("expecting a method sig");
                        var annotations = new Seq<Annotation>();
                        annotations.Add(AnnotationFromCallingConvention(methodSig.CallingConvention));
                        res = SignatureFromMethodSig
                            (ctxt, definingTypeRef, memberRefRow.Name.Value, methodSig.IsStatic, methodSig);
                        break;
                    }
                case PE.TableTag.MethodSpec:
                    throw new InvalidOperationException("expecting polymorphic method");
                default:
                    throw new InvalidOperationException("unexpected row for method ref");
                }
                ctxt.MethodSignatureCache.Add(row, res);
            }
            return res;
        }

        // ----------------------------------------------------------------------
        // Field definitions
        // ----------------------------------------------------------------------

        private FieldDef FieldDefFromRow(DllLoadContext ctxt, PE.FieldRow row)
        {
            var annotations = new Seq<Annotation>();

            var accessibility = default(Accessibility);
            switch (row.Flags & PE.FieldAttributes.FieldAccessMask)
            {
            case PE.FieldAttributes.CompilerControlled:
                accessibility = Accessibility.CompilerControlled;
                break;
            case PE.FieldAttributes.Private:
                accessibility = Accessibility.Private;
                break;
            case PE.FieldAttributes.FamANDAssem:
                accessibility = Accessibility.FamilyANDAssembly;
                break;
            case PE.FieldAttributes.Assembly:
                accessibility = Accessibility.Assembly;
                break;
            case PE.FieldAttributes.Family:
                accessibility = Accessibility.Family;
                break;
            case PE.FieldAttributes.FamORAssem:
                accessibility = Accessibility.FamilyORAssembly;
                break;
            case PE.FieldAttributes.Public:
                accessibility = Accessibility.Public;
                break;
            }
            annotations.Add(new AccessibilityAnnotation(accessibility));

            var isSpecialName = (row.Flags & PE.FieldAttributes.SpecialName) != 0;
            var isRTSpecialName = (row.Flags & PE.FieldAttributes.RTSpecialName) != 0;
            if (isSpecialName || isRTSpecialName)
                annotations.Add(new SpecialNameAnnotation(isRTSpecialName));

            if ((row.Flags & PE.FieldAttributes.PInvokeImpl) != 0)
                annotations.Add(new PInvokeAnnotation());

            if ((row.Flags & PE.FieldAttributes.HasFieldMarshal) != 0)
                annotations.Add(new MarshalAnnotation());

            var isInitOnly = (row.Flags & PE.FieldAttributes.InitOnly) != 0;
            annotations.Add(new FieldAccessAnnotation(isInitOnly));

            var isStatic = (row.Flags & PE.FieldAttributes.Static) != 0;

            var hasDefault = (row.Flags & PE.FieldAttributes.HasDefault) != 0;
            var hasRVA = (row.Flags & PE.FieldAttributes.HasFieldRVA) != 0;

            var fieldType = TypeRefFromTypeSigWithCustomMods(ctxt, row.Signature.Value.Type);

            var fieldInit = default(FieldInit);
            if (hasDefault)
            {
                var constantRow = (from r in ctxt.File.Tables.ConstantTable
                                   where r.Parent.Value == row
                                   select r).FirstOrDefault();
                if (constantRow == null)
                    throw new InvalidOperationException("no constant row for field");
                fieldInit = new ConstFieldInit(constantRow.Value.Value);
            }
            else if (hasRVA)
            {
                var dataRow = (from r in ctxt.File.Tables.FieldRVATable
                               where r.Field.Value == row
                               select r).FirstOrDefault();
                if (dataRow == null)
                    throw new InvalidOperationException("no rva row for field");
                dataRow.GetData(ctxt.ReaderContext);
                fieldInit = new RawFieldInit(dataRow.Data.Value);
            }

            var fieldDef = new FieldDef(annotations, null, row.Name.Value, isStatic, fieldType, fieldInit);
            ctxt.MemberDefs.Add(row, fieldDef);
            return fieldDef;
        }

        // ----------------------------------------------------------------------
        // Method definitions
        // ----------------------------------------------------------------------

        private MethodCallingConventionAnnotation AnnotationFromCallingConvention(PE.CallingConvention conv)
        {
            var callingConvention = default(CallingConvention);
            switch (conv)
            {
                case PE.CallingConvention.Managed:
                    callingConvention = CallingConvention.Managed;
                    break;
                case PE.CallingConvention.ManagedVarArg:
                    callingConvention = CallingConvention.ManagedVarArg;
                    break;
                case PE.CallingConvention.NativeC:
                    callingConvention = CallingConvention.NativeC;
                    break;
                case PE.CallingConvention.NativeStd:
                    callingConvention = CallingConvention.NativeStd;
                    break;
                case PE.CallingConvention.NativeThis:
                    callingConvention = CallingConvention.NativeThis;
                    break;
                case PE.CallingConvention.NativeFast:
                    callingConvention = CallingConvention.NativeFast;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return new MethodCallingConventionAnnotation(callingConvention);
        }

        private ParameterTypeDef ParameterTypeDefFromRow(DllLoadContext ctxt, ParameterFlavor flavor, PE.GenericParamRow row)
        {
            var annotations = default(Seq<Annotation>);
            if (!row.Name.IsNull)
            {
                annotations = new Seq<Annotation>();
                annotations.Add(new NameAnnotation(row.Name.Value));
            }

            var variance = default(ParameterVariance);
            switch (row.Flags & PE.GenericParamAttributes.VarianceMask)
            {
            case PE.GenericParamAttributes.None:
                variance = ParameterVariance.Invariant;
                break;
            case PE.GenericParamAttributes.Covariant:
                variance = ParameterVariance.Covariant;
                break;
            case PE.GenericParamAttributes.Contravariant:
                variance = ParameterVariance.Contravariant;
                break;
            }


            // Watch out: the SpecialConstraintMask sub-field is a bit field and not an enum
            var constraint = ParameterConstraint.Unconstrained;
            if ((row.Flags & PE.GenericParamAttributes.ReferenceTypeConstraint) != 0)
                constraint = ParameterConstraint.ReferenceType;
            else if ((row.Flags & PE.GenericParamAttributes.NotNullableValueTypeConstraint) != 0)
            {
                constraint = ParameterConstraint.NonNullableValueType;
                // implies DefaultConstructor also
            }
            else if ((row.Flags & PE.GenericParamAttributes.DefaultConstructorConstraint) != 0)
                constraint = ParameterConstraint.DefaultConstructor;

            var extends = default(TypeRef);
            var implements = default(Seq<TypeRef>);
            var constraintRows = from r in ctxt.File.Tables.GenericParamConstraintTable
                                 where r.Owner.Value == row
                                 select r.Constraint.Value;
            foreach (var constraintRow in constraintRows)
            {
                var constraintTypeRef = TypeRefFromRow(ctxt, null, constraintRow, true);
                // NOTE: We can't distinguish class vs interface yet, so always add to implements clause and
                //       fixup in last pass
                if (implements == null)
                    implements = new Seq<TypeRef>();
                implements.Add(constraintTypeRef);
            }

            var typeDef = new ParameterTypeDef
                (annotations, null, extends, implements, flavor, row.Number, variance, constraint);
            ctxt.TypeDefs.Add(row, typeDef);
            return typeDef;
        }

        private object ResolveRow(DllLoadContext ctxt, PE.OpCode opCode, PE.Row row)
        {
            switch (row.Tag)
            {
            case PE.TableTag.Field:
                return FieldRefFromRow(ctxt, row);
            case PE.TableTag.MethodDef:
            case PE.TableTag.MethodSpec:
                return MethodRefFromRow(ctxt, row);
            case PE.TableTag.MemberRef:
                {
                    var memberRefRow = (PE.MemberRefRow)row;
                    switch (memberRefRow.Signature.Value.Flavor)
                    {
                    case PE.MemberSigFlavor.Method:
                    case PE.MemberSigFlavor.MethodSpec:
                        return MethodRefFromRow(ctxt, row);
                    case PE.MemberSigFlavor.Field:
                        return FieldRefFromRow(ctxt, row);
                    case PE.MemberSigFlavor.Property:
                    case PE.MemberSigFlavor.LocalVar:
                        throw new InvalidOperationException("expecting method or field sig");
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
            case PE.TableTag.TypeDef:
            case PE.TableTag.TypeRef:
            case PE.TableTag.TypeSpec:
                // NOTE: LdToken <type> can refer to higher-kinded type, all other type refs are first-kinded
                return TypeRefFromRow(ctxt, null, row, opCode != PE.OpCode.Ldtoken);
            default:
                throw new InvalidOperationException("expecting method, field or type row");
            }
        }

        private MethodDef MethodDefFromRow(DllLoadContext ctxt, TypeRef thisType, PE.MethodDefRow row)
        {
            var name = row.Name.Value;
            var annotations = new Seq<Annotation>();

            var accessibility = default(Accessibility);
            switch (row.Flags & PE.MethodAttributes.MemberAccessMask)
            {
            case PE.MethodAttributes.CompilerControlled:
                accessibility = Accessibility.CompilerControlled;
                break;
            case PE.MethodAttributes.Private:
                accessibility = Accessibility.Private;
                break;
            case PE.MethodAttributes.FamANDAssem:
                accessibility = Accessibility.FamilyANDAssembly;
                break;
            case PE.MethodAttributes.Assem:
                accessibility = Accessibility.Assembly;
                break;
            case PE.MethodAttributes.Family:
                accessibility = Accessibility.Family;
                break;
            case PE.MethodAttributes.FamORAssem:
                accessibility = Accessibility.FamilyORAssembly;
                break;
            case PE.MethodAttributes.Public:
                accessibility = Accessibility.Public;
                break;
            }
            annotations.Add(new AccessibilityAnnotation(accessibility));

            var isSpecialName = (row.Flags & PE.MethodAttributes.SpecialName) != 0;
            var isRTSpecialName = (row.Flags & PE.MethodAttributes.RTSpecialName) != 0;
            if (isSpecialName || isRTSpecialName)
                annotations.Add(new SpecialNameAnnotation(isRTSpecialName));

            annotations.Add
                (new MethodOverriddingControlAnnotation
                     ((row.Flags & PE.MethodAttributes.Final) != 0,
                      (row.Flags & PE.MethodAttributes.HideBySig) != 0,
                      (row.Flags & PE.MethodAttributes.Strict) != 0));

            if ((row.Flags & PE.MethodAttributes.PInvokeImpl) != 0)
                annotations.Add(new PInvokeAnnotation());

            annotations.Add
                (new MethodSecurityAnnotation
                     ((row.Flags & PE.MethodAttributes.HasSecurity) != 0,
                      (row.Flags & PE.MethodAttributes.RequireSecObject) != 0));

            var methodMemberSig = row.Signature.Value;

            var typeParameters = default(Seq<ParameterTypeDef>);
            if (methodMemberSig.TypeArity > 0)
            {
                typeParameters = (from r in ctxt.File.Tables.GenericParamTable
                                  where r.Owner.Value == row
                                  select ParameterTypeDefFromRow(ctxt, ParameterFlavor.Method, r)).ToSeq();
                if (typeParameters.Count != methodMemberSig.TypeArity)
                    throw new InvalidOperationException("mismatched method type arity");
            }

            var implicitThis = !methodMemberSig.IsStatic && !methodMemberSig.IsExplicitThis;
            var valueArity = methodMemberSig.Parameters.Count;
            if (implicitThis)
                valueArity++;

            var result = default(ParameterOrLocalOrResult);
            var resultType = TypeRefFromTypeSigWithCustomMods(ctxt, methodMemberSig.ReturnType);
            if (resultType.Equals(global.VoidRef))
                resultType = null;
            else
                result = new ParameterOrLocalOrResult(null, null, resultType);

            var valueParameters = default(Seq<ParameterOrLocalOrResult>);
            valueParameters = new Seq<ParameterOrLocalOrResult>(valueParameters);
            var annotsArr = new Seq<Annotation>[valueArity];
            var rowArr = new PE.ParamRow[valueArity];
            foreach (var pRow in row.ParamList.Value)
            {
                if (pRow.Sequence == 0)
                {
                    if (result != null)
                        ctxt.ParametersAndLocals.Add(pRow, result);
                }
                else
                {
                    var pAnnotations = new Seq<Annotation>();
                    if (!pRow.Name.IsNull)
                        pAnnotations.Add(new NameAnnotation(pRow.Name.Value));

                    var convention = default(ParamPassingConvention);
                    if ((pRow.Flags & PE.ParamAttributes.In) != 0)
                        convention = ParamPassingConvention.In;
                    else if ((pRow.Flags & PE.ParamAttributes.Out) != 0)
                        convention = ParamPassingConvention.Out;
                    else
                        convention = ParamPassingConvention.Norm;

                    if ((pRow.Flags & PE.ParamAttributes.HasFieldMarshal) != 0)
                        pAnnotations.Add(new MarshalAnnotation());

                    var isOpt = (pRow.Flags & PE.ParamAttributes.Optional) != 0;
                    var hasDef = (pRow.Flags & PE.ParamAttributes.HasDefault) != 0;

                    pAnnotations.Add(new ParamPassingAnnotation(convention, isOpt, hasDef));
                    var index = pRow.Sequence - 1 + (implicitThis ? 1 : 0);
                    annotsArr[index] = pAnnotations;
                    rowArr[index] = pRow;
                }
            }

            var i = 0;
            if (implicitThis)
            {
                var paramOrLocal = new ParameterOrLocalOrResult(null, null, thisType);
                valueParameters.Add(paramOrLocal);
                i++;
            }
            foreach (var p in methodMemberSig.Parameters)
            {
                var paramOrLocal = new ParameterOrLocalOrResult
                    (annotsArr[i], null, TypeRefFromTypeSigWithCustomMods(ctxt, p));
                if (rowArr[i] != null)
                    ctxt.ParametersAndLocals.Add(rowArr[i], paramOrLocal);
                valueParameters.Add(paramOrLocal);
                i++;
            }

            annotations.Add(AnnotationFromCallingConvention(methodMemberSig.CallingConvention));

            var methodStyle = default(MethodStyle);
            if ((row.Flags & PE.MethodAttributes.SpecialName) != 0 && (row.Flags & PE.MethodAttributes.RTSpecialName) != 0 &&
                (row.Name.Value.Equals(".ctor", StringComparison.Ordinal) ||
                 row.Name.Value.Equals(".cctor", StringComparison.Ordinal)))
                methodStyle = MethodStyle.Constructor;
            else if ((row.Flags & PE.MethodAttributes.Abstract) != 0)
                methodStyle = MethodStyle.Abstract;
            else if ((row.Flags & PE.MethodAttributes.Virtual) != 0)
                methodStyle = MethodStyle.Virtual;
            else
                methodStyle = MethodStyle.Normal;

            var isStatic = (row.Flags & PE.MethodAttributes.Static) != 0;

            row.GetData(ctxt.ReaderContext, (opcode, obj) => ResolveRow(ctxt, opcode, obj));
            var methodBody = row.Body.Value;

            var codeFlavor = default(MethodCodeFlavor);
            switch (row.ImplFlags & PE.MethodImplAttributes.CodeTypeMask)
            {
            case PE.MethodImplAttributes.IL:
            case PE.MethodImplAttributes.OPTIL:
                codeFlavor = MethodCodeFlavor.Managed;
                break;
            case PE.MethodImplAttributes.Native:
                codeFlavor = MethodCodeFlavor.Native;
                break;
            case PE.MethodImplAttributes.Runtime:
                codeFlavor = MethodCodeFlavor.Runtime;
                break;
            }
            if ((row.ImplFlags & PE.MethodImplAttributes.ForwardRef) != 0)
                codeFlavor = MethodCodeFlavor.ForwardRef;
            if (methodBody == null)
                codeFlavor = MethodCodeFlavor.ManagedExtern;

            var isSyncronized = (row.ImplFlags & PE.MethodImplAttributes.Synchronized) != 0;
            var noInlining = (row.ImplFlags & PE.MethodImplAttributes.NoInlining) != 0;
            var hasNewSlot = (row.Flags & PE.MethodAttributes.NewSlot) != 0;
            var isInitLocals = methodBody == null ? false : methodBody.IsInitLocals;
            var locals = default(Seq<ParameterOrLocalOrResult>);

            if (codeFlavor == MethodCodeFlavor.Managed)
            {
                if (methodBody.LocalVariables != null && methodBody.LocalVariables.Variables.Count > 0)
                {
                    locals = new Seq<ParameterOrLocalOrResult>();
                    for (var j = 0; j < methodBody.LocalVariables.Variables.Count; j++)
                    {
                        var lv = methodBody.LocalVariables.Variables[j];
                        var lvAnnotations = default(Seq<Annotation>);
                        if (lv.IsPinned)
                        {
                            lvAnnotations = new Seq<Annotation>();
                            lvAnnotations.Add(new LocalVarPinnedAnnotation());
                        }
                        var paramOrLocal = new ParameterOrLocalOrResult
                            (lvAnnotations, null, TypeRefFromTypeSigWithCustomMods(ctxt, lv.Type));
                        // Locals can't have custom attributes, so no need to add this row into info.ParametersAndLocals
                        locals.Add(paramOrLocal);
                    }
                }

                annotations.Add(new MethodImplementationAnnotation(methodBody.MaxStack));
            }

            var methodDef = new MethodDef
                (annotations,
                 null,
                 name,
                 isStatic,
                 typeParameters,
                 valueParameters,
                 result,
                 methodStyle,
                 hasNewSlot,
                 codeFlavor,
                 isSyncronized,
                 noInlining,
                 isInitLocals,
                 locals,
                 methodBody);

            ctxt.MemberDefs.Add(row, methodDef);

            return methodDef;
        }


        // ----------------------------------------------------------------------
        // Type definitions
        // ----------------------------------------------------------------------

        private TypeDef TypeDefFromRow(DllLoadContext ctxt, PE.TypeDefRow row)
        {
            var annotations = new Seq<Annotation>();

            var accessibility = default(Accessibility);
            switch (row.Flags & PE.TypeAttributes.VisibilityMask)
            {
            case PE.TypeAttributes.NotPublic:
                accessibility = Accessibility.Private;
                break;
            case PE.TypeAttributes.Public:
                accessibility = Accessibility.Public;
                break;
            case PE.TypeAttributes.NestedPublic:
                accessibility = Accessibility.Public;
                break;
            case PE.TypeAttributes.NestedPrivate:
                accessibility = Accessibility.Private;
                break;
            case PE.TypeAttributes.NestedFamily:
                accessibility = Accessibility.Family;
                break;
            case PE.TypeAttributes.NestedAssembly:
                accessibility = Accessibility.Assembly;
                break;
            case PE.TypeAttributes.NestedFamANDAssem:
                accessibility = Accessibility.FamilyANDAssembly;
                break;
            case PE.TypeAttributes.NestedFamORAssem:
                accessibility = Accessibility.FamilyORAssembly;
                break;
            }
            annotations.Add(new AccessibilityAnnotation(accessibility));

            var layout = default(TypeLayout);
            switch (row.Flags & PE.TypeAttributes.LayoutMask)
            {
            case PE.TypeAttributes.AutoLayout:
                layout = TypeLayout.Auto;
                break;
            case PE.TypeAttributes.SequentialLayout:
                layout = TypeLayout.Sequential;
                break;
            case PE.TypeAttributes.ExplicitLayout:
                layout = TypeLayout.Explicit;
                break;
            }

            var stringFormat = default(StringFormat);
            switch (row.Flags & PE.TypeAttributes.StringFormatMask)
            {
            case PE.TypeAttributes.AnsiClass:
                stringFormat = StringFormat.Ansi;
                break;
            case PE.TypeAttributes.UnicodeClass:
                stringFormat = StringFormat.Unicode;
                break;
            case PE.TypeAttributes.AutoClass:
                stringFormat = StringFormat.Auto;
                break;
            case PE.TypeAttributes.CustomFormatClass:
                stringFormat = StringFormat.Custom;
                break;
            }

            var isSerializable = (row.Flags & PE.TypeAttributes.Serializable) != 0;
            annotations.Add(new TypeInteropAnnotation(layout, stringFormat, isSerializable));

            var hasSecurity = (row.Flags & PE.TypeAttributes.HasSecurity) != 0;
            annotations.Add(new TypeSecurityAnnotation(hasSecurity));

            var isSpecialName = (row.Flags & PE.TypeAttributes.SpecialName) != 0;
            var isRTSpecialName = (row.Flags & PE.TypeAttributes.RTSpecialName) != 0;
            if (isSpecialName || isRTSpecialName)
                annotations.Add(new SpecialNameAnnotation(isRTSpecialName));

            var isAbstract = (row.Flags & PE.TypeAttributes.Abstract) != 0;
            var isSealed = (row.Flags & PE.TypeAttributes.Serializable) != 0;

            var isCallStaticConstructorEarly = (row.Flags & PE.TypeAttributes.BeforeFieldInit) != 0;

            var isInterface = (row.Flags & PE.TypeAttributes.Interface) != 0;

            // TODO: TypeAttributes.Import

            var typeName = TypeNameFromTypeDefRow(ctxt, row);

            var extends = default(TypeRef);
            if (!row.Extends.IsNull)
                extends = TypeRefFromRow(ctxt, null, row.Extends.Value, true);

            var thisTypeRef = TypeRefFromRow(ctxt, null, row, true);
            if (extends != null && (extends.Equals(global.EnumRef) || (!thisTypeRef.Equals(global.EnumRef) && extends.Equals(global.ValueTypeRef))))
                // Implicit 'this' to instance members of value types is a pointer to the value type
                thisTypeRef = global.ManagedPointerTypeConstructorRef.ApplyTo(thisTypeRef);

            var members = default(Seq<MemberDef>);
            foreach (var fieldRow in row.FieldList.Value)
            {
                if (members == null)
                    members = new Seq<MemberDef>();
                members.Add(FieldDefFromRow(ctxt, fieldRow));
            }
            foreach (var methodRow in row.MethodList.Value)
            {
                if (members == null)
                    members = new Seq<MemberDef>();
                members.Add(MethodDefFromRow(ctxt, thisTypeRef, methodRow));
            }

            var parameters = (from r in ctxt.File.Tables.GenericParamTable
                              where r.Owner.Value == row
                              select ParameterTypeDefFromRow(ctxt, ParameterFlavor.Type, r)).ToSeq();
            if (parameters.Count == 0)
                parameters = null;

            var implementRows = from r in ctxt.File.Tables.InterfaceImplTable
                                where r.Class.Value == row
                                select r.Interface.Value;
            var implements = default(Seq<TypeRef>);
            foreach (var r in implementRows)
            {
                if (implements == null)
                    implements = new Seq<TypeRef>();
                implements.Add(TypeRefFromRow(ctxt, null, r, true));
            }

            var propertyList =
                (from r in ctxt.File.Tables.PropertyMapTable where r.Parent.Value == row select r.PropertyList.Value).
                    FirstOrDefault();
            if (propertyList != null)
            {
                foreach (var pRow in propertyList)
                {
                    var pAnnotations = new Seq<Annotation>();
                    var pIsSpecialName = (pRow.Flags & PE.PropertyAttributes.SpecialName) != 0;
                    var pIsRTSpecialName = (pRow.Flags & PE.PropertyAttributes.RTSpecialName) != 0;

                    if (pIsSpecialName || pIsRTSpecialName)
                        pAnnotations.Add(new SpecialNameAnnotation(pIsRTSpecialName));

                    var hasDefault = (pRow.Flags & PE.PropertyAttributes.HasDefault) != 0;

                    // All we extract from the property signature is the static/instance flag
                    var isStatic = pRow.Type.Value.IsStatic;

                    var fieldType = TypeRefFromTypeSigWithCustomMods(ctxt, pRow.Type.Value.ReturnType);

                    var getMethod = (from r in ctxt.File.Tables.MethodSemanticsTable
                                     where
                                         r.Association.Value == pRow &&
                                         (r.Semantics & PE.MethodSemanticsAttributes.Getter) != 0
                                     select SignatureFromRow(ctxt, r.Method.Value)).FirstOrDefault();
                    var setMethod = (from r in ctxt.File.Tables.MethodSemanticsTable
                                     where
                                         r.Association.Value == pRow &&
                                         (r.Semantics & PE.MethodSemanticsAttributes.Setter) != 0
                                     select SignatureFromRow(ctxt, r.Method.Value)).FirstOrDefault();

                    if (getMethod == null && setMethod == null)
                        throw new InvalidOperationException("missing add or remove methods");

                    var propertyDef = new PropertyDef
                        (pAnnotations, null, pRow.Name.Value, isStatic, getMethod, setMethod, fieldType);
                    ctxt.MemberDefs.Add(pRow, propertyDef);
                    members.Add(propertyDef);
                }
            }

            var eventList =
                (from r in ctxt.File.Tables.EventMapTable where r.Parent.Value == row select r.EventList.Value).
                    FirstOrDefault();
            if (eventList != null)
            {
                foreach (var eRow in eventList)
                {
                    var eAnnotations = new Seq<Annotation>();
                    var eIsSpecialName = (eRow.EventFlags & PE.EventAttributes.SpecialName) != 0;
                    var eIsRTSpecialName = (eRow.EventFlags & PE.EventAttributes.RTSpecialName) != 0;
                    if (eIsSpecialName || eIsRTSpecialName)
                        eAnnotations.Add(new SpecialNameAnnotation(eIsRTSpecialName));

                    var eventType = TypeRefFromRow(ctxt, null, eRow.EventType.Value, true);

                    var addMethod = (from r in ctxt.File.Tables.MethodSemanticsTable
                                     where
                                         r.Association.Value == eRow &&
                                         (r.Semantics & PE.MethodSemanticsAttributes.AddOn) != 0
                                     select SignatureFromRow(ctxt, r.Method.Value)).FirstOrDefault();
                    var removeMethod = (from r in ctxt.File.Tables.MethodSemanticsTable
                                        where
                                            r.Association.Value == eRow &&
                                            (r.Semantics & PE.MethodSemanticsAttributes.RemoveOn) != 0
                                        select SignatureFromRow(ctxt, r.Method.Value)).FirstOrDefault();

                    if (addMethod == null && removeMethod == null)
                        throw new InvalidOperationException("missing add or remove methods");

                    var isStatic = addMethod == null ? removeMethod.IsStatic : addMethod.IsStatic;

                    var eventDef = new EventDef
                        (eAnnotations, null, eRow.Name.Value, isStatic, addMethod, removeMethod, eventType);
                    ctxt.MemberDefs.Add(eRow, eventDef);
                    members.Add(eventDef);
                }
            }

            // Place members in canonical order
            var signatureToMember = default(Map<Signature, MemberDef>);
            if (members != null)
            {
                var signatures = new Seq<Signature>();
                signatureToMember = new Map<Signature, MemberDef>();
                for (var i = 0; i < members.Count; i++)
                {
                    var signature = members[i].Signature;
                    if (signatureToMember.ContainsKey(signature))
                        throw new InvalidOperationException("invalid type definition");
                    signatures.Add(signature);
                    signatureToMember.Add(signature, members[i]);
                }
                signatures.Sort((l, r) => l.CompareTo(r));
                members = new Seq<MemberDef>();
                for (var i = 0; i < signatures.Count; i++)
                    members.Add(signatureToMember[signatures[i]]);
            }

            var explicitInterfaceImplementationRows = from r in ctxt.File.Tables.MethodImplTable
                                                      where r.Class.Value == row
                                                      select r;
            var explicitInterfaceImplementations = new Map<PolymorphicMethodRef, PolymorphicMethodRef>();
            foreach (var r in explicitInterfaceImplementationRows)
            {
                explicitInterfaceImplementations.Add
                    (PolymorphicMethodRefFromRow(ctxt, r.MethodDeclaration.Value),
                     PolymorphicMethodRefFromRow(ctxt, r.MethodBody.Value));
            }
            if (explicitInterfaceImplementations.Count == 0)
                explicitInterfaceImplementations = null;
            // NOTE: Implicit overrides and instance method implementations are determined in last pass

            var strongName = SubstAssemblyName(ctxt, ctxt.AssemblyName);
            var qtn = new QualifiedTypeName(strongName, typeName);
            var res = default(TypeDef);
            if (isInterface)
            {
                if (extends != null)
                    throw new InvalidOperationException("invalid interface type definition");
                if (qtn.Equals(global.IEnumerableTypeConstructorRef.QualifiedTypeName))
                    res = new GenericIEnumerableTypeDef(annotations, null, implements, parameters, typeName, members);
                else
                    res = new InterfaceTypeDef(annotations, null, implements, parameters, typeName, members);
            }
            else
            {
                if (global.QualifiedTypeNameToAbbreviation.ContainsKey(qtn))
                {
                    var numberFlavor = default(NumberFlavor);
                    var handleFlavor = default(HandleFlavor);
                    if (global.QualifiedTypeNameToNumberFlavor.TryGetValue(qtn, out numberFlavor))
                        res = new NumberTypeDef
                            (annotations,
                             null,
                             extends,
                             implements,
                             parameters,
                             qtn.Type,
                             members,
                             numberFlavor,
                             explicitInterfaceImplementations,
                             isCallStaticConstructorEarly);
                    else if (global.QualifiedTypeNameToHandleFlavor.TryGetValue(qtn, out handleFlavor))
                        res = new HandleTypeDef
                            (annotations,
                             null,
                             extends,
                             implements,
                             parameters,
                             qtn.Type,
                             members,
                             handleFlavor,
                             explicitInterfaceImplementations,
                             isCallStaticConstructorEarly);
                    else if (qtn.Equals(global.VoidRef.QualifiedTypeName))
                        res = new VoidTypeDef
                            (annotations,
                             null,
                             extends,
                             implements,
                             parameters,
                             qtn.Type,
                             members,
                             explicitInterfaceImplementations,
                             isCallStaticConstructorEarly);
                    else if (qtn.Equals(global.ObjectRef.QualifiedTypeName))
                        res = new ObjectTypeDef
                            (annotations,
                             null,
                             extends,
                             implements,
                             parameters,
                             qtn.Type,
                             members,
                             explicitInterfaceImplementations,
                             isCallStaticConstructorEarly);
                    else if (qtn.Equals(global.StringRef.QualifiedTypeName))
                        res = new StringTypeDef
                            (annotations,
                             null,
                             extends,
                             implements,
                             parameters,
                             qtn.Type,
                             members,
                             explicitInterfaceImplementations,
                             isCallStaticConstructorEarly);
                    else
                        throw new InvalidOperationException("unrecognised special type");
                }
                else if (qtn.Equals(global.EnumRef.QualifiedTypeName) || qtn.Equals(global.ValueTypeRef.QualifiedTypeName) ||
                         qtn.Equals(global.DelegateRef.QualifiedTypeName) || qtn.Equals(global.MulticastDelegateRef.QualifiedTypeName) ||
                         qtn.Equals(global.ObjectRef.QualifiedTypeName))
                    res = new ClassTypeDef
                        (annotations,
                         null,
                         extends,
                         implements,
                         parameters,
                         qtn.Type,
                         members,
                         explicitInterfaceImplementations,
                         isSealed,
                         isAbstract,
                         isCallStaticConstructorEarly);
                else if (qtn.Equals(global.NullableTypeConstructorRef.QualifiedTypeName))
                    res = new NullableTypeDef
                        (annotations,
                         null,
                         extends,
                         implements,
                         parameters,
                         qtn.Type,
                         members,
                         explicitInterfaceImplementations,
                         isCallStaticConstructorEarly);
                else if (extends != null)
                {
                    // Look at extends type to decide if delegate, value type or ordinary class.
                    // (Can get aways with this because delegates and value types are sealed.)
                    if (extends.Equals(global.DelegateRef) || extends.Equals(global.MulticastDelegateRef))
                    {
                        // Is a user-defined delegate type. 
                        // Fixup the constructor to take a properly typed function pointer as third argument
                        // instead of just IntPtr.
                        var invokeDef =
                            members.OfType<MethodDef>().Where
                                (m => !m.IsStatic && m.Name.Equals("Invoke", StringComparison.Ordinal)).FirstOrDefault
                                ();
                        if (invokeDef == null)
                            throw new InvalidOperationException("delegate does not have Invoke method");
                        var codePointer = ((MethodSignature)invokeDef.Signature).WithoutThis().ToCodePointer(global);
                        var ctorDef =
                            members.OfType<MethodDef>().Where(m => !m.IsStatic && m.IsConstructor && m.Arity == 3).
                                FirstOrDefault();
                        if (ctorDef == null)
                            throw new InvalidOperationException("delegate does not have expected constructor");
                        var newCtorParams = new Seq<ParameterOrLocalOrResult>
                                                {
                                                    ctorDef.ValueParameters[0],
                                                    ctorDef.ValueParameters[1],
                                                    new ParameterOrLocalOrResult
                                                        (ctorDef.ValueParameters[2].Annotations,
                                                         ctorDef.ValueParameters[2].CustomAttributes,
                                                         codePointer)
                                                };
                        var newCtorDef = new MethodDef
                            (ctorDef.Annotations,
                             ctorDef.CustomAttributes,
                             ctorDef.Name,
                             ctorDef.IsStatic,
                             ctorDef.TypeParameters,
                             newCtorParams,
                             ctorDef.Result,
                             ctorDef.MethodStyle,
                             ctorDef.HasNewSlot,
                             ctorDef.CodeFlavor,
                             ctorDef.IsSyncronized,
                             ctorDef.NoInlining,
                             ctorDef.IsInitLocals,
                             ctorDef.Locals,
                             ctorDef.MethodBody);
                        var newMembers = new Seq<MemberDef>();
                        foreach (var member in members)
                            newMembers.Add(member == ctorDef ? newCtorDef : member);
                        res = new DelegateTypeDef
                            (annotations,
                             null,
                             extends,
                             implements,
                             parameters,
                             qtn.Type,
                             newMembers,
                             explicitInterfaceImplementations,
                             isCallStaticConstructorEarly);
                    }
                    else if (extends.Equals(global.EnumRef))
                        // Is a user-defined enumeration
                        res = new EnumTypeDef
                            (annotations,
                             null,
                             extends,
                             implements,
                             qtn.Type,
                             members,
                             explicitInterfaceImplementations,
                             isCallStaticConstructorEarly);
                    else if (extends.Equals(global.ValueTypeRef))
                        // Is a user-defined struct
                        res = new StructTypeDef
                            (annotations,
                             null,
                             extends,
                             implements,
                             parameters,
                             qtn.Type,
                             members,
                             explicitInterfaceImplementations,
                             isCallStaticConstructorEarly);
                    else
                        res = new ClassTypeDef
                            (annotations,
                             null,
                             extends,
                             implements,
                             parameters,
                             qtn.Type,
                             members,
                             explicitInterfaceImplementations,
                             isSealed,
                             isAbstract,
                             isCallStaticConstructorEarly);
                }
                else
                    // Probably the <Module> type
                    res = new ClassTypeDef
                        (annotations,
                         null,
                         extends,
                         implements,
                         parameters,
                         qtn.Type,
                         members,
                         explicitInterfaceImplementations,
                         isSealed,
                         isAbstract,
                         isCallStaticConstructorEarly);
            }
            ctxt.TypeDefs.Add(row, res);
            return res;
        }

        // ----------------------------------------------------------------------
        // Assembly definitions
        // ----------------------------------------------------------------------

        private void MergeAssemblyDefs(AssemblyLoadContext assmLoadContext)
        {
            var annotations = new Seq<Annotation>();
            var referencesSet = new Set<AssemblyName>();
            var typesMap = new Map<TypeName, TypeDef>();
            var entryPoint = default(MethodRef);

            foreach (var dllInfo in assmLoadContext.Dlls)
            {
                if (annotations.Count == 0)
                    annotations.Add
                        (new AssemblyFileAnnotation(dllInfo.CanonicalFileName, dllInfo.ReaderContext.LastWriteTime));

                foreach (var nm in (from r in dllInfo.File.Tables.AssemblyRefTable
                                    let name = AssemblyNameFromAssemblyRefRow(dllInfo, r)
                                    where name.Name != null && !name.Equals(assmLoadContext.TargetAssemblyName)
                                    select name))
                    referencesSet.Add(nm);

                foreach (var row in dllInfo.File.Tables.TypeDefTable)
                {
                    var nm = TypeNameFromTypeDefRow(dllInfo, row);
                    var typeRef = new NamedTypeRef(new QualifiedTypeName(assmLoadContext.TargetAssemblyName, nm));
                    if (typesMap.ContainsKey(nm))
                        log(new IgnoringTypeDefMessage(typeRef, dllInfo.CanonicalFileName));
                    else
                    {
                        var td = TypeDefFromRow(dllInfo, row);
                        typesMap.Add(nm, td);
                    }
                }

                if (dllInfo.File.EntryPointToken > 0)
                {
                    var tokenRef = default(PE.TokenRef);
                    tokenRef.CodedIndex = dllInfo.File.EntryPointToken;
                    tokenRef.ResolveIndexes(dllInfo.ReaderContext);
                    var thisEntryPoint = MethodRefFromRow(dllInfo, tokenRef.Value);
                    if (entryPoint == null)
                        entryPoint = thisEntryPoint;
                    else
                        log(new IgnoringEntryPointMessage(thisEntryPoint, dllInfo.CanonicalFileName));
                }
            }

            if (assmLoadContext.TargetAssemblyName.Equals(global.MsCorLibName))
            {
                // The special built-in type definitions
                typesMap.Add(global.ArrayTypeConstructorRef.QualifiedTypeName.Type, global.ArrayTypeConstructorDef);
                typesMap.Add(global.ManagedPointerTypeConstructorRef.QualifiedTypeName.Type, global.ManagedPointerTypeConstructorDef);
            }

            var references = referencesSet.ToSeq();
            references.Sort((l, r) => l.CompareTo(r));

            var typeNames = typesMap.Select(kv => kv.Key).ToSeq();
            typeNames.Sort((l, r) => l.CompareTo(r));
            var types = new Seq<TypeDef>();
            foreach (var nm in typeNames)
                types.Add(typesMap[nm]);

            assmLoadContext.AssemblyDef = new AssemblyDef
                (global, annotations, null, assmLoadContext.TargetAssemblyName, references, types, entryPoint);
        }

        // ----------------------------------------------------------------------
        // Custom attributes
        // ----------------------------------------------------------------------

        // NOTE: We can assume type refs may be resolved to type defs in the following.
        //       However, it is possible a ref may not resolve to any type.

        private TypeRef PropertyTypeNameToTypeRef(DllLoadContext info, string name)
        {
            var assemblyName = default(AssemblyName);
            var typeName = default(TypeName);
            var i = name.IndexOf(',');
            if (i > 0)
            {
                typeName = TypeName.FromReflectionName(name.Substring(0, i));
                i++;
                while (i < name.Length && Char.IsWhiteSpace(name[i]))
                    i++;
                assemblyName = AssemblyName.FromReflectionName(loadedAssemblies.Resolution, name.Substring(i));
                assemblyName = SubstAssemblyName(info, assemblyName);
            }
            else
            {
                assemblyName = info.AssemblyName;
                typeName = TypeName.FromReflectionName(name);
            }
            return new NamedTypeRef(new QualifiedTypeName(assemblyName, typeName));
        }

        private string TypeRefToPropertyTypeName(TypeRef typeRef)
        {
            if (typeRef.Arguments.Count > 0)
                throw new InvalidOperationException("only types without type arguments may be used as custom attribute property types");
            return typeRef.QualifiedTypeName.Type + ", " + typeRef.QualifiedTypeName.Assembly;
        }

        private PE.CustomAttributePropertyType PropertyTypeFromTypeRef(DllLoadContext info, TypeRef typeRef)
        {
            var assemblyDef = default(AssemblyDef);
            var typeDef = default(TypeDef);
            if (!typeRef.PrimTryResolve(global, out assemblyDef, out typeDef))
                return null;

            var style = typeDef.Style;
            if (style is NumberTypeStyle)
            {
                switch (((NumberTypeStyle)style).Flavor)
                {
                case NumberFlavor.Int8:
                    return new PE.PrimitiveCustomAttributePropertyType { Type = PE.PrimitiveType.Int8 };
                case NumberFlavor.Int16:
                    return new PE.PrimitiveCustomAttributePropertyType { Type = PE.PrimitiveType.Int16 };
                case NumberFlavor.Int32:
                    return new PE.PrimitiveCustomAttributePropertyType { Type = PE.PrimitiveType.Int32 };
                case NumberFlavor.Int64:
                    return new PE.PrimitiveCustomAttributePropertyType { Type = PE.PrimitiveType.Int64 };
                case NumberFlavor.UInt8:
                    return new PE.PrimitiveCustomAttributePropertyType { Type = PE.PrimitiveType.UInt8 };
                case NumberFlavor.UInt16:
                    return new PE.PrimitiveCustomAttributePropertyType { Type = PE.PrimitiveType.UInt16 };
                case NumberFlavor.UInt32:
                    return new PE.PrimitiveCustomAttributePropertyType { Type = PE.PrimitiveType.UInt32 };
                case NumberFlavor.UInt64:
                    return new PE.PrimitiveCustomAttributePropertyType { Type = PE.PrimitiveType.UInt64 };
                case NumberFlavor.Single:
                    return new PE.PrimitiveCustomAttributePropertyType { Type = PE.PrimitiveType.Single };
                case NumberFlavor.Double:
                    return new PE.PrimitiveCustomAttributePropertyType { Type = PE.PrimitiveType.Double };
                case NumberFlavor.Boolean:
                    return new PE.PrimitiveCustomAttributePropertyType { Type = PE.PrimitiveType.Boolean };
                case NumberFlavor.Char:
                    return new PE.PrimitiveCustomAttributePropertyType { Type = PE.PrimitiveType.Char };
                default:
                    throw new ArgumentOutOfRangeException();
                }
            }
            else if (style is StringTypeStyle)
                return new PE.PrimitiveCustomAttributePropertyType { Type = PE.PrimitiveType.String };
            else if (style is ObjectTypeStyle)
                return new PE.ObjectCustomAttributePropertyType();
            else if (style is ClassTypeStyle)
            {
                if (typeRef.Equals(global.TypeRef))
                    return new PE.PrimitiveCustomAttributePropertyType { Type = PE.PrimitiveType.Type };
                else
                    throw new InvalidOperationException("type cannot be used for custom attribute property");
            }
            else if (style is EnumTypeStyle)
            {
                var enumDef = (EnumTypeDef)typeDef;
                var implType = PropertyTypeFromTypeRef(info, enumDef.Implementation);
                if (implType == null)
                    return null;
                return new PE.EnumCustomAttributePropertyType
                       { TypeName = TypeRefToPropertyTypeName(typeRef), UnderlyingType = implType };
            }
            else if (style is ArrayTypeStyle)
            {
                var elemType = PropertyTypeFromTypeRef(info, typeRef.Arguments[0]);
                if (elemType == null)
                    return null;
                return new PE.ArrayCustomAttributePropertyType { ElementType = elemType };
            }
            else
                throw new InvalidOperationException("type cannot be used for custom attribute property");
        }

        private object ValueFromCustomAttributeProperty(DllLoadContext info, PE.CustomAttributeProperty p)
        {
            if (p.Value == null)
                return null;

            var typeValue = p.Value as PE.TypeCustomAttributePropertyValue;
            if (typeValue != null)
                return PropertyTypeNameToTypeRef(info, typeValue.Name);

            var enumValue = p.Value as PE.EnumCustomAttributePropertyValue;
            if (enumValue != null)
                // Collapse enum values to their underlying representation
                return enumValue.Value;

            return p.Value;
        }

        private CustomAttribute CustomAttributeFromRow(MessageContext ctxt, DllLoadContext info, PE.CustomAttributeRow row)
        {
            var ctor = MethodRefFromRow(info, row.Type.Value);
            if (knownUnavailableCustomAttributeTypes.Contains(ctor.DefiningType))
                return null;

            var fixedArgTypes = ctor.ValueParameters.Skip(1).Select(p => PropertyTypeFromTypeRef(info, p)).ToSeq();
            if (ctor.DefiningType.QualifiedTypeName.IsResolvable(global, log, ctxt) && fixedArgTypes.All(t => t != null))
            {
                // NOTE: Difficult to avoid the try/catch here...
                try
                {
                    Func<string, PE.CustomAttributePropertyType> resolveType = nm =>
                                                                                {
                                                                                    var res = PropertyTypeFromTypeRef
                                                                                        (info,
                                                                                         PropertyTypeNameToTypeRef
                                                                                             (info, nm));
                                                                                    if (res == null)
                                                                                        throw new InvalidOperationException
                                                                                            ();
                                                                                    return res;
                                                                                };
                    var sig = row.GetCustomAttribute(fixedArgTypes, resolveType);
                    var qtn = ctor.DefiningType.QualifiedTypeName;
                    var positionalProperties = default(Seq<object>);
                    if (sig.FixedArgs.Count > 0)
                    {
                        positionalProperties = new Seq<object>(sig.FixedArgs.Count);
                        foreach (var p in sig.FixedArgs)
                            positionalProperties.Add(ValueFromCustomAttributeProperty(info, p));
                    }
                    var namedProperties = default(Map<string, object>);
                    if (sig.FieldArgs.Count + sig.PropertyArgs.Count > 0)
                    {
                        namedProperties = new Map<string, object>();
                        foreach (var kv in sig.FieldArgs)
                        {
                            namedProperties.Add(kv.Key, ValueFromCustomAttributeProperty(info, kv.Value));
                        }
                        foreach (var kv in sig.PropertyArgs)
                        {
                            if (namedProperties.ContainsKey(kv.Key))
                                throw new NotSupportedException
                                    ("custom attribute properties and fields cannot share the same name");
                            namedProperties.Add(kv.Key, ValueFromCustomAttributeProperty(info, kv.Value));
                        }
                    }
                    return new CustomAttribute(new NamedTypeRef(qtn), positionalProperties, namedProperties);
                }
                catch (InvalidOperationException)
                {
                    // fall-through
                }
            }
            // else: fall-through
            knownUnavailableCustomAttributeTypes.Add(ctor.DefiningType);
            log
                (new InvalidCustomAttribute
                     (null,
                      info.AssemblyName,
                      ctor.DefiningType,
                      "Attribute type or one of its property types is not available"));
            return null;
        }

        private void LoadCustomAttributes(AssemblyLoadContext assmLoadContext)
        {
            var ctxt = MessageContextBuilders.Assembly(global, assmLoadContext.AssemblyDef);
            var first = true;
            foreach (var dllInfo in assmLoadContext.Dlls)
            {
                foreach (var customAttributeRow in dllInfo.File.Tables.CustomAttributeTable)
                {
                    var ca = CustomAttributeFromRow(ctxt, dllInfo, customAttributeRow);
                    if (ca != null)
                    {
                        if (customAttributeRow.Parent.Value == dllInfo.AssemblyRow)
                        {
                            if (first)
                                assmLoadContext.AssemblyDef.CustomAttributes.Add(ca);
                        }
                        else
                        {
                            var typeDef = default(TypeDef);
                            if (dllInfo.TypeDefs.TryGetValue(customAttributeRow.Parent.Value, out typeDef))
                                typeDef.CustomAttributes.Add
                                    (CustomAttributeFromRow(ctxt, dllInfo, customAttributeRow));
                            else
                            {
                                var memberDef = default(MemberDef);
                                if (dllInfo.MemberDefs.TryGetValue(customAttributeRow.Parent.Value, out memberDef))
                                    memberDef.CustomAttributes.Add
                                        (CustomAttributeFromRow(ctxt, dllInfo, customAttributeRow));
                                else
                                {
                                    var paramOrLocal = default(ParameterOrLocalOrResult);
                                    if (dllInfo.ParametersAndLocals.TryGetValue
                                        (customAttributeRow.Parent.Value, out paramOrLocal))
                                        paramOrLocal.CustomAttributes.Add
                                            (CustomAttributeFromRow(ctxt, dllInfo, customAttributeRow));
                                    // else: ignore
                                }
                            }
                        }
                    }
                }
                first = false;
            }
        }

        // ----------------------------------------------------------------------
        // Loading
        // ----------------------------------------------------------------------

        private void LoadMetadata(AssemblyLoadContext assmLoadContext)
        {
            foreach (var dllInfo in assmLoadContext.Dlls)
            {
                foreach (var typeDefRow in dllInfo.File.Tables.TypeDefTable)
                {
                    foreach (var methodDefRow in typeDefRow.MethodList.Value)
                    {
                        if (dllInfo.MemberToDefiningType.ContainsKey(methodDefRow))
                            throw new InvalidOperationException("method def is owned by more than one type def");
                        dllInfo.MemberToDefiningType.Add(methodDefRow, typeDefRow);
                    }
                    foreach (var fieldRow in typeDefRow.FieldList.Value)
                    {
                        if (dllInfo.MemberToDefiningType.ContainsKey(fieldRow))
                            throw new InvalidOperationException("field is owned by more than one type def");
                        dllInfo.MemberToDefiningType.Add(fieldRow, typeDefRow);
                    }
                }
            }

            MergeAssemblyDefs(assmLoadContext);
        }

        public Global Load()
        {
            knownUnavailableCustomAttributeTypes = new Set<TypeRef>();

            // Setup global using actual mscorlib name
            global = new Global(loadedAssemblies.Resolution, loadedAssemblies.FindSpecialAssembly(Global.MSCorLibSimpleName));

            // Lift into CST representation
            var assemblyDefs = new Seq<AssemblyDef>();
            foreach (var assmInfo in loadedAssemblies)
            {
                LoadMetadata(assmInfo);
                assemblyDefs.Add(assmInfo.AssemblyDef);
            }

            // Register assemblies with global environment
            global.AddAssemblies(assemblyDefs);

            // Load custom attributes
            foreach (var assmInfo in loadedAssemblies)
                LoadCustomAttributes(assmInfo);

            knownUnavailableCustomAttributeTypes = null;

            return global;
        }
    }
}